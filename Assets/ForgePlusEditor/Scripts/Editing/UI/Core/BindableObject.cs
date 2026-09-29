using System;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A data source for UI bindings, which tells its bindings when a property changes (so they update however it changed)
    public abstract class BindableObject : INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        protected void Notify(string propertyName)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
        }
    }
}
