using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TCLauncher.Models
{
    public sealed class PackageManifest
    {
        public const int CurrentVersion = 2;

        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = CurrentVersion;

        [JsonProperty("packageId")] public Guid PackageId { get; set; }

        [JsonProperty("createdAtUtc")] public DateTime CreatedAtUtc { get; set; }

        [JsonProperty("instance")] public Instance Instance { get; set; }

        [JsonProperty("payload")] public PackagePayload Payload { get; set; }

        [JsonProperty("files")] public List<PackageFileEntry> Files { get; set; } = new List<PackageFileEntry>();
    }

    public sealed class PackagePayload
    {
        [JsonProperty("path")] public string Path { get; set; } = "payload.zip";

        [JsonProperty("sha256")] public string Sha256 { get; set; }

        [JsonProperty("uncompressedBytes")] public long UncompressedBytes { get; set; }
    }

    public sealed class PackageFileEntry
    {
        [JsonProperty("path")] public string Path { get; set; }

        [JsonProperty("size")] public long Size { get; set; }

        [JsonProperty("sha256")] public string Sha256 { get; set; }
    }

    public enum ImportConflictResolution
    {
        Cancel,
        Replace,
        ImportAsCopy
    }

    public sealed class ImportPreview
    {
        public string SourcePath { get; set; }
        public PackageManifest Manifest { get; set; }
        public bool IsLegacy { get; set; }
        public bool HasConflict { get; set; }
        public long PackageBytes { get; set; }
    }
}