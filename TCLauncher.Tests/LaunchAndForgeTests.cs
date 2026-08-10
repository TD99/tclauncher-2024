using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.Tests
{
    [TestClass]
    public class LaunchAndForgeTests
    {
        [TestMethod]
        public void LaunchOptionsNormalizeNullArgumentsAndAbsentServer()
        {
            var instance = new Instance { JVMArguments = null, MaximumRamMb = 2048 };

            var options = LaunchService.BuildOptions(instance, MSession.CreateOfflineSession("LocalPlayer"), null, new MinecraftPath("C:\\game"));

            Assert.IsNotNull(options.ExtraJvmArguments);
            Assert.AreEqual(0, options.ExtraJvmArguments.Count());
            Assert.IsTrue(string.IsNullOrEmpty(options.ServerIp));
        }

        [TestMethod]
        public void LaunchOptionsRemoveWhitespaceArguments()
        {
            var instance = new Instance { JVMArguments = new[] { "", "  ", "-XX:+UseG1GC" } };

            var options = LaunchService.BuildOptions(instance, MSession.CreateOfflineSession("LocalPlayer"), new Server("Local", "play.example.test"), new MinecraftPath("C:\\game"));

            Assert.AreEqual(1, options.ExtraJvmArguments.Count());
            Assert.AreEqual("play.example.test", options.ServerIp);
            Assert.AreEqual(25565, options.ServerPort);
        }

        [TestMethod]
        public async Task ForgeResolutionUsesRecommendedPromotionAndDirectMavenArtifact()
        {
            var root = NewTemporaryDirectory();
            try
            {
                var resolver = CreateResolver(root, new ForgeMetadataHandler());
                var resolved = await resolver.ResolveAsync("1.20.1", null, CancellationToken.None);

                Assert.AreEqual("47.2.0", resolved.ForgeVersion);
                Assert.IsTrue(resolved.IsRecommended);
                Assert.AreEqual("1.20.1-47.2.0", resolved.ArtifactVersion);
                Assert.AreEqual("https://maven.minecraftforge.net/net/minecraftforge/forge/1.20.1-47.2.0/forge-1.20.1-47.2.0-installer.jar", resolved.InstallerUrl);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public async Task ForgeResolutionAcceptsQualifiedVersionAndUsesCachedMetadataOffline()
        {
            var root = NewTemporaryDirectory();
            try
            {
                var online = CreateResolver(root, new ForgeMetadataHandler());
                await online.ResolveAsync("1.20.1", null, CancellationToken.None);

                var offline = CreateResolver(root, new ForgeMetadataHandler(HttpStatusCode.ServiceUnavailable));
                var resolved = await offline.ResolveAsync("1.20.1", "1.20.1-47.3.0", CancellationToken.None);

                Assert.AreEqual("47.3.0", resolved.ForgeVersion);
                Assert.AreEqual("1.20.1-47.3.0", resolved.ArtifactVersion);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public async Task FabricResolutionReplacesInvalidRequestedVersionWithStableLoader()
        {
            var json = "[" +
                       "{\"loader\":{\"version\":\"0.19.3\",\"stable\":true}}," +
                       "{\"loader\":{\"version\":\"0.18.4\",\"stable\":false}}]";
            var http = new HttpClient(new StaticJsonHandler(json));

            var resolved = await ModLoaderService.ResolveFabricLoaderVersionAsync(
                http, "1.20.1", "0.91.1", CancellationToken.None);

            Assert.AreEqual("0.19.3", resolved);
        }

        [TestMethod]
        public async Task FabricResolutionPreservesValidRequestedVersion()
        {
            var json = "[" +
                       "{\"loader\":{\"version\":\"0.19.3\",\"stable\":true}}," +
                       "{\"loader\":{\"version\":\"0.18.4\",\"stable\":false}}]";
            var http = new HttpClient(new StaticJsonHandler(json));

            var resolved = await ModLoaderService.ResolveFabricLoaderVersionAsync(
                http, "1.21.1", "0.18.4", CancellationToken.None);

            Assert.AreEqual("0.18.4", resolved);
        }

        private static ForgeVersionResolver CreateResolver(string root, HttpMessageHandler handler)
        {
            return new ForgeVersionResolver(new HttpClient(handler), Path.Combine(root, "cache"), new AtomicFileService(), new RollingLogService(Path.Combine(root, "logs")));
        }

        private static string NewTemporaryDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "tcl-forge-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private sealed class ForgeMetadataHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            public ForgeMetadataHandler(HttpStatusCode status = HttpStatusCode.OK) { _status = status; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_status != HttpStatusCode.OK)
                    return Task.FromResult(new HttpResponseMessage(_status));

                var content = request.RequestUri.AbsolutePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                    ? "<metadata><versioning><versions><version>1.20.1-47.2.0</version><version>1.20.1-47.3.0</version></versions></versioning></metadata>"
                    : "{\"promos\":{\"1.20.1-recommended\":\"47.2.0\",\"1.20.1-latest\":\"47.3.0\"}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
            }
        }

        private sealed class StaticJsonHandler : HttpMessageHandler
        {
            private readonly string _json;
            public StaticJsonHandler(string json) { _json = json; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_json) });
        }
    }
}
