using AlephOne;
using System;
using System.Collections.Generic;
using System.IO;

namespace ForgePlus.DataFileIO
{
    public class MapsFile : IFileLoadable
    {
        public FileSpecifier File { get; private set; }

        public List<directory_data> Levels { get; } = new List<directory_data>();

        // The file's wad header, as it was loaded
        public wad_header Header { get; private set; }

        // The name in the header (Mac Roman, with room for its NUL), which saving keeps along with the checksum (and
        // otherwise names after the saved file). Marathon doesn't show it.
        public byte[] Name { get; } = new byte[wad.MAXIMUM_WADFILE_NAME_LENGTH];

        // The rest of the Mac file (such as its resource fork, with the terminals' pictures), which saving carries through
        public MacFileForks Forks { get; private set; }

        public void Load(string fileName)
        {
            File = new FileSpecifier(fileName);

            if (!game_wad.get_level_directory(File, Levels))
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon map file ({DescribeGameError()}).");
            }

            Header = ReadHeader(File);
            Array.Copy(Header.file_name, Name, Name.Length);
            Forks = MacFileForks.Read(fileName);
            embeddedPhysicsLevelCount = -1;

            OpenScenarioImages();
        }

        public void Close()
        {
            ScenarioPictures.Clear();
            images.unset_scenario_images_file();
        }

        public MapLevel LoadLevel(int levelIndex)
        {
            if (!game_wad.load_level_from_map(File, (short) levelIndex, out var level))
            {
                throw new IOException($"Level {levelIndex} of \"{File.GetPath()}\" could not be loaded ({DescribeGameError()}).");
            }

            // No editor sets the detached flag (it's left from a feature Bungie never finished), and Aleph One asserts on
            // detached polygons, so it's cleared (and the level saved without it)
            foreach (var polygon in level.PolygonList)
            {
                map.SET_POLYGON_DETACHED_STATE(polygon, false);
            }

            return level;
        }

        // How many of the levels have physics saved in them (which the original Marathon Infinity and Aleph One use
        // instead of the physics file), read from the file the first time it's asked
        public int EmbeddedPhysicsLevelCount
        {
            get
            {
                if (embeddedPhysicsLevelCount < 0)
                {
                    embeddedPhysicsLevelCount = CountEmbeddedPhysicsLevels();
                }

                return embeddedPhysicsLevelCount;
            }
        }

        private int embeddedPhysicsLevelCount = -1;

        private static readonly uint[] PhysicsTags =
        {
            tags.MONSTER_PHYSICS_TAG, tags.EFFECTS_PHYSICS_TAG, tags.PROJECTILE_PHYSICS_TAG, tags.PHYSICS_PHYSICS_TAG,
            tags.WEAPONS_PHYSICS_TAG, tags.M1_MONSTER_PHYSICS_TAG, tags.M1_EFFECTS_PHYSICS_TAG,
            tags.M1_PROJECTILE_PHYSICS_TAG, tags.M1_PHYSICS_PHYSICS_TAG, tags.M1_WEAPONS_PHYSICS_TAG,
        };

        private int CountEmbeddedPhysicsLevels()
        {
            var count = 0;
            var openedFile = new OpenedFile();

            if (wad.open_wad_file_for_reading(File, openedFile))
            {
                var header = new wad_header();
                if (wad.read_wad_header(openedFile, header))
                {
                    for (short index = 0; index < header.wad_count; index++)
                    {
                        var levelWad = wad.read_indexed_wad_from_file(openedFile, header, index, true);
                        if (levelWad != null && Array.Exists(PhysicsTags, tag => wad.extract_type_from_wad(levelWad, tag, out var length) != null && length > 0))
                        {
                            count++;
                        }
                    }
                }

                wad.close_wad_file(openedFile);
            }

            return count;
        }

        // ---------- Chapter screens: the picture Aleph One shows as a level starts (show_movie's chapter heading,
        // interface.cpp), the map's PICT resource 1500 + the level's index, at the deepest bit depth it has (its 32-bit
        // and 16-bit versions are 20000 and 10000 more, images.cpp's _scenario_file_delta32/16)

        private const short ChapterScreenBase = 1500;
        private static readonly short[] ChapterScreenDepthOffsets = { 20000, 10000, 0 };
        private static readonly uint PictType = 0x50494354; // 'PICT'
        private static readonly uint ClutType = 0x636c7574; // 'clut'

        // The level's chapter screen's picture ID, at the deepest bit depth the map has it (or -1, if it hasn't one)
        public short ChapterScreenPictureId(int levelIndex)
        {
            var resources = Forks.ResourceFork;

            foreach (var offset in ChapterScreenDepthOffsets)
            {
                var id = (short) (ChapterScreenBase + levelIndex + offset);
                if (resources != null && resources.Has(PictType, id))
                {
                    return id;
                }
            }

            return -1;
        }

