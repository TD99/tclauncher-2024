using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace TCLauncher.Controls.Gallery
{
    public enum StoryPresentation
    {
        Standard,
        Borderless
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class StoryAttribute : Attribute
    {
        public string Component { get; }
        public string Variant { get; }
        public string Description { get; set; }
        public StoryPresentation Presentation { get; set; }

        public StoryAttribute(string component, string variant = null)
        {
            Component = component;
            Variant = variant;
        }
    }

    public sealed class StoryDescriptor
    {
        private readonly Type _pageType;

        public string Component { get; }
        public string Variant { get; }
        public string Description { get; }
        public StoryPresentation Presentation { get; }
        public string Title => string.IsNullOrWhiteSpace(Variant) ? Component : Component + " · " + Variant;

        public StoryDescriptor(Type pageType, StoryAttribute story)
        {
            _pageType = pageType;
            Component = story.Component;
            Variant = story.Variant;
            Description = story.Description;
            Presentation = story.Presentation;
        }

        public UserControl CreatePage() => (UserControl)Activator.CreateInstance(_pageType);
    }

    public static class StoryCatalog
    {
        public static IReadOnlyList<StoryDescriptor> Discover()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Select(type => new { Type = type, Story = type.GetCustomAttribute<StoryAttribute>() })
                .Where(item => item.Story != null && typeof(UserControl).IsAssignableFrom(item.Type) && !item.Type.IsAbstract)
                .Select(item => new StoryDescriptor(item.Type, item.Story))
                .OrderBy(story => story.Component)
                .ThenBy(story => story.Variant)
                .ToList();
        }
    }
}
