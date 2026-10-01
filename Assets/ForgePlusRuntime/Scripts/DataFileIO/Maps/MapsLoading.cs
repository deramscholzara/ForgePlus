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

        // The loaded map file (null while none is loaded)
        public MapsFile MapsFile
        {
            get
            {
                return data?.MapsFile;
            }
        }

        // The open level's index in the map file (-1 while none is open)
        public int OpenLevelIndex
        {
            get
            {
                return data != null ? data.OpenLevelIndex : -1;
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

        // While the open level is being rebuilt (so, for example, the camera stays where it is)
        public bool IsRebuildingLevel { get; private set; }

        // Rebuilds the open level's runtime objects from its data, for edits that change its structure (such as making a
        // polygon a platform, which changes the sides around it). It's closed and opened as switching levels does, but
        // from the level's current data rather than the file's.
        public void RebuildLevel()
        {
            if (data == null || !LevelEntity_Level.Instance)
            {
                // No level is open, so exit
                return;
            }

            UIBlocking.Instance.Block();
            IsRebuildingLevel = true;

            try
            {
                data.CloseCurrentLevelObjects();
                OnLevelClosed?.Invoke();

                data.ReopenCurrentLevelObjects();
                OnLevelOpened_Sender?.Invoke(LevelEntity_Level.Instance.Level.GetLevelName());
            }
            finally
            {
                IsRebuildingLevel = false;
                UIBlocking.Instance.Unblock();
            }
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
