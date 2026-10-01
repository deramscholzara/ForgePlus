using ForgePlus.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForgePlus.DataFileIO
{
    public class MapsData : FileDataBase<MapsFile>
    {
        private LevelData currentlyOpenLevel;

        // The loaded map file (null until it's loaded)
        public MapsFile MapsFile
        {
            get
            {
                return file;
            }
        }

        public IReadOnlyCollection<string> LevelNames
        {
            get
            {
                LoadData();

                return file == null ? null : file.Levels.Select(level => level.GetLevelName()).ToArray();
            }
        }

        public void OpenLevel(int levelIndex)
        {
            LoadData();

            currentlyOpenLevel = new LevelData(levelIndex, file);

            currentlyOpenLevel.OpenLevel();
        }

        // Destroys and rebuilds the open level's runtime objects from its data, keeping the data (and its edits)
        public void CloseCurrentLevelObjects()
        {
            currentlyOpenLevel?.CloseLevel();
        }

        public void ReopenCurrentLevelObjects()
        {
            currentlyOpenLevel?.OpenLevel();
        }

        public void CloseFile()
        {
            file?.Close();
        }

        public void CloseAndUnloadCurrentLevel()
        {
            if (currentlyOpenLevel == null)
            {
                // Nothing currently open, so exit
                return;
            }

            currentlyOpenLevel.UnloadData();
        }

        public void SaveCurrentLevel(string savePath)
        {
            if (currentlyOpenLevel == null)
            {
                throw new IOException($"Tried saving Level with no LevelData loaded.");
            }

            currentlyOpenLevel.SaveAsSingleLevelFile(savePath);
        }

        public void SaveMerged(string savePath, bool keepChecksum)
        {
            if (currentlyOpenLevel == null)
            {
                throw new IOException($"Tried saving merged levels with no LevelData loaded.");
            }

            currentlyOpenLevel.SaveMerged(savePath, keepChecksum);
        }
    }
}
