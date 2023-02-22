using System.Windows;

namespace T_Craft_Game_Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += App_Startup;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "--installSuccess")
                {
                    try
                    {
                        MessageBox.Show($"Das Paket '{e.Args[i+1]}' wurde erfolgreich installiert.");
                    }
                    catch {}
                }
                if (e.Args[i] == "--uninstallSuccess")
                {
                    try
                    {
                        MessageBox.Show($"Das Paket '{e.Args[i + 1]}' wurde erfolgreich deinstalliert.");
                    }
                    catch { }
                }
            }

        }
    }
}
