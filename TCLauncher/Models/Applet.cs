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
        public bool is_action => ActionURL != null;

        public Applet(int weight, string name, string coverURL, string title, string description, string actionURL)
        {
            Weight = weight;
            Name = name;
            CoverURL = coverURL;
            Title = title;
            Description = description;
            ActionURL = actionURL;
        }
    }
}
