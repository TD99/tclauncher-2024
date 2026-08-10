using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public interface IActivityStore
    {
        IReadOnlyList<LaunchActivity> List();
        LaunchActivity RecordStarted(Guid profileId, string server);
        void RecordCompleted(Guid activityId, TimeSpan duration, int exitCode);
    }

    public sealed class ActivityStore : IActivityStore
    {
        private readonly string _path;
        private readonly IAtomicFileService _files;
        private readonly object _sync = new object();
        public ActivityStore(string path, IAtomicFileService files)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("An activity store path is required.", nameof(path));
            _path = path;
            _files = files ?? throw new ArgumentNullException(nameof(files));
        }
        public IReadOnlyList<LaunchActivity> List() { lock (_sync) return Load().OrderByDescending(item => item.LaunchedAtUtc).ToList(); }
        public LaunchActivity RecordStarted(Guid profileId, string server)
        {
            lock (_sync)
            {
                var items = Load();
                var activity = new LaunchActivity { Id = Guid.NewGuid(), ProfileId = profileId, LaunchedAtUtc = DateTime.UtcNow, LastServer = server };
                items.Insert(0, activity); Save(items.Take(20).ToList()); return activity;
            }
        }
        public void RecordCompleted(Guid activityId, TimeSpan duration, int exitCode)
        {
            lock (_sync)
            {
                var items = Load(); var activity = items.FirstOrDefault(item => item.Id == activityId); if (activity == null) return;
                activity.DurationSeconds = Math.Max(0, (long)duration.TotalSeconds); activity.ExitCode = exitCode; Save(items);
            }
        }
        private List<LaunchActivity> Load()
        {
            if (!File.Exists(_path)) return new List<LaunchActivity>();
            try { return JsonConvert.DeserializeObject<List<LaunchActivity>>(File.ReadAllText(_path)) ?? new List<LaunchActivity>(); }
            catch { return new List<LaunchActivity>(); }
        }
        private void Save(List<LaunchActivity> items) => _files.WriteAllText(_path, JsonConvert.SerializeObject(items, Formatting.Indented));
    }
}
