using System;
using System.Collections.Generic;

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

        public List<OfflineProfile> Profiles { get; set; } =
            new List<OfflineProfile>();
    }
}