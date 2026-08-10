using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IInstanceHealthService
    {
        InstanceHealthReport Inspect(InstalledInstance instance);
    }

    public sealed class InstanceHealthService : IInstanceHealthService
    {
        private readonly IInstanceConfigService _configs;
        private readonly IBackupService _backups;

        public InstanceHealthService(IInstanceConfigService configs, IBackupService backups)
        {
            _configs = configs;
            _backups = backups;
        }

        public InstanceHealthReport Inspect(InstalledInstance instance)
        {
            var report = new InstanceHealthReport { Instance = instance };
            if (instance == null || !Directory.Exists(instance.InstallationDir))
            {
                report.Checks.Add(Check("Installation", "The profile folder is missing.", HealthSeverity.Error,
                    "repair"));
                report.OverallSeverity = HealthSeverity.Error;
                return report;
            }

            var validation = _configs.Validate(instance);
            report.Checks.Add(validation.Count == 0
                ? Check("Configuration", "Profile configuration is valid.", HealthSeverity.Healthy)
                : Check("Configuration", string.Join(Environment.NewLine, validation), HealthSeverity.Error, "edit"));

            report.Checks.Add(Directory.Exists(instance.DataDir)
                ? Check("Game data", "The game data folder is available.", HealthSeverity.Healthy, "open-folder")
                : Check("Game data", "The game data folder is missing.", HealthSeverity.Error, "repair"));

            var loader = instance.GetEffectiveLoader();
            report.Checks.Add(Check("Loader",
                loader.Type + (string.IsNullOrWhiteSpace(loader.Version) ? string.Empty : " " + loader.Version),
                HealthSeverity.Information));
            if (loader.Type == LoaderType.NeoForge && CompareMinecraftVersions(instance.McVersion, "1.20.2") < 0)
                report.Checks.Add(Check("NeoForge compatibility",
                    "NeoForge profiles require Minecraft 1.20.2 or newer.", HealthSeverity.Error, "edit"));

            if (Directory.Exists(instance.InstallationDir))
                report.StorageBytes = Directory.GetFiles(instance.InstallationDir, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
            report.Checks.Add(Check("Storage", FormatBytes(report.StorageBytes), HealthSeverity.Information,
                "open-folder"));

            report.LatestBackup = _backups.List(instance.Guid).FirstOrDefault();
            report.Checks.Add(report.LatestBackup == null
                ? Check("Backup", "No backup exists for this profile.", HealthSeverity.Warning, "backup")
                : Check("Backup", "Latest: " + report.LatestBackup.Manifest.CreatedAtUtc.ToLocalTime().ToString("g"),
                    HealthSeverity.Healthy, "backups"));

            InspectManagedFiles(instance, report);
            var crashRoot = Path.Combine(instance.DataDir, "crash-reports");
            var latestCrash = Directory.Exists(crashRoot)
                ? Directory.GetFiles(crashRoot, "*.txt").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                : null;
            if (latestCrash != null)
                report.Checks.Add(Check("Recent crash", Path.GetFileName(latestCrash), HealthSeverity.Warning, "logs"));

            report.OverallSeverity = report.Checks.Max(item => item.Severity);
            return report;
        }

        private static void InspectManagedFiles(InstalledInstance instance, InstanceHealthReport report)
        {
            var path = Path.Combine(instance.InstallationDir, "managed.json");
            if (!File.Exists(path))
            {
                report.Checks.Add(Check("Managed files", "Local profile; no managed-file manifest is available.",
                    HealthSeverity.Information));
                return;
            }

            try
            {
                var manifest = JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(path));
                var invalid = manifest.Files.Where(file =>
                {
                    var candidate = Path.Combine(instance.DataDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
                    return !File.Exists(candidate) || new FileInfo(candidate).Length != file.Size ||
                           (!string.IsNullOrWhiteSpace(file.Sha256) && !HashService.Sha256(candidate)
                               .Equals(file.Sha256, StringComparison.OrdinalIgnoreCase));
                }).Take(10).ToList();
                report.Checks.Add(invalid.Count == 0
                    ? Check("Managed files", "All managed files are present.", HealthSeverity.Healthy)
                    : Check("Managed files", invalid.Count + " managed file(s) need repair.", HealthSeverity.Warning,
                        "repair"));
            }
            catch
            {
                report.Checks.Add(Check("Managed files", "The managed-file manifest is invalid.",
                    HealthSeverity.Warning, "repair"));
            }
        }

        private static HealthCheckItem Check(string name, string message, HealthSeverity severity,
            string action = null) =>
            new HealthCheckItem { Name = name, Message = message, Severity = severity, Action = action };

        private static int CompareMinecraftVersions(string left, string right)
        {
            Version leftVersion, rightVersion;
            return Version.TryParse(left, out leftVersion) && Version.TryParse(right, out rightVersion)
                ? leftVersion.CompareTo(rightVersion)
                : 0;
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            var index = 0;
            while (value >= 1024 && index < units.Length - 1)
            {
                value /= 1024;
                index++;
            }

            return value.ToString("0.##") + " " + units[index];
        }
    }
}