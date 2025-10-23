namespace TCLauncher.Models
{
    public class Server
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string ThumbnailURL { get; set; }

        public Server(string name, string address, string thumbnailURL)
        {
            Name = name;
            Address = address;
            ThumbnailURL = thumbnailURL;
        }

        public Server(string name, string address)
        {
            Name = name;
            Address = address;
            ThumbnailURL = "/Assets/Images/nothumb.png";
        }

        public Server()
        {
            ThumbnailURL = "/Assets/Images/nothumb.png";
        }

        public bool IsSameAs(object compare)
        {
            if (compare == null) return false;
            if (compare.GetType() != GetType()) return false;
            
            var server = (Server) compare;

            return Name == server.Name &&
                   Address == server.Address &&
                   ThumbnailURL == server.ThumbnailURL;
        }

    }
}
