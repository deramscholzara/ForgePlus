#if !NO_EDITING
using System;
using System.Collections;
using System.Collections.Generic;

namespace ForgePlus.History
{
    // Data whose changes can be undone. Its baseline (a copy of it as last recorded) is what changes are found against,
    // and follows whichever state is restored.
    public abstract class TrackedData
    {
        protected TrackedData(string name)
        {
            Name = name;
        }

        // Such as "PolygonList", which tells what restoring it needs to refresh
        public string Name { get; }

        public abstract void Capture();

        // Null if it hasn't changed since its baseline, which is then moved to its current state
        public abstract DataChange RecordChange();
    }

    public abstract class DataChange
    {
        protected DataChange(TrackedData data)
        {
            Data = data;
        }

        public TrackedData Data { get; }

        // Copied into the data's own objects where they're still there
        public abstract void Restore(bool after);
    }

    // A value changed as a whole (such as the level's static data)
    public class TrackedValue : TrackedData
    {
        private readonly Func<object> getValue;
        private readonly Action<object> setValue;

        private object baseline;

        // setValue replaces the value when it can't be restored in place (such as an array of another length)
        public TrackedValue(string name, Func<object> getValue, Action<object> setValue = null) : base(name)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }

        public override void Capture()
        {
            baseline = DataState.Clone(getValue());
        }

        public override DataChange RecordChange()
        {
            var value = getValue();
            if (DataState.AreEqual(value, baseline))
            {
                return null;
            }

            var change = new ValueChange(this, baseline, DataState.Clone(value));
            baseline = change.After;

            return change;
        }

        private void Restore(object state)
        {
            var value = getValue();
            var restored = DataState.CopyInto(value, state);

            if (!ReferenceEquals(restored, value))
            {
                if (setValue == null)
                {
                    throw new InvalidOperationException($"{Name} can't be restored in place, and can't be replaced.");
                }

                setValue(restored);
            }

            baseline = state;
        }

        public class ValueChange : DataChange
        {
            public ValueChange(TrackedValue data, object before, object after) : base(data)
            {
                Before = before;
                After = after;
            }

            public object Before { get; }

            public object After { get; }

            public override void Restore(bool after)
            {
                ((TrackedValue) Data).Restore(after ? After : Before);
            }
        }
    }

    // A list (such as the level's polygons) recorded element by element, so only changed elements are kept
    public class TrackedList : TrackedData
    {
        private readonly Func<IList> getList;

        private readonly List<object> baseline = new List<object>();

        public TrackedList(string name, Func<IList> getList) : base(name)
        {
            this.getList = getList;
        }

        public override void Capture()
        {
            baseline.Clear();

            foreach (var element in getList())
            {
                baseline.Add(DataState.Clone(element));
            }
        }

        public override DataChange RecordChange()
        {
            var list = getList();
            var sharedCount = Math.Min(list.Count, baseline.Count);
            var changedElements = new List<ElementState>();

            for (var i = 0; i < sharedCount; i++)
            {
                if (!DataState.AreEqual(list[i], baseline[i]))
                {
                    changedElements.Add(new ElementState(i, baseline[i], DataState.Clone(list[i])));
                }
            }

            // Removed elements come back by undoing, and added ones by redoing
            for (var i = sharedCount; i < baseline.Count; i++)
            {
                changedElements.Add(new ElementState(i, baseline[i], null));
            }

            for (var i = sharedCount; i < list.Count; i++)
            {
                changedElements.Add(new ElementState(i, null, DataState.Clone(list[i])));
            }

            if (changedElements.Count == 0)
            {
                return null;
            }

            var change = new ListChange(this, baseline.Count, list.Count, changedElements);
            Move(baseline, change, after: true);

            return change;
        }

        private void Restore(ListChange change, bool after)
        {
            var list = getList();
            var count = after ? change.AfterCount : change.BeforeCount;

            while (list.Count > count)
            {
                list.RemoveAt(list.Count - 1);
            }

            // In index order, so elements past the end are added in place
            foreach (var element in change.Elements)
            {
                var state = after ? element.After : element.Before;
                if (element.Index >= count)
                {
                    continue;
                }

                if (element.Index < list.Count)
                {
                    list[element.Index] = DataState.CopyInto(list[element.Index], state);
                }
                else
                {
                    list.Add(DataState.Clone(state));
                }
            }

            Move(baseline, change, after);
        }

        private static void Move(List<object> elements, ListChange change, bool after)
        {
            var count = after ? change.AfterCount : change.BeforeCount;

            if (elements.Count > count)
            {
                elements.RemoveRange(count, elements.Count - count);
            }

            foreach (var element in change.Elements)
            {
                if (element.Index >= count)
                {
                    continue;
                }

                var state = after ? element.After : element.Before;
                if (element.Index < elements.Count)
                {
                    elements[element.Index] = state;
                }
                else
                {
                    elements.Add(state);
                }
            }
        }

        public readonly struct ElementState
        {
            public ElementState(int index, object before, object after)
            {
                Index = index;
                Before = before;
                After = after;
            }

            public int Index { get; }

            // Null where the element wasn't there
            public object Before { get; }

            public object After { get; }

            public bool IsAddedOrRemoved
            {
                get
                {
                    return Before == null || After == null;
                }
            }
        }

        public class ListChange : DataChange
        {
            private HashSet<string>[] changedFields;

            public ListChange(TrackedList data, int beforeCount, int afterCount, List<ElementState> elements) : base(data)
            {
                BeforeCount = beforeCount;
                AfterCount = afterCount;
                Elements = elements;
            }

            public int BeforeCount { get; }

            public int AfterCount { get; }

            // In index order
            public IReadOnlyList<ElementState> Elements { get; }

            public bool CountChanged
            {
                get
                {
                    return BeforeCount != AfterCount;
                }
            }

            // The names of the element's changed fields, worked out when first asked for
            public HashSet<string> ChangedFields(int elementIndex)
            {
                if (changedFields == null)
                {
                    changedFields = new HashSet<string>[Elements.Count];
                }

                if (changedFields[elementIndex] == null)
                {
                    var element = Elements[elementIndex];
                    changedFields[elementIndex] = DataState.ChangedFields(element.Before, element.After);
                }

                return changedFields[elementIndex];
            }

            public override void Restore(bool after)
            {
                ((TrackedList) Data).Restore(this, after);
            }
        }
    }
}
#endif
