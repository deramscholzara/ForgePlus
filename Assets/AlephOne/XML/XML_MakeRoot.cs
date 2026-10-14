// Port of Aleph One: Source_Files/XML/XML_MakeRoot.cpp (resetting and parsing MML)
//
// Not ported: the MML for everything an editor doesn't show or use (strings, the interface, the player's name, logging,
// faders, the motion sensor, the overhead map, infravision, animated textures, the view, weapons, damage kicks,
// monsters, landscapes, texture loading, the software renderer, dynamic limits, the console and the default levels'
// music and load screens). Each section ForgePlus parses is parsed in the same order as Aleph One parses it.
using Unity.Scripting.LifecycleManagement;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class XML_MakeRoot
    {
        // This will reset all values changed by MML scripts which implement ResetValues() method
        // and are part of the master MarathonParser tree.
        public static void ResetAllMMLValues()
        {
            // reset_mml_stringset();
            // reset_mml_interface();
            // reset_mml_motion_sensor();
            // reset_mml_overhead_map();
            // reset_mml_infravision();
            // reset_mml_animated_textures();
            devices.reset_mml_control_panels();
            platforms.reset_mml_platforms();
            media.reset_mml_liquids();
            SoundManager.reset_mml_sounds();
            // reset_mml_faders();
            player.reset_mml_player();
            // reset_mml_view();
            // reset_mml_weapons();
            items.reset_mml_items();
            // reset_mml_damage_kicks();
            // reset_mml_monsters();
            scenery.reset_mml_scenery();
            // reset_mml_landscapes();
            // reset_mml_texture_loading();
            OGL_Setup.reset_mml_opengl();
            // reset_mml_software();
            // reset_mml_dynamic_limits();
            // reset_mml_player_name();
            Scenario.reset_mml_scenario();
            // reset_mml_logging();
            // reset_mml_console();
            // reset_mml_default_levels();
        }

        private static void _ParseAllMML(InfoTree fileroot, bool load_menu_mml_only)
        {
            foreach (var root in fileroot.children_named("marathon"))
            {
                foreach (var child in root.children_named("scenario"))
                    Scenario.parse_mml_scenario(child);
                foreach (var child in root.children_named("sounds"))
                    SoundManager.parse_mml_sounds(child);
                foreach (var child in root.children_named("player"))
                    player.parse_mml_player(child);

                if (load_menu_mml_only) continue;

                foreach (var child in root.children_named("control_panels"))
                    devices.parse_mml_control_panels(child);
                foreach (var child in root.children_named("platforms"))
                    platforms.parse_mml_platforms(child);
                foreach (var child in root.children_named("liquids"))
                    media.parse_mml_liquids(child);
                foreach (var child in root.children_named("items"))
                    items.parse_mml_items(child);
                foreach (var child in root.children_named("scenery"))
                    scenery.parse_mml_scenery(child);
                foreach (var child in root.children_named("opengl"))
                    OGL_Setup.parse_mml_opengl(child);
            }
        }

        public static bool ParseMMLFromFile(FileSpecifier FileSpec, bool load_menu_mml_only)
        {
            var parse_error = false;
            try
            {
                var fileroot = InfoTree.load_xml(FileSpec);
                _ParseAllMML(fileroot, load_menu_mml_only);
            }
            catch (InfoTree.parse_error ex)
            {
                Logging.logError($"Error parsing MML file ({FileSpec.GetPath()}): {ex.Message}");
                parse_error = true;
            }
            catch (InfoTree.path_error ep)
            {
                Logging.logError($"Path error parsing MML file ({FileSpec.GetPath()}): {ep.Message}");
                parse_error = true;
            }

            return !parse_error;
        }

        public static bool ParseMMLFromData(byte[] buffer, int buflen)
        {
            var parse_error = false;
            try
            {
                var fileroot = InfoTree.load_xml(buffer, buflen);
                _ParseAllMML(fileroot, false);
            }
            catch (InfoTree.parse_error ex)
            {
                Logging.logError($"Error parsing MML data: {ex.Message}");
                parse_error = true;
            }
            catch (InfoTree.path_error ep)
            {
                Logging.logError($"Path error parsing MML data: {ep.Message}");
                parse_error = true;
            }

            return !parse_error;
        }
    }
}
