using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IPackageService
    {
        OperationResult<string> Export(InstalledInstance instance, string destinationPath, bool includeSaves);
        OperationResult<ImportPreview> PreviewImport(string sourcePath);
        OperationResult<InstalledInstance> Import(string sourcePath, ImportConflictResolution conflictResolution);
    }

    public sealed class PackageService : IPackageService
    {
        private const long MaximumPackageBytes = 32L * 1024 * 1024 * 1024;
        private readonly string _instancesRoot;
        private readonly ISafeArchiveService _archives;
        private readonly IAtomicFileService _atomicFiles;
        private readonly IInstanceConfigService _configs;
        private readonly ILogService _log;

        public PackageService(string instancesRoot, ISafeArchiveService archives, IAtomicFileService atomicFiles, IInstanceConfigService configs, ILogService log)
        {
            _instancesRoot = instancesRoot;
            _archives = archives;
            _atomicFiles = atomicFiles;
            _configs = configs;
            _log = log;
        }

        public OperationResult<string> Export(InstalledInstance instance, string destinationPath, bool includeSaves)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var temporary = Path.Combine(Path.GetTempPath(), "tcl-export-" + operationId);
            try
            {
                if (instance == null || !Directory.Exists(instance.DataDir))
                    return OperationResult<string>.Failure(LauncherErrorCode.InvalidConfiguration, "The selected instance is not installed.", operationId: operationId);

                Directory.CreateDirectory(temporary);
                var payloadSource = Path.Combine(temporary, "payload");
                Directory.CreateDirectory(payloadSource);
                DirectoryService.Copy(instance.DataDir, payloadSource, path => includeSaves || !IsSavePath(path));

                var payloadPath = Path.Combine(temporary, "payload.zip");
                ZipFile.CreateFromDirectory(payloadSource, payloadPath, CompressionLevel.Optimal, false);
                Directory.Delete(payloadSource, true);

                var portable = JsonConvert.DeserializeObject<Instance>(JsonConvert.SerializeObject(instance));
                portable.Is_Installed = false;
                portable.Is_LocalSource = true;
                portable.PrepareForV2Save();
                var manifest = new PackageManifest
                {
                    PackageId = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow,
                    Instance = portable,
                    Payload = new PackagePayload
                    {
                        Path = "payload.zip",
                        Sha256 = HashService.Sha256(payloadPath),
                        UncompressedBytes = DirectoryService.EnumerateRelativeFiles(instance.DataDir)
                            .Where(path => includeSaves || !IsSavePath(path))
                            .Sum(path => new FileInfo(Path.Combine(instance.DataDir, path)).Length)
                    }
                };
                manifest.Files = DirectoryService.EnumerateRelativeFiles(instance.DataDir)
                    .Where(path => includeSaves || !IsSavePath(path))
                    .Select(path => new PackageFileEntry
                    {
                        Path = path.Replace('\\', '/'),
                        Size = new FileInfo(Path.Combine(instance.DataDir, path)).Length,
                        Sha256 = HashService.Sha256(Path.Combine(instance.DataDir, path))
                    }).ToList();

                File.WriteAllText(Path.Combine(temporary, "manifest.json"), JsonConvert.SerializeObject(manifest, Formatting.Indented));
                File.WriteAllText(Path.Combine(temporary, "config.json"), JsonConvert.SerializeObject(portable, Formatting.Indented));
                CopyArtwork(instance, temporary);

                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                ZipFile.CreateFromDirectory(temporary, destinationPath, CompressionLevel.Optimal, false);
                _log.Info("package.exported", destinationPath, operationId);
                return OperationResult<string>.Success(destinationPath, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("package.export_failed", exception, operationId);
                return OperationResult<string>.Failure(LauncherErrorCode.Unexpected, "The profile package could not be exported.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
            }
        }

        public OperationResult<ImportPreview> PreviewImport(string sourcePath)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var temporary = Path.Combine(Path.GetTempPath(), "tcl-preview-" + operationId);
            try
            {
                if (!File.Exists(sourcePath))
                    return OperationResult<ImportPreview>.Failure(LauncherErrorCode.InvalidConfiguration, "The package does not exist.", operationId: operationId);
                _archives.Extract(sourcePath, temporary, MaximumPackageBytes);
                var manifestPath = Path.Combine(temporary, "manifest.json");
                var isLegacy = !File.Exists(manifestPath);
                PackageManifest manifest;
                if (isLegacy)
                {
                    var configPath = Path.Combine(temporary, "config.json");
                    if (!File.Exists(configPath)) throw new InvalidDataException("The package has no manifest or legacy configuration.");
                    var instance = JsonConvert.DeserializeObject<Instance>(File.ReadAllText(configPath));
                    instance?.NormalizeLegacyConfiguration();
                    manifest = new PackageManifest { SchemaVersion = 1, PackageId = instance?.Guid ?? Guid.Empty, Instance = instance, Payload = new PackagePayload() };
                }
                else
                {
                    manifest = JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(manifestPath));
                }

                ValidateManifest(manifest, temporary);
                var preview = new ImportPreview
                {
                    SourcePath = sourcePath,
                    Manifest = manifest,
                    IsLegacy = isLegacy,
                    HasConflict = Directory.Exists(Path.Combine(_instancesRoot, manifest.Instance.Guid.ToString())),
                    PackageBytes = new FileInfo(sourcePath).Length
                };
                return OperationResult<ImportPreview>.Success(preview, operationId);
            }
            catch (InvalidDataException exception)
            {
                _log.Error("package.preview_invalid", exception, operationId);
                return OperationResult<ImportPreview>.Failure(LauncherErrorCode.UnsafeArchive, exception.Message, exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("package.preview_failed", exception, operationId);
                return OperationResult<ImportPreview>.Failure(LauncherErrorCode.InvalidConfiguration, "The package could not be inspected.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
            }
        }

        public OperationResult<InstalledInstance> Import(string sourcePath, ImportConflictResolution conflictResolution)
        {
            var previewResult = PreviewImport(sourcePath);
            if (!previewResult.IsSuccess)
                return OperationResult<InstalledInstance>.Failure(previewResult.ErrorCode, previewResult.Message, previewResult.Exception, previewResult.OperationId);

            var operationId = Guid.NewGuid().ToString("N");
            var temporary = Path.Combine(Path.GetTempPath(), "tcl-import-" + operationId);
            string staging = null;
            try
            {
                var manifest = previewResult.Value.Manifest;
                var instance = manifest.Instance;
                if (previewResult.Value.HasConflict && conflictResolution == ImportConflictResolution.Cancel)
                    return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.Conflict, "An instance with this ID is already installed.", operationId: operationId);
                if (previewResult.Value.HasConflict && conflictResolution == ImportConflictResolution.ImportAsCopy)
                {
                    instance.Guid = Guid.NewGuid();
                    instance.Name += "-copy";
                    instance.DisplayName += " (Copy)";
                }

                _archives.Extract(sourcePath, temporary, MaximumPackageBytes);
                var payloadPath = Path.Combine(temporary, manifest.Payload?.Path ?? "payload.zip");
                if (!File.Exists(payloadPath)) throw new InvalidDataException("The package payload is missing.");
                if (!string.IsNullOrWhiteSpace(manifest.Payload?.Sha256) &&
                    !HashService.Sha256(payloadPath).Equals(manifest.Payload.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The package payload checksum does not match.");

                var destination = Path.Combine(_instancesRoot, instance.Guid.ToString());
                staging = destination + ".staging-" + operationId;
                var rollback = destination + ".rollback-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                Directory.CreateDirectory(staging);
                var dataPath = Path.Combine(staging, "data");
                _archives.Extract(payloadPath, dataPath, MaximumPackageBytes);
                VerifyManagedFiles(dataPath, manifest.Files);

                var installed = new InstalledInstance(instance)
                {
                    Is_LocalSource = true,
                    InstallationDir = destination,
                    DataDir = Path.Combine(destination, "data"),
                    ConfigFile = Path.Combine(destination, "config.json")
                };
                CopyImportedArtwork(temporary, staging, destination, installed);
                var saveResult = _configs.Save(installed, Path.Combine(staging, "config.json"));
                if (!saveResult.IsSuccess) throw new InvalidDataException(saveResult.Message);
                File.WriteAllText(Path.Combine(staging, "managed.json"), JsonConvert.SerializeObject(manifest, Formatting.Indented));

                _atomicFiles.ReplaceDirectory(staging, destination, rollback);
                _log.Info("package.imported", sourcePath, operationId);
                return OperationResult<InstalledInstance>.Success(installed, operationId);
            }
            catch (InvalidDataException exception)
            {
                _log.Error("package.import_invalid", exception, operationId);
                return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.ChecksumMismatch, exception.Message, exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("package.import_failed", exception, operationId);
                return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.Unexpected, "The package could not be imported. Existing data was preserved.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                if (staging != null && Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        private void ValidateManifest(PackageManifest manifest, string root)
        {
            if (manifest?.Instance == null) throw new InvalidDataException("The package manifest has no instance.");
            if (manifest.SchemaVersion < 1 || manifest.SchemaVersion > PackageManifest.CurrentVersion) throw new InvalidDataException("The package schema is not supported.");
            var errors = _configs.Validate(manifest.Instance);
            if (errors.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
            var payload = Path.Combine(root, (manifest.Payload?.Path ?? "payload.zip").Replace('/', Path.DirectorySeparatorChar));
            var safeRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(payload).StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The payload path is unsafe.");
            if (!File.Exists(payload)) throw new InvalidDataException("The package payload is missing.");
            _archives.Validate(payload, MaximumPackageBytes);
        }

        private static void VerifyManagedFiles(string dataRoot, IReadOnlyCollection<PackageFileEntry> files)
        {
            if (files == null || files.Count == 0) return;
            foreach (var file in files)
            {
                var path = Path.Combine(dataRoot, file.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path) || new FileInfo(path).Length != file.Size ||
                    !HashService.Sha256(path).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Managed file verification failed for " + file.Path);
            }
        }

        private static bool IsSavePath(string path) => path.Replace('\\', '/').StartsWith("saves/", StringComparison.OrdinalIgnoreCase);

        private static void CopyArtwork(InstalledInstance instance, string output)
        {
            if (string.IsNullOrWhiteSpace(instance.ThumbnailURL) || !File.Exists(instance.ThumbnailURL)) return;
            File.Copy(instance.ThumbnailURL, Path.Combine(output, "thumb" + Path.GetExtension(instance.ThumbnailURL)), true);
        }

        private static void CopyImportedArtwork(string extracted, string staging, string finalDestination, InstalledInstance instance)
        {
            var artwork = Directory.GetFiles(extracted, "thumb.*").FirstOrDefault();
            if (artwork == null) return;
            var destination = Path.Combine(staging, Path.GetFileName(artwork));
            File.Copy(artwork, destination, true);
            instance.ThumbnailURL = Path.Combine(finalDestination, Path.GetFileName(artwork));
        }
    }
}
