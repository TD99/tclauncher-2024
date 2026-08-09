using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.Tests
{
    [TestClass]
    public class CompatibilityTests
    {
        [TestMethod]
        public void LegacyForgeFlagMigratesToLoaderModel()
        {
            var instance = JsonConvert.DeserializeObject<Instance>("{\"Guid\":\"a66afb78-f1e4-42b6-9b02-06eddf82cce1\",\"Name\":\"forge\",\"DisplayName\":\"Forge\",\"McVersion\":\"1.20.1\",\"UseForge\":true,\"MaximumRamMb\":4096}");
            instance.NormalizeLegacyConfiguration();

            Assert.AreEqual(LoaderType.Forge, instance.Loader.Type);
            Assert.AreEqual(0, instance.SchemaVersion);
        }

        [TestMethod]
        public void V2SaveIsAtomicAndSetsSchemaVersion()
        {
            var root = Path.Combine(Path.GetTempPath(), "tcl-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                var log = new RollingLogService(Path.Combine(root, "logs"));
                var service = new InstanceConfigService(new AtomicFileService(), log);
                var instance = new Instance
                {
                    Guid = Guid.NewGuid(), Name = "test", DisplayName = "Test", McVersion = "1.21.1",
                    MaximumRamMb = 4096, Loader = new LoaderConfiguration { Type = LoaderType.NeoForge, Version = "21.1.1" }
                };
                var path = Path.Combine(root, "config.json");

                var result = service.Save(instance, path);

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(2, instance.SchemaVersion);
                Assert.IsTrue(File.Exists(path));
                Assert.IsFalse(File.Exists(path + ".tmp"));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
