using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TCLauncher.Controls.Gallery
{
    public sealed class StoryPresenter : UserControl
    {
        public StoryPresenter(StoryDescriptor story, UserControl page)
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;

            if (story.Presentation == StoryPresentation.Borderless)
            {
                Content = page;
                return;
            }

            var content = new StackPanel { MaxWidth = 820 };
            content.Children.Add(new TextBlock
            {
                FontSize = 32,
                FontWeight = FontWeights.SemiBold,
                Text = story.Title
            });
            if (!string.IsNullOrWhiteSpace(story.Description))
                content.Children.Add(new TextBlock
                {
                    Margin = new Thickness(0, 8, 0, 28),
                    Foreground = new SolidColorBrush(Color.FromRgb(174, 184, 199)),
                    Text = story.Description,
                    TextWrapping = TextWrapping.Wrap
                });
            content.Children.Add(page);
            Content = content;
        }
    }
}
