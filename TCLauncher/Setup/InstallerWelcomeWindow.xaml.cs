using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TCLauncher.Properties;
using TCLauncher.Setup.Steps;

namespace TCLauncher.Setup
{   public partial class InstallerWelcomeWindow
    {
        private readonly Dictionary<int, UserControl> _steps = new Dictionary<int, UserControl>
        {
            {0, new StepWelcome()},
            {1, new StepLanguage()},
            {2, new StepPath()},
            {3, new StepDone()}
        };

        private int _currentStep;
        public int CurrentStep
        {
            get => _currentStep;
            private set
            {
                if (!_steps.ContainsKey(value)) return;
                _currentStep = value;
                UpdateControls();
            }
        }

        public InstallerWelcomeWindow(int step = 0)
        {
            InitializeComponent();

            CurrentStep = step;
            UpdateControls();
        }

        private void UpdateControls()
        {
            // Normal buttons
            BackBtn.Visibility = CurrentStep > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextBtn.Visibility = CurrentStep < _steps.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

            // Special buttons
            SkipBtn.Visibility = CurrentStep == 0 && CurrentStep != _steps.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
            FinishBtn.Visibility = CurrentStep == _steps.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

            FrameView.Content = _steps[CurrentStep];

            // Update progress label
            ProgressLabel.Content = string.Format(Languages.step_count_key, CurrentStep + 1, _steps.Count);
        }

        private void BackBtn_OnClick(object sender, RoutedEventArgs e)
        {
            CurrentStep--;
        }

        private void NextBtn_OnClick(object sender, RoutedEventArgs e)
        {
            CurrentStep++;
        }

        private void FinishBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void SkipBtn_OnClick(object sender, RoutedEventArgs e)
        {
            CurrentStep = _steps.Count - 1;
        }

        private void TopDrag_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        { 
            DragMove();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
