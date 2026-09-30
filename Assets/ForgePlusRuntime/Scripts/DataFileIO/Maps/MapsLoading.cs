using ForgePlus.ApplicationGeneral;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using ForgePlus.Extensions;

namespace ForgePlus.DataFileIO
{
    public partial class MapsLoading : FileLoadingBase<MapsLoading, MapsData, MapsFile>
    {
        private event Action<string> OnLevelOpened_Sender;

        public event Action<string> OnLevelOpened
        {
            add
            {
                OnLevelOpened_Sender += value;

                value.Invoke(LevelEntity_Level.Instance ? LevelEntity_Level.Instance.Level.GetLevelName() : null);
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

            data?.CloseFile();

            base.UnloadFile();
        }

        public void OpenLevel(int levelIndex = 0)
        {
            UIBlocking.Instance.Block();

            LoadFile(forceReload: false);

            if (data == null)
            {
                Debug.LogError($"Tried opening level index {levelIndex} with no loaded Maps file.  You may need to call LoadData() first.");
                // No maps data is loaded, so exit
                return;
            }

            CloseLevel();

            data.OpenLevel(levelIndex);

            OnLevelOpened_Sender?.Invoke(LevelEntity_Level.Instance.Level.GetLevelName());

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
