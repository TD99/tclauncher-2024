using System;

namespace TCLauncher.Models
{
    public enum LauncherErrorCode
    {
        None,
        InvalidConfiguration,
        NetworkUnavailable,
        DownloadFailed,
        ChecksumMismatch,
        UnsafeArchive,
        DiskFull,
        Conflict,
        AuthenticationFailed,
        LaunchFailed,
        UpdateVerificationFailed,
        Cancelled,
        Unexpected
    }

    public class OperationResult
    {
        public bool IsSuccess { get; }
        public LauncherErrorCode ErrorCode { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public string OperationId { get; }

        protected OperationResult(bool isSuccess, LauncherErrorCode errorCode, string message, Exception exception,
            string operationId)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            Message = message;
            Exception = exception;
            OperationId = operationId ?? Guid.NewGuid().ToString("N");
        }

        public static OperationResult Success(string operationId = null) =>
            new OperationResult(true, LauncherErrorCode.None, null, null, operationId);

        public static OperationResult Failure(LauncherErrorCode errorCode, string message, Exception exception = null,
            string operationId = null) =>
            new OperationResult(false, errorCode, message, exception, operationId);
    }

    public sealed class OperationResult<T> : OperationResult
    {
        public T Value { get; }

        private OperationResult(bool isSuccess, T value, LauncherErrorCode errorCode, string message,
            Exception exception, string operationId)
            : base(isSuccess, errorCode, message, exception, operationId)
        {
            Value = value;
        }

        public static OperationResult<T> Success(T value, string operationId = null) =>
            new OperationResult<T>(true, value, LauncherErrorCode.None, null, null, operationId);

        public new static OperationResult<T> Failure(LauncherErrorCode errorCode, string message,
            Exception exception = null, string operationId = null) =>
            new OperationResult<T>(false, default(T), errorCode, message, exception, operationId);
    }
}