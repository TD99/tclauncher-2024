using System;
using System.Windows;
using System.Windows.Controls;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft.Sessions;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.Controls;

namespace TCLauncher.MVVM.View
{
    public partial class AccountListView
    {
        public AccountListView()
        {
            InitializeComponent();
        }

        private void AccountView_OnLoaded(object sender, RoutedEventArgs e)
        {
            ListAccounts();
        }

        private void ListAccounts()
        {
            LogoutBtn.Visibility = Visibility.Collapsed;
            Accounts.Items.Clear();
            var accounts = App.LoginHandler.AccountManager.GetAccounts();
            foreach (var account in accounts)
            {
                if (!(account is JEGameAccount jeGameAccount))
                    continue;

                var isPrimary = jeGameAccount?.Profile?.UUID == App.Session?.UUID;

                var control = new AccountControl(jeGameAccount, isPrimary)
                {
                    DataContext = jeGameAccount
                };

                control.LoginBtn.Click += Control_OnLoginClicked;
                control.RemoveBtn.Click += Control_OnRemoveClicked;

                Accounts.Items.Add(control);
            }
            var selectedOffline = AppServices.OfflineProfiles.GetSelected();
            foreach (var profile in AppServices.OfflineProfiles.List())
            {
                var control = new OfflineAccountControl(profile, selectedOffline?.Id == profile.Id);
                control.LoginBtn.Click += (sender, args) => SelectOffline(profile);
                control.RemoveBtn.Click += (sender, args) => RemoveOffline(profile);
                Accounts.Items.Add(control);
            }
            if (App.Session != null)
                LogoutBtn.Visibility = Visibility.Visible;
        }

        private async void NewAccountBtn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.LoginHandler.AuthenticateInteractively();
                ListAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void NewOfflineBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new TCLauncher.MVVM.Windows.CustomInputDialog("Offline Minecraft name (3–16 letters, numbers, or underscores)") { Owner = App.MainWin };
            dialog.Show();
            if (!await dialog.Result) return;
            var result = AppServices.OfflineProfiles.Add(dialog.ResponseText);
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message, "TCLauncher", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectOffline(result.Value);
        }

        private void SelectOffline(OfflineProfile profile)
        {
            AppServices.OfflineProfiles.Select(profile.Id);
            App.Session = MSession.CreateOfflineSession(profile.Username);
            App.MainWin.SetDisplayAccount(profile.Username + " (Offline)");
            ListAccounts();
        }

        private void RemoveOffline(OfflineProfile profile)
        {
            if (MessageBox.Show($"Remove offline profile {profile.Username}?", "TCLauncher", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            var selected = AppServices.OfflineProfiles.GetSelected();
            AppServices.OfflineProfiles.Remove(profile.Id);
            if (selected?.Id == profile.Id) SetSession(null);
            ListAccounts();
        }

        private async void Control_OnLoginClicked(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var control = XamlUtils.FindParent<AccountControl>(btn);
            try
            {
                var selectedAccount = control.Account ?? throw new InvalidOperationException();
                var result = await App.LoginHandler.Authenticate(selectedAccount);
                SetSession(result);
                App.MainWin.navigateToHome();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Control_OnRemoveClicked(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var control = XamlUtils.FindParent<AccountControl>(btn);
            try
            {
                var selectedAccount = control.Account ?? throw new InvalidOperationException();
                if (MessageBox.Show("Remove Microsoft account " + (selectedAccount.Profile?.Username ?? "") + " from this device?",
                        "TCLauncher", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                App.LoginHandler.Signout(selectedAccount);
                SetSession(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetSession(MSession session)
        {
            if (session == null) AppServices.OfflineProfiles.Select(null);
            App.MainWin.SetDisplayAccount(session?.Username);
            App.Session = session;
            ListAccounts();
        }

        private void LogoutBtn_OnClick(object sender, RoutedEventArgs e)
        {
            SetSession(null);
        }
    }
}
