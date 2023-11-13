using System.Windows;
using System.Windows.Input;

namespace T_Craft_Game_Launcher.MVVM.Windows
{
    public partial class CustomInputDialog
    {
        public CustomInputDialog(string title = "")
        {
            InitializeComponent();
            TitleLabel.Content = title;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        public string ResponseText
        {
            get => ResponseTextBox.Text;
            set => ResponseTextBox.Text = value;
        }

        public bool Result { get; set; }

        private void CustomInputDialog_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