        // Whether it's saved (it isn't once it's turned off, until it's turned on again)
        public bool IsChapterScreenShown(int levelIndex)
        {
            var id = ChapterScreenPictureId(levelIndex);

            return id >= 0 && !Forks.IsResourceRemoved(PictType, id);
        }

        // Turning it off leaves the chapter screen's pictures (every bit depth's, and their color tables) out of the
        // saved resource fork, so Aleph One shows none for the level
        public void SetChapterScreenShown(int levelIndex, bool isShown)
        {
            if (!Forks.CanEditResources)
            {
                return;
            }

            foreach (var offset in ChapterScreenDepthOffsets)
            {
                var id = (short) (ChapterScreenBase + levelIndex + offset);
                if (Forks.ResourceFork.Has(PictType, id))
                {
                    Forks.SetResourceRemoved(PictType, id, !isShown);
                }

                if (Forks.ResourceFork.Has(ClutType, id))
                {
                    Forks.SetResourceRemoved(ClutType, id, !isShown);
                }
            }
        }

        // Just the level, in a map file of its own (with its own checksum and name, as a new file), as level 0. With physics,
        // it has the physics saved in the level; with resources, it has those of the map file's it uses
        // (SingleLevelResources), and otherwise no resource fork. Afterward, this refers to the saved file.
        public void SaveAsSingleLevelFile(MapLevel level, int levelIndex, string savePath, bool withPhysics, bool withResources)
        {
            var resourceFork = withResources ? SingleLevelResources.ResourceForkFor(this, level, levelIndex) : new byte[0];
            var excludedTags = withPhysics ? null : PhysicsTags;

            Save(savePath, saveFile => game_wad.save_level(saveFile, level, excluded_tags: excludedTags), resourceFork);
        }

        // Every level of the map file in one file, in order: the open level as edited, and the others as the file has them
        // (each written as saving a level writes it, so its unchanged chunks are as they were). When the checksum is kept,
        // the name is too, so the header is the loaded file's; otherwise both are the saved file's own. Afterward, this
        // refers to the saved file.
        public void SaveMerged(MapLevel openLevel, int openLevelIndex, string savePath, bool keepChecksum)
        {
            var levels = new List<MapLevel>(Levels.Count);
            for (var levelIndex = 0; levelIndex < Levels.Count; levelIndex++)
            {
                levels.Add(levelIndex == openLevelIndex ? openLevel : LoadLevel(levelIndex));
            }

            var checksum = keepChecksum ? Header.checksum : (uint?) null;
            var name = keepChecksum ? Name : null;

            Save(savePath, saveFile => game_wad.save_levels(saveFile, levels, file_name: name, checksum: checksum));
        }

        // Writes the data fork (where the wads are) with the save, then the rest of this file around it. The save may
        // replace this file, so its scenario images (which are read from it while it's open) are closed meanwhile.
        private void Save(string savePath, Func<FileSpecifier, bool> save, byte[] resourceFork = null)
        {
            var saveFile = new FileSpecifier(savePath);

            Close();

            try
            {
                if (!save(saveFile))
                {
                    throw new IOException($"Levels could not be saved to \"{savePath}\" ({DescribeGameError()}).");
                }

                Forks.WriteAround(savePath, resourceFork);
            }
            catch
            {
                OpenScenarioImages();
                throw;
            }

            Load(savePath);
        }

        private static wad_header ReadHeader(FileSpecifier file)
        {
            var header = new wad_header();
            var openedFile = new OpenedFile();

            if (!wad.open_wad_file_for_reading(file, openedFile))
            {
                throw new IOException($"\"{file.GetPath()}\" could not be opened ({DescribeGameError()}).");
            }

            var isRead = wad.read_wad_header(openedFile, header);
            wad.close_wad_file(openedFile);

            if (!isRead)
            {
                throw new IOException($"The header of \"{file.GetPath()}\" could not be read ({DescribeGameError()}).");
            }

            return header;
        }

        // As Aleph One's set_map_file does, for the terminals' pictures (with its default 32-bit interface)
        private void OpenScenarioImages()
        {
            ScenarioPictures.Clear();
            screen.interface_bit_depth = 32;
            images.set_scenario_images_file(File);
        }

        private static string DescribeGameError()
        {
            var error = game_errors.get_game_error(out var type);
            return type == game_errors.gameError ? $"game error {error}" : $"system error {error}";
        }
    }
}
