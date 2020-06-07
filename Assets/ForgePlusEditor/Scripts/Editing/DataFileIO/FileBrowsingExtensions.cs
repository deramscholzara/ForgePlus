namespace ForgePlus.DataFileIO.Extensions
{
    public static class FileBrowsingExtensions
    {
        public static string FileExtension(this DataFileTypes type)
        {
            switch (type)
            {
                case DataFileTypes.Maps:
                    return "sceA";
                case DataFileTypes.Shapes:
                    return "shpA";
                case DataFileTypes.Sounds:
                    return "sndA";
                case DataFileTypes.Physics:
                    return "phyA";
                case DataFileTypes.Images:
                    return "imgA";
                default:
                    return "*";
            }
        }

        public static string FileExtensionWithPeriod(this DataFileTypes type)
        {
            return string.Concat(".", type.FileExtension());
        }
    }
}
