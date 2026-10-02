using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // What the ambient and random sound inspectors share: the entry's index, its sound, which polygons play it (a list
    // to uncheck them from), playing it, and deleting it
    public abstract class Inspector_SoundImage : Inspector_Base<SoundImageEntry>
    {
        protected Inspector_SoundImage(SoundImageEntry entry) : base(entry)
        {
        }

        private bool IsAmbient
        {
            get
            {
                return Entity.Kind == SoundImageKinds.Ambient;
            }
        }

        [CreateProperty]
        public string Index
        {
            get
            {
                return Entity.Index.ToString();
            }
        }

        // Only the sounds the loaded sounds file has can be chosen, so without one it can't be edited
        [CreateProperty]
        public bool IsSoundEditable
        {
            get
            {
                return SoundsLoading.Instance.IsLoaded;
            }
        }

        [CreateProperty]
        public string PlayedIn
        {
            get
            {
                var count = SoundImageEditing.UsageCount(Entity.Kind, Entity.Index);

                return count == 1 ? Strings.Get(Strings.Sounds, "Inspector.SoundImage.PlayedIn.One") : Strings.Get(Strings.Sounds, "Inspector.SoundImage.PlayedIn.Many", count);
            }
        }

        // Playing it needs its sound in the sounds file
        [CreateProperty]
        public bool IsSoundPlayable
        {
            get
            {
                return SoundPreviews.CanPlay(Entity.Kind, Entity.Index);
            }
        }

        // As the game plays it (at its volume, and a random one's pitch)
        public void PlaySound()
        {
            SoundPreviews.Play(Entity.Kind, Entity.Index);
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Q<Label>(className: "fp-inspector-header").text = Strings.Get(Strings.Sounds, IsAmbient ? "Inspector.SoundImage.Header.Ambient" : "Inspector.SoundImage.Header.Random", Entity.Index);
            Root.Q("aleph-one-note").style.display = Entity.Index >= SoundImageEditing.MaximumOriginalEntries ? DisplayStyle.Flex : DisplayStyle.None;

            var playedIn = Root.Find<Button>(nameof(PlayedIn));
            playedIn.Bind("text", this, nameof(PlayedIn));
            playedIn.clicked += () => ShowPolygons(playedIn);

            Root.Find<Button>("delete").clicked += Delete;

            SoundImageEditing.OnChanged += RefreshValuesInInspector;
            SoundsLoading.Instance.OnDataLoadCompleted += OnSoundsLoadCompleted;
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();

            SoundImageEditing.OnChanged -= RefreshValuesInInspector;
            SoundsLoading.Instance.OnDataLoadCompleted -= OnSoundsLoadCompleted;
        }

        // The polygons that play it as the list opens, which stay listed (to check again) while it's open
        private void ShowPolygons(VisualElement anchor)
        {
            var kind = Entity.Kind;
            var index = Entity.Index;
            var polygons = SoundImageEditing.PolygonsUsing(kind, index).ToList();
            var labels = polygons.Select(polygon => Strings.Get(Strings.Sounds, "Inspector.SoundImage.Polygon", polygon.NativeIndex)).ToList();

            ChecklistPicker.Show(anchor, labels,
                isChecked: item => SoundImageEditing.IndexOf(polygons[item].NativeObject, kind) == index,
                setChecked: (item, isChecked) => SoundImageEditing.Assign(polygons[item], kind, isChecked ? index : cstypes.NONE));
        }

        // After asking, since it changes the polygons that play it (and those that play later entries)
        private async void Delete()
        {
            var kind = Entity.Kind;
            var index = Entity.Index;
            var count = SoundImageEditing.UsageCount(kind, index);
            var message = IsAmbient ?
                          (count == 1 ? Strings.Get(Strings.Sounds, "Inspector.SoundImage.Delete.Message.Ambient.One") : Strings.Get(Strings.Sounds, "Inspector.SoundImage.Delete.Message.Ambient.Many", count)) :
                          (count == 1 ? Strings.Get(Strings.Sounds, "Inspector.SoundImage.Delete.Message.Random.One") : Strings.Get(Strings.Sounds, "Inspector.SoundImage.Delete.Message.Random.Many", count));

            var result = await DialogManager.Instance.DisplayQueuedDialog(
                title: Strings.Get(Strings.Sounds, IsAmbient ? "Inspector.SoundImage.Delete.Title.Ambient" : "Inspector.SoundImage.Delete.Title.Random", index),
                message: message,
                options: new[] { "Delete" },
                optionLabels: new[] { Strings.Get(Strings.Sounds, "Inspector.SoundImage.Delete.Confirm") },
                checkboxLabel: null);

            if (result.Option != "Delete")
            {
                return;
            }

            // Its inspector (this) goes with its selection, before its index means another entry
            SelectionManager.Instance.DeselectAll();
            SoundImageEditing.Delete(kind, index);
        }

        protected void EditEntry(System.Action edit)
        {
            edit();
            SoundImageEditing.EntryChanged(Entity);
        }

        private void OnSoundsLoadCompleted(bool isLoaded)
        {
            RefreshValuesInInspector();
        }

        // The choices for a code: those the sounds file has, and the current one (as its number, if the engine has no
        // sound for it)
        protected static List<string> BuildSoundChoices(short current, int codeCount, System.Func<short, bool> hasSound, System.Func<short, string> name)
        {
            var choices = new List<string>();

            for (short code = 0; code < codeCount; code++)
            {
                if (code == current || hasSound(code))
                {
                    choices.Add(name(code));
                }
            }

            if (!choices.Contains(name(current)))
            {
                choices.Insert(0, name(current));
            }

            return choices;
        }

        protected static bool TryFindCode(string choice, int codeCount, System.Func<short, string> name, out short code)
        {
            for (code = 0; code < codeCount; code++)
            {
                if (name(code) == choice)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
