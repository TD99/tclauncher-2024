using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using T_Craft_Game_Launcher.MVVM.Model;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for ServerListView.xaml
    /// </summary>
    public partial class ServerListView : UserControl
    {
        public ServerListView()
        {
            InitializeComponent();
        }

        private void ServerItem_Clicked(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            Instance instance = (Instance)border.DataContext;

            MessageBox.Show(instance.DisplayName);
        }
    }
}
