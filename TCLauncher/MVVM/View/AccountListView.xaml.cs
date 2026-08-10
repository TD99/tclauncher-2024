using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CmlLib.Core.Auth.Microsoft.Sessions;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.View
{
    public partial class AccountListView
    {
        public ObservableCollection<AccountRow> Rows { get; } = new ObservableCollection<AccountRow>();

        public AccountListView() => InitializeComponent();
        private void AccountView_OnLoaded(object sender, RoutedEventArgs e) => ListAccounts();

        private void ListAccounts()
        {
            Rows.Clear();
            var selected = AppServices.AccountSelection.Get();
            foreach (var account in App.LoginHandler.AccountManager.GetAccounts())
            {
                if (!(account is JEGameAccount gameAccount)) continue;
                var id = gameAccount.Profile?.UUID;
                Rows.Add(new AccountRow
                {
                    MicrosoftAccount = gameAccount,
                    StableId = id,
                    Username = gameAccount.Profile?.Username ?? gameAccount.Gamertag ?? "Microsoft account",
                    Subtitle = string.IsNullOrWhiteSpace(gameAccount.Gamertag)
                        ? "Microsoft"
                        : gameAccount.Gamertag + " • Microsoft",
                    AvatarUri = GetAvatarUri(id),
                    IsSelected = selected?.Kind == AccountSelectionKind.Microsoft &&
                                 string.Equals(selected.StableId, id, StringComparison.OrdinalIgnoreCase)
                });
            }

            foreach (var profile in AppServices.OfflineProfiles.List())
            {
                Rows.Add(new AccountRow
                {
                    OfflineProfile = profile,
                    StableId = profile.Id.ToString("D"),
                    Username = profile.Username,
                    Subtitle = "Offline • stored on this device",
                    IsSelected = selected?.Kind == AccountSelectionKind.Offline && string.Equals(selected.StableId,
                        profile.Id.ToString("D"), StringComparison.OrdinalIgnoreCase)
                });
            }

            LogoutBtn.Visibility = selected == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void NewAccountBtn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var session = await App.LoginHandler.AuthenticateInteractively();
                App.SetMicrosoftSession(session);
                ListAccounts();
            }
            catch (Exception exception)
            {
                AppServices.Overlays.ShowToast("Sign-in failed", exception.Message, ToastTone.Error);
            }
        }

        private void NewOfflineBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _ = AppServices.Overlays.ShowSheetAsync("Add offline profile", new TextPromptSheet(
                "Minecraft name (3–16 letters, numbers, or underscores)", value =>
                {
                    var result = AppServices.OfflineProfiles.Add(value);
                    if (!result.IsSuccess)
                        return OperationResult.Failure(result.ErrorCode, result.Message, result.Exception,
                            result.OperationId);
                    App.SetOfflineSession(result.Value);
                    ListAccounts();
                    AppServices.Overlays.ShowToast("Offline profile added", result.Value.Username);
                    return OperationResult.Success(result.OperationId);
                }), false);
        }

        private async void SelectAccount_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is AccountRow row) || row.IsSelected) return;
            try
            {
                if (row.IsOffline) App.SetOfflineSession(row.OfflineProfile);
                else App.SetMicrosoftSession(await App.LoginHandler.Authenticate(row.MicrosoftAccount));
                ListAccounts();
            }
            catch (Exception exception)
            {
                AppServices.Overlays.ShowToast("Account unavailable", exception.Message, ToastTone.Error);
            }
        }

        private async void RemoveAccount_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is AccountRow row)) return;
            if (!await AppServices.Overlays.ConfirmAsync("Remove account",
                    "Remove " + row.Username + " from this device?", "Remove", "Cancel")) return;
            try
            {
                if (row.IsOffline) AppServices.OfflineProfiles.Remove(row.OfflineProfile.Id);
                else await App.LoginHandler.Signout(row.MicrosoftAccount);
                if (row.IsSelected) App.SetOfflineSession(null);
                ListAccounts();
            }
            catch (Exception exception)
            {
                AppServices.Overlays.ShowToast("Account could not be removed", exception.Message, ToastTone.Error);
            }
        }

        private void LogoutBtn_OnClick(object sender, RoutedEventArgs e)
        {
            App.SetOfflineSession(null);
            ListAccounts();
        }

        private static string GetAvatarUri(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return null;
            var cached = Path.Combine(IoUtils.Tcl.CachePath, "avatar_" + uuid + ".png");
            return File.Exists(cached) ? cached : "https://mc-heads.net/avatar/" + uuid;
        }
    }

    public sealed class AccountRow
    {
        public string StableId { get; set; }
        public string Username { get; set; }
        public string Subtitle { get; set; }
        public string AvatarUri { get; set; }
        public bool IsSelected { get; set; }
        public JEGameAccount MicrosoftAccount { get; set; }
        public OfflineProfile OfflineProfile { get; set; }
        public bool IsOffline => OfflineProfile != null;
        public bool IsMicrosoft => MicrosoftAccount != null;
    }
}