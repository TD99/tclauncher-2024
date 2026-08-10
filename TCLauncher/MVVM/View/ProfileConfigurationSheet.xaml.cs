using System;
using System.Linq;
using System.Windows;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.View
{
    public partial class ProfileConfigurationSheet
    {
        private readonly InstalledInstance _instance;
        private readonly Action<Instance> _changed;

        public ProfileConfigurationSheet(InstalledInstance instance, Action<Instance> changed)
        {
            _instance = instance;
            _changed = changed;
            InitializeComponent();
            DisplayName.Text = instance.DisplayName;
            MinimumRam.Text = (instance.MinimumRamMb ?? 0).ToString();
            MaximumRam.Text = (instance.MaximumRamMb ?? 4096).ToString();
            JvmArguments.Text = string.Join(Environment.NewLine, instance.JVMArguments ?? Array.Empty<string>());
            Isolated.IsChecked = instance.UseIsolation == true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DisplayName.Text) || !int.TryParse(MinimumRam.Text, out var minimum) ||
                !int.TryParse(MaximumRam.Text, out var maximum) || minimum < 0 || maximum <= 0 || minimum > maximum)
            {
                Error.Text = "Enter a name and a valid memory range.";
                return;
            }

            _instance.DisplayName = DisplayName.Text.Trim();
            _instance.MinimumRamMb = minimum;
            _instance.MaximumRamMb = maximum;
            _instance.JVMArguments = JvmArguments.Text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim())
                .Where(value => value.Length > 0).ToArray();
            _instance.UseIsolation = Isolated.IsChecked == true;
            var result = AppServices.InstanceConfigs.Save(_instance, _instance.ConfigFile);
            if (!result.IsSuccess)
            {
                Error.Text = result.Message;
                return;
            }

            _changed?.Invoke(_instance);
            AppServices.Overlays.Close(true);
            AppServices.Overlays.ShowToast("Profile saved", _instance.DisplayName);
        }
    }
}