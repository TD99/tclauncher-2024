using System.Windows;
using System.Windows.Controls;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for InstanceView.xaml
    /// </summary>
    public partial class InstanceView : UserControl
    {
        public InstanceView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).navigateToConfigEditor();
                }
            }
        }
    }
}
