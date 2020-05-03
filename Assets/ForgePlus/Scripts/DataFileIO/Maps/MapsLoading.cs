using ForgePlus.ApplicationGeneral;
using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.DataFileIO
{
    public class MapsLoading : FileLoadingBase<MapsLoading, MapsData, MapFile>
    {
        public IReadOnlyCollection<string> LevelNames
        {
            get
            {
                if (data == null)
                {
                    return null;
                }

                return data.LevelNames;
            }
        }

        protected override DataFileTypes DataFileType
        {
            get
            {
                return DataFileTypes.Maps;
            }
        }

        public async void OpenLevel(int levelIndex = 0)
        {
            UIBlocking.Instance.Block();

            LoadFile(forceReload: false);

            if (data == null)
            {
                Debug.LogError($"Tried opening level index {levelIndex} with no loaded Maps file.  You may need to call LoadData() first.");
                // No maps data is loaded, so exit
                return;
            }

            await data.OpenLevel(levelIndex);

            UIBlocking.Instance.Unblock();
        }

        public void CloseLevel()
        {
            if (data == null)
            {
                Debug.LogError("Tried closing level with no loaded maps file.");
                // No maps data is loaded, so exit
                return;
            }

            data.CloseAndUnloadCurrentLevel();
        }

        protected override void OnLoadedChanged()
        {
            MapsSaving.Instance.CurrentMapsData = data;
        }
    }
}
