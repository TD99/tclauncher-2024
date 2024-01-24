using System.IO;

namespace TCLauncher.Models
{
    public class AssetFragment
    {
        public string TargetUrl { get; set; }
        public string SourcePath { get; set; }

        public AssetFragment(string targetUrl, string sourcePathRelative)
        {
            TargetUrl = targetUrl;
            SourcePath = Path.Combine(App.MinecraftPath.BasePath + sourcePathRelative);

            if (!SourcePath.StartsWith(App.MinecraftPath.BasePath))
                throw new System.Exception("Escaping is not allowed!");
        }
    }
}
