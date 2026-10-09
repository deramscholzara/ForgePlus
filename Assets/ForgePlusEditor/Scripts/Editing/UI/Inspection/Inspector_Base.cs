using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.Localization;
using ForgePlus.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // An inspector is the data source for its layout (under Resources/UI/Inspectors): each row, texture and flag
    // there is a template instance named after the property of the inspector it shows.
    // Inspected objects don't announce their changes, so the bindings update when RefreshValuesInInspector is called.
    // A field row is editable while its property has a setter, and grayed out until then.
    [AutoStaticsCleanup]
    public abstract partial class Inspector_Base : UIPanel, IDataSourceViewHashProvider
    {
        // Longer text than this is cut off (a text element can only draw so many characters)
        protected const int MaximumTextLength = 12000;

        // Not readonly, so AutoStaticsCleanup resets it each Play session
        private static List<Inspector_Base> loadedInspectors = new List<Inspector_Base>();

        private long version;

        protected abstract object InspectedObject { get; }

        // Refreshes every inspector showing what was edited (such as its selection inspector and its entry in a list)
        public static void RefreshInspectorsOf(object inspectedObject)
        {
            LevelHistory.MarkEdited();

            // An edit may have changed the level's errors
            var ui = ForgePlusUI.Instance;
            if (ui && ui.Errors != null)
            {
                ui.Errors.RequestRefresh();
            }

            foreach (var inspector in loadedInspectors)
            {
                if (inspector.InspectedObject == inspectedObject)
                {
                    inspector.RefreshValuesInInspector();
                }
            }
        }

        public void RefreshValuesInInspector()
        {
            version++;
        }

        public long GetViewHashCode()
        {
            return version;
        }

        // The choice for each item
        internal static List<string> ChoicesOf<T>(IEnumerable<T> items, Func<T, string> describe)
        {
            var choices = new List<string>();
            foreach (var item in items)
            {
                choices.Add(describe(item));
            }

            return choices;
        }

        // The choice for each item, and the current item's first if it isn't one of them
        internal static List<string> ChoicesOf<T>(IEnumerable<T> items, Func<T, string> describe, T current)
        {
            var choices = ChoicesOf(items, describe);

            var currentChoice = describe(current);
            if (!choices.Contains(currentChoice))
            {
                choices.Insert(0, currentChoice);
            }

            return choices;
        }

        internal static bool TryFindChoice<T>(IEnumerable<T> items, Func<T, string> describe, string choice, out T item)
        {
            foreach (var candidate in items)
            {
                if (describe(candidate) == choice)
                {
                    item = candidate;
                    return true;
                }
            }

            item = default;
            return false;
        }

        // Aleph One's codes and indexes are shorts
        internal static IEnumerable<short> ShortRange(int start, int count)
        {
            for (var value = start; value < start + count; value++)
            {
                yield return (short) value;
            }
        }

        // The sound codes the sounds file has sounds for, and the current one
        protected static List<string> SoundCodeChoices(short current, int codeCount, Func<short, bool> hasSound, Func<short, string> name)
        {
            return ChoicesOf(ShortRange(0, codeCount).Where(code => code == current || hasSound(code)), name, current);
        }

        protected override void OnLoaded()
        {
            Root.BindInspectorFields(this);

            loadedInspectors.Add(this);
        }

        protected override void OnUnloading()
        {
            loadedInspectors.Remove(this);
        }

        // Changes the inspected object, then shows the change in every inspector of it
        protected void Edit(Action edit)
        {
            edit();

            RefreshInspectorsOf(InspectedObject);
        }

        // A landscape (or a surface with no texture) isn't lit
        protected static bool UsesLight(ushort shapeDescriptor)
        {
            return !shapeDescriptor.IsEmptyShapeDescriptor() && !shapeDescriptor.UsesLandscapeCollection();
        }

        protected void MakeTextBlocksSelectable()
        {
            Root.Query<Label>(className: "fp-inspector-text-block__text").ForEach(text => text.selection.isSelectable = true);
        }

        // Aleph One stores most numbers as shorts, which number fields edit as ints
        internal static short ClampToShort(int value)
        {
            return (short) Mathf.Clamp(value, short.MinValue, short.MaxValue);
        }

        protected static short ClampToNonNegativeShort(int value)
        {
            return (short) Mathf.Clamp(value, 0, short.MaxValue);
        }

        protected static ushort WithFlag(ushort flags, int flag, bool isSet)
        {
            return (ushort) csmacros.SET_FLAG(flags, flag, isSet);
        }

        protected static short WithFlag(short flags, int flag, bool isSet)
        {
            return (short) csmacros.SET_FLAG((ushort) flags, flag, isSet);
        }

        protected static uint WithFlag(uint flags, int flag, bool isSet)
        {
            return csmacros.SET_FLAG(flags, (uint) flag, isSet);
        }

        // Intensities are fractions of full light (FIXED_ONE), shown to four decimal places
        protected static float DisplayedIntensity(int fixedIntensity)
        {
            return (float) Math.Round(AlephOneExtensions.FixedToFloat(fixedIntensity), 4);
        }

        protected static int FixedIntensity(float intensity)
        {
            return (int) Math.Round(Math.Clamp(intensity, 0f, 1f) * cstypes.FIXED_ONE);
        }

        // "lava (3)"
        protected static string NameAndNumber(string name, int number)
        {
            return Strings.Get(Strings.Common, "Inspector.Base.NameAndNumber", name, number);
        }

        protected static string ByteCount(byte[] bytes)
        {
            return bytes != null ? Strings.Get(Strings.Common, "Inspector.Base.Bytes", bytes.Length.ToString("N0")) : NoneText;
        }

        protected static string NoneText
        {
            get
            {
                return Strings.Get(Strings.Common, "Inspector.Base.None");
            }
        }

        protected static string CutOff(string text)
        {
            return text.Length > MaximumTextLength ?
                   Strings.Get(Strings.Common, "Inspector.Base.TextCutOff", text.Substring(0, MaximumTextLength), (text.Length - MaximumTextLength).ToString("N0")) :
                   text;
        }
    }

    public abstract class Inspector_Base<TEntity> : Inspector_Base where TEntity : class, IInspectable
    {
        protected Inspector_Base(TEntity entity)
        {
            Entity = entity;
        }

        protected TEntity Entity { get; private set; }

        protected override object InspectedObject
        {
            get
            {
                return Entity;
            }
        }

        // Changes the entity, then shows the change in every inspector of it
        protected void Edit(Action<TEntity> edit)
        {
            edit(Entity);

            RefreshInspectorsOf(Entity);
        }

        // A negative index (no light) can't be assigned
        protected void SetLight(int lightIndex, Action<TEntity, short> setLight)
        {
            if (lightIndex >= 0)
            {
                Edit(entity => setLight(entity, (short) lightIndex));
            }
        }
    }
}
