using System;
using System.IO;
using Newtonsoft.Json;

namespace TCLauncher.Core.Services
{
    public sealed class RollingLogService : ILogService
    {
        private readonly object _sync = new object();
        private readonly int _retentionDays;

        public string LogDirectory { get; }

        public RollingLogService(string logDirectory, int retentionDays = 7)
        {
            LogDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            _retentionDays = retentionDays;
            Directory.CreateDirectory(LogDirectory);
            DeleteExpiredLogs();
        }

        public void Info(string eventName, string message, string operationId = null) =>
            Write("information", eventName, message, null, operationId);

        public void Warning(string eventName, string message, string operationId = null) =>
            Write("warning", eventName, message, null, operationId);

        public void Error(string eventName, Exception exception, string operationId = null) =>
            Write("error", eventName, exception?.Message, exception, operationId);

        private void Write(string level, string eventName, string message, Exception exception, string operationId)
        {
            var record = new
            {
                timestampUtc = DateTime.UtcNow,
                level,
                eventName,
                operationId,
                message = SecretRedactor.Redact(message),
                exception = exception == null ? null : SecretRedactor.Redact(exception.ToString())
            };

            var line = JsonConvert.SerializeObject(record) + Environment.NewLine;
            var path = Path.Combine(LogDirectory, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".jsonl");
            lock (_sync)
            {
                File.AppendAllText(path, line);
            }
        }

        private void DeleteExpiredLogs()
        {
            foreach (var file in Directory.GetFiles(LogDirectory, "*.jsonl"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-_retentionDays)) File.Delete(file);
                }
                catch
                {
                    // Logging must never prevent startup.
                }
            }
        }
    }
}