using System;
using System.IO;
using System.Net;
using System.Windows.Media.Imaging;
using CmlLib.Core.Auth.Microsoft.Sessions;
using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher.MVVM.Controls
{
    public partial class AccountControl
    {
        public JEGameAccount Account { get; set; }

        public AccountControl()
        {
            InitializeComponent();
        }

        public AccountControl(JEGameAccount account, bool isPrimary = false)
        {
            InitializeComponent();
            Account = account;
            Username.Text = account.Profile?.Username ?? "";
            GamerTag.Text = account.Gamertag ?? "";
            Identifier.Text = account.Identifier ?? "";

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(GetAvatarUrl(account.Profile?.UUID), UriKind.Absolute);
            bitmap.EndInit();

            Avatar.Source = bitmap;

            if (isPrimary)
                LoginBtn.Visibility = System.Windows.Visibility.Hidden;
        }

        private static string GetAvatarUrl(string uuid)
        {
            var avatarSrc = $"https://mc-heads.net/avatar/{uuid}";
            var avatarCacheFile = Path.Combine(IoUtils.Tcl.CachePath, $"avatar_{uuid}.png");

            if (!File.Exists(avatarCacheFile))
            {
                var webClient = new WebClient();
                webClient.DownloadFile(avatarSrc, avatarCacheFile);
            }

            return avatarCacheFile;
        }
    }
}
