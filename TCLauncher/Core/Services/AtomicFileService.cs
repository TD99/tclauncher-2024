using System;
using System.IO;
using System.Text;

namespace TCLauncher.Core.Services
{
    public interface IAtomicFileService
    {
        void WriteAllText(string path, string contents);
        void ReplaceDirectory(string stagingDirectory, string destinationDirectory, string rollbackDirectory);
    }

    public sealed class AtomicFileService : IAtomicFileService
    {
        public void WriteAllText(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("A parent directory is required.", nameof(path));
            Directory.CreateDirectory(directory);

            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temporary, contents, new UTF8Encoding(false));
            try
            {
                if (File.Exists(path))
                {
                    var backup = path + ".bak";
                    File.Replace(temporary, path, backup, true);
                    if (File.Exists(backup)) File.Delete(backup);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public void ReplaceDirectory(string stagingDirectory, string destinationDirectory, string rollbackDirectory)
        {
            if (!Directory.Exists(stagingDirectory)) throw new DirectoryNotFoundException(stagingDirectory);
            if (Directory.Exists(rollbackDirectory)) Directory.Delete(rollbackDirectory, true);

            var hadDestination = Directory.Exists(destinationDirectory);
            if (hadDestination) Directory.Move(destinationDirectory, rollbackDirectory);
            try
            {
                Directory.Move(stagingDirectory, destinationDirectory);
            }
            catch
            {
                if (hadDestination && !Directory.Exists(destinationDirectory) && Directory.Exists(rollbackDirectory))
                    Directory.Move(rollbackDirectory, destinationDirectory);
                throw;
            }
        }
    }
}
