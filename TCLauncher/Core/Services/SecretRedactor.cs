using System.Text.RegularExpressions;

namespace TCLauncher.Core.Services
{
    public static class SecretRedactor
    {
        private static readonly Regex[] Patterns =
        {
            new Regex("(?i)bearer\\s+[a-z0-9._~+/-]+=*", RegexOptions.Compiled),
            new Regex(
                "(?i)(access[_ -]?token|refresh[_ -]?token|authorization|client[_ -]?secret|password)(\\s*[:=]\\s*)([^\\s,;]+)",
                RegexOptions.Compiled),
            new Regex("(?i)(code=|token=)[^&\\s]+", RegexOptions.Compiled)
        };

        public static string Redact(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            foreach (var pattern in Patterns)
            {
                value = pattern.Replace(value, match =>
                    match.Groups.Count >= 4
                        ? match.Groups[1].Value + match.Groups[2].Value + "[REDACTED]"
                        : "[REDACTED]");
            }

            return value;
        }
    }
}