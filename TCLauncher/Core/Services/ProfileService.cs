using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public sealed class ProfileDraft
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string IconPath { get; set; }
        public string MinecraftVersion { get; set; }
        public LoaderType LoaderType { get; set; }
        public string LoaderVersion { get; set; }
        public int MinimumRamMb { get; set; } = 1024;
        public int MaximumRamMb { get; set; } = 4096;
        public string JvmArguments { get; set; }
        public bool Isolated { get; set; } = true;
        public string ServerAddress { get; set; }
    }

    public interface IProfileService
    {
        OperationResult<InstalledInstance> Create(ProfileDraft draft);
        ProfileDraft CloneDraft(InstalledInstance source);
    }

    public sealed class ProfileService : IProfileService
    {
        private readonly string _instancesRoot;
        private readonly IAtomicFileService _atomic;
        private readonly IInstanceConfigService _configs;
        private readonly ILogService _log;

        public ProfileService(string instancesRoot, IAtomicFileService atomic, IInstanceConfigService configs,
            ILogService log)
        {
            _instancesRoot = instancesRoot;
            _atomic = atomic;
            _configs = configs;
            _log = log;
        }

        public OperationResult<InstalledInstance> Create(ProfileDraft draft)
        {
            var operationId = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid();
            var destination = Path.Combine(_instancesRoot, id.ToString());
            var staging = destination + ".staging-" + operationId;
            try
            {
                var instance = new InstalledInstance
                {
                    Guid = id,
                    Name = draft.Name?.Trim(),
                    DisplayName = draft.DisplayName?.Trim(),
                    Version = "1.0.0",
                    Type = "Local profile",
                    McVersion = draft.MinecraftVersion?.Trim(),
                    Loader = new LoaderConfiguration
                        { Type = draft.LoaderType, Version = NullIfWhiteSpace(draft.LoaderVersion) },
                    UseIsolation = draft.Isolated,
                    MinimumRamMb = draft.MinimumRamMb,
                    MaximumRamMb = draft.MaximumRamMb,
                    JVMArguments = SplitArguments(draft.JvmArguments),
                    Servers = string.IsNullOrWhiteSpace(draft.ServerAddress)
                        ? new List<Server>()
                        : new List<Server> { new Server("Default", draft.ServerAddress.Trim()) },
                    Is_LocalSource = true,
                    InstallationDir = destination,
                    DataDir = Path.Combine(destination, "data"),
                    ConfigFile = Path.Combine(destination, "config.json")
                };

                var validation = _configs.Validate(instance);
                if (validation.Count > 0)
                    return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.InvalidConfiguration,
                        string.Join(Environment.NewLine, validation), operationId: operationId);

                Directory.CreateDirectory(Path.Combine(staging, "data"));
                if (!string.IsNullOrWhiteSpace(draft.IconPath) && File.Exists(draft.IconPath))
                {
                    var iconName = "thumb" + Path.GetExtension(draft.IconPath);
                    File.Copy(draft.IconPath, Path.Combine(staging, iconName));
                    instance.ThumbnailURL = Path.Combine(destination, iconName);
                }

                var save = _configs.Save(instance, Path.Combine(staging, "config.json"));
                if (!save.IsSuccess)
                    return OperationResult<InstalledInstance>.Failure(save.ErrorCode, save.Message, save.Exception,
                        operationId);
                _atomic.ReplaceDirectory(staging, destination, destination + ".rollback");
                _log.Info("profile.created", destination, operationId);
                return OperationResult<InstalledInstance>.Success(instance, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("profile.create_failed", exception, operationId);
                return OperationResult<InstalledInstance>.Failure(LauncherErrorCode.Unexpected,
                    "The profile could not be created.", exception, operationId);
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        public ProfileDraft CloneDraft(InstalledInstance source)
        {
            var loader = source.GetEffectiveLoader();
            return new ProfileDraft
            {
                DisplayName = source.DisplayName + " (Copy)",
                Name = source.Name + "-copy",
                IconPath = File.Exists(source.ThumbnailURL) ? source.ThumbnailURL : null,
                MinecraftVersion = source.McVersion,
                LoaderType = loader.Type,
                LoaderVersion = loader.Version,
                MinimumRamMb = source.MinimumRamMb ?? 1024,
                MaximumRamMb = source.MaximumRamMb ?? 4096,
                JvmArguments = source.JVMArguments == null ? null : string.Join(" ", source.JVMArguments),
                Isolated = source.UseIsolation != false,
                ServerAddress = source.Servers?.FirstOrDefault()?.Address
            };
        }

        private static string[] SplitArguments(string arguments) =>
            string.IsNullOrWhiteSpace(arguments)
                ? new string[0]
                : arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private static string NullIfWhiteSpace(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}