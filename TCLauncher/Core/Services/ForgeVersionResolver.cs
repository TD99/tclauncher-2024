using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace TCLauncher.Core.Services
{
    public sealed class ResolvedForgeVersion
    {
        public string MinecraftVersion { get; set; }
        public string ForgeVersion { get; set; }
        public string ArtifactVersion { get; set; }
        public string InstallerUrl { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsLatest { get; set; }
    }

    public interface IForgeVersionResolver
    {
        Task<ResolvedForgeVersion> ResolveAsync(string minecraftVersion, string requestedVersion,
            CancellationToken cancellationToken);
    }

    public sealed class ForgeVersionResolver : IForgeVersionResolver
    {
        private static readonly Uri PromotionsUri =
            new Uri("https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json");

        private static readonly Uri MetadataUri =
            new Uri("https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml");

        private readonly HttpClient _http;
        private readonly string _cacheDirectory;
        private readonly IAtomicFileService _files;
        private readonly ILogService _log;

        public ForgeVersionResolver(HttpClient http, string cacheDirectory, IAtomicFileService files, ILogService log)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<ResolvedForgeVersion> ResolveAsync(string minecraftVersion, string requestedVersion,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(minecraftVersion))
                throw new InvalidDataException("A Minecraft version is required to install Forge.");

            var metadata = await GetWithCacheAsync(MetadataUri, Path.Combine(_cacheDirectory, "maven-metadata.xml"),
                cancellationToken);
            var available = ParseArtifactVersions(metadata);
            var promotions = string.IsNullOrWhiteSpace(requestedVersion)
                ? await GetWithCacheAsync(PromotionsUri, Path.Combine(_cacheDirectory, "promotions.json"),
                    cancellationToken)
                : null;

            var recommended = promotions == null ? null : ReadPromotion(promotions, minecraftVersion + "-recommended");
            var latest = promotions == null ? null : ReadPromotion(promotions, minecraftVersion + "-latest");
            var forgeVersion = NormalizeRequestedVersion(minecraftVersion,
                string.IsNullOrWhiteSpace(requestedVersion) ? recommended ?? latest : requestedVersion);

            if (string.IsNullOrWhiteSpace(forgeVersion))
                throw new InvalidDataException(
                    $"Forge does not publish a recommended or latest build for Minecraft {minecraftVersion}.");

            var artifactVersion = minecraftVersion + "-" + forgeVersion;
            if (!available.Contains(artifactVersion))
                throw new InvalidDataException(
                    $"Forge {artifactVersion} is not available in the official Forge Maven repository.");

            return new ResolvedForgeVersion
            {
                MinecraftVersion = minecraftVersion,
                ForgeVersion = forgeVersion,
                ArtifactVersion = artifactVersion,
                InstallerUrl =
                    $"https://maven.minecraftforge.net/net/minecraftforge/forge/{artifactVersion}/forge-{artifactVersion}-installer.jar",
                IsRecommended = string.Equals(forgeVersion, recommended, StringComparison.OrdinalIgnoreCase),
                IsLatest = string.Equals(forgeVersion, latest, StringComparison.OrdinalIgnoreCase)
            };
        }

        internal static string NormalizeRequestedVersion(string minecraftVersion, string requestedVersion)
        {
            var value = requestedVersion?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            var prefix = minecraftVersion + "-";
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return value.Substring(prefix.Length);
            if (value.Contains("-"))
                throw new InvalidDataException(
                    $"Forge version '{value}' does not belong to Minecraft {minecraftVersion}.");
            return value;
        }

        private async Task<string> GetWithCacheAsync(Uri uri, string cachePath, CancellationToken cancellationToken)
        {
            try
            {
                using (var response = await _http.GetAsync(uri, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    _files.WriteAllText(cachePath, content);
                    return content;
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException) && File.Exists(cachePath))
            {
                _log.Warning("forge.metadata_cache_used", exception.Message);
                return File.ReadAllText(cachePath);
            }
        }

        private static HashSet<string> ParseArtifactVersions(string xml)
        {
            try
            {
                return new HashSet<string>(
                    XDocument.Parse(xml).Descendants("version").Select(node => node.Value.Trim()),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("The official Forge Maven metadata is malformed.", exception);
            }
        }

        private static string ReadPromotion(string json, string key)
        {
            try
            {
                return (string)JObject.Parse(json)["promos"]?[key];
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("The official Forge promotions metadata is malformed.", exception);
            }
        }
    }
}