using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using T_Craft_Game_Launcher.MVVM.ViewModel;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void discoverBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).navigateToServer();
                }
            }
        }

        private void playBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).navigateToPlay();
                }
            }
        }
    }
}
