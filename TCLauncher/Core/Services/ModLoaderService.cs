using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
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

        public ModLoaderService(HttpClient http)
        {
            _http = http;
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
                    return string.IsNullOrWhiteSpace(loader.Version)
                        ? await forge.Install(instance.McVersion)
                        : await forge.Install(instance.McVersion, loader.Version);
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
