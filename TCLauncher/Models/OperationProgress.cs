namespace TCLauncher.Models
{
    public enum OperationStage
    {
        Preparing,
        Downloading,
        Verifying,
        Snapshotting,
        Extracting,
        InstallingLoader,
        Validating,
        Activating,
        CleaningUp,
        Complete
    }

    public sealed class OperationProgress
    {
        public OperationStage Stage { get; set; }
        public string Message { get; set; }
        public double? Percent { get; set; }
        public long? ProgressedBytes { get; set; }
        public long? TotalBytes { get; set; }
    }
}
