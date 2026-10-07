using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Inspection
{
    // The control panel types a side can be (devices.cpp's control panel definitions, a set for each environment's walls
    // collection). The game sets a panel's texture from its type's collection, which the original games only load for
    // the level's environment, so the other environments' types are Aleph One only.
    [NoAutoStaticsCleanup]
    public static class ControlPanelTypes
    {
        // Includes the side's own type, even if it's no type's
        public static List<string> All(short currentType)
        {
            return Inspector_Base.ChoicesOf(Types(currentType), Choice);
        }

        public static List<string> AlephOneOnly(short currentType)
        {
            var alephOneOnly = new List<string>();
            foreach (var type in Types(currentType))
            {
                var definition = devices.get_control_panel_definition(type);
                if (definition != null && !CollectionChoices.IsLevelCollection(definition.collection))
                {
                    alephOneOnly.Add(Choice(type));
                }
            }

            return alephOneOnly;
        }

        public static string Choice(short type)
        {
            var definition = devices.get_control_panel_definition(type);
            if (definition == null)
            {
                return type.ToString();
            }

            var name = AlephOneNames.ControlPanelClass(definition._class);

            // A tag switch can take an item (a chip), or be broken by projectiles
            if (definition.item != cstypes.NONE)
            {
                name = Strings.Get(Strings.Geometry, "ControlPanelType.TakesItem", name);
            }
            else if (definition.sounds[devices._activating_sound] == SoundManagerEnums._snd_destroy_control_panel)
            {
                name = Strings.Get(Strings.Geometry, "ControlPanelType.Breakable", name);
            }

            var choiceKey = CollectionChoices.IsLevelCollection(definition.collection) ? "ControlPanelType.Choice" : "ControlPanelType.Choice.AlephOneOnly";

            return Strings.Get(Strings.Geometry, choiceKey, type, name, AlephOneNames.Collection(definition.collection));
        }

        public static bool TryParse(short currentType, string choice, out short type)
        {
            return Inspector_Base.TryFindChoice(Types(currentType), Choice, choice, out type);
        }

        public static bool IsLevelType(short type)
        {
            var definition = devices.get_control_panel_definition(type);

            return definition != null && definition.collection == CollectionChoices.LevelWallCollection();
        }

        // For a side that becomes a control panel: the level's light switch, or its first type, or the first type
        public static short LevelDefault()
        {
            short firstLevelType = cstypes.NONE;
            for (short type = 0; type < devices.NUMBER_OF_CONTROL_PANEL_DEFINITIONS; type++)
            {
                if (!IsLevelType(type))
                {
                    continue;
                }

                if (devices.get_control_panel_definition(type)._class == map._panel_is_light_switch)
                {
                    return type;
                }

                if (firstLevelType == cstypes.NONE)
                {
                    firstLevelType = type;
                }
            }

            return firstLevelType != cstypes.NONE ? firstLevelType : (short) 0;
        }

        // Empty unless the type's collection is one only Aleph One loads for the level
        public static string Note(short type)
        {
            var definition = devices.get_control_panel_definition(type);
            if (definition == null || CollectionChoices.IsLevelCollection(definition.collection))
            {
                return string.Empty;
            }

            return Strings.Get(Strings.Geometry, "ControlPanelType.Note.AlephOneOnly", AlephOneNames.Collection(definition.collection));
        }

        // The level's types first, then the others
        private static List<short> Types(short currentType)
        {
            var types = new List<short>();
            var otherTypes = new List<short>();
            for (short type = 0; type < devices.NUMBER_OF_CONTROL_PANEL_DEFINITIONS; type++)
            {
                (IsLevelType(type) ? types : otherTypes).Add(type);
            }

            types.AddRange(otherTypes);

            if (devices.get_control_panel_definition(currentType) == null)
            {
                types.Add(currentType);
            }

            return types;
        }
    }
}
