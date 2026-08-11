using System.IO;
using System.Windows;
using TCLauncher.Core.Services;

namespace TCLauncher.Controls.Gallery
{
    public partial class App : Application
    {
        public App()
        {
            AppServices.Initialize(Path.Combine(Path.GetTempPath(), "TCLauncher.ComponentGallery"));
        }
    }
}
