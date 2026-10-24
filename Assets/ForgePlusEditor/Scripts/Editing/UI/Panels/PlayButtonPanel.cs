using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.Localization;
using ForgePlus.Play;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The Play button (left of the errors toggle, in the screen's bottom right corner), which asks before starting Play
    // mode (PlayThisLevel), at a player start or from here, while a level is open
    public class PlayButtonPanel : UIPanel
    {
        private const string BeginOption = "Begin";
        private const string BeginFromHereOption = "BeginFromHere";

        private Button button;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/PlayButton";
            }
        }

        protected override void OnLoaded()
        {
            button = Root.Find<Button>("play");
            button.clicked += Ask;

            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += UpdateState;
#if !NO_EDITING
            History.UndoHistory.OnChanged += UpdateState;
#endif
        }

        protected override void OnUnloading()
        {
            MapsLoading.Instance.OnLevelOpened -= OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed -= UpdateState;
#if !NO_EDITING
            History.UndoHistory.OnChanged -= UpdateState;
#endif
        }

        private void OnLevelOpened(string levelName)
        {
            UpdateState();
        }

        // A level being rebuilt (as it's edited) can't be played until it's done
        private void UpdateState()
        {
            button.SetEnabled(PlayThisLevel.CanBegin);
        }

        private async void Ask()
        {
            if (!PlayThisLevel.CanBegin)
            {
                return;
            }

            var exitKey = InputControlPath.ToHumanReadableString(ForgePlusInput.Play.Exit.bindings[0].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);

            // As the dialog opens (the camera stays put while it's open)
            var result = await DialogManager.Instance.DisplayQueuedDialog(
                title: Strings.Get(Strings.Common, "Dialog.Play.Title"),
                message: Strings.Get(Strings.Common, "Dialog.Play.Message", exitKey),
                options: new[] { BeginOption, BeginFromHereOption },
                optionLabels: new[]
                {
                    Strings.Get(Strings.Common, "Dialog.Play.Begin"),
                    Strings.Get(Strings.Common, "Dialog.Play.BeginFromHere"),
                },
                checkboxLabel: null,
                optionsEnabled: new[] { PlayThisLevel.HasPlayerStarts, PlayThisLevel.CanBeginFromHere },
                optionTooltips: new[]
                {
                    Strings.Get(Strings.Common, "Dialog.Play.Begin.Tooltip"),
                    Strings.Get(Strings.Common, "Dialog.Play.BeginFromHere.Tooltip"),
                });

            if (result.Option == BeginOption || result.Option == BeginFromHereOption)
            {
                PlayThisLevel.Begin(fromHere: result.Option == BeginFromHereOption);
            }
        }
    }
}
