using System.Collections.Generic;

namespace TCLauncher.Models
{
    public class Asset
    {
        public string Name { get; set; }
        public List<AssetFragment> AssetFragments { get; set; }

        public Asset(string name, List<AssetFragment> assetFragments)
        {
            Name = name;
            AssetFragments = assetFragments;
        }
    }
}
