using System;
using System.IO;
using System.Security.Cryptography;

namespace TCLauncher.Core.Services
{
    public static class HashService
    {
        public static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return ToHex(algorithm.ComputeHash(stream));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}