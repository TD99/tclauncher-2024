using Newtonsoft.Json;

namespace TCLauncher.Models
{
    public enum LoaderType
    {
        Vanilla,
        Fabric,
        Forge,
        NeoForge
    }

    public sealed class LoaderConfiguration
    {
        [JsonProperty("type")] public LoaderType Type { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        public static LoaderConfiguration Vanilla()
        {
            return new LoaderConfiguration { Type = LoaderType.Vanilla };
        }
    }
}