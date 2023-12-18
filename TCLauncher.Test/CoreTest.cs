using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TCLauncher.Core;

namespace TCLauncher.Test
{
    public class CoreTest
    {
        [TestClass]
        public class InternetUtilsTests
        {
            [TestMethod]
            public void TestRemoveProtocol_DefaultHttps()
            {
                const string urlWithProtocol = "https://www.google.com";
                const string urlWithoutProtocol = "www.google.com";

                var result = InternetUtils.RemoveProtocol(urlWithProtocol);

                Assert.AreEqual(urlWithoutProtocol, result);
            }

            [TestMethod]
            public void TestRemoveProtocol_DefaultHttp()
            {
                const string urlWithProtocol = "http://www.google.com";
                const string urlWithoutProtocol = "www.google.com";

                var result = InternetUtils.RemoveProtocol(urlWithProtocol);

                Assert.AreEqual(urlWithoutProtocol, result);
            }

            [TestMethod]
            public void TestRemoveProtocol_Custom()
            {
                const string urlWithProtocol = "tcl:www.google.com";
                const string urlWithoutProtocol = "www.google.com";

                string[] protocols = { "tcl:", "tel:", "mail:" };

                var result = InternetUtils.RemoveProtocol(urlWithProtocol, protocols);

                Assert.AreEqual(urlWithoutProtocol, result);
            }

            [TestMethod]
            public void TestRemoveProtocol_NoMatch()
            {
                const string urlWithProtocol = "abc:www.google.com";

                var result = InternetUtils.RemoveProtocol(urlWithProtocol);

                Assert.AreEqual(urlWithProtocol, result);
            }
        }

        [TestClass]
        public class IoUtilsTests
        {
            private string _tempFilledDirectory;
            private string _tempEmptyDirectory;
            private string _tempRawFile;
            private string _tempBinFile;

            [TestInitialize]
            public void TestInitialize()
            {
                _tempFilledDirectory = Path.Combine(Path.GetTempPath(), "tempdirectory", Path.GetRandomFileName());
                Directory.CreateDirectory(_tempFilledDirectory);

                _tempEmptyDirectory = Path.Combine(Path.GetTempPath(), "tempdirectory", Path.GetRandomFileName());
                Directory.CreateDirectory(_tempEmptyDirectory);

                _tempRawFile = Path.Combine(_tempFilledDirectory, "testfile.txt");
                File.WriteAllText(_tempRawFile, @"This is a test file.");

                _tempBinFile = Path.Combine(_tempFilledDirectory, "testfile.bin");
                File.WriteAllBytes(_tempBinFile, new byte[] { 0x0F, 0xF0 });
            }

            [TestCleanup]
            public void TestCleanup()
            {
                File.Delete(_tempRawFile);
                File.Delete(_tempBinFile);
                Directory.Delete(_tempFilledDirectory);
                Directory.Delete(_tempEmptyDirectory);
            }

            [TestMethod]
            public void TestIsBinary_RawFile()
            {
                var resultText = IoUtils.TclFile.IsBinary(_tempRawFile);

                Assert.IsFalse(resultText);
            }

            [TestMethod]
            public void TestIsBinary_BinaryFile()
            {
                var resultBin = IoUtils.TclFile.IsBinary(_tempBinFile);

                Assert.IsTrue(resultBin);
            }

            [TestMethod]
            public void TestIsEmpty_FilledDirectory()
            {
                var resultFilled = IoUtils.TclDirectory.IsEmpty(_tempFilledDirectory);

                Assert.IsFalse(resultFilled);
            }

            [TestMethod]
            public void TestIsEmpty_EmptyDirectory()
            {
                var resultEmpty = IoUtils.TclDirectory.IsEmpty(_tempEmptyDirectory);

                Assert.IsTrue(resultEmpty);
            }

            [TestMethod]
            public void TestGetSize_FilledDirectory()
            {
                var size = IoUtils.TclDirectory.GetSize(_tempFilledDirectory);

                Assert.IsTrue(size > 0);
            }

            [TestMethod]
            public void TestGetSize_EmptyDirectory()
            {
                var size = IoUtils.TclDirectory.GetSize(_tempEmptyDirectory);

                Assert.IsTrue(size == 0);
            }
        }
    }
}
