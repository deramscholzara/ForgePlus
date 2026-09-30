using AlephOne;
using System.Collections.Generic;
using System.IO;

namespace ForgePlus.DataFileIO
{
    public class MapsFile : IFileLoadable
    {
        public FileSpecifier File { get; private set; }

        public List<directory_data> Levels { get; } = new List<directory_data>();

        public void Load(string fileName)
        {
            File = new FileSpecifier(fileName);

            if (!game_wad.get_level_directory(File, Levels))
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon map file ({DescribeGameError()}).");
            }

            // As Aleph One's set_map_file does, for the terminals' pictures (with its default 32-bit interface)
            ScenarioPictures.Clear();
            screen.interface_bit_depth = 32;
            images.set_scenario_images_file(File);
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

        // Afterward, this refers to the saved file
        public void SaveAsSingleLevelFile(MapLevel level, string savePath)
        {
            var saveFile = new FileSpecifier(savePath);

            if (!game_wad.save_level(saveFile, level))
            {
                throw new IOException($"Level could not be saved to \"{savePath}\" ({DescribeGameError()}).");
            }

            Load(savePath);
        }

        private static string DescribeGameError()
        {
            var error = game_errors.get_game_error(out var type);
            return type == game_errors.gameError ? $"game error {error}" : $"system error {error}";
        }
    }
}
