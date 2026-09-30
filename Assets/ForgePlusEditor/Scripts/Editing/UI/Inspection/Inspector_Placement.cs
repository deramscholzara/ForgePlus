using AlephOne;
using System;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    // How the level places one type of item or monster (shared by every object of the type, and by the ones it makes
    // at random): how many it starts with, keeps up to, and adds at random, every 15 seconds (monsters only when the
    // game's options have them replenish)
    public class Inspector_Placement : Inspector_Base
    {
        private readonly object_frequency_definition placement;

        public Inspector_Placement(object_frequency_definition placement)
        {
            this.placement = placement;
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Placement";
            }
        }

        protected override object InspectedObject
        {
            get
            {
                return placement;
            }
        }

        // Can be more than the maximum count
        [CreateProperty]
        public int InitialCount
        {
            get { return placement.initial_count; }
            set { EditPlacement(() => placement.initial_count = ClampToCount(value)); }
        }

        // Kept in the level (replacing ones that are destroyed or picked up), whatever the maximum count
        [CreateProperty]
        public int MinimumCount
        {
            get { return placement.minimum_count; }
            set { EditPlacement(() => placement.minimum_count = ClampToCount(value)); }
        }

        // Only limits the ones added at random
        [CreateProperty]
        public int MaximumCount
        {
            get { return placement.maximum_count; }
            set { EditPlacement(() => placement.maximum_count = ClampToCount(value)); }
        }

        // An infinite random count (NONE) keeps adding them at random for as long as the level lasts
        [CreateProperty]
        public bool RandomCountIsInfinite
        {
            get { return placement.random_count == cstypes.NONE; }
            set { EditPlacement(() => placement.random_count = value ? cstypes.NONE : (short) 0); }
        }

        [CreateProperty]
        public int RandomCount
        {
            get { return RandomCountIsInfinite ? 0 : placement.random_count; }
            set { EditPlacement(() => placement.random_count = ClampToCount(value)); }
        }

        [CreateProperty]
        public bool IsRandomCountEditable
        {
            get { return !RandomCountIsInfinite; }
        }

        // The chance of adding one at random at each 15-second check, stored out of 65535 and edited as a percentage
        [CreateProperty]
        public int RandomChance
        {
            get { return (int) Math.Round(placement.random_chance * 100.0 / ushort.MaxValue); }
            set { EditPlacement(() => placement.random_chance = (ushort) Math.Round(Math.Clamp(value, 0, 100) * ushort.MaxValue / 100.0)); }
        }

        // Nothing is added at random without a random count
        [CreateProperty]
        public bool IsRandomChanceEditable
        {
            get { return RandomCountIsInfinite || placement.random_count > 0; }
        }

        // Otherwise, ones that aren't there from the start appear where the level places objects of the type
        [CreateProperty]
        public bool ReappearsInRandomLocation
        {
            get
            {
                return csmacros.TEST_FLAG(placement.flags, map._reappears_in_random_location);
            }
            set
            {
                EditPlacement(() => placement.flags = value ?
                                                      (ushort) (placement.flags | map._reappears_in_random_location) :
                                                      (ushort) (placement.flags & ~map._reappears_in_random_location));
            }
        }

        private void EditPlacement(Action edit)
        {
            edit();

            RefreshInspectorsOf(placement);
        }

        private static short ClampToCount(int value)
        {
            return ClampToShort(Math.Max(0, value));
        }
    }
}
