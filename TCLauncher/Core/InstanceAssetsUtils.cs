using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;

namespace TCLauncher.Core
{
    public class InstanceAssetsUtils
    {
        private static readonly ActionWindow ActionWindow = new ActionWindow();

        public static async Task GetAssets(List<string> names, bool isSandboxed,
            string url = "https://tcraft.link/tclauncher/api/mcassets/")
        {
            ActionWindow.text = Languages.loading_assets;
            ActionWindow.Show();

            var downloadFiles = new List<AssetDownloadRecord>();

            var assetsHistoryPath = Path.Combine(isSandboxed ? App.MinecraftPath.BasePath : IoUtils.Tcl.SharedPath,
                "DO_NOT_MODIFY_assetsHistory.json");

            List<AssetDownloadRecord> assetsHistory;
            if (File.Exists(assetsHistoryPath))
            {
                var assetsHistoryJson = File.ReadAllText(assetsHistoryPath);
                assetsHistory = JsonConvert.DeserializeObject<List<AssetDownloadRecord>>(assetsHistoryJson);
            }
            else
            {
                assetsHistory = new List<AssetDownloadRecord>();
            }

            for (var index = 0; index < names.Count; index++)
            {
                var name = names[index];

                ActionWindow.percent = (index / names.Count) * 100;

                var assetsJson = await LauncherHttpClient.Instance.GetStringAsync(url + "?name=" + name);
                var assets = JsonConvert.DeserializeObject<List<Asset>>(assetsJson);

                foreach (var fragments in assets.Select(asset => asset.AssetFragments))
                {
                    foreach (var fragment in fragments)
                    {
                        var path = Path.Combine(isSandboxed ? App.MinecraftPath.BasePath : IoUtils.Tcl.SharedPath,
                            fragment.SourcePath);
                        var directoryPath = Path.GetDirectoryName(path);
                        if (!Directory.Exists(directoryPath) && directoryPath != null)
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        var downloadFile = new AssetDownloadRecord(fragment.SourcePath, fragment.TargetUrl);

                        // Skip if the file is already in the assetsHistory
                        if (assetsHistory.Any(assetsHistoryFile => assetsHistoryFile.Url == downloadFile.Url))
                        {
                            continue;
                        }

                        downloadFiles.Add(downloadFile);

                        var downloadFileContent = await LauncherHttpClient.Instance.GetByteArrayAsync(downloadFile.Url);
                        File.WriteAllBytes(path, downloadFileContent);
                    }
                }
            }

            var downloadedAssetsPath = Path.Combine(isSandboxed ? App.MinecraftPath.BasePath : IoUtils.Tcl.SharedPath,
                "DO_NOT_MODIFY_assetsHistory.json");

            if (!File.Exists(downloadedAssetsPath))
            {
                File.Create(downloadedAssetsPath).Close();
            }

            assetsHistory.AddRange(downloadFiles);

            var assetsHistoryJsonNew = JsonConvert.SerializeObject(assetsHistory);
            File.WriteAllText(downloadedAssetsPath, assetsHistoryJsonNew);

            ActionWindow.Hide();
        }
    }

    internal sealed class AssetDownloadRecord
    {
        public string Path { get; set; }
        public string Url { get; set; }

        public AssetDownloadRecord(string path, string url)
        {
            Path = path;
            Url = url;
        }

        public AssetDownloadRecord()
        {
        }
    }
}