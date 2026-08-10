using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IUpdateService
    {
        Task<OperationResult<UpdateCheckResult>>
            CheckAsync(Version currentVersion, CancellationToken cancellationToken);

        Task<OperationResult<string>> DownloadAndVerifyAsync(UpdateManifest manifest, string stagingDirectory,
            CancellationToken cancellationToken);
    }

    public sealed class UpdateService : IUpdateService
    {
        private readonly HttpClient _http;
        private readonly Uri _manifestUri;
        private readonly ILogService _log;

        public UpdateService(HttpClient http, Uri manifestUri, ILogService log)
        {
            _http = http;
            _manifestUri = manifestUri;
            _log = log;
        }

        public async Task<OperationResult<UpdateCheckResult>> CheckAsync(Version currentVersion,
            CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid().ToString("N");
            try
            {
                using (var response = await _http.GetAsync(_manifestUri, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    var manifest =
                        JsonConvert.DeserializeObject<UpdateManifest>(await response.Content.ReadAsStringAsync());
                    ValidateManifest(manifest);
                    Version available;
                    if (!Version.TryParse(manifest.Version.Split('-')[0], out available))
                        throw new InvalidDataException("The update version is invalid.");
                    var compatibility = CheckCompatibility(manifest);
                    return OperationResult<UpdateCheckResult>.Success(new UpdateCheckResult
                    {
                        Manifest = manifest,
                        IsUpdateAvailable = available > currentVersion,
                        IsCompatible = compatibility == null,
                        CompatibilityMessage = compatibility
                    }, operationId);
                }
            }
            catch (OperationCanceledException exception)
            {
                return OperationResult<UpdateCheckResult>.Failure(LauncherErrorCode.Cancelled,
                    "Update checking was cancelled.", exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Warning("update.check_failed", exception.Message);
                return OperationResult<UpdateCheckResult>.Failure(LauncherErrorCode.NetworkUnavailable,
                    "Updates could not be checked. You can continue using TCLauncher.", exception, operationId);
            }
        }

        public async Task<OperationResult<string>> DownloadAndVerifyAsync(UpdateManifest manifest,
            string stagingDirectory, CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid().ToString("N");
            try
            {
                ValidateManifest(manifest);
                Directory.CreateDirectory(stagingDirectory);
                var destination = Path.Combine(stagingDirectory, "TCLauncher-" + manifest.Version + ".msi");
                using (var response = await _http.GetAsync(new Uri(manifest.InstallerUrl),
                           HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    using (var input = await response.Content.ReadAsStreamAsync())
                    using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                        await input.CopyToAsync(output, 81920, cancellationToken);
                }

                if (!HashService.Sha256(destination).Equals(manifest.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The update checksum does not match.");
                VerifyPublisher(destination, manifest.Publisher);
                _log.Info("update.verified", destination, operationId);
                return OperationResult<string>.Success(destination, operationId);
            }
            catch (OperationCanceledException exception)
            {
                return OperationResult<string>.Failure(LauncherErrorCode.Cancelled, "Update download was cancelled.",
                    exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Error("update.verification_failed", exception, operationId);
                return OperationResult<string>.Failure(LauncherErrorCode.UpdateVerificationFailed,
                    "The update could not be verified and will not be installed.", exception, operationId);
            }
        }

        private static void ValidateManifest(UpdateManifest manifest)
        {
            if (manifest == null || manifest.SchemaVersion != 1)
                throw new InvalidDataException("Unsupported update manifest.");
            Uri uri;
            if (!Uri.TryCreate(manifest.InstallerUrl, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("The installer URL must use HTTPS.");
            if (string.IsNullOrWhiteSpace(manifest.Sha256) || manifest.Sha256.Length != 64)
                throw new InvalidDataException("The update checksum is invalid.");
            if (string.IsNullOrWhiteSpace(manifest.Publisher))
                throw new InvalidDataException("The expected update publisher is missing.");
            Version ignored;
            if (!Version.TryParse(manifest.MinimumWindowsVersion, out ignored))
                throw new InvalidDataException("The minimum Windows version is invalid.");
            if (!Version.TryParse(manifest.MinimumFrameworkVersion, out ignored))
                throw new InvalidDataException("The minimum .NET Framework version is invalid.");
        }

        private static string CheckCompatibility(UpdateManifest manifest)
        {
            Version minimumWindows;
            if (Version.TryParse(manifest.MinimumWindowsVersion, out minimumWindows) &&
                Environment.OSVersion.Version < minimumWindows)
                return "This update requires Windows " + manifest.MinimumWindowsVersion + " or newer.";
            Version minimumFramework;
            if (Version.TryParse(manifest.MinimumFrameworkVersion, out minimumFramework) &&
                new Version(4, 8, 1) < minimumFramework)
                return "This update requires .NET Framework " + manifest.MinimumFrameworkVersion + " or newer.";
            return null;
        }

        private static void VerifyPublisher(string path, string expectedPublisher)
        {
            var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
            if (certificate.Subject.IndexOf(expectedPublisher, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidDataException("The update publisher does not match.");
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                if (!chain.Build(certificate)) throw new InvalidDataException("The update signature is not trusted.");
            }
        }
    }
}