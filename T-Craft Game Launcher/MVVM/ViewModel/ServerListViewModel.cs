using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Model;
using T_Craft_Game_Launcher.MVVM.View;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
{
    class ServerListViewModel : ObservableObject
    {
        private ObservableCollection<Instance> _serverList;
        private readonly HttpClient _httpClient = new HttpClient();

        public ObservableCollection<Instance> ServerList
        {
            get => _serverList;
            set
            {
                _serverList = value;
                OnPropertyChanged();
            }
        }

        public ServerListViewModel()
        {
            LoadServers();
        }

        private async void LoadServers()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://tcraft.link/tclauncher/api/");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    ServerList = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content);
                }
            }
            catch 
            {
                MessageBox.Show("Ein Fehler beim Laden der verfügbaren Profile ist aufgetreten.", "Fehler");
            }
        }
    }
}
