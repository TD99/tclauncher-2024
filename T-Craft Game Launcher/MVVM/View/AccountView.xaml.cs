using CmlLib.Core.Auth.Microsoft.Sessions;
using CmlLib.Core.Auth;
using System.Windows;
using System;
using System.Windows.Controls;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Controls;

namespace T_Craft_Game_Launcher.MVVM.View
{
    public partial class AccountView
    {
        public AccountView()
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
