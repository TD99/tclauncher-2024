namespace TCLauncher.Models
{
    public class SocialProfile
    {
        public int Id { get; set; }
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string ImageSource { get; set; }
        public string Nickname { get; set; }
        public bool IsRequestFrom { get; set; }
        public bool IsRequestTo { get; set; }
        public SocialProfile[] Friends { get; set; }

        public SocialProfile()
        {
            
        }

        public SocialProfile(int id, string uuid, string nickname)
        {
            Id = id;
            Uuid = uuid;
            Nickname = nickname;
        }

        public SocialProfile(int id, string uuid, string name, string imageSource, string nickname, SocialProfile[] friends, bool isRequestFrom, bool isRequestTo) : this(id, uuid, name)
        {
            ImageSource = imageSource;
            Nickname = nickname;
            Friends = friends;
            IsRequestFrom = isRequestFrom;
            IsRequestTo = isRequestTo;
        }
    }
}
