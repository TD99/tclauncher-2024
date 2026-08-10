using System;

namespace TCLauncher.Core.Services
{
    public interface ILogService
    {
        string LogDirectory { get; }
        void Info(string eventName, string message, string operationId = null);
        void Warning(string eventName, string message, string operationId = null);
        void Error(string eventName, Exception exception, string operationId = null);
    }
}