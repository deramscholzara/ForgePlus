using ForgePlus.LevelManipulation;
using System.Collections.Generic;
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
                    if (currentLevelNames == null)
                    {
                        currentLevelNames = file.Overlays.Select(item => item.Value.LevelName).ToArray();
                    }

                    return currentLevelNames;
                }
            }
        }

        public void OpenLevel(int levelIndex)
        {
            LoadData();

            CloseAndUnloadCurrentLevel();

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

        protected override void PreUnloadDataCleanup()
        {
            CloseAndUnloadCurrentLevel();
        }
    }
}
