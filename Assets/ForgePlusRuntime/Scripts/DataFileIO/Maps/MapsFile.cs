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

            return level;
        }

        // Just the level, in a map file of its own (with its own checksum and name, as a new file). Afterward, this refers
        // to the saved file.
        public void SaveAsSingleLevelFile(MapLevel level, string savePath)
        {
            Save(savePath, saveFile => game_wad.save_level(saveFile, level));
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
        private void Save(string savePath, Func<FileSpecifier, bool> save)
        {
            var saveFile = new FileSpecifier(savePath);

            Close();

            try
            {
                if (!save(saveFile))
                {
                    throw new IOException($"Levels could not be saved to \"{savePath}\" ({DescribeGameError()}).");
                }

                Forks.WriteAround(savePath);
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
