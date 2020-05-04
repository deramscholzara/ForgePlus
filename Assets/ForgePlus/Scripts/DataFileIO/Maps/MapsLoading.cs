using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation;
using System;
using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.DataFileIO
{
    public partial class MapsLoading : FileLoadingBase<MapsLoading, MapsData, MapFile>
    {
        private event Action<string> OnLevelOpened_Sender;

        public event Action<string> OnLevelOpened
        {
            add
            {
                OnLevelOpened_Sender += value;

                value.Invoke(FPLevel.Instance ? FPLevel.Instance.Level.Name : null);
            }
            remove
            {
                OnLevelOpened_Sender -= value;
            }
        }

        public event Action OnLevelClosed;

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

        public override void UnloadFile()
        {
            CloseLevel();

            base.UnloadFile();
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

            OnLevelOpened_Sender?.Invoke(FPLevel.Instance.Level.Name);

            UIBlocking.Instance.Unblock();
        }

        public void CloseLevel()
        {
            if (data == null)
            {
                // No maps data is loaded, so exit
                return;
            }

            data.CloseAndUnloadCurrentLevel();

            OnLevelClosed?.Invoke();
        }
    }
}
