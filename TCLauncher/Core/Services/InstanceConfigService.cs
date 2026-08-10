using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IInstanceConfigService
    {
        OperationResult<Instance> Load(string path);
        OperationResult Save(Instance instance, string path);
        IReadOnlyList<string> Validate(Instance instance);
    }

    public sealed class InstanceConfigService : IInstanceConfigService
    {
        private readonly IAtomicFileService _files;
        private readonly ILogService _log;

        public InstanceConfigService(IAtomicFileService files, ILogService log)
        {
            _files = files;
            _log = log;
        }

        public OperationResult<Instance> Load(string path)
        {
            var operationId = Guid.NewGuid().ToString("N");
            try
            {
                var instance = JsonConvert.DeserializeObject<Instance>(File.ReadAllText(path));
                if (instance == null)
                    return OperationResult<Instance>.Failure(LauncherErrorCode.InvalidConfiguration,
                        "The instance configuration is empty.", operationId: operationId);

                instance.NormalizeLegacyConfiguration();
                var errors = Validate(instance);
                if (errors.Count > 0)
                    return OperationResult<Instance>.Failure(LauncherErrorCode.InvalidConfiguration,
                        string.Join(Environment.NewLine, errors), operationId: operationId);

                return OperationResult<Instance>.Success(instance, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("instance.config.load_failed", exception, operationId);
                return OperationResult<Instance>.Failure(LauncherErrorCode.InvalidConfiguration,
                    "The instance configuration could not be read.", exception, operationId);
            }
        }

        public OperationResult Save(Instance instance, string path)
        {
            var operationId = Guid.NewGuid().ToString("N");
            try
            {
                var errors = Validate(instance);
                if (errors.Count > 0)
                    return OperationResult.Failure(LauncherErrorCode.InvalidConfiguration,
                        string.Join(Environment.NewLine, errors), operationId: operationId);

                instance.PrepareForV2Save();
                _files.WriteAllText(path, JsonConvert.SerializeObject(instance, Formatting.Indented));
                _log.Info("instance.config.saved", path, operationId);
                return OperationResult.Success(operationId);
            }
            catch (Exception exception)
            {
                _log.Error("instance.config.save_failed", exception, operationId);
                return OperationResult.Failure(LauncherErrorCode.Unexpected,
                    "The instance configuration could not be saved.", exception, operationId);
            }
        }

        public IReadOnlyList<string> Validate(Instance instance)
        {
            var errors = new List<string>();
            if (instance == null)
            {
                errors.Add("An instance is required.");
                return errors;
            }

            if (instance.Guid == Guid.Empty) errors.Add("The instance ID is missing.");
            if (string.IsNullOrWhiteSpace(instance.Name)) errors.Add("The internal name is required.");
            if (string.IsNullOrWhiteSpace(instance.DisplayName)) errors.Add("The display name is required.");
            if (string.IsNullOrWhiteSpace(instance.McVersion)) errors.Add("A Minecraft version is required.");
            if (instance.MinimumRamMb < 0) errors.Add("Minimum RAM cannot be negative.");
            if (instance.MaximumRamMb <= 0) errors.Add("Maximum RAM must be greater than zero.");
            if ((instance.MinimumRamMb ?? 0) > (instance.MaximumRamMb ?? int.MaxValue))
                errors.Add("Minimum RAM cannot exceed maximum RAM.");
            return errors;
        }
    }
}