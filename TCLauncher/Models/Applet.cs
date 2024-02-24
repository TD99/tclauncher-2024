namespace TCLauncher.Models
{
    public class Applet
    {
        public int Weight { get; set; }
        public string Name { get; set; }
        public string CoverURL { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ActionURL { get; set; }
        public bool OpenExternal { get; set; }
        public bool is_action => ActionURL != null;
        
        public Applet(int weight, string name, string coverUrl, string title, string description, string actionUrl, bool openExternal=false)
        {
            Weight = weight;
            Name = name;
            CoverURL = coverUrl;
            Title = title;
            Description = description;
            ActionURL = actionUrl;
            OpenExternal = openExternal;
        }

        public Applet()
        {
        }
    }
}
