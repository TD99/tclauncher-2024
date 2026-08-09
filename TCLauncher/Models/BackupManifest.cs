using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TCLauncher.Models
{
    public sealed class BackupManifest
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonProperty("backupId")]
        public Guid BackupId { get; set; }

        [JsonProperty("instanceId")]
        public Guid InstanceId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; }

        [JsonProperty("automatic")]
        public bool Automatic { get; set; }

        [JsonProperty("fullInstance")]
        public bool FullInstance { get; set; }

        [JsonProperty("files")]
        public List<PackageFileEntry> Files { get; set; } = new List<PackageFileEntry>();
    }

    public sealed class BackupInfo
    {
        public string Path { get; set; }
        public BackupManifest Manifest { get; set; }
        public long SizeBytes { get; set; }
        public string DisplayLabel => $"{Manifest?.Name} — {Manifest?.CreatedAtUtc.ToLocalTime():g} ({(Manifest?.Automatic == true ? "automatic" : "manual")})";
    }
}
