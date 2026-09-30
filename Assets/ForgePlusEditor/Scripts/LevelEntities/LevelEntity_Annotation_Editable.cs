#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;

namespace RuntimeCore.Entities
{
    public partial class LevelEntity_Annotation
    {
        // Keeps only the characters Aleph One can draw, as many as fit its text
        public void SetText(string text)
        {
            NativeObject.SetText(text);

            RefreshLabel();
        }

        public void SetLocation(world_point2d location)
        {
            NativeObject.location = location;

            RefreshPosition();
        }
    }
}
#endif
