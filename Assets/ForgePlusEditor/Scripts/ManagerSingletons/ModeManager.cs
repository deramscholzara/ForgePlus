using ForgePlus.DataFileIO;
using System;

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

        #region UI_Event_Methods
        public void SetSecondaryToNone(bool shouldSet)
        {
            if (shouldSet)
            {
                SecondaryMode = SecondaryModes.None;
            }
        }

        public void SetSecondaryToSelection(bool shouldSet)
        {
            if (shouldSet)
            {
                SecondaryMode = SecondaryModes.Selection;
            }
        }

        public void SetSecondaryToPainting(bool shouldSet)
        {
            if (shouldSet)
            {
                SecondaryMode = SecondaryModes.Painting;
            }
        }

        public void SetSecondaryToEditing(bool shouldSet)
        {
            if (shouldSet)
            {
                SecondaryMode = SecondaryModes.Editing;
            }
        }
        #endregion UI_Event_Methods

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
