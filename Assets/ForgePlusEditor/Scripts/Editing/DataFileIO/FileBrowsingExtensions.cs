using ForgePlus.Localization;

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

        // The type's name, as file dialogs' titles show it
        public static string DisplayName(this DataFileTypes type)
        {
            switch (type)
            {
                case DataFileTypes.Maps:
                    return Strings.Get(Strings.Common, "FileBrowser.Type.Maps");
                case DataFileTypes.Shapes:
                    return Strings.Get(Strings.Common, "FileBrowser.Type.Shapes");
                case DataFileTypes.Sounds:
                    return Strings.Get(Strings.Common, "FileBrowser.Type.Sounds");
                case DataFileTypes.Physics:
                    return Strings.Get(Strings.Common, "FileBrowser.Type.Physics");
                case DataFileTypes.Images:
                    return Strings.Get(Strings.Common, "FileBrowser.Type.Images");
                default:
                    return type.ToString();
            }
        }
    }
}
