using System.Collections.Generic;
using System.Windows;

namespace TCLauncher.Controls.Gallery
{
    public partial class MainWindow : Window
    {
        public IReadOnlyList<StoryDescriptor> Stories { get; } = StoryCatalog.Discover();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            StoryList.SelectionChanged += StoryList_OnSelectionChanged;
            if (StoryList.Items.Count > 0) StoryList.SelectedIndex = 0;
        }

        private void StoryList_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (StoryList.SelectedItem is StoryDescriptor story)
            {
                var borderless = story.Presentation == StoryPresentation.Borderless;
                PageScrollViewer.Padding = borderless ? new Thickness(0) : new Thickness(34);
                PageHost.Content = new StoryPresenter(story, story.CreatePage());
            }
        }
    }
}
