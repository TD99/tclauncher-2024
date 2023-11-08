using CmlLib.Core.Auth.Microsoft.Sessions;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.IO;
using System.Windows.Controls;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Controls;

namespace T_Craft_Game_Launcher.MVVM.View
{
    public partial class AccountView
    {
        private readonly JELoginHandler _loginHandler;
        private readonly string _primaryAccountUuid;
        //private TaskCompletionSource<MSession> LoginTask { get; }

        public AccountView(/*string primaryAccountUuid = null*/)
        {
            InitializeComponent();
            _loginHandler = new JELoginHandlerBuilder()
                .WithAccountManager(Path.Combine(IoUtils.Tcl.UdataPath, "tcl_accounts.json"))
                .Build();
            //_primaryAccountUuid = primaryAccountUuid;
            //LoginTask = new TaskCompletionSource<MSession>();
        }

        private void AccountView_OnLoaded(object sender, RoutedEventArgs e)
        {
            ListAccounts();
        }

        private void AccountView_OnUnloaded(object sender, EventArgs e)
        {
            //if (!LoginTask.Task.IsCompleted)
            //    LoginTask.SetResult(null);
        }

        private void ListAccounts()
        {
            Accounts.Items.Clear();
            var accounts = _loginHandler.AccountManager.GetAccounts();
            foreach (var account in accounts)
            {
                if (!(account is JEGameAccount jeGameAccount))
                    continue;

                var isPrimary = jeGameAccount?.Profile?.UUID == _primaryAccountUuid;

                var control = new AccountControl(jeGameAccount, isPrimary)
                {
                    DataContext = jeGameAccount
                };

                control.LoginBtn.Click += Control_OnLoginClicked;
                control.RemoveBtn.Click += Control_OnRemoveClicked;

                Accounts.Items.Add(control);
            }
        }

        private async void NewAccountBtn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await _loginHandler.AuthenticateInteractively();
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
                var result = await _loginHandler.Authenticate(selectedAccount);
                ReturnSession(result);
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
                _loginHandler.Signout(selectedAccount);
                ListAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ReturnSession(MSession session)
        {
            //LoginTask.SetResult(session);
            //this.Close();
        }
    }
}
