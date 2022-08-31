namespace ForgePlus.LevelManipulation.Utilities
{
    public static class MathUtilities
    {
        public static bool IsPowerOfTwo(int i)
        {
            return (i > 0) && ((i & (i - 1)) == 0);
        }
    }
}