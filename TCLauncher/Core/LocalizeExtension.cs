using System;
using System.Windows.Markup;
using TCLauncher.Properties;

namespace TCLauncher.Core
{
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class LocalizeExtension : MarkupExtension
    {
        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Languages.ResourceManager.GetString(Key, Languages.Culture) ?? Key;
        }
    }
}
