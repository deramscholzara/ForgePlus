using AlephOne;
using System;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // Chooses the physics model as Aleph One does: the open level's embedded physics if it has any,
    // otherwise the selected physics file's, otherwise the engine's built-in definitions
    [AutoStaticsCleanup]
    public partial class PhysicsLoading : FileLoadingBase<PhysicsLoading, PhysicsData, PhysicsFile>
    {
        private static LoadedPhysicsModel engineDefaults;

        private MapLevel openLevel;
        private LoadedPhysicsModel openLevelModel;

        private string failedPath;
        private bool isChangingFile;

        public PhysicsModel Model
        {
            get
            {
                return LoadedModel.Model;
            }
        }

        public PhysicsModelSource Source
        {
            get
            {
                return LoadedModel.Source;
            }
        }

        protected override DataFileTypes DataFileType
        {
            get
            {
                return DataFileTypes.Physics;
            }
        }

        private LoadedPhysicsModel LoadedModel
        {
            get
            {
                if (openLevelModel != null)
                {
                    return openLevelModel;
                }

                return SelectedFileModel;
            }
        }

        private LoadedPhysicsModel SelectedFileModel
        {
            get
            {
                TryLoadFile();

                return data?.PhysicsFile?.Model ?? EngineDefaults;
            }
        }

        private static LoadedPhysicsModel EngineDefaults
        {
            get
            {
                if (engineDefaults == null)
                {
                    engineDefaults = LoadedPhysicsModel.FromDefaults();
                }

                return engineDefaults;
            }
        }

        // Builds the level's physics on a worker thread, for ApplyLevelModel to make it the open level's.
        // (The selected physics file is loaded first, here on the main thread.)
        public Task<LoadedPhysicsModel> BuildLevelModelAsync(MapLevel level)
        {
            var physicsFileModel = SelectedFileModel;

            return Task.Run(() => LoadedPhysicsModel.ForLevel(level, physicsFileModel));
        }

        public void ApplyLevelModel(MapLevel level, LoadedPhysicsModel levelModel)
        {
            openLevel = level;
            openLevelModel = levelModel;

            LogLevelModel();
        }

        public void ClearLevel()
        {
            openLevel = null;
            openLevelModel = null;
        }

        public override void LoadFile(bool forceReload = true)
        {
            failedPath = null;

            LoadFileWithoutRefreshing(forceReload);

            RefreshOpenLevel();
        }

        public override void UnloadFile()
        {
            base.UnloadFile();

            // Refreshing while FileLoadingBase.LoadFile unloads before loading would load again (and recurse).
            // Otherwise, the open level no longer has a physics file (the selected path is cleared only after this).
            if (!isChangingFile && openLevel != null)
            {
                SetLevel(openLevel, EngineDefaults);
            }
        }

        private void SetLevel(MapLevel level, LoadedPhysicsModel physicsFileModel)
        {
            ApplyLevelModel(level, LoadedPhysicsModel.ForLevel(level, physicsFileModel));
        }

        private void LogLevelModel()
        {
            Debug.Log($"--- Physics: using {openLevelModel.Source}{(openLevelModel.FilePath != null ? $" ({openLevelModel.FilePath})" : string.Empty)}");
        }

        private void LoadFileWithoutRefreshing(bool forceReload)
        {
            isChangingFile = true;

            try
            {
                base.LoadFile(forceReload);
            }
            finally
            {
                isChangingFile = false;
            }
        }

        // A level without embedded physics uses the selected file's
        private void RefreshOpenLevel()
        {
            if (openLevel != null)
            {
                SetLevel(openLevel, SelectedFileModel);
            }
        }

        private void TryLoadFile()
        {
            if (data != null)
            {
                // Already loaded, so exit
                return;
            }

            var path = FileSettings.Instance.GetFilePath(DataFileType);

            if (path == failedPath)
            {
                // This file already failed to load, so don't retry (and log) for every lookup
                return;
            }

            try
            {
                LoadFileWithoutRefreshing(forceReload: false);
            }
            catch (Exception exception)
            {
                // A bad physics file falls back to the engine defaults, as in Aleph One
                Debug.LogError($"Physics file \"{path}\" could not be loaded, so engine defaults will be used instead: {exception}");

                base.UnloadFile();

                failedPath = path;
            }
        }
    }
}
