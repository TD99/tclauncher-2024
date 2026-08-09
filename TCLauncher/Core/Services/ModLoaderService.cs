using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
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
                    return string.IsNullOrWhiteSpace(loader.Version)
                        ? await fabric.Install(instance.McVersion, launcher.MinecraftPath)
                        : await fabric.Install(instance.McVersion, loader.Version, launcher.MinecraftPath);
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
    }
}
