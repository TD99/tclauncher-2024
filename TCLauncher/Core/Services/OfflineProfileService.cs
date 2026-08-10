using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IOfflineProfileService
    {
        IReadOnlyList<OfflineProfile> List();
        OperationResult<OfflineProfile> Add(string username);
        OperationResult Remove(Guid id);
        void Select(Guid? id);
        OfflineProfile GetSelected();
    }

    public sealed class OfflineProfileService : IOfflineProfileService
    {
        private static readonly Regex ValidUsername = new Regex("^[A-Za-z0-9_]{3,16}$", RegexOptions.Compiled);
        private readonly string _path;
        private readonly IAtomicFileService _files;
        private readonly object _sync = new object();

        public OfflineProfileService(string path, IAtomicFileService files)
        {
            _path = path;
            _files = files;
        }

        public IReadOnlyList<OfflineProfile> List()
        {
            lock (_sync) return Load().Profiles.OrderBy(profile => profile.Username).ToList();
        }

        public OperationResult<OfflineProfile> Add(string username)
        {
            username = username?.Trim();
            if (!ValidUsername.IsMatch(username ?? string.Empty))
                return OperationResult<OfflineProfile>.Failure(LauncherErrorCode.InvalidConfiguration,
                    "Offline names must contain 3–16 letters, numbers, or underscores.");
            lock (_sync)
            {
                var document = Load();
                if (document.Profiles.Any(profile =>
                        profile.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                    return OperationResult<OfflineProfile>.Failure(LauncherErrorCode.Conflict,
                        "An offline profile with this name already exists.");
                var profile = new OfflineProfile
                    { Id = Guid.NewGuid(), Username = username, CreatedAtUtc = DateTime.UtcNow };
                document.Profiles.Add(profile);
                document.SelectedProfileId = profile.Id;
                Save(document);
                return OperationResult<OfflineProfile>.Success(profile);
            }
        }

        public OperationResult Remove(Guid id)
        {
            lock (_sync)
            {
                var document = Load();
                var profile = document.Profiles.FirstOrDefault(item => item.Id == id);
                if (profile == null)
                    return OperationResult.Failure(LauncherErrorCode.InvalidConfiguration,
                        "The offline profile no longer exists.");
                document.Profiles.Remove(profile);
                if (document.SelectedProfileId == id) document.SelectedProfileId = null;
                Save(document);
                return OperationResult.Success();
            }
        }

        public void Select(Guid? id)
        {
            lock (_sync)
            {
                var document = Load();
                document.SelectedProfileId =
                    id.HasValue && document.Profiles.Any(item => item.Id == id.Value) ? id : null;
                Save(document);
            }
        }

        public OfflineProfile GetSelected()
        {
            lock (_sync)
            {
                var document = Load();
                return document.Profiles.FirstOrDefault(item => item.Id == document.SelectedProfileId);
            }
        }

        private OfflineProfileDocument Load()
        {
            if (!File.Exists(_path)) return new OfflineProfileDocument();
            try
            {
                return JsonConvert.DeserializeObject<OfflineProfileDocument>(File.ReadAllText(_path)) ??
                       new OfflineProfileDocument();
            }
            catch
            {
                return new OfflineProfileDocument();
            }
        }

        private void Save(OfflineProfileDocument document) =>
            _files.WriteAllText(_path, JsonConvert.SerializeObject(document, Formatting.Indented));
    }
}