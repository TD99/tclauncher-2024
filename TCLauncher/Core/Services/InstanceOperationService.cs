using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IInstanceOperationService
    {
        Task<OperationResult<InstalledInstance>> InstallOrUpdateAsync(Instance instance,
            IProgress<OperationProgress> progress, CancellationToken cancellationToken);
    }

    public sealed class InstanceOperationService : IInstanceOperationService
    {
        private const long MaximumPayloadBytes = 32L * 1024 * 1024 * 1024;
        private readonly string _instancesRoot;
        private readonly HttpClient _http;
        private readonly ISafeArchiveService _archives;
        private readonly IAtomicFileService _atomic;
        private readonly IInstanceConfigService _configs;
        private readonly IBackupService _backups;
        private readonly ILogService _log;

        public InstanceOperationService(string instancesRoot, HttpClient http, ISafeArchiveService archives,
            IAtomicFileService atomic, IInstanceConfigService configs, IBackupService backups, ILogService log)
        {
            _instancesRoot = instancesRoot;
            _http = http;
            _archives = archives;
            _atomic = atomic;
            _configs = configs;
            _backups = backups;
            _log = log;
        }

        public async Task<OperationResult<InstalledInstance>> InstallOrUpdateAsync(Instance instance,
            IProgress<OperationProgress> progress, CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var destination = Path.Combine(_instancesRoot, instance.Guid.ToString());
            var staging = destination + ".staging-" + operationId;
            var temporary = Path.Combine(Path.GetTempPath(), "tcl-operation-" + operationId);
            try
            {
                Report(progress, OperationStage.Preparing, "Preparing profile");
                var errors = _configs.Validate(instance);
                if (errors.Count > 0)
                    return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.InvalidConfiguration,
                        string.Join(Environment.NewLine, errors), operationId: operationId);
                Directory.CreateDirectory(temporary);
                Directory.CreateDirectory(Path.Combine(staging, "data"));

                if (Directory.Exists(destination))
                {
                    Report(progress, OperationStage.Snapshotting, "Creating a rollback backup");
                    var currentResult = _configs.Load(Path.Combine(destination, "config.json"));
                    if (currentResult.IsSuccess)
                    {
                        var current = new InstalledInstance(currentResult.Value);
                        var backup = _backups.Create(current, "Before update", true, false);
                        if (!backup.IsSuccess)
                            return OperationResult<InstalledInstance>.Failure(backup.ErrorCode, backup.Message,
                                backup.Exception, operationId);
                    }
                }

                var archives = BuildArchiveList(instance);
                for (var index = 0; index < archives.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var payload = Path.Combine(temporary, "payload-" + index + ".zip");
                    await DownloadAsync(archives[index].Url, payload, progress, cancellationToken);
                    Report(progress, OperationStage.Verifying, "Verifying download");
                    if (!string.IsNullOrWhiteSpace(archives[index].Sha256) && !HashService.Sha256(payload)
                            .Equals(archives[index].Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The downloaded package checksum does not match.");
                    var extracted = Path.Combine(temporary, "extracted-" + index);
                    Report(progress, OperationStage.Extracting,
                        "Extracting package " + (index + 1) + " of " + archives.Count);
                    _archives.Extract(payload, extracted, MaximumPayloadBytes);
                    DirectoryService.Copy(extracted, Path.Combine(staging, "data"));
                }

                Report(progress, OperationStage.Validating, "Validating profile");
                var installed = new InstalledInstance(instance)
                {
                    InstallationDir = destination,
                    DataDir = Path.Combine(destination, "data"),
                    ConfigFile = Path.Combine(destination, "config.json")
                };
                var save = _configs.Save(installed, Path.Combine(staging, "config.json"));
                if (!save.IsSuccess)
                    return OperationResult<InstalledInstance>.Failure(save.ErrorCode, save.Message, save.Exception,
                        operationId);

                var managed = new PackageManifest
                {
                    PackageId = instance.Guid,
                    CreatedAtUtc = DateTime.UtcNow,
                    Instance = instance,
                    Files = DirectoryService.EnumerateRelativeFiles(Path.Combine(staging, "data")).Select(path =>
                        new PackageFileEntry
                        {
                            Path = path.Replace('\\', '/'),
                            Size = new FileInfo(Path.Combine(staging, "data", path)).Length,
                            Sha256 = HashService.Sha256(Path.Combine(staging, "data", path))
                        }).ToList()
                };
                File.WriteAllText(Path.Combine(staging, "managed.json"),
                    JsonConvert.SerializeObject(managed, Formatting.Indented));

                Report(progress, OperationStage.Activating, "Activating profile");
                _atomic.ReplaceDirectory(staging, destination,
                    destination + ".rollback-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
                Report(progress, OperationStage.Complete, "Profile is ready", 100);
                _log.Info("instance.operation_complete", destination, operationId);
                return OperationResult<InstalledInstance>.Success(installed, operationId);
            }
            catch (OperationCanceledException exception)
            {
                _log.Warning("instance.operation_cancelled", operationId);
                return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.Cancelled,
                    "The operation was cancelled. Existing data was preserved.", exception, operationId);
            }
            catch (InvalidDataException exception)
            {
                _log.Error("instance.operation_invalid", exception, operationId);
                return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.ChecksumMismatch, exception.Message,
                    exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("instance.operation_failed", exception, operationId);
                return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.Unexpected,
                    "The profile operation failed. Existing data was preserved.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        private async Task DownloadAsync(string url, string destination, IProgress<OperationProgress> progress,
            CancellationToken cancellationToken)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("Package downloads must use HTTPS.");
            using (var response =
                   await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                using (var input = await response.Content.ReadAsStreamAsync())
                using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81920];
                    long downloaded = 0;
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, cancellationToken);
                        downloaded += read;
                        progress?.Report(new OperationProgress
                        {
                            Stage = OperationStage.Downloading,
                            Message = "Downloading package",
                            ProgressedBytes = downloaded,
                            TotalBytes = total,
                            Percent = total > 0 ? downloaded * 100d / total : (double?)null
                        });
                    }
                }
            }
        }

        private static List<ArchiveSource> BuildArchiveList(Instance instance)
        {
            if (instance.UsePatch && instance.Patches != null && instance.Patches.Count > 0)
                return instance.Patches.OrderBy(patch => patch.ID)
                    .Select(patch => new ArchiveSource { Url = patch.URL }).ToList();
            if (string.IsNullOrWhiteSpace(instance.WorkingDirZipURL)) return new List<ArchiveSource>();
            return new List<ArchiveSource>
                { new ArchiveSource { Url = instance.WorkingDirZipURL, Sha256 = instance.PayloadSha256 } };
        }

        private static void Report(IProgress<OperationProgress> progress, OperationStage stage, string message,
            double? percent = null) =>
            progress?.Report(new OperationProgress { Stage = stage, Message = message, Percent = percent });

        private sealed class ArchiveSource
        {
            public string Url { get; set; }
            public string Sha256 { get; set; }
        }
    }
}