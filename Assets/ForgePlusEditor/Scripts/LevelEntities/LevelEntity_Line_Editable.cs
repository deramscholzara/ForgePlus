#if !NO_EDITING
using ForgePlus.LevelManipulation;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Line
    {
        // Its sides are built again from the level's data, as loading the level builds them
        public void RegenerateSurfaces()
        {
            foreach (var side in new[] { ClockwiseSide, CounterclockwiseSide })
            {
                if (side)
                {
                    side.PrepareForDestruction();

                    // Destroying waits for the end of the frame, so it's hidden (and out of the pointer's way) now
                    side.gameObject.SetActive(false);
                    Destroy(side.gameObject);
                }
            }

            ClockwiseSide = null;
            CounterclockwiseSide = null;

            GenerateSurfaces();

            foreach (var side in new[] { ClockwiseSide, CounterclockwiseSide })
            {
                if (side)
                {
                    SelectionManager.Instance.MatchSelectabilityToMode(side);
                }
            }
        }
    }
}
#endif
