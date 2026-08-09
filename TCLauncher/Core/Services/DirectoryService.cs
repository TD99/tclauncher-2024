using System;
using System.Collections.Generic;
using System.IO;

namespace TCLauncher.Core.Services
{
    internal static class DirectoryService
    {
        public static void Copy(string source, string destination, Func<string, bool> include = null)
        {
            var sourceRoot = EnsureSeparator(Path.GetFullPath(source));
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetFullPath(directory).Substring(sourceRoot.Length);
                if (include == null || include(relative)) Directory.CreateDirectory(Path.Combine(destination, relative));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetFullPath(file).Substring(sourceRoot.Length);
                if (include != null && !include(relative)) continue;
                var target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }

        public static IEnumerable<string> EnumerateRelativeFiles(string root)
        {
            var rootPath = EnsureSeparator(Path.GetFullPath(root));
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                yield return Path.GetFullPath(file).Substring(rootPath.Length);
        }

        private static string EnsureSeparator(string path) =>
            path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
    }
}
