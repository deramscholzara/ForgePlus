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

        public event Action<PrimaryModes, SecondaryModes> OnModeChanged_Sender;

        public event Action<PrimaryModes, SecondaryModes> OnModeChanged
        {
            add
            {
                OnModeChanged_Sender += value;
                value.Invoke(primaryMode, secondaryMode);
            }
            remove
            {
                OnModeChanged_Sender -= value;
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

                    secondaryMode = SecondaryModes.Selection;

                    OnModeChanged_Sender?.Invoke(primaryMode, secondaryMode);
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

                    OnModeChanged_Sender?.Invoke(primaryMode, secondaryMode);
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
            OnModeChanged_Sender?.Invoke(primaryMode, secondaryMode);
        }

        private void OnLevelClosed()
        {
            OnModeChanged_Sender?.Invoke(PrimaryModes.None, SecondaryModes.None);
        }

        private void Awake()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
        }
    }
}
