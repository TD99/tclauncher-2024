namespace TCLauncher.Models
{
    public class Patch
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }

        public Patch(int id, string name, string url)
        {
            ID = id;
            Name = name;
            URL = url;
        }

        public Patch()
        {
            
        }

        public bool IsSameAs(object compare)
        {
            if (compare == null) return false;
            if (compare.GetType() != GetType()) return false;
            
            var patch = (Patch) compare;

            return ID == patch.ID &&
                   Name == patch.Name &&
                   URL == patch.URL;
        }
    }
}
