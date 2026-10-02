using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using ForgePlus.UI;
using System.Text;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // The map file as a whole: its header, and the rest of the Mac file (its resource fork), which no level has
    public class Inspector_Map : Inspector_Base
    {
        // Longer text than this is cut off (a text element can only draw so many characters)
        private const int MaximumTextLength = 12000;

        private static readonly uint LevelScriptType = ResourceType("TEXT");
        private const short LevelScriptId = 128;

        private static readonly uint VersionType = ResourceType("vers");
        private const short VersionId = 1;

        private readonly MapsFile mapsFile;

        public Inspector_Map(MapsFile mapsFile)
        {
            this.mapsFile = mapsFile;
        }

        protected override object InspectedObject
        {
            get
            {
                return mapsFile;
            }
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Map";
            }
        }

        private wad_header Header
        {
            get
            {
                return mapsFile.Header;
            }
        }

        private MacResourceFork Resources
        {
            get
            {
                return mapsFile.Forks.ResourceFork;
            }
        }

        [CreateProperty]
        public string Name
        {
            get
            {
                return csstrings.mac_roman_to_utf8(mapsFile.Name);
            }
            set
            {
                mapsFile.Name.SetMacRomanText(value);

                RefreshInspectorsOf(mapsFile);
            }
        }

        [CreateProperty]
        public string FileName
        {
            get
            {
                return mapsFile.File.GetName();
            }
        }

        [CreateProperty]
        public string LevelCount
        {
            get
            {
                return mapsFile.Levels.Count.ToString();
            }
        }

        // How many levels have physics saved in them (which Marathon Infinity and Aleph One use instead of the physics
        // file)
        [CreateProperty]
        public string EmbeddedPhysicsLevelCount
        {
            get
            {
                return Strings.Get(Strings.Map, "Inspector.Map.EmbeddedPhysicsLevelCount", mapsFile.EmbeddedPhysicsLevelCount, mapsFile.Levels.Count);
            }
        }

        // The wad format's version (wad.h), which saving writes as Marathon Infinity's
        [CreateProperty]
        public string FileVersion
        {
            get
            {
                switch (Header.version)
                {
                    case wad.PRE_ENTRY_POINT_WADFILE_VERSION:
                        return Strings.Get(Strings.Map, "Inspector.Map.Version.Marathon", Header.version);
                    case wad.WADFILE_HAS_DIRECTORY_ENTRY:
                        return Strings.Get(Strings.Map, "Inspector.Map.Version.MarathonWithDirectory", Header.version);
                    case wad.WADFILE_SUPPORTS_OVERLAYS:
                        return Strings.Get(Strings.Map, "Inspector.Map.Version.Marathon2", Header.version);
                    case wad.WADFILE_HAS_INFINITY_STUFF:
                        return Strings.Get(Strings.Map, "Inspector.Map.Version.MarathonInfinity", Header.version);
                    default:
                        return Header.version.ToString();
                }
            }
        }

        // The levels' data's version (editor.h)
        [CreateProperty]
        public string DataVersion
        {
            get
            {
                switch (Header.data_version)
                {
                    case editor.MARATHON_ONE_DATA_VERSION:
                        return Strings.Get(Strings.Map, "Inspector.Map.Version.Marathon", Header.data_version);
                    case editor.MARATHON_TWO_DATA_VERSION:
                        return Strings.Get(Strings.Map, "Inspector.Map.Version.Marathon2", Header.data_version);
                    case editor.MARATHON_INFINITY_DATA_VERSION:
                        return Strings.Get(Strings.Map, "Inspector.Map.Version.MarathonInfinity", Header.data_version);
                    default:
                        return Header.data_version.ToString();
                }
            }
        }

        // What saved games, films and physics files made for the map find it by (Save Merged can keep it)
        [CreateProperty]
        public string Checksum
        {
            get
            {
                return $"0x{Header.checksum:X8}";
            }
        }

        // The checksum of the map a patch file (such as a physics file) is for
        [CreateProperty]
        public string ParentChecksum
        {
            get
            {
                return Header.parent_checksum == 0 ? Strings.Get(Strings.Map, "Inspector.Map.None") : $"0x{Header.parent_checksum:X8}";
            }
        }

        [CreateProperty]
        public string Container
        {
            get
            {
                return MacFileForks.ContainerName(mapsFile.Forks.Container);
            }
        }

        [CreateProperty]
        public string ResourceFork
        {
            get
            {
                return Resources != null ? mapsFile.Forks.ResourceForkSource : Strings.Get(Strings.Map, "Inspector.Map.None");
            }
        }

        // The file's version information, as the Finder shows it ('vers' 1's long version string, or its short one)
        [CreateProperty]
        public string Version
        {
            get
            {
                var version = Resources?.Get(VersionType, VersionId);
                if (version == null || version.Length < 7)
                {
                    return Strings.Get(Strings.Map, "Inspector.Map.None");
                }

                var shortVersion = PascalString(version, 6);
                var longVersionOffset = 7 + version[6];
                var longVersion = longVersionOffset < version.Length ? PascalString(version, longVersionOffset) : string.Empty;

                return string.IsNullOrEmpty(longVersion) ? shortVersion : longVersion;
            }
        }

        [CreateProperty]
        public string LevelScriptSize
        {
            get
            {
                var levelScript = Resources?.Get(LevelScriptType, LevelScriptId);

                return levelScript != null ? Strings.Get(Strings.Map, "Inspector.Map.Bytes", levelScript.Length.ToString("N0")) : Strings.Get(Strings.Map, "Inspector.Map.None");
            }
        }

        // Aleph One parses it as XML text (with the Mac's line breaks, if it was written on one)
        [CreateProperty]
        public string LevelScriptText
        {
            get
            {
                var levelScript = Resources?.Get(LevelScriptType, LevelScriptId);
                if (levelScript == null)
                {
                    return string.Empty;
                }

                var text = Encoding.UTF8.GetString(levelScript).TrimEnd('\0').Replace("\r\n", "\n").Replace('\r', '\n');

                return text.Length > MaximumTextLength ?
                       Strings.Get(Strings.Map, "Inspector.Map.TextCutOff", text.Substring(0, MaximumTextLength), (text.Length - MaximumTextLength).ToString("N0")) :
                       text;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Find<TextField>(nameof(Name)).LimitToMacRomanText(mapsFile.Name.MacRomanTextCapacity());
            Root.Query<Label>(className: "fp-inspector-text-block__text").ForEach(text => text.selection.isSelectable = true);

            // A map without a level script has no text to show
            Root.Q(nameof(LevelScriptText)).style.display = string.IsNullOrEmpty(LevelScriptText) ? DisplayStyle.None : DisplayStyle.Flex;

            AddResourceRows();
        }

        // A row for each type of resource, with how many there are, in the placeholder's place (among the other rows)
        private void AddResourceRows()
        {
            var placeholder = Root.Q("resources");
            var rows = placeholder.parent;
            var index = rows.IndexOf(placeholder);
            placeholder.RemoveFromHierarchy();

            if (Resources == null)
            {
                return;
            }

            var rowTemplate = LoadTemplate("InspectorRow");

            foreach (var type in Resources.Types)
            {
                var code = TypeCode(type.Key);

                var row = rowTemplate.Instantiate();
                row.Q<Label>("label").text = $"{code.TrimEnd()}:";
                var name = ResourceTypeName(code);
                row.Q<Label>("value").text = name != null ? Strings.Get(Strings.Map, "Inspector.Map.ResourceCount", type.Value, name) : type.Value.ToString();

                // Read-only, as rows with no setter are
                row.SetEnabled(false);
                rows.Insert(index++, row);
            }
        }

        // What Aleph One uses the type for (images.cpp, XML_LevelScript.cpp), or null for a type it doesn't use
        private static string ResourceTypeName(string code)
        {
            switch (code)
            {
                case "PICT":
                    return Strings.Get(Strings.Map, "Inspector.Map.ResourceType.Pictures");
                case "clut":
                    return Strings.Get(Strings.Map, "Inspector.Map.ResourceType.ColorTables");
                case "snd ":
                    return Strings.Get(Strings.Map, "Inspector.Map.ResourceType.Sounds");
                case "TEXT":
                    return Strings.Get(Strings.Map, "Inspector.Map.ResourceType.Texts");
                case "vers":
                    return Strings.Get(Strings.Map, "Inspector.Map.ResourceType.Versions");
                default:
                    return null;
            }
        }

        private static uint ResourceType(string code)
        {
            return (uint) ((code[0] << 24) | (code[1] << 16) | (code[2] << 8) | code[3]);
        }

        private static string TypeCode(uint type)
        {
            return csstrings.mac_roman_to_utf8(new[] { (byte) (type >> 24), (byte) (type >> 16), (byte) (type >> 8), (byte) type });
        }

        private static string PascalString(byte[] bytes, int offset)
        {
            var length = System.Math.Min(bytes[offset], bytes.Length - offset - 1);
            var text = new byte[length];
            System.Array.Copy(bytes, offset + 1, text, 0, length);

            return csstrings.mac_roman_to_utf8(text);
        }
    }
}
