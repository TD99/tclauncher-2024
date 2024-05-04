using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using CmlLib.Core.Downloader;
using Newtonsoft.Json;
using TCLauncher.Models;
using TCLauncher.MVVM.View;
using TCLauncher.MVVM.Windows;

namespace TCLauncher.Core
{
    public class InstanceAssetsUtils
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly ActionWindow ActionWindow = new ActionWindow();

        public static async Task GetAssets(List<string> names, bool isSandboxed, string url = "https://tcraft.link/tclauncher/api/mcassets/")
        {
            ActionWindow.text = "Lade Assets herunter...";
            ActionWindow.Show();

            var downloadFiles = new List<DownloadFile>();

            var assetsHistoryPath = Path.Combine(isSandboxed ? App.MinecraftPath.BasePath : IoUtils.Tcl.SharedPath, "DO_NOT_MODIFY_assetsHistory.json");

            List<DownloadFile> assetsHistory;
            if (File.Exists(assetsHistoryPath))
            {
                var assetsHistoryJson = File.ReadAllText(assetsHistoryPath);
                assetsHistory = JsonConvert.DeserializeObject<List<DownloadFile>>(assetsHistoryJson);
            }
            else
            {
                assetsHistory = new List<DownloadFile>();
            }

            for (var index = 0; index < names.Count; index++)
            {
                var name = names[index];

                ActionWindow.percent = (index / names.Count) * 100;

                var assetsJson = await HttpClient.GetStringAsync(url + "?name=" + name);
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

                        var downloadFile = new DownloadFile(fragment.SourcePath, fragment.TargetUrl);

                        // Skip if the file is already in the assetsHistory
                        if (assetsHistory.Any(assetsHistoryFile => assetsHistoryFile.Url == downloadFile.Url))
                        {
                            continue;
                        }

                        downloadFiles.Add(downloadFile);

                        var downloadFileContent = await HttpClient.GetByteArrayAsync(downloadFile.Url);
                        File.WriteAllBytes(path, downloadFileContent);
                    }
                }
            }

            var downloadedAssetsPath = Path.Combine(isSandboxed ? App.MinecraftPath.BasePath : IoUtils.Tcl.SharedPath, "DO_NOT_MODIFY_assetsHistory.json");

            if (!File.Exists(downloadedAssetsPath))
            {
                File.Create(downloadedAssetsPath).Close();
            }

            foreach (var downloadFile in downloadFiles)
            {
                assetsHistory.Add(downloadFile);
            }

            var assetsHistoryJsonNew = JsonConvert.SerializeObject(assetsHistory);
            File.WriteAllText(downloadedAssetsPath, assetsHistoryJsonNew);

            ActionWindow.Hide();
        }

    }
}
