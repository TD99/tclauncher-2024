using static TCLauncher.Models.DialogButton;

namespace TCLauncher.Models
{
    public static class DialogButtons
    {
        // Default button groups
        public static readonly DialogButton[] AbortRetryIgnore = { Abort, Retry, Ignore };
        public static readonly DialogButton[] CancelTryContinue = { Cancel, Try, Continue };
        public static readonly DialogButton[] Ok = { DialogButton.Ok };
        public static readonly DialogButton[] OkCancel = { DialogButton.Ok, Cancel };
        public static readonly DialogButton[] RetryCancel = { Retry, Cancel };
        public static readonly DialogButton[] YesNo = { Yes, No };
        public static readonly DialogButton[] YesNoCancel = { Yes, No, Cancel };
    }
}
