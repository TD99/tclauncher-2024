using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CmlLib.Core.Downloader;
using TCLauncher.Models;

namespace TCLauncher.Core
{
    public class InstanceAssetsUtils
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static bool GetAssets(string id, string installationPath, string url = "https://tcraft.link/tclauncher/api/mcassets/")
        {
            // Get file without downloading from url + id + ".json" and parse as json
            var assetsJson = _httpClient.GetStringAsync(url + "index.php").Result;

            var downloadFiles = overrideFragments.Select(overrideFragment => new DownloadFile(installationPath, url + overrideFragment + ".json")).ToList();
            App.Launcher.DownloadGameFiles(downloadFiles.ToArray());

        }
    }
}
