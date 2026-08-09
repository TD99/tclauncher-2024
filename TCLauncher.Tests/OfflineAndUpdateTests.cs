using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.Tests
{
    [TestClass]
    public class OfflineAndUpdateTests
    {
        [TestMethod]
        public void OfflineProfilesValidatePersistAndAvoidDuplicates()
        {
            var root = Path.Combine(Path.GetTempPath(), "tcl-offline-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var path = Path.Combine(root, "offline.json");
                var service = new OfflineProfileService(path, new AtomicFileService());
                Assert.IsFalse(service.Add("x").IsSuccess);
                var added = service.Add("Local_Player");
                Assert.IsTrue(added.IsSuccess);
                Assert.IsFalse(service.Add("local_player").IsSuccess);

                var reloaded = new OfflineProfileService(path, new AtomicFileService());
                Assert.AreEqual("Local_Player", reloaded.GetSelected().Username);
                Assert.IsTrue(reloaded.Remove(added.Value.Id).IsSuccess);
                Assert.IsNull(reloaded.GetSelected());
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void ActiveAccountSelectionMigratesAndDiscriminatesAccountKinds()
        {
            var root = Path.Combine(Path.GetTempPath(), "tcl-account-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var path = Path.Combine(root, "selection.json");
                var service = new AccountSelectionService(path, new AtomicFileService(), "microsoft-uuid", null);
                Assert.AreEqual(AccountSelectionKind.Microsoft, service.Get().Kind);
                Assert.AreEqual("microsoft-uuid", service.Get().StableId);

                var offlineId = Guid.NewGuid();
                service.SetOffline(offlineId, "LocalPlayer");
                Assert.AreEqual(AccountSelectionKind.Offline, service.Get().Kind);
                Assert.AreEqual(offlineId.ToString("D"), service.Get().StableId);

                service.Clear();
                Assert.IsNull(service.Get());
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public async Task InvalidUpdateManifestFailsWithoutBlockingTheLauncher()
        {
            var handler = new StaticHandler("{\"schemaVersion\":1,\"version\":\"2.0.0\",\"installerUrl\":\"http://unsafe.test/app.msi\",\"sha256\":\"bad\",\"publisher\":\"T-Craft\"}");
            var root = Path.Combine(Path.GetTempPath(), "tcl-update-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new UpdateService(new HttpClient(handler), new Uri("https://example.test/manifest"), new RollingLogService(root));
                var result = await service.CheckAsync(new Version(1, 0), CancellationToken.None);
                Assert.IsFalse(result.IsSuccess);
                StringAssert.Contains(result.Message, "continue using TCLauncher");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private sealed class StaticHandler : HttpMessageHandler
        {
            private readonly string _content;
            public StaticHandler(string content) { _content = content; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_content) });
            }
        }
    }
}
