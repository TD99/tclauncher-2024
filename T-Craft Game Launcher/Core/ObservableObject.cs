using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace T_Craft_Game_Launcher.Core
{
    /// <summary>
    /// A base class for objects that notifies clients that a property value has changed.
    /// </summary>
    internal class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event if the property specified in the CallerMemberName attribute has changed.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
