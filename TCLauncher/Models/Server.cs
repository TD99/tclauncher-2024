namespace TCLauncher.Models
{
    public class Server
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public string ThumbnailURL { get; set; }

        public Server(string name, string ip, string thumbnailURL)
        {
            Name = name;
            IP = ip;
            ThumbnailURL = thumbnailURL;
        }
    }
}
