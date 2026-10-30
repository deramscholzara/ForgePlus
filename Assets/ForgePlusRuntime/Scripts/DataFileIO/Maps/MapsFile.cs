using AlephOne;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForgePlus.Extensions;

namespace ForgePlus.DataFileIO
{
    public class MapsFile : IFileLoadable
    {
        // The PICT Aleph One shows as a level starts is this + the level's index (interface.cpp: show_movie)
        public const short ChapterScreenBase = 1500;

        // Deepest first: its 32-bit, 16-bit and 8-bit versions (images.cpp: _scenario_file_delta32/16)
        public static readonly short[] ChapterScreenDepthOffsets = { 20000, 10000, 0 };

        // An inter-level teleport to this ends the game (interface.cpp: EPILOGUE_LEVEL_NUMBER), as Marathon Infinity's last
        // terminals use it
        public const short EpilogueLevel = 256;

        private static readonly uint[] PhysicsTags =
        {
            tags.MONSTER_PHYSICS_TAG, tags.EFFECTS_PHYSICS_TAG, tags.PROJECTILE_PHYSICS_TAG, tags.PHYSICS_PHYSICS_TAG,
            tags.WEAPONS_PHYSICS_TAG, tags.M1_MONSTER_PHYSICS_TAG, tags.M1_EFFECTS_PHYSICS_TAG,
            tags.M1_PROJECTILE_PHYSICS_TAG, tags.M1_PHYSICS_PHYSICS_TAG, tags.M1_WEAPONS_PHYSICS_TAG,
        };

        private int embeddedPhysicsLevelCount = -1;

        public FileSpecifier File { get; private set; }

        public List<directory_data> Levels { get; } = new List<directory_data>();

        // As it was loaded
        public wad_header Header { get; private set; }

        // The header's name (Mac Roman, with room for its NUL), kept when the checksum is. Marathon doesn't show it.
        public byte[] Name { get; } = new byte[wad.MAXIMUM_WADFILE_NAME_LENGTH];

        // The rest of the Mac file (such as the terminals' pictures), which saving carries through
        public MacFileForks Forks { get; private set; }

        // Levels with physics saved in them (which Marathon Infinity and Aleph One use instead of the physics file)
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

        // Rereads the directory, dropping a discarded level's edits. Returns whether any level's name changed.
        public bool ReloadDirectory()
        {
            var previousNames = Levels.ConvertAll(level => level.GetLevelName());

            game_wad.get_level_directory(File, Levels);

            return !previousNames.SequenceEqual(Levels.ConvertAll(level => level.GetLevelName()));
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

            // The detached flag is left from a feature Bungie never finished, and Aleph One asserts on it
            foreach (var polygon in level.PolygonList)
            {
                map.SET_POLYGON_DETACHED_STATE(polygon, false);
            }

            return level;
        }

        // At the deepest bit depth the map has it (or -1, if it hasn't one)
        public short ChapterScreenPictureId(int levelIndex)
        {
            var resources = Forks.ResourceFork;

            foreach (var offset in ChapterScreenDepthOffsets)
            {
                var id = (short) (ChapterScreenBase + levelIndex + offset);
                if (resources != null && resources.Has(MacResourceFork.PictType, id))
                {
                    return id;
                }
            }

            return -1;
        }

        public bool IsChapterScreenShown(int levelIndex)
        {
            var id = ChapterScreenPictureId(levelIndex);

            return id >= 0 && !Forks.IsResourceRemoved(MacResourceFork.PictType, id);
        }

        // Turning it off leaves its pictures and color tables, at every bit depth, out of the saved resource fork
        public void SetChapterScreenShown(int levelIndex, bool isShown)
        {
            if (!Forks.CanEditResources)
            {
                return;
            }

            foreach (var offset in ChapterScreenDepthOffsets)
            {
                var id = (short) (ChapterScreenBase + levelIndex + offset);
                if (Forks.ResourceFork.Has(MacResourceFork.PictType, id))
                {
                    Forks.SetResourceRemoved(MacResourceFork.PictType, id, !isShown);
                }

                if (Forks.ResourceFork.Has(MacResourceFork.ClutType, id))
                {
                    Forks.SetResourceRemoved(MacResourceFork.ClutType, id, !isShown);
                }
            }
        }

        // As level 0 of a new map file. Without resources, it has no resource fork. Afterward, this refers to the saved file.
        public void SaveAsSingleLevelFile(MapLevel level, int levelIndex, string savePath, bool withPhysics, bool withResources)
        {
            var resourceFork = withResources ? SingleLevelResources.ResourceForkFor(this, level, levelIndex) : new byte[0];
            var excludedTags = withPhysics ? null : PhysicsTags;

            Save(savePath, saveFile => game_wad.save_level(saveFile, level, excluded_tags: excludedTags), resourceFork);
        }

