using System;

namespace TCLauncher.Models
{
    public sealed class LaunchActivity
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public DateTime LaunchedAtUtc { get; set; }
        public long? DurationSeconds { get; set; }
        public int? ExitCode { get; set; }
        public string LastServer { get; set; }
    }
}
