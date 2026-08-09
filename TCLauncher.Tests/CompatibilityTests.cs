using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.Properties;
using System.Globalization;

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

        [TestMethod]
        public void LegacyCatalogConversionPreservesProfileAppletEndpoint()
        {
            var legacy = new Instance
            {
                Guid = Guid.NewGuid(), Name = "legacy", DisplayName = "Legacy", McVersion = "1.20.1",
                AppletURL = "https://tcraft.link/profile/applets"
            };
            var item = CatalogItem.FromLegacy(legacy);
            var restored = item.ToInstance();
            Assert.AreEqual(legacy.AppletURL, item.LegacyAppletUrl);
            Assert.AreEqual(legacy.AppletURL, restored.AppletURL);
        }

        [TestMethod]
        public void ActivityStorePersistsOnlyLaunchMetadataAndCompletesRecord()
        {
            var root = Path.Combine(Path.GetTempPath(), "tcl-activity-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var path = Path.Combine(root, "activity.json");
                var store = new ActivityStore(path, new AtomicFileService());
                var profileId = Guid.NewGuid();
                var activity = store.RecordStarted(profileId, "play.example.test");
                store.RecordCompleted(activity.Id, TimeSpan.FromSeconds(42), 0);
                var saved = store.List()[0];
                Assert.AreEqual(profileId, saved.ProfileId);
                Assert.AreEqual(42, saved.DurationSeconds);
                Assert.AreEqual(0, saved.ExitCode);
                Assert.IsFalse(File.ReadAllText(path).IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void RenovatedNavigationLabelsExistInEverySupportedLanguage()
        {
            var keys = new[] { "home_continue", "home_recent_profiles", "discovery_empty_title", "settings_downloads", "accounts_subtitle", "manage_backups" };
            foreach (var culture in new[] { "en", "de", "fr" })
            foreach (var key in keys)
                Assert.IsFalse(string.IsNullOrWhiteSpace(Languages.ResourceManager.GetString(key, new CultureInfo(culture))), culture + ":" + key);
        }
    }
}
