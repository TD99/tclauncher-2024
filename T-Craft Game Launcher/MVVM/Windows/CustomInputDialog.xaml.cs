using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace T_Craft_Game_Launcher.MVVM.Windows
{
    public partial class CustomInputDialog
    {
        private TaskCompletionSource<bool> tcs;

        public CustomInputDialog(string title = "")
        {
            InitializeComponent();
            TitleLabel.Content = title;
            tcs = new TaskCompletionSource<bool>();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult(true);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult(false);
            Close();
        }

        public string ResponseText
        {
            get => ResponseTextBox.Text;
            set => ResponseTextBox.Text = value;
        }

        public Task<bool> Result => tcs.Task;

        private void CustomInputDialog_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

         private void CustomInputDialog_OnClosing(object sender, CancelEventArgs e)
         {
             tcs.TrySetResult(false);
         }
    }
}