using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using T_Craft_Game_Launcher.MVVM.ViewModel;

namespace T_Craft_Game_Launcher.MVVM.View
{
    public partial class StatusView : UserControl
    {
        private readonly StatusViewModel _vm;

        public StatusView()
        {
            InitializeComponent();
            _vm = (StatusViewModel)DataContext;
        }

        private void RefreshBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _vm.RefreshData();
        }
    }
}
