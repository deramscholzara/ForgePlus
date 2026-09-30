using ForgePlus.DataFileIO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgePlus.LevelManipulation
{
    public class ModeManager : OnDemandSingletonMonoBehaviour<ModeManager>
    {
        public enum PrimaryModes
        {
            None,
            Geometry,
            Textures,
            Lights,
            Media,
            Platforms,
            Objects,
            Annotations,
            Level,
            Terminals,
        }

        public enum SecondaryModes
        {
            None,
            Selection,
            Painting,
            Editing,
        }

        private event Action<PrimaryModes> OnPrimaryModeChanged_Sender;
        public event Action<PrimaryModes> OnPrimaryModeChanged
        {
            add
            {
                OnPrimaryModeChanged_Sender += value;
                value.Invoke(primaryMode);
            }
            remove
            {
                OnPrimaryModeChanged_Sender -= value;
            }
        }

        private event Action<SecondaryModes> OnSecondaryModeChanged_Sender;
        public event Action<SecondaryModes> OnSecondaryModeChanged
        {
            add
            {
                OnSecondaryModeChanged_Sender += value;
                value.Invoke(secondaryMode);
            }
            remove
            {
                OnSecondaryModeChanged_Sender -= value;
            }
        }

        private readonly SecondaryModes[] allSecondaryModes = { SecondaryModes.Selection, SecondaryModes.Painting, SecondaryModes.Editing };
        private readonly SecondaryModes[] selectionAndPainting = { SecondaryModes.Selection, SecondaryModes.Painting };
        private readonly SecondaryModes[] selectionOnly = { SecondaryModes.Selection };

        private PrimaryModes primaryMode = PrimaryModes.Geometry;
        private SecondaryModes secondaryMode = SecondaryModes.Selection;

        public PrimaryModes PrimaryMode
        {
            get
            {
                return primaryMode;
            }
            set
            {
                if (primaryMode != value)
                {
                    primaryMode = value;

                    // Falls back to selection when the new mode can't use the current secondary mode
                    if (secondaryMode != SecondaryModes.None && !IsAvailable(secondaryMode))
                    {
                        SecondaryMode = SecondaryModes.Selection;
                    }

                    OnPrimaryModeChanged_Sender?.Invoke(primaryMode);
                }
            }
        }

        public SecondaryModes SecondaryMode
        {
            get
            {
                return secondaryMode;
            }
            set
            {
                if (secondaryMode != value)
                {
                    secondaryMode = value;

                    if (secondaryMode == SecondaryModes.Painting)
                    {
                        SelectionManager.Instance.DeselectAll();
                    }

                    OnSecondaryModeChanged_Sender?.Invoke(secondaryMode);
                }
            }
        }

        // The secondary modes the current primary mode can use (Selection is always one of them)
        public IReadOnlyList<SecondaryModes> AvailableSecondaryModes
        {
            get
            {
                switch (primaryMode)
                {
                    case PrimaryModes.Textures:
                        return allSecondaryModes;
                    case PrimaryModes.Lights:
                        return selectionAndPainting;
                    default:
                        return selectionOnly;
                }
            }
        }

        private bool IsAvailable(SecondaryModes mode)
        {
            return AvailableSecondaryModes.Contains(mode);
        }

        private void OnLevelOpened(string levelName)
        {
            OnPrimaryModeChanged_Sender?.Invoke(primaryMode);
            OnSecondaryModeChanged_Sender?.Invoke(secondaryMode);
        }

        private void OnLevelClosed()
        {
            OnPrimaryModeChanged_Sender?.Invoke(PrimaryModes.None);
            OnSecondaryModeChanged_Sender?.Invoke(SecondaryModes.None);
        }

        private void Awake()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
        }
    }
}
