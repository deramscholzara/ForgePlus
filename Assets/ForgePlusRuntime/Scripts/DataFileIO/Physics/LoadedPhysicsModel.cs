using AlephOne;
using System.Collections.Generic;

namespace ForgePlus.DataFileIO
{
    public enum PhysicsModelSource
    {
        // Also used when a physics file can't be read
        EngineDefaults,

        // The rest overwrite the built-in definitions
        PhysicsFile,
        Marathon1PhysicsFile,
        EmbeddedInLevel,
    }

    // A physics model, loaded as Aleph One loads it, and where it came from
    public sealed class LoadedPhysicsModel
    {
        private static readonly uint[] PhysicsTags =
        {
            tags.MONSTER_PHYSICS_TAG,
            tags.EFFECTS_PHYSICS_TAG,
            tags.PROJECTILE_PHYSICS_TAG,
            tags.PHYSICS_PHYSICS_TAG,
            tags.WEAPONS_PHYSICS_TAG,
        };

        private LoadedPhysicsModel(PhysicsModel model, PhysicsModelSource source, string filePath)
        {
            Model = model;
            Source = source;
            FilePath = filePath;
        }

        public PhysicsModel Model { get; }

        public PhysicsModelSource Source { get; }

        // PhysicsFile and Marathon1PhysicsFile only
        public string FilePath { get; }

        public static LoadedPhysicsModel FromDefaults()
        {
            var model = new PhysicsModel();
            import_definitions.init_physics_wad_data(model);

            return new LoadedPhysicsModel(model, PhysicsModelSource.EngineDefaults, filePath: null);
        }

        // The built-in model if the file can't be read (import_definition_structures)
        public static LoadedPhysicsModel FromPhysicsFile(string path)
        {
            var file = new FileSpecifier(path);

            PhysicsModelSource source;
            if (import_definitions.physics_file_is_m1(file))
            {
                source = PhysicsModelSource.Marathon1PhysicsFile;
            }
            else
            {
                var physicsWad = import_definitions.get_physics_wad_data(file, out _);
                source = physicsWad != null ? PhysicsModelSource.PhysicsFile : PhysicsModelSource.EngineDefaults;
            }

            var model = new PhysicsModel();
            import_definitions.import_definition_structures(model, file);

            return new LoadedPhysicsModel(model, source, source == PhysicsModelSource.EngineDefaults ? null : path);
        }

        // The level's embedded physics if it has any, otherwise the physics file's (process_map_wad)
        public static LoadedPhysicsModel ForLevel(MapLevel level, LoadedPhysicsModel physicsFileModel)
        {
            return ForLevel(level.loaded_wad.preserved_chunks, physicsFileModel);
        }

        public static LoadedPhysicsModel ForLevel(IReadOnlyDictionary<uint, byte[]> levelChunks, LoadedPhysicsModel physicsFileModel)
        {
            var levelWad = wad.create_empty_wad();
            foreach (var tag in PhysicsTags)
            {
                // append_data_to_wad can't append an empty chunk, which counts as absent anyway
                if (levelChunks.TryGetValue(tag, out var data) && data != null && data.Length > 0)
                {
                    levelWad = wad.append_data_to_wad(levelWad, tag, data, data.Length, 0);
                }
            }

            var model = new PhysicsModel();
            if (game_wad.process_map_wad_physics(levelWad, model))
            {
                return new LoadedPhysicsModel(model, PhysicsModelSource.EmbeddedInLevel, filePath: null);
            }

            return physicsFileModel;
        }
    }
}
