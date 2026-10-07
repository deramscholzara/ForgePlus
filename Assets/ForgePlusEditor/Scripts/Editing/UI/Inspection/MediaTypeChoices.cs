using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Inspection
{
    // The kinds of liquid a media can be (media.cpp: media_definitions). A type's texture is from a walls collection,
    // which the original games only load for the level's environment, so the other types are Aleph One only.
    [NoAutoStaticsCleanup]
    public static class MediaTypeChoices
    {
        // Includes the media's own type, even if it's no type's
        public static List<string> All(short currentType)
        {
            return Inspector_Base.ChoicesOf(Types(currentType), Choice);
        }

        public static List<string> AlephOneOnly(short currentType)
        {
            var alephOneOnly = new List<string>();
            foreach (var type in Types(currentType))
            {
                if (IsAlephOneOnly(type))
                {
                    alephOneOnly.Add(Choice(type));
                }
            }

            return alephOneOnly;
        }

        // "lava", or "lava (Aleph One only)"
        public static string Choice(short type)
        {
            var name = AlephOneNames.MediaType(type);

            return IsAlephOneOnly(type) ? Strings.Get(Strings.Media, "Inspector.Media.Type.Choice.AlephOneOnly", name) : name;
        }

        public static bool TryParse(short currentType, string choice, out short type)
        {
            return Inspector_Base.TryFindChoice(Types(currentType), Choice, choice, out type);
        }

        // Empty unless the type's texture is from a collection only Aleph One loads for the level
        public static string Note(short type)
        {
            return IsAlephOneOnly(type) ?
                   Strings.Get(Strings.Media, "Inspector.Media.TypeNote.AlephOneOnly", AlephOneNames.Collection(media.get_media_definition(type).collection)) :
                   string.Empty;
        }

        private static bool IsAlephOneOnly(short type)
        {
            var definition = media.get_media_definition(type);

            return definition != null && !CollectionChoices.IsLevelCollection(definition.collection);
        }

        private static List<short> Types(short currentType)
        {
            var types = new List<short>(Inspector_Base.ShortRange(0, media.NUMBER_OF_MEDIA_TYPES));
            if (currentType < 0 || currentType >= media.NUMBER_OF_MEDIA_TYPES)
            {
                types.Add(currentType);
            }

            return types;
        }
    }
}
