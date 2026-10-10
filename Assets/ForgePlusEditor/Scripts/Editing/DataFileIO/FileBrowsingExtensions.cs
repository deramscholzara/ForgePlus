using ForgePlus.Localization;
using SFB;

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

        // Every extension a file of the type can have, as Aleph One recognizes them (FileHandler.cpp's extensions):
        // Marathon 2 and Infinity's (which saving uses), then Marathon 1's
        public static string[] FileExtensions(this DataFileTypes type)
        {
            switch (type)
            {
                case DataFileTypes.Maps:
                    return new[] { "sceA", "scen" };
                case DataFileTypes.Shapes:
                    return new[] { "shpA", "shps" };
                case DataFileTypes.Sounds:
                    return new[] { "sndA", "sndz" };
                case DataFileTypes.Physics:
                    return new[] { "phyA", "phys" };
                default:
                    return new[] { type.FileExtension() };
            }
        }

        // The type's extensions, then any file (as Mac files often have no extension)
        public static ExtensionFilter[] OpenFileFilters(this DataFileTypes type)
        {
            return new[]
            {
                new ExtensionFilter(type.DisplayName(), type.FileExtensions()),
                new ExtensionFilter(Strings.Get(Strings.Common, "FileBrowser.Filter.AllFiles"), "*"),
            };
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
