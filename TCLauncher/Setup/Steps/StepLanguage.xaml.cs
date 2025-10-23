using System.Windows;
using System.Windows.Controls;
using TCLauncher.Properties;

namespace TCLauncher.Setup.Steps
{
    public partial class StepLanguage
    {
        public StepLanguage()
        {
            InitializeComponent();
        }

        private void StepLanguage_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (ComboBoxItem item in LanguageSelector.Items)
            {
                if ((string)item.Tag != Settings.Default.Language) continue;
                LanguageSelector.SelectedItem = item;
                break;
            }
        }

        private void LanguageSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            var tag = (string)selectedItem.Tag;

            if (tag == Settings.Default.Language) return;
            Settings.Default.Language = tag;
            Settings.Default.Save();
            App.SetLanguage(Settings.Default.Language);
            App.HotReloadInstaller();
        }
    }
}
