using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public sealed class GameLaunchHandle
    {
        public Process Process { get; set; }
        public string Version { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public Guid ProfileId { get; set; }
        public string AccountName { get; set; }
        public string ServerAddress { get; set; }
    }

    public interface ILaunchService
    {
        Task<OperationResult<GameLaunchHandle>> StartAsync(Instance instance, MSession session, Server server,
            MinecraftLauncher launcher, MinecraftPath path, IProgress<OperationProgress> progress,
            CancellationToken cancellationToken);
    }

    public sealed class LaunchService : ILaunchService
    {
        private readonly IModLoaderService _modLoaders;
        private readonly ILogService _log;
        private readonly IActivityStore _activity;

        public LaunchService(IModLoaderService modLoaders, ILogService log, IActivityStore activity)
        {
            _modLoaders = modLoaders ?? throw new ArgumentNullException(nameof(modLoaders));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        }

        public async Task<OperationResult<GameLaunchHandle>> StartAsync(Instance instance, MSession session,
            Server server,
            MinecraftLauncher launcher, MinecraftPath path, IProgress<OperationProgress> progress,
            CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid().ToString("N");
            try
            {
                if (instance == null) throw new ArgumentNullException(nameof(instance));
                if (session == null) throw new InvalidOperationException("Select an account before launching.");
                if (launcher == null) throw new ArgumentNullException(nameof(launcher));
                instance.NormalizeLegacyConfiguration();

                var version = await _modLoaders.EnsureInstalledAsync(instance, launcher, progress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new OperationProgress
                    { Stage = OperationStage.Validating, Message = "Preparing Minecraft" });

                var options = BuildOptions(instance, session, server, path);
                var process = await launcher.CreateProcessAsync(version, options);
                cancellationToken.ThrowIfCancellationRequested();
                var startedAt = DateTime.UtcNow;
                var activity = _activity.RecordStarted(instance.Guid, server?.Address);
                process.EnableRaisingEvents = true;
                process.Exited += (sender, args) =>
                {
                    var duration = DateTime.UtcNow - startedAt;
                    _activity.RecordCompleted(activity.Id, duration, process.ExitCode);
                    _log.Info("game.exited",
                        $"profile={instance.Guid}; exitCode={process.ExitCode}; durationSeconds={(long)duration.TotalSeconds}",
                        operationId);
                };
                process.Start();
                _log.Info("game.started",
                    $"profile={instance.Guid}; processId={process.Id}; version={version}; account={session.Username}; server={server?.Address ?? "local"}",
                    operationId);

                return OperationResult<GameLaunchHandle>.Success(new GameLaunchHandle
                {
                    Process = process,
                    Version = version,
                    StartedAtUtc = startedAt,
                    ProfileId = instance.Guid,
                    AccountName = session.Username,
                    ServerAddress = server?.Address
                }, operationId);
            }
            catch (OperationCanceledException exception)
            {
                return OperationResult<GameLaunchHandle>.Failure(LauncherErrorCode.Cancelled, "Launch cancelled.",
                    exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("game.launch_failed", exception, operationId);
                return OperationResult<GameLaunchHandle>.Failure(LauncherErrorCode.LaunchFailed, exception.Message,
                    exception, operationId);
            }
        }

        internal static MLaunchOption BuildOptions(Instance instance, MSession session, Server server,
            MinecraftPath path)
        {
            var options = new MLaunchOption
            {
                Session = session,
                Path = path,
                MinimumRamMb = instance.MinimumRamMb ?? 0,
                MaximumRamMb = instance.MaximumRamMb ?? 1024,
                ExtraJvmArguments = (instance.JVMArguments ?? Array.Empty<string>())
                    .Where(argument => !string.IsNullOrWhiteSpace(argument))
                    .Select(argument => new MArgument(argument))
                    .ToArray(),
                VersionType = "\u00a7b@TCLauncher\u00a7r"
            };

            if (!string.IsNullOrWhiteSpace(server?.Address))
            {
                var address = InternetUtils.GetMcServerAddress(server.Address.Trim());
                if (!string.IsNullOrWhiteSpace(address.IP))
                {
                    options.ServerIp = address.IP;
                    options.ServerPort = address.Port ?? 25565;
                }
            }

            return options;
        }
    }
}