// Port of Aleph One: Source_Files/CSeries/cseries.h (Rect only)
namespace AlephOne
{
    public class Rect
    {
        public short top, left;
        public short bottom, right;

        public Rect Clone()
        {
            return (Rect) MemberwiseClone();
        }
    }
}
