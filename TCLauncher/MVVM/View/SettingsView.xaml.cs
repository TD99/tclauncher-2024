using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.View
{
    public partial class SettingsView
    {
        public SettingsView()
        {
            InitializeComponent();
            assemblyVersion.Text = string.Format(Languages.version_text, Assembly.GetExecutingAssembly().GetName().Version);
            string behaviourTagToSelect = Settings.Default.StartBehaviour.ToString();
            foreach (ComboBoxItem item in Behaviour.Items)
            {
                if ((string) item.Tag == behaviourTagToSelect)
                {
                    Behaviour.SelectedItem = item;
                    break;
                }
            }

            string multiInstancesTagToSelect = Settings.Default.MultiInstances.ToString();
            foreach (ComboBoxItem item in MultiInstances.Items)
            {
                if ((string)item.Tag == multiInstancesTagToSelect)
                {
                    MultiInstances.SelectedItem = item;
                    break;
                }
            }

            string sandboxLevelTagToSelect = Settings.Default.SandboxLevel.ToString();
            foreach (ComboBoxItem item in SandboxLevel.Items)
            {
                if ((string)item.Tag == sandboxLevelTagToSelect)
                {
                    SandboxLevel.SelectedItem = item;
                    break;
                }
            }

            //hostBtn.Content = App.DbgHttpServer == null ? Languages.debug_server_start_text : Languages.debug_server_stop_text;

            AppDataPath.Text = Settings.Default.VirtualAppDataPath;

            frameworkVersion.Text = string.Format(Languages.framework_version_text, RuntimeInformation.FrameworkDescription);

            string languageTagToSelect = Settings.Default.Language;
            foreach (ComboBoxItem item in LanguageSelector.Items)
            {
                if ((string)item.Tag == languageTagToSelect)
                {
                    LanguageSelector.SelectedItem = item;
                    break;
                }
            }

            CheckBoxUsePixelFontEverywhere.IsChecked = Settings.Default.UsePixelFontEverywhere;

            CopyrightCompilationYear.Text = AppUtils.GetCompilationDate().Year.ToString();
        }

        private void resetSettBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(Languages.reset_settings_message, Languages.reset_settings_caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;
            Settings.Default.Reset();
            Settings.Default.Save();
        }

        private void resetDataBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(Languages.reset_data_message, Languages.reset_data_caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Directory.Delete(IoUtils.Tcl.RootPath, true);
                    Settings.Default.Reset();
                    Settings.Default.Save();

                    MessageBox.Show(Languages.reset_data_success_message);
                    string appPath = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appPath);
                    Application.Current.Shutdown();
                }
                catch
                {
                    MessageBox.Show(Languages.reset_data_error_message);
                }
            }
        }

        private void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.HandleUpdates(true);
        }

        //private void codeBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Cursor = Cursors.Wait;
        //    EditorWindow editorWindow = new EditorWindow(IoUtils.Tcl.InstancesPath, true);
        //    editorWindow.Show();
        //    this.Cursor = null;
        //}

        private void Behaviour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            string tag = (string)selectedItem.Tag;
            byte value;

            try
            {
                value = Byte.Parse(tag);
            } catch
            {
                MessageBox.Show(Languages.startup_behavior_error_message);
                return;
            }

            Settings.Default.StartBehaviour = value;
            Settings.Default.Save();
        }

        private async void HostBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (App.DbgHttpServer == null)
            {
                var dialog = new CustomInputDialog(Languages.debug_server_url_prompt)
                {
                    Owner = App.MainWin,
                    ResponseText = "http://localhost:4535/"
                };

                dialog.Show();

                if (!await dialog.Result) return;
                try
                {
                    App.DbgHttpServer = new SimpleHttpServer(SendResponse, true, dialog.ResponseText);
                    App.DbgHttpServer.Run();
                    //hostBtn.Content = Languages.debug_server_stop_text;
                    Process.Start(dialog.ResponseText);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }

            }
            else
            {
                App.DbgHttpServer.Stop();
                App.DbgHttpServer = null;
                //hostBtn.Content = Languages.debug_server_start_text;
            }
        }

        private string SendResponse(HttpListenerRequest request)
        {
            return JsonConvert.SerializeObject(AppUtils.GetDebugObject().Result);
        }

        private void MultiInstances_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            string tag = (string)selectedItem.Tag;
            byte value;

            try
            {
                value = Byte.Parse(tag);
            }
            catch
            {
                MessageBox.Show(Languages.multi_instance_setting_error_message);
                return;
            }

            Settings.Default.MultiInstances = value;
            Settings.Default.Save();
        }

        private void SandboxLevel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            string tag = (string)selectedItem.Tag;
            byte value;

            try
            {
                value = byte.Parse(tag);
            }
            catch
            {
                MessageBox.Show(Languages.sandbox_level_setting_error_message);
                return;
            }

            Settings.Default.SandboxLevel = value;
            Settings.Default.Save();
        }

        private void AppDataPathBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var oldPath = IoUtils.Tcl.RootPath;
            var newPath = AppDataPath.Text == "" ? IoUtils.FileSystem.RealAppDataPath : AppDataPath.Text;

            if (!IoUtils.FileSystem.HasFullAccess(newPath))
            {
                MessageBox.Show(Languages.invalid_path_error_message);
                return;
            }

            newPath = Path.Combine(newPath, "TCL");

            Settings.Default.VirtualAppDataPath = AppDataPath.Text;
            Settings.Default.Save();

            var result = MessageBox.Show(Languages.path_saved_prompt, Languages.path_saved, MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Directory.Move(oldPath, newPath);
                        MessageBox.Show(Languages.files_migrated_success_message);
                    }
                    catch
                    {
                        MessageBox.Show(Languages.copy_error_message);
                    }
                });
            }

            var appPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(appPath);
            Application.Current.Shutdown();
        }

        private void LanguageSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            string tag = (string)selectedItem.Tag;

            if (tag == Settings.Default.Language) return;
            Settings.Default.Language = tag;
            Settings.Default.Save();
            App.SetLanguage(Settings.Default.Language, true);
        }

        private void ReSetupBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var appPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(appPath, "--installer-part-welcome");
            Application.Current.Shutdown();
        }

        private void CheckBoxUsePixelFontEverywhere_OnCheckStateChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox)) return;

            var isChecked = checkBox.IsChecked ?? false;

            if (Settings.Default.UsePixelFontEverywhere == isChecked) return;

            Settings.Default.UsePixelFontEverywhere = checkBox.IsChecked ?? false;
            Settings.Default.Save();
            App.HotReload();
        }
    }
}
