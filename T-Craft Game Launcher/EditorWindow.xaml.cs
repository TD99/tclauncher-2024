using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher
{
    public partial class EditorWindow : Window
    {
        string RootPath;
        bool IsDirectory;
        TreeViewItem currentItem;
        bool IsRecovery;

        public EditorWindow(string rootPath, bool isDirectory = true)
        {
            InitializeComponent();

            this.RootPath = rootPath;
            this.IsDirectory = isDirectory;
            currentItem = treeRoot;

            if (IsDirectory)
            {
                loadDir(rootPath, treeRoot);
            } else
            {
                fileBrowserView.Visibility = Visibility.Collapsed;
                GridSplitter1.Visibility = Visibility.Collapsed;
                fileBrowserColumn.Width = GridLength.Auto;
            }

            webView2Control.CoreWebView2InitializationCompleted += WebView2Control_CoreWebView2InitializationCompleted;
            webView2Control.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key == Key.F5)
                {
                    e.Handled = true;
                }
            };
        }

        private void webView2Control_Loaded(object sender, RoutedEventArgs e)
        {
            webView2Control.Source = new Uri(System.IO.Path.GetFullPath("Plugins/Monaco/index.html"));
        }

        private void WebView2Control_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                webView2Control.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            }
            else
            {
                // Handle error
            }
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                if (!IsDirectory)
                {
                    OpenFile(RootPath);
                }
            }
            else
            {
                // Handle error
            }
        }

        private void loadDir(string rootPath, TreeViewItem parentItem)
        {
            int inaccessibleItems = 0;
            try
            {
                foreach (string subdirectory in Directory.GetDirectories(rootPath))
                {
                    TreeViewItem subdirectoryItem = new TreeViewItem();
                    subdirectoryItem.Header = Path.GetFileName(subdirectory);
                    try
                    {
                        // Check if the directory is accessible
                        Directory.GetDirectories(subdirectory);
                        parentItem.Items.Add(subdirectoryItem);
                        loadDir(subdirectory, subdirectoryItem); // Recursively call loadDir on the subdirectory
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // If the directory is not accessible due to permission issues, increment the inaccessible items count
                        inaccessibleItems++;
                    }
                }

                foreach (string file in Directory.GetFiles(rootPath))
                {
                    try
                    {
                        TreeViewItem fileItem = new TreeViewItem();
                        fileItem.Header = Path.GetFileName(file);
                        fileItem.Tag = file; // Store the file object in the Tag property

                        // Check if the file is accessible
                        File.GetAttributes(file);
                        if ((File.GetAttributes(file) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            // If the file is read-only, add "(readonly)" to the header
                            fileItem.Header += " (readonly)";
                        }
                        parentItem.Items.Add(fileItem);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // If the file is not accessible due to permission issues, increment the inaccessible items count
                        inaccessibleItems++;
                    }
                }
                if (inaccessibleItems > 0)
                {
                    MessageBox.Show(inaccessibleItems + " items could not be read.");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions thrown by Directory.GetDirectories(RootPath)
                MessageBox.Show("An error occurred while accessing the root directory: " + ex.Message);
            }
        }

        private void CloseWin()
        {
            this.Close();
        }

        private void ShowAbout()
        {
            Window window = new Window
            {
                Title = "TCL Editor",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x1d, 0x26, 0x33)),
                Foreground = Brushes.White,
                Owner = this
            };

            window.SetValue(WindowStyleProperty, WindowStyle.ToolWindow);

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock textBlock1 = new TextBlock
            {
                Text = "Powered by simpleEdit",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 10)
            };

            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            TextBlock textBlock2 = new TextBlock
            {
                Text = $"Version: {version}",
                FontSize = 16
            };

            panel.Children.Add(textBlock1);
            panel.Children.Add(textBlock2);

            window.Content = panel;
            window.Show();
        }
        private void Format()
        {
            webView2Control.CoreWebView2.ExecuteScriptAsync("editor.trigger('event', 'editor.action.formatDocument');");
        }

        private async void OpenFile(string file)
        {
            if (String.IsNullOrEmpty(file)) return;
            if (Directory.Exists(file)) return;
            if (!File.Exists(file)) return;
            if (IOTools.File.IsBinary(file))
            {
                MessageBox.Show("Binary files are not supported.");
                return;
            }
            if (new FileInfo(file).Length > 255 * 1024)
            {
                MessageBox.Show("This file is too large. (> 255 KB)");
                return;
            }

            try
            {
                using (var reader = File.OpenText(file))
                {
                    var text = await reader.ReadToEndAsync();
                    string ext = Path.GetExtension(file).Substring(1);
                    await webView2Control.CoreWebView2.ExecuteScriptAsync($"openText({JsonConvert.SerializeObject(text)}, {JsonConvert.SerializeObject(ext)})");
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task<string> GetText()
        {
            string json = await webView2Control.CoreWebView2.ExecuteScriptAsync("editor.getValue();");
            return JsonConvert.DeserializeObject<string>(json);
        }

        private async void SaveFile()
        {
            string file;
            if (IsDirectory)
            {
                if (fileBrowserView.SelectedItem == null) return;

                TreeViewItem selectedItem = (TreeViewItem)fileBrowserView.SelectedItem;
                if (selectedItem.Tag == null) return;

                file = selectedItem.Tag.ToString();
            }
            else
            {
                file = RootPath;
            }

            if (Directory.Exists(file)) return;
            if (!File.Exists(file)) return;

            string text = await GetText();

            try
            {
                using (StreamWriter writer = new StreamWriter(file, append: false, Encoding.UTF8))
                {
                    await writer.WriteAsync(text);
                    writer.Close();
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void fileBrowserView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedItem = (TreeViewItem)fileBrowserView.SelectedItem;
            try
            {
                if (IsRecovery)
                {
                    IsRecovery = false;
                    return;
                }

                OpenFile(selectedItem.Tag.ToString());
                currentItem = selectedItem;
            } catch
            {
                IsRecovery = true;
                currentItem.IsSelected = true;
                return;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseWin();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            ShowAbout();
        }

        private void Format_Click(object sender, RoutedEventArgs e)
        {
            Format();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Handle "Ctrl+S"
                e.Handled = true;
                SaveFile();
            }
            else if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Handle "Ctrl+W"
                e.Handled = true;
                CloseWin();
            }
        }
    }
}
