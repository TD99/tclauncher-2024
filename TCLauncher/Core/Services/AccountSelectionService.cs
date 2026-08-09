using System;
using System.IO;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IAccountSelectionService
    {
        ActiveAccountSelection Get();
        void SetMicrosoft(string uuid, string username);
        void SetOffline(Guid id, string username);
        void Clear();
    }

    public sealed class AccountSelectionService : IAccountSelectionService
    {
        private readonly string _path;
        private readonly IAtomicFileService _files;
        private readonly object _sync = new object();

        public AccountSelectionService(string path, IAtomicFileService files, string legacyMicrosoftId, OfflineProfile legacyOffline)
        {
            _path = path;
            _files = files;
            if (File.Exists(_path)) return;
            if (!string.IsNullOrWhiteSpace(legacyMicrosoftId)) SetMicrosoft(legacyMicrosoftId, null);
            else if (legacyOffline != null) SetOffline(legacyOffline.Id, legacyOffline.Username);
        }

        public ActiveAccountSelection Get()
        {
            lock (_sync)
            {
                if (!File.Exists(_path)) return null;
                try { return JsonConvert.DeserializeObject<ActiveAccountSelection>(File.ReadAllText(_path)); }
                catch { return null; }
            }
        }

        public void SetMicrosoft(string uuid, string username) => Save(new ActiveAccountSelection
        {
            Kind = AccountSelectionKind.Microsoft,
            StableId = uuid,
            DisplayName = username
        });

        public void SetOffline(Guid id, string username) => Save(new ActiveAccountSelection
        {
            Kind = AccountSelectionKind.Offline,
            StableId = id.ToString("D"),
            DisplayName = username
        });

        public void Clear()
        {
            lock (_sync)
            {
                if (File.Exists(_path)) File.Delete(_path);
            }
        }

        private void Save(ActiveAccountSelection selection)
        {
            lock (_sync) _files.WriteAllText(_path, JsonConvert.SerializeObject(selection, Formatting.Indented));
        }
    }
}
