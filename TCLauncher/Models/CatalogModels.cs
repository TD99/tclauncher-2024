using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TCLauncher.Models
{
    public sealed class CatalogDocument
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonProperty("generatedAtUtc")]
        public DateTime GeneratedAtUtc { get; set; }

        [JsonProperty("items")]
        public List<CatalogItem> Items { get; set; } = new List<CatalogItem>();

        [JsonProperty("content")]
        public List<ContentCard> Content { get; set; } = new List<ContentCard>();
    }

    public sealed class CatalogItem
    {
        public Guid Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public List<string> Screenshots { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public string MinecraftVersion { get; set; }
        public LoaderConfiguration Loader { get; set; }
        public string PackVersion { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public List<Server> Servers { get; set; } = new List<Server>();
        public string PayloadUrl { get; set; }
        public string PayloadSha256 { get; set; }
        public bool Featured { get; set; }
        public DateTime PublishedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public static CatalogItem FromLegacy(Instance instance)
        {
            instance.NormalizeLegacyConfiguration();
            return new CatalogItem
            {
                Id = instance.Guid,
                Slug = instance.Name,
                Title = instance.DisplayName,
                Summary = instance.Type,
                Description = instance.WorkingDirDesc == null ? null : string.Join(Environment.NewLine, instance.WorkingDirDesc.Keys),
                ThumbnailUrl = instance.ThumbnailURL,
                MinecraftVersion = instance.McVersion,
                Loader = instance.Loader,
                PackVersion = instance.Version,
                Requirements = instance.Requirements,
                Servers = instance.Servers,
                PayloadUrl = instance.WorkingDirZipURL
            };
        }

        public Instance ToInstance()
        {
            var loader = Loader ?? LoaderConfiguration.Vanilla();
            return new Instance
            {
                SchemaVersion = 2,
                Guid = Id,
                Name = Slug,
                DisplayName = Title,
                Version = PackVersion,
                Type = Tags == null ? "T-Craft" : string.Join(", ", Tags),
                McVersion = MinecraftVersion,
                Loader = loader,
                UseFabric = loader.Type == LoaderType.Fabric,
                UseForge = loader.Type == LoaderType.Forge,
                ThumbnailURL = ThumbnailUrl,
                WorkingDirZipURL = PayloadUrl,
                PayloadSha256 = PayloadSha256,
                Requirements = Requirements,
                Servers = Servers,
                WorkingDirDesc = string.IsNullOrWhiteSpace(Description)
                    ? null
                    : new Dictionary<string, List<string>> { { Summary ?? Title, new List<string> { Description } } },
                MaximumRamMb = 4096,
                MinimumRamMb = 1024
            };
        }
    }

    public enum ContentCardType
    {
        Announcement,
        ServerStatus,
        ReleaseNote,
        FeaturedPack
    }

    public sealed class ContentCard
    {
        public ContentCardType Type { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string ImageUrl { get; set; }
        public string ActionUrl { get; set; }
        public Guid? CatalogItemId { get; set; }
        public int Weight { get; set; }
    }

    public sealed class CatalogLoadResult
    {
        public CatalogDocument Catalog { get; set; }
        public bool IsOffline { get; set; }
        public bool IsStale { get; set; }
        public DateTime? CachedAtUtc { get; set; }
    }
}
