using System.Windows.Media;

namespace TCLauncher.Models
{
    public class DialogButton
    {
        public string Text { get; set; }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }

        public DialogButton(string text, Brush background, Brush foreground, bool isDefault, bool isCancel)
        {
            Text = text;
            Background = background;
            Foreground = foreground;
            IsDefault = isDefault;
            IsCancel = isCancel;
        }

        public DialogButton(string text)
        {
            Text = text;
        }

        public DialogButton()
        {
        }

        // Default buttons
        public static readonly DialogButton Ok = new DialogButton
        {
            Text = "OK",
            IsDefault = true
        };

        public static readonly DialogButton Cancel = new DialogButton
        {
            Text = "Cancel",
            IsCancel = true
        };

        public static readonly DialogButton CancelColored = new DialogButton
        {
            Text = "Cancel",
            Background = new SolidColorBrush(Color.FromRgb(233, 52, 69)),
            Foreground = new SolidColorBrush(Colors.White),
            IsCancel = true
        };

        public static readonly DialogButton Yes = new DialogButton
        {
            Text = "Yes",
            IsDefault = true
        };

        public static readonly DialogButton No = new DialogButton
        {
            Text = "No",
            IsCancel = true
        };

        public static readonly DialogButton Retry = new DialogButton
        {
            Text = "Retry",
            IsDefault = true
        };

        public static readonly DialogButton Try = new DialogButton
        {
            Text = "Try"
        };

        public static readonly DialogButton Continue = new DialogButton
        {
            Text = "Continue",
            IsDefault = true
        };

        public static readonly DialogButton Abort = new DialogButton
        {
            Text = "Abort",
            IsCancel = true
        };

        public static readonly DialogButton AbortColored = new DialogButton
        {
            Text = "Abort",
            Background = new SolidColorBrush(Color.FromRgb(233, 52, 69)),
            Foreground = new SolidColorBrush(Colors.White),
            IsCancel = true
        };

        public static readonly DialogButton Ignore = new DialogButton
        {
            Text = "Ignore",
            IsCancel = true
        };
    }
}
