using System.Windows.Media;
using TCLauncher.Properties;

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
            Text = Languages.button_ok,
            IsDefault = true
        };

        public static readonly DialogButton Cancel = new DialogButton
        {
            Text = Languages.button_cancel,
            IsCancel = true
        };

        public static readonly DialogButton CancelColored = new DialogButton
        {
            Text = Languages.button_cancel,
            Background = new SolidColorBrush(Color.FromRgb(233, 52, 69)),
            Foreground = new SolidColorBrush(Colors.White),
            IsCancel = true
        };

        public static readonly DialogButton Yes = new DialogButton
        {
            Text = Languages.button_yes,
            IsDefault = true
        };

        public static readonly DialogButton No = new DialogButton
        {
            Text = Languages.button_no,
            IsCancel = true
        };

        public static readonly DialogButton Retry = new DialogButton
        {
            Text = Languages.button_retry,
            IsDefault = true
        };

        public static readonly DialogButton Try = new DialogButton
        {
            Text = Languages.button_try
        };

        public static readonly DialogButton Continue = new DialogButton
        {
            Text = Languages.button_continue,
            IsDefault = true
        };

        public static readonly DialogButton Abort = new DialogButton
        {
            Text = Languages.button_abort,
            IsCancel = true
        };

        public static readonly DialogButton AbortColored = new DialogButton
        {
            Text = Languages.button_abort,
            Background = new SolidColorBrush(Color.FromRgb(233, 52, 69)),
            Foreground = new SolidColorBrush(Colors.White),
            IsCancel = true
        };

        public static readonly DialogButton Ignore = new DialogButton
        {
            Text = Languages.button_ignore,
            IsCancel = true
        };
    }
}
