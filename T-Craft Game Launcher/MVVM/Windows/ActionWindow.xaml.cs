using System.Windows.Shell;

namespace T_Craft_Game_Launcher.MVVM.Windows
{
    /// <summary>
    /// Interaction logic for ActionWindow.xaml
    /// </summary>
    public partial class ActionWindow
    {
        private TaskbarItemInfo taskbarInfo;
        private int _percent;
        public int percent
        {
            get
            {
                return _percent;
            }
            set
            {
                _percent = value;
                percentText.Text = value.ToString();
                taskbarInfo.ProgressValue = value / 100.0;
            }
        }

        public ActionWindow(string action)
        {
            InitializeComponent();
            actionText.Text = action;
            taskbarInfo = new TaskbarItemInfo();
            taskbarInfo.ProgressState = TaskbarItemProgressState.Normal;
            taskbarInfo.ProgressValue = 0.0;
            this.TaskbarItemInfo = taskbarInfo;
        }
    }
}
