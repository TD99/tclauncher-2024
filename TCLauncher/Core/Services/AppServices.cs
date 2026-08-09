using System.IO;

namespace TCLauncher.Core.Services
{
    public static class AppServices
    {
        public static ILogService Log { get; private set; }
        public static IAtomicFileService AtomicFiles { get; private set; }
        public static ISafeArchiveService Archives { get; private set; }
        public static IInstanceConfigService InstanceConfigs { get; private set; }
        public static IBackupService Backups { get; private set; }
        public static IPackageService Packages { get; private set; }
        public static ICatalogService Catalog { get; private set; }
        public static IInstanceHealthService Health { get; private set; }
        public static ISupportBundleService SupportBundles { get; private set; }
        public static IUpdateService Updates { get; private set; }
        public static IProfileService Profiles { get; private set; }
        public static IOfflineProfileService OfflineProfiles { get; private set; }
        public static IModLoaderService ModLoaders { get; private set; }
        public static IInstanceOperationService InstanceOperations { get; private set; }

        public static void Initialize(string rootPath)
        {
            Log = new RollingLogService(Path.Combine(rootPath, "Logs"));
            AtomicFiles = new AtomicFileService();
            Archives = new SafeArchiveService();
            InstanceConfigs = new InstanceConfigService(AtomicFiles, Log);
            Backups = new BackupService(Path.Combine(rootPath, "Backups"), AtomicFiles, Archives, InstanceConfigs, Log);
            Packages = new PackageService(Path.Combine(rootPath, "Instances"), Archives, AtomicFiles, InstanceConfigs, Log);
            Catalog = new CatalogService(
                LauncherHttpClient.Instance,
                new System.Uri("https://tcraft.link/tclauncher/api/v2/catalog"),
                new System.Uri("https://tcraft.link/tclauncher/api/"),
                Path.Combine(rootPath, "Cache", "catalog-v2.json"),
                AtomicFiles,
                Log);
            Health = new InstanceHealthService(InstanceConfigs, Backups);
            SupportBundles = new SupportBundleService(Log);
            Updates = new UpdateService(LauncherHttpClient.Instance, new System.Uri("https://tcraft.link/tclauncher/api/v2/update-manifest"), Log);
            Profiles = new ProfileService(Path.Combine(rootPath, "Instances"), AtomicFiles, InstanceConfigs, Log);
            OfflineProfiles = new OfflineProfileService(Path.Combine(rootPath, "Udata", "offline_profiles.json"), AtomicFiles);
            ModLoaders = new ModLoaderService(LauncherHttpClient.Instance);
            InstanceOperations = new InstanceOperationService(Path.Combine(rootPath, "Instances"), LauncherHttpClient.Instance, Archives, AtomicFiles, InstanceConfigs, Backups, Log);
        }
    }
}
