using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.IntegrationTests
{
    [TestClass]
    public class PackageAndBackupTests
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
        public void V2PackageRoundTripPreservesPayload()
        {
            var source = CreateInstance(Path.Combine(_root, "source"));
            File.WriteAllText(Path.Combine(source.DataDir, "options.txt"), "music:0.5");
            var packagePath = Path.Combine(_root, "profile.tcl");
            var importRoot = Path.Combine(_root, "imported");
            Directory.CreateDirectory(importRoot);
            var service = new PackageService(importRoot, _archives, _atomic, _configs, _log);

            Assert.IsTrue(service.Export(source, packagePath, false).IsSuccess);
            var preview = service.PreviewImport(packagePath);
            Assert.IsTrue(preview.IsSuccess);
            Assert.IsFalse(preview.Value.IsLegacy);

            var imported = service.Import(packagePath, ImportConflictResolution.Cancel);
            Assert.IsTrue(imported.IsSuccess, imported.Message);
            Assert.AreEqual("music:0.5", File.ReadAllText(Path.Combine(imported.Value.DataDir, "options.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(imported.Value.InstallationDir, "managed.json")));
        }

        [TestMethod]
        public void BackupRestoreRollsBackChangedOptions()
        {
            var instance = CreateInstance(Path.Combine(_root, "instance"));
            var options = Path.Combine(instance.DataDir, "options.txt");
            File.WriteAllText(options, "before");
            var service = new BackupService(Path.Combine(_root, "backups"), _atomic, _archives, _configs, _log);
            var backup = service.Create(instance, "Known good", false, false);
            Assert.IsTrue(backup.IsSuccess);

            File.WriteAllText(options, "after");
            var restored = service.Restore(instance, backup.Value.Path);

            Assert.IsTrue(restored.IsSuccess, restored.Message);
            Assert.AreEqual("before", File.ReadAllText(options));
            Assert.AreEqual(1, Directory.GetDirectories(Path.GetDirectoryName(instance.InstallationDir), Path.GetFileName(instance.InstallationDir) + ".rollback-*").Length);
        }

        [TestMethod]
        public void BackupCanRestoreAsIndependentCopy()
        {
            var instancesRoot = Path.Combine(_root, "instances");
            var instance = CreateInstance(Path.Combine(instancesRoot, Guid.NewGuid().ToString()));
            File.WriteAllText(Path.Combine(instance.DataDir, "options.txt"), "snapshot");
            var service = new BackupService(Path.Combine(_root, "backups"), _atomic, _archives, _configs, _log);
            var backup = service.Create(instance, "Copy source", false, false);

            var copy = service.RestoreAsCopy(instance, backup.Value.Path);

            Assert.IsTrue(copy.IsSuccess, copy.Message);
            Assert.AreNotEqual(instance.Guid, copy.Value.Guid);
            Assert.AreEqual("snapshot", File.ReadAllText(Path.Combine(copy.Value.DataDir, "options.txt")));
            Assert.IsTrue(File.Exists(copy.Value.ConfigFile));
        }

        private InstalledInstance CreateInstance(string root)
        {
            Directory.CreateDirectory(root);
            var data = Path.Combine(root, "data");
            Directory.CreateDirectory(data);
            var instance = new InstalledInstance
            {
                Guid = Guid.NewGuid(),
                Name = "test-profile",
                DisplayName = "Test Profile",
                McVersion = "1.21.1",
                Version = "1.0.0",
                MaximumRamMb = 4096,
                Loader = LoaderConfiguration.Vanilla(),
                InstallationDir = root,
                DataDir = data,
                ConfigFile = Path.Combine(root, "config.json")
            };
            Assert.IsTrue(_configs.Save(instance, instance.ConfigFile).IsSuccess);
            return instance;
        }
    }
}
