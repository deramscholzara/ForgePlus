namespace Weland.Extensions
{
    public static class ShapeDescriptorExtensions
    {
        public static bool UsesLandscapeCollection(this ShapeDescriptor shapeDescriptor)
        {
            return shapeDescriptor.Collection >= 27 && shapeDescriptor.Collection <= 30;
        }
    }
}
