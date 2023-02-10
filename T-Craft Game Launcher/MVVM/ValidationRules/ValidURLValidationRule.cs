using System.Globalization;
using System.Windows.Controls;
using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher.MVVM.ValidationRules
{
    public class ValidURLValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ValidationResult.ValidResult;
            //if (INetTools.requestPage(value.ToString()))
            //{
            //    return ValidationResult.ValidResult;
            //}

            //return new ValidationResult(false, "Die angegebene URL ist nicht erreichbar.");
        }
    }
}
