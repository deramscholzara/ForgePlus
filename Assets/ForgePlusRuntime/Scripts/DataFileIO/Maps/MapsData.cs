using ForgePlus.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForgePlus.DataFileIO
{
    public class MapsData : FileDataBase<MapsFile>
    {
        private LevelData currentlyOpenLevel;

        public MapsFile MapsFile
        {
            get
            {
                return file;
            }
        }

        // -1 while none is open
        public int OpenLevelIndex
        {
            get
            {
                return currentlyOpenLevel != null ? currentlyOpenLevel.LevelIndex : -1;
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

        // Destroys the open level's runtime objects, keeping its data (and its edits)
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

        // Returns whether discarding the level's unsaved edits changed a level's name in the map's directory
        public bool CloseAndUnloadCurrentLevel()
        {
            if (currentlyOpenLevel == null)
            {
                // Nothing currently open, so exit
                return false;
            }

            currentlyOpenLevel.UnloadData();

            return file != null && file.ReloadDirectory();
        }

        public void SaveCurrentLevel(string savePath, bool withPhysics, bool withResources)
        {
            if (currentlyOpenLevel == null)
            {
                throw new IOException($"Tried saving Level with no LevelData loaded.");
            }

            currentlyOpenLevel.SaveAsSingleLevelFile(savePath, withPhysics, withResources);
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
