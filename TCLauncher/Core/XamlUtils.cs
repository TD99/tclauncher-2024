using System.Windows;
using System.Windows.Media;

namespace TCLauncher.Core
{
    /// <summary>
    /// Provides utility methods for working with XAML in the TCLauncher application.
    /// </summary>
    public static class XamlUtils
    {
        /// <summary>
        /// Finds the first parent of the specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the parent to find.</typeparam>
        /// <param name="child">The starting point for the search.</param>
        /// <returns>The first parent of the specified type, or null if no such parent exists.</returns>
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (true)
            {
                // Get parent item
                var parentObject = VisualTreeHelper.GetParent(child);

                switch (parentObject)
                {
                    // We've reached the end of the tree
                    case null:
                        return null;
                    // Check if the parent matches the type we're looking for
                    case T parent:
                        return parent;
                    default:
                        child = parentObject;
                        continue;
                }
            }
        }
    }
}
