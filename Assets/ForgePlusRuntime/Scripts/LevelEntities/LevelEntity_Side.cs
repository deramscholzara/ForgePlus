using Unity.Scripting.LifecycleManagement;

namespace RuntimeCore.Entities.Geometry
{
    [AutoStaticsCleanup]
    public partial class LevelEntity_Side : LevelEntity_GeometryBase
    {
        // TODO: this is where things like culling and 5D clipping implementations from abstract base class will go

        private static bool placeholdersAreVisible = true;

        // Of the level's PlaceholderSides (built where a line has no side data for a polygon it faces)
        public static bool PlaceholdersAreVisible
        {
            get
            {
                return placeholdersAreVisible;
            }
            set
            {
                placeholdersAreVisible = value;

                var level = LevelEntity_Level.Instance;
                if (level && level.PlaceholderSides != null)
                {
                    foreach (var placeholderSide in level.PlaceholderSides)
                    {
                        placeholderSide.ApplyPlaceholderVisibility();
                    }
                }
            }
        }

        private void ApplyPlaceholderVisibility()
        {
            gameObject.SetActive(placeholdersAreVisible);
        }
    }
}
