using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public sealed class SupportBundlePreview
    {
        public List<string> IncludedFiles { get; set; } = new List<string>();
        public List<string> ExcludedData { get; set; } = new List<string>();
    }

    public interface ISupportBundleService
    {
        SupportBundlePreview Preview(InstalledInstance instance);
        OperationResult<string> Export(string destinationPath, InstalledInstance instance);
    }

    public sealed class SupportBundleService : ISupportBundleService
    {
        private readonly ILogService _log;

        public SupportBundleService(ILogService log)
        {
            _log = log;
        }

        public SupportBundlePreview Preview(InstalledInstance instance)
        {
            var preview = new SupportBundlePreview();
            preview.IncludedFiles.Add("system.json");
            preview.IncludedFiles.AddRange(Directory.Exists(_log.LogDirectory)
                ? Directory.GetFiles(_log.LogDirectory, "*.jsonl").OrderByDescending(File.GetLastWriteTimeUtc).Take(3)
                    .Select(path => "logs/" + Path.GetFileName(path))
                : Enumerable.Empty<string>());
            if (instance != null)
            {
                preview.IncludedFiles.Add("instance.json");
                var crashRoot = Path.Combine(instance.DataDir, "crash-reports");
                if (Directory.Exists(crashRoot))
                    preview.IncludedFiles.AddRange(Directory.GetFiles(crashRoot, "*.txt")
                        .OrderByDescending(File.GetLastWriteTimeUtc).Take(3)
                        .Select(path => "crash-reports/" + Path.GetFileName(path)));
            }

            preview.ExcludedData.AddRange(new[]
            {
                "Microsoft account/token storage", "saves and worlds", "game assets and mods", "unrelated profile data"
            });
            return preview;
        }

        public OperationResult<string> Export(string destinationPath, InstalledInstance instance)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var temporary = Path.Combine(Path.GetTempPath(), "tcl-support-" + operationId);
            try
            {
                Directory.CreateDirectory(temporary);
                var system = new
                {
                    generatedAtUtc = DateTime.UtcNow,
                    launcherVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    windowsVersion = Environment.OSVersion.VersionString,
                    framework = Environment.Version.ToString(),
                    is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                    processorCount = Environment.ProcessorCount,
                    rootDriveFreeBytes = new DriveInfo(Path.GetPathRoot(IoUtils.Tcl.RootPath)).AvailableFreeSpace
                };
                File.WriteAllText(Path.Combine(temporary, "system.json"),
                    JsonConvert.SerializeObject(system, Formatting.Indented));

                CopyRecentLogs(temporary);
                if (instance != null)
                {
                    var safeInstance = new
                    {
                        instance.Guid, instance.Name, instance.DisplayName, instance.Version, instance.McVersion,
                        Loader = instance.GetEffectiveLoader(), instance.MinimumRamMb, instance.MaximumRamMb,
                        instance.UseIsolation, ServerCount = instance.Servers?.Count ?? 0
                    };
                    File.WriteAllText(Path.Combine(temporary, "instance.json"),
                        JsonConvert.SerializeObject(safeInstance, Formatting.Indented));
                    CopyCrashReports(instance, temporary);
                }

                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                ZipFile.CreateFromDirectory(temporary, destinationPath, CompressionLevel.Optimal, false);
                _log.Info("support_bundle.exported", destinationPath, operationId);
                return OperationResult<string>.Success(destinationPath, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("support_bundle.failed", exception, operationId);
                return OperationResult<string>.Failure(LauncherErrorCode.Unexpected,
                    "The support bundle could not be created.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
            }
        }

        private void CopyRecentLogs(string temporary)
        {
            if (!Directory.Exists(_log.LogDirectory)) return;
            var output = Path.Combine(temporary, "logs");
            Directory.CreateDirectory(output);
            foreach (var path in Directory.GetFiles(_log.LogDirectory, "*.jsonl")
                         .OrderByDescending(File.GetLastWriteTimeUtc).Take(3))
                File.WriteAllText(Path.Combine(output, Path.GetFileName(path)),
                    SecretRedactor.Redact(File.ReadAllText(path)));
        }

        private static void CopyCrashReports(InstalledInstance instance, string temporary)
        {
            var root = Path.Combine(instance.DataDir, "crash-reports");
            if (!Directory.Exists(root)) return;
            var output = Path.Combine(temporary, "crash-reports");
            Directory.CreateDirectory(output);
            foreach (var path in Directory.GetFiles(root, "*.txt").OrderByDescending(File.GetLastWriteTimeUtc).Take(3))
                File.WriteAllText(Path.Combine(output, Path.GetFileName(path)),
                    SecretRedactor.Redact(File.ReadAllText(path)));
        }
    }
}