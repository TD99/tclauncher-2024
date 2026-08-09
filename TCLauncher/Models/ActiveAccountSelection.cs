namespace TCLauncher.Models
{
    public enum AccountSelectionKind
    {
        Microsoft,
        Offline
    }

    public sealed class ActiveAccountSelection
    {
        public int SchemaVersion { get; set; } = 1;
        public AccountSelectionKind Kind { get; set; }
        public string StableId { get; set; }
        public string DisplayName { get; set; }
    }
}
