using ForgePlus.ApplicationGeneral;
using System;

namespace ForgePlus.LevelManipulation
{
    public class AxisLocks : OnDemandSingletonMonoBehaviour<AxisLocks>
    {
        public event Action OnChanged;

        private bool xLocked = false;
        private bool yLocked = false;
        private bool snapToGridEnabled = false;

        // Locking one axis unlocks the other
        public bool XLocked
        {
            get
            {
                return xLocked;
            }
            set
            {
                if (xLocked != value)
                {
                    xLocked = value;

                    if (value)
                    {
                        yLocked = false;
                    }

                    OnChanged?.Invoke();
                }
            }
        }

        public bool YLocked
        {
            get
            {
                return yLocked;
            }
            set
            {
                if (yLocked != value)
                {
                    yLocked = value;

                    if (value)
                    {
                        xLocked = false;
                    }

                    OnChanged?.Invoke();
                }
            }
        }

        // The setting, without the alt/option key's inversion
        public bool SnapToGridEnabled
        {
            get
            {
                return snapToGridEnabled;
            }
            set
            {
                if (snapToGridEnabled != value)
                {
                    snapToGridEnabled = value;
                    OnChanged?.Invoke();
                }
            }
        }

        public bool SnapToGrid
        {
            get
            {
                if (ForgePlusInput.Editing.InvertGridSnap.IsPressed())
                {
                    return !snapToGridEnabled;
                }

                return snapToGridEnabled;
            }
        }

        private void Update()
        {
            // Ctrl+Y is Redo
            if (Hotkeys.IsShortcutModifierPressed)
            {
                return;
            }

            if (Hotkeys.WasPressed(ForgePlusInput.Editing.LockX))
            {
                XLocked = !XLocked;
            }

            if (Hotkeys.WasPressed(ForgePlusInput.Editing.LockY))
            {
                YLocked = !YLocked;
            }
        }
    }
}
