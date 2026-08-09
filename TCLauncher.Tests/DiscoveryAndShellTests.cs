using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.ViewModel;

namespace TCLauncher.Tests
{
    [TestClass]
    public class DiscoveryAndShellTests
    {
        [TestMethod]
        public void DiscoverySearchesMetadataAndSortsInstalledFirst()
        {
            var viewModel = new ServerListViewModel(false);
            viewModel.SetInstancesForTesting(new[]
            {
                Game("remote", "Sky Factory", "1.20.1", LoaderType.Forge, false, "automation, tech"),
                Game("local", "Vanilla Friends", "1.21.1", LoaderType.Vanilla, true, "survival")
            });

            Assert.IsTrue(viewModel.ServerList.First().Is_Installed);
            viewModel.SearchText = "automation";
            Assert.AreEqual(1, viewModel.ServerList.Count);
            Assert.AreEqual("Sky Factory", viewModel.ServerList[0].DisplayName);
            viewModel.SearchText = "forge";
            Assert.AreEqual(1, viewModel.ServerList.Count);
        }

        [TestMethod]
        public void DiscoveryFiltersImmediatelyByLoaderVersionAndAvailability()
        {
            var viewModel = new ServerListViewModel(false);
            viewModel.SetInstancesForTesting(new[]
            {
                Game("forge", "Forge", "1.20.1", LoaderType.Forge, false, ""),
                Game("fabric", "Fabric", "1.21.1", LoaderType.Fabric, true, "")
            });
            viewModel.LoaderFilter = "Fabric";
            viewModel.MinecraftVersionFilters.Add("1.21.1");
            viewModel.MinecraftVersionFilter = "1.21.1";
            viewModel.AvailabilityFilter = "Installed";

            Assert.AreEqual(1, viewModel.ServerList.Count);
            Assert.AreEqual("Fabric", viewModel.ServerList[0].DisplayName);
        }

        [TestMethod]
        public async Task OperationCoordinatorRejectsConcurrentWorkWithoutQueueing()
        {
            var coordinator = new OperationCoordinator();
            var release = new TaskCompletionSource<bool>();
            var first = coordinator.RunAsync("Install", true, async (progress, token) =>
            {
                await release.Task;
                return OperationResult<string>.Success("done");
            });

            var second = await coordinator.RunAsync("Backup", true,
                (progress, token) => Task.FromResult(OperationResult<string>.Success("unexpected")));
            Assert.IsFalse(second.IsSuccess);
            Assert.AreEqual(LauncherErrorCode.Conflict, second.ErrorCode);
            release.SetResult(true);
            Assert.IsTrue((await first).IsSuccess);
            Assert.IsFalse(coordinator.IsBusy);
        }

        [TestMethod]
        public void OverlayHostExposesDrawerState()
        {
            var host = new OverlayHostViewModel
            {
                Current = new OverlaySurface { Kind = OverlayKind.Drawer, Title = "Details", Content = "Content" }
            };
            Assert.IsTrue(host.IsOpen);
            Assert.IsTrue(host.IsDrawer);
            host.Current = null;
            Assert.IsFalse(host.IsOpen);
        }

        private static Instance Game(string name, string title, string minecraft, LoaderType loader, bool installed, string tags)
        {
            return new Instance
            {
                Guid = Guid.NewGuid(), Name = name, DisplayName = title, McVersion = minecraft, Type = tags,
                Version = "1.0", Loader = new LoaderConfiguration { Type = loader }, Is_Installed = installed,
                WorkingDirDesc = new Dictionary<string, List<string>> { { "Summary", new List<string> { tags } } }
            };
        }
    }
}
