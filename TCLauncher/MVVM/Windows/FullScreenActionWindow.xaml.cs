using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using TCLauncher.Core;

namespace TCLauncher.MVVM.Windows
{
    /// <summary>
    /// Interaction logic for FullScreenActionWindow.xaml
    /// </summary>
    public partial class FullScreenActionWindow : Window
    {
        private string _instanceName;
        public string InstanceName
        {
            get => _instanceName;
            set
            {
                _instanceName = value;
                LblName.Content = _instanceName;
            }
        }

        private string _instanceVersion;
        public string InstanceVersion
        {
            get => _instanceVersion;
            set
            {
                _instanceVersion = value;
                LblVersion.Content = _instanceVersion;
            }
        }

        private string _instanceType;
        public string InstanceType
        {
            get => _instanceType;
            set
            {
                _instanceType = value;
                LblType.Content = _instanceType;
            }
        }

        private string _instanceStatus;
        public string InstanceStatus
        {
            get => _instanceStatus;
            set
            {
                _instanceStatus = value;
                LblStatus.Content = _instanceStatus;
            }
        }

        private int _instanceProgress;
        public int InstanceProgress
        {
            get => _instanceProgress;
            set
            {
                _instanceProgress = value;
            }
        }

        private MusicPlayer _player;
        private string _tempFile;

        public FullScreenActionWindow()
        {
            InitializeComponent();

            Stream load0Sound = Properties.Resources.load0s;
            _player = new MusicPlayer(load0Sound);

            byte[] load0Video = Properties.Resources.load0v;
            _tempFile = Path.Combine(IoUtils.Tcl.CachePath, "anyo0.wmv");
            try
            {
                File.WriteAllBytes(_tempFile, load0Video);
            }
            catch {}
            MediaElement.Source = new Uri(_tempFile);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _player.Stop();

            try
            {
                File.Delete(_tempFile);
            }
            catch { }
        }

        private async void MediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,  // Start opacity
                To = 1,    // End opacity
                Duration = TimeSpan.FromSeconds(1.5)  // Duration of 1 second
            };

            MediaElement.BeginAnimation(MediaElement.OpacityProperty, animation);

            await Task.Delay(1500);
            _player.Play();
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaElement.Position = TimeSpan.Zero;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double newWindowHeight = e.NewSize.Height;
            double newWindowWidth = e.NewSize.Width;
            double prevWindowHeight = e.PreviousSize.Height;
            double prevWindowWidth = e.PreviousSize.Width;

            LoadingProgressRect.Width = (_instanceProgress / 100) * newWindowWidth; // TODO FIX
        }
    }
}
