#if !NO_EDITING
using AlephOne;
using ForgePlus.Inspection;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.LevelManipulation
{
    // The two lists of sounds a level's polygons play while the player is in them: ambient sounds ('ambi', looped) and
    // random sounds ('bonk', played now and then), which each polygon picks one of (or none) by index. Both are the
    // level's own data (what each entry plays is one of the engine's ambient or random sound codes, whose audio the
    // sounds file has).
    public enum SoundImageKinds
    {
        Ambient,
        Random,
    }

    // An entry in one of the lists, which is selected (from the Sounds palette) to inspect it
    public class SoundImageEntry : ISelectable, IInspectable
    {
        public SoundImageKinds Kind { get; }
        public short Index { get; }

        public SoundImageEntry(SoundImageKinds kind, short index)
        {
            Kind = kind;
            Index = index;
        }

        public ambient_sound_image_data Ambient
        {
            get
            {
                return Kind == SoundImageKinds.Ambient ? SoundImageEditing.Level.AmbientSoundImageList[Index] : null;
            }
        }

        public random_sound_image_data Random
        {
            get
            {
                return Kind == SoundImageKinds.Random ? SoundImageEditing.Level.RandomSoundImageList[Index] : null;
            }
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank: it's selected from the palette
        }

        public void Inspect()
        {
            InspectorPanel.Instance.AddInspector(Kind == SoundImageKinds.Ambient ? (Inspector_Base) new Inspector_AmbientSound(this) : new Inspector_RandomSound(this));
        }
    }

    [AutoStaticsCleanup]
    public static partial class SoundImageEditing
    {
        // The original Marathon 2 and Infinity hold this many of each (Aleph One holds any number)
        public const int MaximumOriginalEntries = 64;

        // After an entry is added, changed or removed, or a polygon's sound changes
        public static event Action OnChanged;

        // One entry object for each, so selecting an entry again finds the same one (remade when the lists change shape)
        private static readonly Dictionary<(SoundImageKinds, short), SoundImageEntry> entries = new Dictionary<(SoundImageKinds, short), SoundImageEntry>();
        private static MapLevel entriesLevel;

        public static MapLevel Level
        {
            get
            {
                return LevelEntity_Level.Instance ? LevelEntity_Level.Instance.Level : null;
            }
        }

        public static int Count(SoundImageKinds kind)
        {
            var level = Level;
            if (level == null)
            {
                return 0;
            }

            return kind == SoundImageKinds.Ambient ? level.AmbientSoundImageList.Count : level.RandomSoundImageList.Count;
        }

        public static SoundImageEntry GetEntry(SoundImageKinds kind, short index)
        {
            if (entriesLevel != Level)
            {
                entries.Clear();
                entriesLevel = Level;
            }

            if (index < 0 || index >= Count(kind))
            {
                return null;
            }

            if (!entries.TryGetValue((kind, index), out var entry))
            {
                entry = new SoundImageEntry(kind, index);
                entries[(kind, index)] = entry;
            }

            return entry;
        }

        // The polygon's entry in the list (or NONE)
        public static short IndexOf(polygon_data polygon, SoundImageKinds kind)
        {
            return kind == SoundImageKinds.Ambient ? polygon.ambient_sound_image_index : polygon.random_sound_image_index;
        }

        public static IEnumerable<LevelEntity_Polygon> PolygonsUsing(SoundImageKinds kind, short index)
        {
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return Enumerable.Empty<LevelEntity_Polygon>();
            }

            return level.Polygons.OrderBy(pair => pair.Key).Select(pair => pair.Value).Where(polygon => IndexOf(polygon.NativeObject, kind) == index);
        }

        public static int UsageCount(SoundImageKinds kind, short index)
        {
            var level = Level;

            return level == null ? 0 : level.PolygonList.Count(polygon => IndexOf(polygon, kind) == index);
        }

        // Sets which of the list's entries the polygon plays (or NONE, for none)
        public static void Assign(LevelEntity_Polygon polygon, SoundImageKinds kind, short index)
        {
            if (IndexOf(polygon.NativeObject, kind) == index)
            {
                return;
            }

            if (kind == SoundImageKinds.Ambient)
            {
                polygon.NativeObject.ambient_sound_image_index = index;
            }
            else
            {
                polygon.NativeObject.random_sound_image_index = index;
            }

            Inspector_Base.RefreshInspectorsOf(polygon);
            Changed(shapeChanged: false);
        }

        // A new entry at the end of the list: full volume, of the first sound (a random one every 10 seconds, at its
        // own pitch, from every direction's 0). Returns its index.
        public static short Add(SoundImageKinds kind)
        {
            var level = Level;

            if (kind == SoundImageKinds.Ambient)
            {
                level.AmbientSoundImageList.Add(new ambient_sound_image_data
                {
                    sound_index = 0,
                    volume = SoundManagerEnums.MAXIMUM_SOUND_VOLUME,
                });
            }
            else
            {
                level.RandomSoundImageList.Add(new random_sound_image_data
                {
                    sound_index = 0,
                    volume = SoundManagerEnums.MAXIMUM_SOUND_VOLUME / 2,
                    period = 10 * 30,
                    pitch = cstypes.FIXED_ONE,
                    phase = cstypes.NONE,
                });
            }

            Changed(shapeChanged: true);

            return (short) (Count(kind) - 1);
        }

        // A copy of the entry at the end of the list. Returns its index.
        public static short Duplicate(SoundImageKinds kind, short index)
        {
            var level = Level;

            if (kind == SoundImageKinds.Ambient)
            {
                level.AmbientSoundImageList.Add(level.AmbientSoundImageList[index].Clone());
            }
            else
            {
                level.RandomSoundImageList.Add(level.RandomSoundImageList[index].Clone());
            }

            Changed(shapeChanged: true);

            return (short) (Count(kind) - 1);
        }

        // Removes the entry: the polygons that played it play none, and those that played a later one follow it to its
        // new index (one less)
        public static void Delete(SoundImageKinds kind, short index)
        {
            var level = Level;

            if (kind == SoundImageKinds.Ambient)
            {
                level.AmbientSoundImageList.RemoveAt(index);
            }
            else
            {
                level.RandomSoundImageList.RemoveAt(index);
            }

            foreach (var polygon in level.PolygonList)
            {
                var polygonIndex = IndexOf(polygon, kind);
                var newIndex = polygonIndex == index ? cstypes.NONE : polygonIndex > index ? (short) (polygonIndex - 1) : polygonIndex;

                if (kind == SoundImageKinds.Ambient)
                {
                    polygon.ambient_sound_image_index = newIndex;
                }
                else
                {
                    polygon.random_sound_image_index = newIndex;
                }
            }

            Changed(shapeChanged: true);
        }

        // An entry's fields changed (so what shows it, such as the palette and the inspectors, shows the change)
        public static void EntryChanged(SoundImageEntry entry)
        {
            Inspector_Base.RefreshInspectorsOf(entry);
            Changed(shapeChanged: false);
        }

        private static void Changed(bool shapeChanged)
        {
            if (shapeChanged)
            {
                // Indexes moved, so the entry objects are made again
                entries.Clear();
            }

            OnChanged?.Invoke();
        }
    }
}
#endif
