using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;

namespace TCLauncher.Core.Services
{
    public interface ISafeArchiveService
    {
        long Validate(string archivePath, long maximumUncompressedBytes);
        void Extract(string archivePath, string destinationDirectory, long maximumUncompressedBytes);
    }

    public sealed class SafeArchiveService : ISafeArchiveService
    {
        public long Validate(string archivePath, long maximumUncompressedBytes)
        {
            if (maximumUncompressedBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maximumUncompressedBytes));
            long total = 0;
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    ValidateEntryName(entry.FullName);
                    var normalized = entry.FullName.Replace('\\', '/').TrimEnd('/');
                    if (!destinations.Add(normalized)) throw new InvalidDataException("The archive contains duplicate destinations.");
                    var unixType = (entry.ExternalAttributes >> 16) & 0xF000;
                    if (unixType == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Archive links are not supported.");
                    checked { total += entry.Length; }
                    if (total > maximumUncompressedBytes)
                        throw new InvalidDataException("The archive exceeds the allowed extracted size.");
                }
            }
            return total;
        }

        public void Extract(string archivePath, string destinationDirectory, long maximumUncompressedBytes)
        {
            Validate(archivePath, maximumUncompressedBytes);
            Directory.CreateDirectory(destinationDirectory);
            var root = EnsureTrailingSeparator(Path.GetFullPath(destinationDirectory));

            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    var destination = Path.GetFullPath(Path.Combine(root, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Archive entry escapes the destination directory.");

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (var input = entry.Open())
                    using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                }
            }
        }

        private static void ValidateEntryName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidDataException("Archive contains an unnamed entry.");
            var normalized = name.Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.Contains(":") ||
                normalized.Split('/').Any(part => part == ".."))
                throw new InvalidDataException("Archive contains an unsafe path.");
        }

        private static string EnsureTrailingSeparator(string path) =>
            path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
    }
}
