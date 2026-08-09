using Newtonsoft.Json;

namespace TCLauncher.Models
{
    public sealed class UpdateManifest
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("minimumWindowsVersion")]
        public string MinimumWindowsVersion { get; set; }
        [JsonProperty("minimumFrameworkVersion")]
        public string MinimumFrameworkVersion { get; set; }
        [JsonProperty("installerUrl")]
        public string InstallerUrl { get; set; }
        [JsonProperty("sha256")]
        public string Sha256 { get; set; }
        [JsonProperty("publisher")]
        public string Publisher { get; set; }
        [JsonProperty("releaseNotes")]
        public string ReleaseNotes { get; set; }
        [JsonProperty("mandatory")]
        public bool Mandatory { get; set; }
    }

    public sealed class UpdateCheckResult
    {
        public UpdateManifest Manifest { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public bool IsCompatible { get; set; } = true;
        public string CompatibilityMessage { get; set; }
    }
}
