using System;

namespace TCLauncher.Models
{
    public sealed class OfflineProfile
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    internal sealed class OfflineProfileDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Guid? SelectedProfileId { get; set; }
        public System.Collections.Generic.List<OfflineProfile> Profiles { get; set; } = new System.Collections.Generic.List<OfflineProfile>();
    }
}
