using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using TCLauncher.Core;
using TCLauncher.Models;

namespace TCLauncher.MVVM.View
{
    /// <summary>
    /// Interaction logic for SocialView.xaml
    /// </summary>
    public partial class SocialView : UserControl
    {
        public SocialView()
        {
            InitializeComponent();
            DisplayFriends();
        }

        private SocialProfile GetProfile()
        {
            var token = App.Session?.AccessToken;

            if (token == null)
            {
                return null;
            }

            var client = new System.Net.WebClient();
            client.Headers.Add("Authorization", $"Bearer {token}");
            string response;
            try
            {
                response = client.DownloadString("https://tcraft.link/tclauncher/api/social/getProfile.php");
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            
            var profile = JsonConvert.DeserializeObject<SocialProfile>(response);
            var friends = profile.Friends.ToList();
            var newFriends = new List<SocialProfile>();

            var ignore = new List<int>();

            foreach (var friend in friends)
            {
                if (ignore.Contains(friend.Id)) continue;

                if (friend.IsRequestFrom)
                {
                    var correspondingFriend = friends.Find(f => (f.Id == friend.Id) && f.IsRequestTo && !f.IsRequestFrom);
                    if (correspondingFriend != null)
                    {
                        friend.IsRequestTo = true;
                        ignore.Add(correspondingFriend.Id);
                    }
                    newFriends.Add(friend);
                }
                else if (friend.IsRequestTo)
                {
                    var correspondingFriend = friends.Find(f => (f.Id == friend.Id) && f.IsRequestFrom && !f.IsRequestTo);
                    if (correspondingFriend != null)
                    {
                        friend.IsRequestFrom = true;
                        ignore.Add(correspondingFriend.Id);
                    }
                    newFriends.Add(friend);
                }
            }

            profile.Friends = newFriends.ToArray();
            return profile;
        }

        private void DisplayFriends()
        {
            var profile = GetProfile();

            if (profile == null) return;

            FriendsList.Items.Clear();
            FriendRequestsList.Items.Clear();
            FriendRequestsFromList.Items.Clear();
            foreach (var friend in profile.Friends)
            {
                friend.ImageSource = $"https://mc-heads.net/avatar/{friend.Uuid}";
                friend.Name = friend.Nickname; // TODO: Change this to the real name once it's implemented
                friend.Nickname = $"@{friend.Nickname}";

                if (friend.IsRequestFrom == true && friend.IsRequestTo != true)
                {
                    FriendRequestsFromList.Items.Add(friend);
                }
                else if (friend.IsRequestTo == true && friend.IsRequestFrom != true)
                {
                    FriendRequestsList.Items.Add(friend);
                }
                else
                {
                    FriendsList.Items.Add(friend);
                }
            }
        }

        private async void MenuItem_Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!(FriendsList.SelectedItem is SocialProfile))
            {
                return;
            }

            var selectedItem = (SocialProfile) FriendsList.SelectedItem;
            var selectedId = selectedItem.Id;
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Delete, "https://tcraft.link/tclauncher/api/social/removeFriend.php");
            request.Headers.Add("Authorization", "Bearer " + App.Session.AccessToken);
            var collection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("friendId", $"{selectedId}")
            };
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await client.SendAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                MessageBoxUtils.ShowToVoid("Der Freund konnte nicht entfernt werden.", "Fehler");
            }

            DisplayFriends();
        }

        private async void AddFriendButton_Click(object sender, RoutedEventArgs e)
        {
            var userName = await MessageBoxUtils.AskForString("Gib den Namen deines Freundes ein.");

            var client = new WebClient();
            client.Headers[HttpRequestHeader.ContentType] = "application/json";

            string jsonBody = "{\"person\":\"" + userName + "\"}";

            var response = client.UploadString("https://tcraft.link/tclauncher/api/social/getProfiles.php", "POST", jsonBody);
            
            if (response == "[]")
            {
                MessageBoxUtils.ShowToVoid("Der Freund konnte nicht gefunden werden.", "Fehler");
                return;
            }

            var profiles = JsonConvert.DeserializeObject<SocialProfile[]>(response);
            var profile = profiles[0];

            var client2 = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://tcraft.link/tclauncher/api/social/addFriend.php");
            request.Headers.Add("Authorization", "Bearer " + App.Session.AccessToken);
            var collection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("friendId", $"{profile.Id}")
            };
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response2 = client2.SendAsync(request).Result;
            if (response2.StatusCode != System.Net.HttpStatusCode.OK)
            {
                MessageBoxUtils.ShowToVoid("Der Freund konnte nicht hinzugefügt werden.", "Fehler");
            }

            DisplayFriends();
        }

        private void MenuItemRequests_Accept_Click (object sender, RoutedEventArgs e)
        {
            
        }

        private void MenuItemRequests_Reject_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