        // Every level, with the open one as edited. Keeping the checksum keeps the header's name too. Afterward, this
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

        // A new map file with just the level (a map file must have at least one: wad.cs: read_wad_header)
        public static void CreateNew(string savePath, MapLevel firstLevel)
        {
            if (!game_wad.save_level(new FileSpecifier(savePath), firstLevel))
            {
                throw new IOException($"A new map file could not be created at \"{savePath}\" ({DescribeGameError()}).");
            }
        }

        // The level, after the others, which are saved as they are in the file (so the open level's unsaved edits stay
        // unsaved). Its contents change, so its checksum is regenerated. Afterward, this refers to the saved file.
        public void AddLevel(MapLevel level)
        {
            var levels = new List<MapLevel>(Levels.Count + 1);
            for (var levelIndex = 0; levelIndex < Levels.Count; levelIndex++)
            {
                levels.Add(LoadLevel(levelIndex));
            }

            levels.Add(level);

            var savePath = File.GetPath();
            Save(savePath, saveFile => game_wad.save_levels(saveFile, levels));
        }

        // The level, taken out of the file, with the others saved as they are in the file (as AddLevel does). Later levels
        // move up one, so their chapter screens' pictures (and color tables) are renumbered to follow them, and the level's
        // own are left out. The other levels' terminals are kept pointing where they did: their inter-level teleports to
        // later levels, and their pictures that were renumbered. Its contents change, so its checksum is regenerated.
        // Afterward, this refers to the saved file.
        public void DeleteLevel(int deletedLevelIndex)
        {
            if (deletedLevelIndex < 0 || deletedLevelIndex >= Levels.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(deletedLevelIndex));
            }

            if (Levels.Count <= 1)
            {
                throw new InvalidOperationException("A map file must have at least one level.");
            }

            var oldLevelCount = Levels.Count;

            // The ID the resource has once the level's gone (or null, if it's left out)
            short? NewResourceId(uint type, short id)
            {
                if (Forks.IsResourceRemoved(type, id))
                {
                    return null;
                }

                if (type != MacResourceFork.PictType && type != MacResourceFork.ClutType)
                {
                    return id;
                }

                foreach (var offset in ChapterScreenDepthOffsets)
                {
                    var levelIndex = id - ChapterScreenBase - offset;
                    if (levelIndex >= 0 && levelIndex < oldLevelCount)
                    {
                        if (levelIndex == deletedLevelIndex)
                        {
                            return null;
                        }

                        return levelIndex > deletedLevelIndex ? (short) (id - 1) : id;
                    }
                }

                return id;
            }

            var levels = new List<MapLevel>(oldLevelCount - 1);
            for (var levelIndex = 0; levelIndex < oldLevelCount; levelIndex++)
            {
                if (levelIndex == deletedLevelIndex)
                {
                    continue;
                }

                // Only pictures that are renumbered (which needs an editable resource fork that has them)
                var level = LoadLevel(levelIndex);
                RetargetTerminals(level, deletedLevelIndex, pictId => Forks.CanEditResources && Forks.ResourceFork.Has(MacResourceFork.PictType, pictId) ? NewResourceId(MacResourceFork.PictType, pictId) : pictId);
                levels.Add(level);
            }

            var resourceFork = Forks.ResourceFork?.Rebuild(NewResourceId);

            var savePath = File.GetPath();
            Save(savePath, saveFile => game_wad.save_levels(saveFile, levels), resourceFork);
        }

        // Inter-level teleports past the deleted level go one level lower (one to the deleted level now goes to the level
        // that followed it), and pictures go to their new IDs (a picture that's left out stays as it was)
        private static void RetargetTerminals(MapLevel level, int deletedLevelIndex, Func<short, short?> newPictId)
        {
            foreach (var terminal in level.map_terminal_text)
            {
                foreach (var group in terminal.groupings)
                {
                    switch (group.type)
                    {
                        case computer_interface._interlevel_teleport_group:
                            if (group.permutation > deletedLevelIndex && group.permutation != EpilogueLevel)
                            {
                                group.permutation--;
                            }

                            break;
                        case computer_interface._pict_group:
                            group.permutation = newPictId(group.permutation) ?? group.permutation;
                            break;
                    }
                }
            }
        }

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

        // The save may replace this file, so its scenario images (read from it while open) are closed meanwhile
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
