using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IBackupService
    {
        OperationResult<BackupInfo> Create(InstalledInstance instance, string name, bool automatic, bool fullInstance);
        OperationResult Restore(InstalledInstance instance, string backupPath);
        OperationResult<InstalledInstance> RestoreAsCopy(InstalledInstance instance, string backupPath);
        IReadOnlyList<BackupInfo> List(Guid instanceId);
    }

    public sealed class BackupService : IBackupService
    {
        private readonly string _backupRoot;
        private readonly IAtomicFileService _atomicFiles;
        private readonly ISafeArchiveService _archives;
        private readonly ILogService _log;
        private readonly IInstanceConfigService _configs;
        private const long MaximumBackupBytes = 32L * 1024 * 1024 * 1024;

        public BackupService(string backupRoot, IAtomicFileService atomicFiles, ISafeArchiveService archives,
            IInstanceConfigService configs, ILogService log)
        {
            _backupRoot = backupRoot;
            _atomicFiles = atomicFiles;
            _archives = archives;
            _configs = configs;
            _log = log;
            Directory.CreateDirectory(_backupRoot);
        }

        public OperationResult<InstalledInstance> RestoreAsCopy(InstalledInstance instance, string backupPath)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var extraction = Path.Combine(Path.GetTempPath(), "tcl-restore-copy-" + operationId);
            var id = Guid.NewGuid();
            var destination = Path.Combine(Path.GetDirectoryName(instance.InstallationDir), id.ToString());
            var staging = destination + ".staging-" + operationId;
            try
            {
                _archives.Extract(backupPath, extraction, MaximumBackupBytes);
                var manifest =
                    JsonConvert.DeserializeObject<BackupManifest>(
                        File.ReadAllText(Path.Combine(extraction, "backup.json")));
                if (manifest == null || manifest.InstanceId != instance.Guid)
                    return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.InvalidConfiguration,
                        "The backup belongs to a different instance.", operationId: operationId);
                VerifyFiles(extraction, manifest.Files);

                Directory.CreateDirectory(staging);
                DirectoryService.Copy(instance.InstallationDir, staging);
                DirectoryService.Copy(extraction, staging,
                    path => !path.Equals("backup.json", StringComparison.OrdinalIgnoreCase));
                var configPath = Path.Combine(staging, "config.json");
                var loaded = _configs.Load(configPath);
                if (!loaded.IsSuccess) throw new InvalidDataException(loaded.Message);
                var copy = new InstalledInstance(loaded.Value)
                {
                    Guid = id,
                    Name = loaded.Value.Name + "-restored-copy",
                    DisplayName = loaded.Value.DisplayName + " (Restored copy)",
                    Is_LocalSource = true,
                    InstallationDir = destination,
                    DataDir = Path.Combine(destination, "data"),
                    ConfigFile = Path.Combine(destination, "config.json")
                };
                var saved = _configs.Save(copy, configPath);
                if (!saved.IsSuccess) throw new InvalidDataException(saved.Message);
                _atomicFiles.ReplaceDirectory(staging, destination, destination + ".rollback");
                _log.Info("backup.restored_as_copy", backupPath, operationId);
                return OperationResult<InstalledInstance>.Success(copy, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("backup.restore_copy_failed", exception, operationId);
                return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.Unexpected,
                    "The backup could not be restored as a copy.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(extraction)) Directory.Delete(extraction, true);
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        public OperationResult<BackupInfo> Create(InstalledInstance instance, string name, bool automatic,
            bool fullInstance)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var temporary = Path.Combine(Path.GetTempPath(), "tcl-backup-" + operationId);
            try
            {
                if (instance == null || instance.Guid == Guid.Empty || !Directory.Exists(instance.InstallationDir))
                    return OperationResult<BackupInfo>.Failure(LauncherErrorCode.InvalidConfiguration,
                        "The selected instance is not installed.", operationId: operationId);

                Directory.CreateDirectory(temporary);
                DirectoryService.Copy(instance.InstallationDir, temporary,
                    relative => fullInstance || IncludeDefault(relative));

                var manifest = new BackupManifest
                {
                    BackupId = Guid.NewGuid(),
                    InstanceId = instance.Guid,
                    Name = string.IsNullOrWhiteSpace(name)
                        ? (automatic ? "Before update" : "Manual backup")
                        : name.Trim(),
                    CreatedAtUtc = DateTime.UtcNow,
                    Automatic = automatic,
                    FullInstance = fullInstance
                };
                manifest.Files = DirectoryService.EnumerateRelativeFiles(temporary)
                    .Where(path => !path.Equals("backup.json", StringComparison.OrdinalIgnoreCase))
                    .Select(path => CreateFileEntry(temporary, path)).ToList();
                File.WriteAllText(Path.Combine(temporary, "backup.json"),
                    JsonConvert.SerializeObject(manifest, Formatting.Indented));

                var instanceBackupRoot = Path.Combine(_backupRoot, instance.Guid.ToString());
                Directory.CreateDirectory(instanceBackupRoot);
                var safeName = string.Join("-", manifest.Name.Split(Path.GetInvalidFileNameChars())).Trim('-');
                var output = Path.Combine(instanceBackupRoot,
                    $"{manifest.CreatedAtUtc:yyyyMMdd-HHmmss}-{(automatic ? "auto" : "manual")}-{safeName}.tclbackup");
                ZipFile.CreateFromDirectory(temporary, output, CompressionLevel.Optimal, false);

                if (automatic) TrimAutomaticBackups(instance.Guid);
                var info = new BackupInfo
                    { Path = output, Manifest = manifest, SizeBytes = new FileInfo(output).Length };
                _log.Info("backup.created", output, operationId);
                return OperationResult<BackupInfo>.Success(info, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("backup.create_failed", exception, operationId);
                return OperationResult<BackupInfo>.Failure(LauncherErrorCode.Unexpected,
                    "The backup could not be created.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
            }
        }

        public OperationResult Restore(InstalledInstance instance, string backupPath)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var extraction = Path.Combine(Path.GetTempPath(), "tcl-restore-" + operationId);
            var staging = instance.InstallationDir + ".staging-" + operationId;
            var rollback = instance.InstallationDir + ".rollback-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            try
            {
                _archives.Extract(backupPath, extraction, MaximumBackupBytes);
                var manifest =
                    JsonConvert.DeserializeObject<BackupManifest>(
                        File.ReadAllText(Path.Combine(extraction, "backup.json")));
                if (manifest == null || manifest.InstanceId != instance.Guid)
                    return OperationResult.Failure(LauncherErrorCode.InvalidConfiguration,
                        "The backup belongs to a different instance.", operationId: operationId);
                VerifyFiles(extraction, manifest.Files);

                Directory.CreateDirectory(staging);
                DirectoryService.Copy(instance.InstallationDir, staging);
                DirectoryService.Copy(extraction, staging,
                    path => !path.Equals("backup.json", StringComparison.OrdinalIgnoreCase));
                _atomicFiles.ReplaceDirectory(staging, instance.InstallationDir, rollback);
                _log.Info("backup.restored", backupPath, operationId);
                return OperationResult.Success(operationId);
            }
            catch (InvalidDataException exception)
            {
                _log.Error("backup.restore_invalid", exception, operationId);
                return OperationResult.Failure(LauncherErrorCode.ChecksumMismatch,
                    "The backup is invalid or corrupted.", exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("backup.restore_failed", exception, operationId);
                return OperationResult.Failure(LauncherErrorCode.Unexpected,
                    "The backup could not be restored. Existing files were preserved.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(extraction)) Directory.Delete(extraction, true);
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        public IReadOnlyList<BackupInfo> List(Guid instanceId)
        {
            var root = Path.Combine(_backupRoot, instanceId.ToString());
            if (!Directory.Exists(root)) return new List<BackupInfo>();
            var result = new List<BackupInfo>();
            foreach (var path in Directory.GetFiles(root, "*.tclbackup"))
            {
                try
                {
                    using (var archive = ZipFile.OpenRead(path))
                    using (var reader = new StreamReader(archive.GetEntry("backup.json").Open()))
                    {
                        var manifest = JsonConvert.DeserializeObject<BackupManifest>(reader.ReadToEnd());
                        result.Add(new BackupInfo
                            { Path = path, Manifest = manifest, SizeBytes = new FileInfo(path).Length });
                    }
                }
                catch (Exception exception)
                {
                    _log.Warning("backup.list_skipped", exception.Message);
                }
            }

            return result.OrderByDescending(item => item.Manifest.CreatedAtUtc).ToList();
        }

        private void TrimAutomaticBackups(Guid instanceId)
        {
            foreach (var old in List(instanceId).Where(item => item.Manifest.Automatic).Skip(3))
                File.Delete(old.Path);
        }

        private static bool IncludeDefault(string relative)
        {
            var path = relative.Replace('\\', '/');
            return path.Equals("config.json", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("data/saves/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("data/config/", StringComparison.OrdinalIgnoreCase) ||
                   path.Equals("data/options.txt", StringComparison.OrdinalIgnoreCase) ||
                   path.Equals("data/servers.dat", StringComparison.OrdinalIgnoreCase);
        }

        private static PackageFileEntry CreateFileEntry(string root, string relative)
        {
            var path = Path.Combine(root, relative);
            return new PackageFileEntry
            {
                Path = relative.Replace('\\', '/'), Size = new FileInfo(path).Length, Sha256 = HashService.Sha256(path)
            };
        }

        private static void VerifyFiles(string root, IEnumerable<PackageFileEntry> files)
        {
            foreach (var file in files)
            {
                var path = Path.Combine(root, file.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path) ||
                    !HashService.Sha256(path).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Backup checksum verification failed for " + file.Path);
            }
        }
    }
}