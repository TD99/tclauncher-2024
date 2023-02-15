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

            itemFocusBanner.Source = new BitmapImage(new Uri(instance.ThumbnailURL, UriKind.RelativeOrAbsolute));
            itemFocusVersion.Text = instance.Version;
            itemFocusType.Text = instance.Type;
            itemFocusMCVersion.Text = instance.McVersion;

            if (instance.WorkingDirDesc != null)
            {
                foreach (string key in instance.WorkingDirDesc.Keys)
                {
                    TextBlock keyTextBlock = new TextBlock
                    {
                        Text = key,
                        Foreground = Brushes.White,
                        FontSize = 25
                    };
                    itemFocusMCWorkingDirDesc.Children.Add(keyTextBlock);

                    foreach (string description in instance.WorkingDirDesc[key])
                    {
                        TextBlock descTextBlock = new TextBlock
                        {
                            Text = description,
                            Foreground = Brushes.White,
                            FontSize = 16
                        };
                        itemFocusMCWorkingDirDesc.Children.Add(descTextBlock);
                    }
                }
            }

            itemFocus.Visibility = Visibility.Visible;
        }
    }
}
