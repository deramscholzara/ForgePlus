using ForgePlus.ApplicationGeneral;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using AlephOne;
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

        // When a level's name in the map's directory changes (renamed, or a rename discarded)
        public event Action OnLevelNamesChanged;

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

        // Null while none is loaded
        public MapsFile MapsFile
        {
            get
            {
                return data?.MapsFile;
            }
        }

        // -1 while none is open
        public int OpenLevelIndex
        {
            get
            {
                return data != null ? data.OpenLevelIndex : -1;
            }
        }

        // So, for example, the camera stays where it is
        public bool IsRebuildingLevel { get; private set; }

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

        // Rebuilds the open level's directory entry after its name or flags change (game_wad.cpp: build_directory_data)
        public void UpdateOpenLevelDirectory()
        {
            var level = LevelEntity_Level.Instance;
            var mapsFile = MapsFile;
            var levelIndex = OpenLevelIndex;
            if (!level || mapsFile == null || levelIndex < 0 || levelIndex >= mapsFile.Levels.Count)
            {
                // No level is open, so exit
                return;
            }

            var directory = mapsFile.Levels[levelIndex];
            var nameChanged = directory.GetLevelName() != level.Level.GetLevelName();

            mapsFile.Levels[levelIndex] = game_wad.build_directory_data(level.Level.static_world);

            if (nameChanged)
            {
                level.gameObject.name = LevelEntity_Level.GameObjectName(level.Level.GetLevelName());
                OnLevelNamesChanged?.Invoke();
            }
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

        // Closes and reopens the open level from its current data, for edits that change its structure
        // (such as making a polygon a platform, which changes the sides around it)
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

            var namesChanged = data.CloseAndUnloadCurrentLevel();

            OnLevelClosed?.Invoke();

            if (namesChanged)
            {
                OnLevelNamesChanged?.Invoke();
            }
        }
    }
}
