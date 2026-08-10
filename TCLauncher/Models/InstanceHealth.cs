using System.Collections.Generic;

namespace TCLauncher.Models
{
    public enum HealthSeverity
    {
        Healthy,
        Information,
        Warning,
        Error
    }

    public sealed class HealthCheckItem
    {
        public string Name { get; set; }
        public string Message { get; set; }
        public HealthSeverity Severity { get; set; }
        public string Action { get; set; }
    }

    public sealed class InstanceHealthReport
    {
        public InstalledInstance Instance { get; set; }
        public List<HealthCheckItem> Checks { get; set; } = new List<HealthCheckItem>();
        public long StorageBytes { get; set; }
        public BackupInfo LatestBackup { get; set; }
        public HealthSeverity OverallSeverity { get; set; }
    }
}