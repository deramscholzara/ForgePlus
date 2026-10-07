#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Inspection;
using RuntimeCore.Entities;

namespace ForgePlus.LevelManipulation
{
    // Changes a level's walls or landscape collection, optionally moving what's drawn from the previous one to the new
    // one (otherwise only Aleph One still loads the previous one), then rebuilds the level to load the new textures
    public static class LevelCollectionEditing
    {
        public static void SetEnvironment(short environmentCode, bool moveTextures)
        {
            var level = LevelEntity_Level.Instance.Level;
            var previousCollection = CollectionChoices.WallCollectionOf(level.static_world.environment_code);
            var newCollection = CollectionChoices.WallCollectionOf(environmentCode);

            level.static_world.environment_code = environmentCode;

            if (moveTextures && previousCollection != newCollection)
            {
                MoveTextures(level, previousCollection, newCollection);
                MoveControlPanels(level, previousCollection, newCollection);
                MoveMedias(level, previousCollection, newCollection);
            }

            LevelEditing.RebuildLevel();
        }

        public static void SetLandscape(short songIndex, bool moveTextures)
        {
            var level = LevelEntity_Level.Instance.Level;
            var previousCollection = CollectionChoices.LandscapeCollectionOf(level.static_world.song_index);
            var newCollection = CollectionChoices.LandscapeCollectionOf(songIndex);

            level.static_world.song_index = songIndex;

            if (moveTextures && previousCollection != newCollection)
            {
                MoveTextures(level, previousCollection, newCollection);
            }

            LevelEditing.RebuildLevel();
        }

        // To the same bitmap and color table of the new collection
        private static void MoveTextures(MapLevel level, int previousCollection, int newCollection)
        {
            foreach (var polygon in level.PolygonList)
            {
                polygon.floor_texture = Moved(polygon.floor_texture, previousCollection, newCollection);
                polygon.ceiling_texture = Moved(polygon.ceiling_texture, previousCollection, newCollection);
            }

            foreach (var side in level.SideList)
            {
                side.primary_texture.texture = Moved(side.primary_texture.texture, previousCollection, newCollection);
                side.secondary_texture.texture = Moved(side.secondary_texture.texture, previousCollection, newCollection);
                side.transparent_texture.texture = Moved(side.transparent_texture.texture, previousCollection, newCollection);
            }
        }

        private static ushort Moved(ushort shapeDescriptor, int previousCollection, int newCollection)
        {
            return !shapeDescriptor.IsEmptyShapeDescriptor() && shapeDescriptor.GetCollection() == previousCollection ?
                   AlephOneExtensions.BuildShapeDescriptor(newCollection, shapeDescriptor.GetShape(), shapeDescriptor.GetCLUT()) :
                   shapeDescriptor;
        }

        // To the new environment's panel type of the same kind, with its texture (devices.cpp: set_control_panel_texture)
        private static void MoveControlPanels(MapLevel level, int previousCollection, int newCollection)
        {
            foreach (var side in level.SideList)
            {
                if (!map.SIDE_IS_CONTROL_PANEL(side))
                {
                    continue;
                }

                var definition = devices.get_control_panel_definition(side.control_panel_type);
                if (definition == null || definition.collection != previousCollection)
                {
                    continue;
                }

                for (short type = 0; type < devices.NUMBER_OF_CONTROL_PANEL_DEFINITIONS; type++)
                {
                    var candidate = devices.get_control_panel_definition(type);
                    if (candidate.collection == newCollection && IsSameKind(candidate, definition))
                    {
                        side.control_panel_type = type;
                        devices.set_control_panel_texture(side);
                        break;
                    }
                }
            }
        }

        private static bool IsSameKind(control_panel_definition a, control_panel_definition b)
        {
            return a._class == b._class &&
                   (a.item != cstypes.NONE) == (b.item != cstypes.NONE) &&
                   IsBreakable(a) == IsBreakable(b);
        }

        private static bool IsBreakable(control_panel_definition definition)
        {
            return definition.sounds[devices._activating_sound] == SoundManagerEnums._snd_destroy_control_panel;
        }

        // A media type's texture is from a walls collection (media.cpp: media_definitions)
        private static void MoveMedias(MapLevel level, int previousCollection, int newCollection)
        {
            foreach (var mediaData in level.MediaList)
            {
                var definition = media.get_media_definition(mediaData.type);
                if (definition == null || definition.collection != previousCollection)
                {
                    continue;
                }

                for (short type = 0; type < media.NUMBER_OF_MEDIA_TYPES; type++)
                {
                    if (media.get_media_definition(type).collection == newCollection)
                    {
                        mediaData.type = type;
                        break;
                    }
                }
            }
        }
    }
}
#endif
