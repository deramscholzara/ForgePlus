using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;

namespace RuntimeCore.Entities.MapObjects
{
    // One high-level shape drawn as part of a map object's sprite
    public readonly struct SpriteLayer
    {
        public readonly byte Collection;
        public readonly byte CLUT;
        public readonly short HighLevelShape;

        public SpriteLayer(int collection, int clut, short highLevelShape)
        {
            Collection = (byte) collection;
            CLUT = (byte) clut;
            HighLevelShape = highLevelShape;
        }

        public ushort ShapeDescriptor
        {
            get
            {
                return AlephOneExtensions.BuildShapeDescriptor(Collection, HighLevelShape, CLUT);
            }
        }

        public override string ToString()
        {
            return $"{Collection}.{CLUT}.{HighLevelShape}";
        }
    }

    // Layers after the first attach at the previous layer's keypoint (like the player's torso on its legs)
    public class SpriteDefinition
    {
        public readonly SpriteLayer[] Layers;
        public readonly float Scale;
        public readonly string Key;

        public SpriteDefinition(float scale, params SpriteLayer[] layers)
        {
            Layers = layers;
            Scale = scale;
            Key = $"{string.Join<SpriteLayer>("+", layers)}@{scale}";
        }
    }

    // The stationary sprite Aleph One gives each placeable map object type
    public static class MapObjectSpriteDefinitions
    {
        // Tiny monsters are drawn at half size (RenderPlaceObjs.cpp)
        private const float TinyScale = 0.5f;

        // Null for objects with no visible sprite (sound sources, goals, and unrecognized types)
        public static SpriteDefinition GetDefinition(map_object mapObject)
        {
            switch (mapObject.type)
            {
                case map._saved_player:
                    // A player start's index is its team (game_wad.cpp: get_player_starting_location_and_facing), which
                    // picks the CLUTs (player.cpp: set_player_shapes), and players start with the pistol
                    var playerShapes = player.get_player_shape_definitions();
                    var teamCLUT = mapObject.index >= 0 && mapObject.index < shape_descriptors.MAXIMUM_CLUTS_PER_COLLECTION ? mapObject.index : 0;

                    return new SpriteDefinition(
                        1f,
                        new SpriteLayer(playerShapes.collection, teamCLUT, playerShapes.legs[player._player_stationary]),
                        new SpriteLayer(playerShapes.collection, teamCLUT, playerShapes.torsos[weapons._weapon_pistol]));

                case map._saved_monster:
                    var monsterDefinitions = PhysicsLoading.Instance.Model.monster_definitions;
                    var monster = csmacros.GetMemberWithBounds(monsterDefinitions, mapObject.index, monsterDefinitions.Length);
                    if (monster == null || monster.collection == cstypes.NONE)
                    {
                        return null;
                    }

                    // A monster's collection also carries its CLUT
                    return new SpriteDefinition(
                        (monster.flags & monster_definitions._monster_is_tiny) != 0 ? TinyScale : 1f,
                        new SpriteLayer(
                            shape_descriptors.GET_COLLECTION(monster.collection),
                            shape_descriptors.GET_COLLECTION_CLUT(monster.collection),
                            (short) monster.stationary_shape));

                case map._saved_item:
                    var itemDefinition = items.get_item_definition(mapObject.index);
                    if (itemDefinition == null)
                    {
                        return null;
                    }

                    return GetDefinition(itemDefinition.base_shape);

                case map._saved_object:
                    var sceneryDefinition = scenery.get_scenery_definition(mapObject.index);
                    if (sceneryDefinition == null)
                    {
                        return null;
                    }

                    return GetDefinition(sceneryDefinition.shape);

                default:
                    return null;
            }
        }

        private static SpriteDefinition GetDefinition(ushort shapeDescriptor)
        {
            if (shapeDescriptor.IsEmptyShapeDescriptor())
            {
                return null;
            }

            return new SpriteDefinition(1f, new SpriteLayer(shapeDescriptor.GetCollection(), shapeDescriptor.GetCLUT(), (short) shapeDescriptor.GetShape()));
        }
    }
}
