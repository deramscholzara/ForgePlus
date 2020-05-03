using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Weland;

namespace ForgePlus.DataFileIO
{
    public class MapsData : FileDataBase<MapFile>
    {
        private LevelData currentlyOpenLevel;

        private string[] currentLevelNames = null;

        public IReadOnlyCollection<string> LevelNames
        {
            get
            {
                LoadData();

                if (file == null)
                {
                    return new string[] { };
                }
                else
                {
                    return file.Overlays.Select(item => item.Value.LevelName).ToArray();
                }
            }
        }

        public async Task OpenLevel(int levelIndex)
        {
            LoadData();

            CloseAndUnloadCurrentLevel();

            currentlyOpenLevel = new LevelData(levelIndex, file);

            await currentlyOpenLevel.OpenLevel();
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

            file.Directory.Clear();
            file.Directory[0] = currentlyOpenLevel.GetSaveWad();
            file.Save(savePath);

            var levelOverlay = file.Overlays[currentlyOpenLevel.LevelIndex];
            file.Overlays.Clear();
            file.Overlays[currentlyOpenLevel.LevelIndex] = levelOverlay;
        }

        protected override void PreUnloadDataCleanup()
        {
            CloseAndUnloadCurrentLevel();
        }
    }
}
