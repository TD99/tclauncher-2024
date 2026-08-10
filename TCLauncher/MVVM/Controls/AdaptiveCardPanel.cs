using System;
using System.Windows;
using System.Windows.Controls;

namespace TCLauncher.MVVM.Controls
{
    public sealed class AdaptiveCardPanel : Panel
    {
        public double MinimumCardWidth { get; set; } = 230;
        public double Gap { get; set; } = 16;
        public int MinimumColumns { get; set; } = 2;
        public int MaximumColumns { get; set; } = 4;

        protected override Size MeasureOverride(Size availableSize)
        {
            var width = double.IsInfinity(availableSize.Width)
                ? MinimumCardWidth * MinimumColumns
                : availableSize.Width;
            var columns = GetColumns(width);
            var itemWidth = Math.Max(MinimumCardWidth, (width - Gap * (columns - 1)) / columns);
            var totalHeight = 0d;
            var rowHeight = 0d;
            for (var index = 0; index < InternalChildren.Count; index++)
            {
                var child = InternalChildren[index];
                child.Measure(new Size(itemWidth, double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
                if ((index + 1) % columns == 0 || index == InternalChildren.Count - 1)
                {
                    totalHeight += rowHeight;
                    if (index < InternalChildren.Count - 1) totalHeight += Gap;
                    rowHeight = 0;
                }
            }

            return new Size(width, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var columns = GetColumns(finalSize.Width);
            var itemWidth = (finalSize.Width - Gap * (columns - 1)) / columns;
            var y = 0d;
            for (var rowStart = 0; rowStart < InternalChildren.Count; rowStart += columns)
            {
                var rowHeight = 0d;
                var rowEnd = Math.Min(rowStart + columns, InternalChildren.Count);
                for (var index = rowStart; index < rowEnd; index++)
                    rowHeight = Math.Max(rowHeight, InternalChildren[index].DesiredSize.Height);
                for (var index = rowStart; index < rowEnd; index++)
                {
                    var column = index - rowStart;
                    InternalChildren[index].Arrange(new Rect(column * (itemWidth + Gap), y, itemWidth, rowHeight));
                }

                y += rowHeight + Gap;
            }

            return finalSize;
        }

        private int GetColumns(double width)
        {
            var count = (int)Math.Floor((width + Gap) / (MinimumCardWidth + Gap));
            return Math.Max(MinimumColumns, Math.Min(MaximumColumns, count));
        }
    }
}