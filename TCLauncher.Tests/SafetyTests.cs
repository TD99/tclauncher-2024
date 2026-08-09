using System;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCLauncher.Core.Services;

namespace TCLauncher.Tests
{
    [TestClass]
    public class SafetyTests
    {
        [TestMethod]
        public void RedactorRemovesBearerAndTokens()
        {
            var value = SecretRedactor.Redact("Authorization: Bearer secret.value access_token=abc123");
            Assert.IsFalse(value.Contains("secret.value"));
            Assert.IsFalse(value.Contains("abc123"));
        }

        [TestMethod]
        public void ArchiveTraversalIsRejected()
        {
            var root = Path.Combine(Path.GetTempPath(), "tcl-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var archivePath = Path.Combine(root, "unsafe.zip");
            try
            {
                using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                    archive.CreateEntry("../escape.txt");

                Assert.ThrowsException<InvalidDataException>(() => new SafeArchiveService().Validate(archivePath, 1024));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void DuplicateArchiveDestinationsAreRejected()
        {
            var root = Path.Combine(Path.GetTempPath(), "tcl-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var archivePath = Path.Combine(root, "duplicate.zip");
            try
            {
                using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                {
                    archive.CreateEntry("mods/example.jar");
                    archive.CreateEntry("MODS/example.jar");
                }
                Assert.ThrowsException<InvalidDataException>(() => new SafeArchiveService().Validate(archivePath, 1024));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
