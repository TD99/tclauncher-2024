using System;
using System.Net.Http;
using System.Windows.Media.Imaging;
using MojangAPI;

namespace TCLauncher.MVVM.View
{
    public partial class AccountOptionsView
    {
        public AccountOptionsView()
        {
            InitializeComponent();
            
            LoadContent();
        }

        private async void LoadContent()
        {
            try
            {
                var mojang = new Mojang(new HttpClient());
                var data = (await mojang.GetPlayerAttributes(App.Session?.AccessToken));
                var ownershipData = (await mojang.CheckGameOwnership(App.Session?.AccessToken));
                var profile = (await mojang.GetProfileUsingAccessToken(App.Session?.AccessToken));
                var privileges = data.Privileges;

                var username = App.Session?.Username;
                UserName.Text = username;
                UserUuids.Text = App.Session?.UUID;
                UserPrivileges.Text = nameof(privileges.MultiplayerRealms) + privileges?.MultiplayerRealms?.Enabled +
                                      nameof(privileges.MultiplayerServer) + privileges?.MultiplayerServer?.Enabled +
                                      nameof(privileges.OnlineChat) + privileges?.OnlineChat?.Enabled +
                                      nameof(privileges.Telemtry) + privileges?.Telemtry?.Enabled;
                UserProfanityFilter.IsChecked = data.ProfanityFilterPreferences?.ProfanityFilterOn;
                UserOwnershipValid.IsChecked = ownershipData;
                UserSkin.Source = new BitmapImage(new Uri(profile.Skin?.Url, UriKind.Absolute));
            } catch
            {
                // TODO: Handle error
            }
        }
    }
}
