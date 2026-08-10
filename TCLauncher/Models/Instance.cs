using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TCLauncher.Core;

namespace TCLauncher.Models
{
    public class Instance
    {
        [JsonProperty("schemaVersion", Order = -20)]
        public int SchemaVersion { get; set; }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Guid Guid { get; set; }
        public string Version { get; set; }
        public bool Upgradeable { get; set; }
        public string ThumbnailURL { get; set; } = "/Assets/Images/nothumb.png";
        public string Type { get; set; }
        public string McVersion { get; set; }
        public bool? UseFabric { get; set; }
        public bool? UseForge { get; set; }

        [JsonProperty("loader", NullValueHandling = NullValueHandling.Ignore, Order = -10)]
        public LoaderConfiguration Loader { get; set; }

        public string WorkingDirZipURL { get; set; }
        public string PayloadSha256 { get; set; }
        public List<Patch> Patches { get; set; }
        public bool UsePatch { get; set; }
        public bool? UseIsolation { get; set; }
        public Dictionary<string, List<string>> WorkingDirDesc { get; set; }
        public bool Is_Installed { get; set; }
        public string AppletURL { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public List<Server> Servers { get; set; }

        public int? MinimumRamMb { get; set; }
        public int? MaximumRamMb { get; set; }
        public string[] JVMArguments { get; set; }
        public bool Is_LocalSource { get; set; }

        public Instance()
        {
        }

        public LoaderConfiguration GetEffectiveLoader()
        {
            if (Loader != null) return Loader;
            if (UseForge == true) return new LoaderConfiguration { Type = LoaderType.Forge };
            if (UseFabric == true) return new LoaderConfiguration { Type = LoaderType.Fabric };
            return LoaderConfiguration.Vanilla();
        }

        public void NormalizeLegacyConfiguration()
        {
            Loader = GetEffectiveLoader();
            NormalizeCompositeLoaderVersion();
            Patches = Patches ?? new List<Patch>();
            Servers = Servers ?? new List<Server>();
            JVMArguments = JVMArguments ?? new string[0];
        }

        private void NormalizeCompositeLoaderVersion()
        {
            var value = McVersion?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return;

            Match match = null;
            switch (Loader.Type)
            {
                case LoaderType.Fabric:
                    match = Regex.Match(value,
                        @"^fabric-loader-(?<loader>\d+(?:\.\d+){1,3})-(?<minecraft>.+)$",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    break;
                case LoaderType.Forge:
                    match = Regex.Match(value, @"^(?<minecraft>.+)-forge-(?<loader>.+)$",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    break;
                case LoaderType.NeoForge:
                    match = Regex.Match(value, @"^(?<minecraft>.+)-neoforge-(?<loader>.+)$",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    break;
            }

            if (match?.Success != true) return;
            McVersion = match.Groups["minecraft"].Value;
            if (string.IsNullOrWhiteSpace(Loader.Version))
                Loader.Version = match.Groups["loader"].Value;
        }

        public void PrepareForV2Save()
        {
            NormalizeLegacyConfiguration();
            SchemaVersion = 2;
            UseFabric = Loader.Type == LoaderType.Fabric;
            UseForge = Loader.Type == LoaderType.Forge;
        }

        public Instance(string name, string displayName, Guid guid, string version, bool upgradeable,
            string thumbnailUrl, string type, string mcVersion, bool? useFabric, bool? useForge,
            string workingDirZipUrl, List<Patch> patches, bool usePatch, bool? useIsolation,
            Dictionary<string, List<string>> workingDirDesc, string appletUrl, Dictionary<string, object> requirements,
            List<Server> servers, int? minimumRamMb, int? maximumRamMb, string[] jvmArguments)
        {
            Name = name;
            DisplayName = displayName;
            Guid = guid;
            Version = version;
            Upgradeable = upgradeable;
            ThumbnailURL = thumbnailUrl;
            Type = type;
            McVersion = mcVersion;
            UseFabric = useFabric;
            WorkingDirZipURL = workingDirZipUrl;
            Patches = patches;
            UsePatch = usePatch;
            UseForge = useForge;
            UseIsolation = useIsolation;
            WorkingDirDesc = workingDirDesc;
            AppletURL = appletUrl;
            Requirements = requirements;
            Servers = servers;
            MinimumRamMb = minimumRamMb;
            MaximumRamMb = maximumRamMb;
            JVMArguments = jvmArguments;
        }

        public Patch GetCurrentPatch()
        {
            return Patches?.OrderByDescending(p => p.ID).FirstOrDefault();
        }

        public bool IsSameAsDecent(object compare)
        {
            if (compare == null) return false;
            if (compare.GetType() != GetType()) return false;

            var instance = (Instance)compare;

            return Guid == instance.Guid;
        }

        public bool IsSameAs(object compare)
        {
            if (compare == null) return false;
            if (compare.GetType() != GetType()) return false;

            var instance = (Instance)compare;

            // ReSharper disable once ReplaceWithSingleAssignment.True
            var areThumbnailsSame =
                true; // IoUtils.TclFile.CompareImages(ThumbnailURL, instance.ThumbnailURL); // TODO: Fix -> too intensive
            if (!IoUtils.TclFile.DoesFileExistByUrlOrPath(ThumbnailURL)) areThumbnailsSame = false;

            var arePatchesSame = !(Patches == null && instance.Patches != null ||
                                   Patches != null && instance.Patches == null);
            if (Patches != null && instance.Patches != null && Patches.Count == instance.Patches.Count)
            {
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var patch in Patches)
                {
                    var comparePatch = instance.Patches.FirstOrDefault(p => p.ID == patch.ID);
                    if (comparePatch != null && patch.IsSameAs(comparePatch)) continue;
                    arePatchesSame = false;
                    break;
                }
            }

            var areWorkingDirDescSame = WorkingDirDesc == null && instance.WorkingDirDesc == null ||
                                        WorkingDirDesc != null && instance.WorkingDirDesc != null &&
                                        WorkingDirDesc.Keys.All(k =>
                                            instance.WorkingDirDesc.ContainsKey(k) && instance.WorkingDirDesc[k]
                                                .SequenceEqual(WorkingDirDesc[k]));

            var areRequirementsSame = Requirements == null && instance.Requirements == null || Requirements != null &&
                instance.Requirements != null && Requirements.Keys.All(k =>
                    instance.Requirements.ContainsKey(k) && instance.Requirements[k].Equals(Requirements[k]));

            var areServersSame = !(Servers == null && instance.Servers != null ||
                                   Servers != null && instance.Servers == null);
            if (Servers != null && instance.Servers != null && Servers.Count == instance.Servers.Count)
            {
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var server in Servers)
                {
                    var compareServer = instance.Servers.FirstOrDefault(s => s.Name == server.Name);
                    var isServerSame = server.IsSameAs(compareServer);
                    if (compareServer != null && server.IsSameAs(compareServer)) continue;
                    areServersSame = false;
                    break;
                }
            }

            var areJVMArgumentsSame = JVMArguments == null && instance.JVMArguments == null || JVMArguments != null &&
                instance.JVMArguments != null && JVMArguments.SequenceEqual(instance.JVMArguments);

            return Name == instance.Name &&
                   DisplayName == instance.DisplayName &&
                   Guid == instance.Guid &&
                   Version == instance.Version &&
                   Upgradeable == instance.Upgradeable &&
                   Type == instance.Type &&
                   McVersion == instance.McVersion &&
                   UseFabric == instance.UseFabric &&
                   UseForge == instance.UseForge &&
                   UseIsolation == instance.UseIsolation &&
                   WorkingDirZipURL == instance.WorkingDirZipURL &&
                   UsePatch == instance.UsePatch &&
                   AppletURL == instance.AppletURL &&
                   MinimumRamMb == instance.MinimumRamMb &&
                   MaximumRamMb == instance.MaximumRamMb &&
                   areThumbnailsSame &&
                   arePatchesSame &&
                   areRequirementsSame &&
                   areServersSame &&
                   areJVMArgumentsSame &&
                   areWorkingDirDescSame;
        }
    }
}