using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.IntegrationTests
{
    [TestClass]
    public class CatalogAndOperationTests
    {
        private string _root;
        private RollingLogService _log;
        private AtomicFileService _atomic;
        private SafeArchiveService _archives;
        private InstanceConfigService _configs;

        [TestInitialize]
        public void Initialize()
        {
            _root = Path.Combine(Path.GetTempPath(), "tcl-integration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _log = new RollingLogService(Path.Combine(_root, "logs"));
            _atomic = new AtomicFileService();
            _archives = new SafeArchiveService();
            _configs = new InstanceConfigService(_atomic, _log);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [TestMethod]
        public async Task CatalogFallsBackToValidatedCacheWhenOffline()
        {
            var item = new CatalogItem { Id = Guid.NewGuid(), Slug = "pack", Title = "Pack", MinecraftVersion = "1.21.1", Loader = LoaderConfiguration.Vanilla() };
            var json = JsonConvert.SerializeObject(new CatalogDocument { SchemaVersion = 2, GeneratedAtUtc = DateTime.UtcNow, Items = new List<CatalogItem> { item } });
            var cache = Path.Combine(_root, "cache", "catalog.json");
            var online = new CatalogService(new HttpClient(new FakeHandler(_ => Response(HttpStatusCode.OK, json))), new Uri("https://example.test/v2"), new Uri("https://example.test/v1"), cache, _atomic, _log);
            Assert.IsTrue((await online.LoadAsync(CancellationToken.None)).IsSuccess);

            var offline = new CatalogService(new HttpClient(new FakeHandler(_ => throw new HttpRequestException("offline"))), new Uri("https://example.test/v2"), new Uri("https://example.test/v1"), cache, _atomic, _log);
            var result = await offline.LoadAsync(CancellationToken.None);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value.IsOffline);
            Assert.AreEqual(item.Id, result.Value.Catalog.Items[0].Id);
        }

        [TestMethod]
        public async Task InstallPipelineActivatesOnlyVerifiedStagingData()
        {
            var source = Path.Combine(_root, "payload-source");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "options.txt"), "safe");
            var payload = Path.Combine(_root, "payload.zip");
            ZipFile.CreateFromDirectory(source, payload);
            var bytes = File.ReadAllBytes(payload);
            var http = new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
            var instances = Path.Combine(_root, "instances");
            var backups = new BackupService(Path.Combine(_root, "backups"), _atomic, _archives, _configs, _log);
            var service = new InstanceOperationService(instances, http, _archives, _atomic, _configs, backups, _log);
            var instance = new Instance
            {
                Guid = Guid.NewGuid(), Name = "safe", DisplayName = "Safe", McVersion = "1.21.1", Version = "1.0.0",
                MaximumRamMb = 4096, Loader = LoaderConfiguration.Vanilla(), WorkingDirZipURL = "https://example.test/payload.zip",
                PayloadSha256 = HashService.Sha256(payload)
            };

            var result = await service.InstallOrUpdateAsync(instance, null, CancellationToken.None);

            Assert.IsTrue(result.IsSuccess, result.Message);
            Assert.AreEqual("safe", File.ReadAllText(Path.Combine(result.Value.DataDir, "options.txt")));
            Assert.IsTrue(File.Exists(result.Value.ConfigFile));
            Assert.IsTrue(File.Exists(Path.Combine(result.Value.InstallationDir, "managed.json")));
        }

        [TestMethod]
        public async Task BadPayloadChecksumLeavesNoInstalledDirectory()
        {
            var http = new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) }));
            var instances = Path.Combine(_root, "instances");
            var service = new InstanceOperationService(instances, http, _archives, _atomic, _configs,
                new BackupService(Path.Combine(_root, "backups"), _atomic, _archives, _configs, _log), _log);
            var id = Guid.NewGuid();
            var instance = new Instance
            {
                Guid = id, Name = "bad", DisplayName = "Bad", McVersion = "1.21.1", MaximumRamMb = 4096,
                Loader = LoaderConfiguration.Vanilla(), WorkingDirZipURL = "https://example.test/payload.zip", PayloadSha256 = new string('0', 64)
            };

            var result = await service.InstallOrUpdateAsync(instance, null, CancellationToken.None);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(LauncherErrorCode.ChecksumMismatch, result.ErrorCode);
            Assert.IsFalse(Directory.Exists(Path.Combine(instances, id.ToString())));
        }

        private static HttpResponseMessage Response(HttpStatusCode status, string content) =>
            new HttpResponseMessage(status) { Content = new StringContent(content) };

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) { _handler = handler; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_handler(request));
        }
    }
}
