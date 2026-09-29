using ForgePlus.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForgePlus.DataFileIO
{
    public class MapsData : FileDataBase<MapsFile>
    {
        private LevelData currentlyOpenLevel;

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
    }
}
