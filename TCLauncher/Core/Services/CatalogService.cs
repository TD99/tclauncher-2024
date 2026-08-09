using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface ICatalogService
    {
        Task<OperationResult<CatalogLoadResult>> LoadAsync(CancellationToken cancellationToken);
    }

    public sealed class CatalogService : ICatalogService
    {
        private readonly HttpClient _http;
        private readonly Uri _v2Uri;
        private readonly Uri _legacyUri;
        private readonly string _cachePath;
        private readonly string _etagPath;
        private readonly IAtomicFileService _files;
        private readonly ILogService _log;

        public CatalogService(HttpClient http, Uri v2Uri, Uri legacyUri, string cachePath, IAtomicFileService files, ILogService log)
        {
            _http = http;
            _v2Uri = v2Uri;
            _legacyUri = legacyUri;
            _cachePath = cachePath;
            _etagPath = cachePath + ".etag";
            _files = files;
            _log = log;
        }

        public async Task<OperationResult<CatalogLoadResult>> LoadAsync(CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid().ToString("N");
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, _v2Uri))
                {
                    if (File.Exists(_etagPath)) request.Headers.TryAddWithoutValidation("If-None-Match", File.ReadAllText(_etagPath));
                    using (var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken))
                    {
                        if (response.StatusCode == HttpStatusCode.NotModified && File.Exists(_cachePath))
                            return OperationResult<CatalogLoadResult>.Success(ReadCache(false), operationId);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var catalog = JsonConvert.DeserializeObject<CatalogDocument>(json);
                            Validate(catalog);
                            _files.WriteAllText(_cachePath, json);
                            if (response.Headers.ETag != null) _files.WriteAllText(_etagPath, response.Headers.ETag.ToString());
                            return OperationResult<CatalogLoadResult>.Success(new CatalogLoadResult { Catalog = catalog }, operationId);
                        }
                    }
                }

                return await LoadLegacyOrCache(operationId, cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                return OperationResult<CatalogLoadResult>.Failure(LauncherErrorCode.Cancelled, "Catalog loading was cancelled.", exception, operationId);
            }
            catch (Exception exception)
            {
                _log.Warning("catalog.v2_failed", exception.Message);
                return await LoadLegacyOrCache(operationId, cancellationToken);
            }
        }

        private async Task<OperationResult<CatalogLoadResult>> LoadLegacyOrCache(string operationId, CancellationToken cancellationToken)
        {
            try
            {
                var json = await _http.GetStringAsync(_legacyUri);
                cancellationToken.ThrowIfCancellationRequested();
                var legacy = JsonConvert.DeserializeObject<List<Instance>>(json) ?? new List<Instance>();
                var document = new CatalogDocument
                {
                    SchemaVersion = 2,
                    GeneratedAtUtc = DateTime.UtcNow,
                    Items = legacy.Select(CatalogItem.FromLegacy).ToList()
                };
                Validate(document);
                _files.WriteAllText(_cachePath, JsonConvert.SerializeObject(document, Formatting.Indented));
                return OperationResult<CatalogLoadResult>.Success(new CatalogLoadResult { Catalog = document }, operationId);
            }
            catch (Exception exception)
            {
                _log.Warning("catalog.legacy_failed", exception.Message);
                if (File.Exists(_cachePath)) return OperationResult<CatalogLoadResult>.Success(ReadCache(true), operationId);
                return OperationResult<CatalogLoadResult>.Failure(LauncherErrorCode.NetworkUnavailable, "The catalog is unavailable and no cached copy exists.", exception, operationId);
            }
        }

        private CatalogLoadResult ReadCache(bool offline)
        {
            var catalog = JsonConvert.DeserializeObject<CatalogDocument>(File.ReadAllText(_cachePath));
            Validate(catalog);
            var cachedAt = File.GetLastWriteTimeUtc(_cachePath);
            return new CatalogLoadResult { Catalog = catalog, IsOffline = offline, IsStale = DateTime.UtcNow - cachedAt > TimeSpan.FromDays(2), CachedAtUtc = cachedAt };
        }

        private static void Validate(CatalogDocument catalog)
        {
            if (catalog == null || catalog.SchemaVersion != 2) throw new InvalidDataException("Unsupported catalog schema.");
            if (catalog.Items == null) throw new InvalidDataException("Catalog items are missing.");
            if (catalog.Items.Any(item => item.Id == Guid.Empty || string.IsNullOrWhiteSpace(item.Title)))
                throw new InvalidDataException("Catalog contains an invalid item.");
            var duplicates = catalog.Items.GroupBy(item => item.Id).Any(group => group.Count() > 1);
            if (duplicates) throw new InvalidDataException("Catalog contains duplicate IDs.");
        }
    }
}
