using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using Newtonsoft.Json.Linq;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IModLoaderService
    {
        Task<string> EnsureInstalledAsync(Instance instance, MinecraftLauncher launcher, IProgress<OperationProgress> progress, CancellationToken cancellationToken);
    }

    public sealed class ModLoaderService : IModLoaderService
    {
        private readonly HttpClient _http;
        private readonly IForgeVersionResolver _forgeVersions;

        public ModLoaderService(HttpClient http, IForgeVersionResolver forgeVersions)
        {
            _http = http;
            _forgeVersions = forgeVersions;
        }

        public async Task<string> EnsureInstalledAsync(Instance instance, MinecraftLauncher launcher, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
        {
            var loader = instance.GetEffectiveLoader();
            progress?.Report(new OperationProgress { Stage = OperationStage.InstallingLoader, Message = "Installing " + loader.Type });
            await launcher.InstallAsync(instance.McVersion, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            switch (loader.Type)
            {
                case LoaderType.Fabric:
                    var fabric = new FabricInstaller(_http);
                    var fabricVersion = await ResolveFabricLoaderVersionAsync(
                        _http, instance.McVersion, loader.Version, cancellationToken);
                    if (!string.Equals(fabricVersion, loader.Version, StringComparison.OrdinalIgnoreCase))
                    {
                        progress?.Report(new OperationProgress
                        {
                            Stage = OperationStage.InstallingLoader,
                            Message = string.IsNullOrWhiteSpace(loader.Version)
                                ? "Using Fabric Loader " + fabricVersion
                                : "Fabric Loader " + loader.Version + " is unavailable; using " + fabricVersion
                        });
                        loader.Version = fabricVersion;
                    }
                    return await fabric.Install(instance.McVersion, fabricVersion, launcher.MinecraftPath);
                case LoaderType.Forge:
                    var forge = new ForgeInstaller(launcher, _http);
                    var resolved = await _forgeVersions.ResolveAsync(instance.McVersion, loader.Version, cancellationToken);
                    var forgeVersion = new ForgeVersion(resolved.MinecraftVersion, resolved.ForgeVersion)
                    {
                        IsLatestVersion = resolved.IsLatest,
                        IsRecommendedVersion = resolved.IsRecommended,
                        Files = new[]
                        {
                            new ForgeVersionFile
                            {
                                Type = "installer",
                                DirectUrl = resolved.InstallerUrl,
                                AdUrl = resolved.InstallerUrl
                            }
                        }
                    };
                    return await forge.Install(forgeVersion, new ForgeInstallOptions
                    {
                        CancellationToken = cancellationToken,
                        SkipIfAlreadyInstalled = true,
                        InstallerOutput = new Progress<string>(message => progress?.Report(new OperationProgress
                        {
                            Stage = OperationStage.InstallingLoader,
                            Message = message
                        }))
                    });
                case LoaderType.NeoForge:
                    var neoForge = new NeoForgeInstaller(launcher);
                    return string.IsNullOrWhiteSpace(loader.Version)
                        ? await neoForge.Install(instance.McVersion)
                        : await neoForge.Install(instance.McVersion, loader.Version);
                default:
                    return instance.McVersion;
            }
        }

        internal static async Task<string> ResolveFabricLoaderVersionAsync(HttpClient http, string minecraftVersion,
            string requestedVersion, CancellationToken cancellationToken)
        {
            if (http == null) throw new ArgumentNullException(nameof(http));
            if (string.IsNullOrWhiteSpace(minecraftVersion))
                throw new InvalidDataException("A Minecraft version is required to install Fabric.");

            var uri = new Uri("https://meta.fabricmc.net/v2/versions/loader/" + Uri.EscapeDataString(minecraftVersion.Trim()));
            using (var response = await http.GetAsync(uri, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var entries = JArray.Parse(json).OfType<JObject>().ToList();
                if (entries.Count == 0)
                    throw new InvalidDataException("Fabric does not support Minecraft " + minecraftVersion + ".");

                var requested = requestedVersion?.Trim();
                var exact = entries.FirstOrDefault(entry => string.Equals(
                    (string)entry["loader"]?["version"], requested, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return (string)exact["loader"]?["version"];

                var fallback = entries.FirstOrDefault(entry => (bool?)entry["loader"]?["stable"] == true) ?? entries[0];
                var version = (string)fallback["loader"]?["version"];
                if (string.IsNullOrWhiteSpace(version))
                    throw new InvalidDataException("Fabric returned an invalid loader version for Minecraft " + minecraftVersion + ".");
                return version;
            }
        }
    }
}
