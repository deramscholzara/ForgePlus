using System;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public abstract class FileLoadingBase<T, U, V> : OnDemandSingleton<T>
        where T : class, new()
        where U : FileDataBase<V>, new()
        where V : class, IFileLoadable, new()
    {
        private event Action<bool> OnDataLoadCompleted_Sender;

        public event Action<bool> OnDataLoadCompleted
        {
            add
            {
                OnDataLoadCompleted_Sender += value;
                value.Invoke(data != null);
            }
            remove
            {
                OnDataLoadCompleted_Sender -= value;
            }
        }

        protected abstract DataFileTypes DataFileType { get; }

        protected U data;

        // For TryLoadSelectedFile
        private string failedPath;

        public virtual void LoadFile(bool forceReload = true)
        {
            failedPath = null;

            if (!forceReload && data != null)
            {
                // Don't load if already loaded, so exit
                return;
            }

            UnloadFile();

            var path = FileSettings.Instance.GetFilePath(DataFileType);

            if (string.IsNullOrEmpty(path))
            {
                // No path to load from, so exit
                return;
            }

            data = new U();
            data.SetPath(path);
            data.LoadData();

            OnDataLoadCompleted_Sender?.Invoke(true);
        }

        public virtual void UnloadFile()
        {
            UnloadData();
        }

        // Loads the selected file with load if it isn't loaded, logging a failure once rather than on every lookup
        // (until LoadFile is called again)
        protected void TryLoadSelectedFile(Action load, string fileKind, string failureConsequence)
        {
            if (data != null)
            {
                // Already loaded, so exit
                return;
            }

            var path = FileSettings.Instance.GetFilePath(DataFileType);

            if (string.IsNullOrEmpty(path) || path == failedPath)
            {
                // No file, or this file already failed to load, so exit
                return;
            }

            try
            {
                load();
            }
            catch (Exception exception)
            {
                Debug.LogError($"{fileKind} file \"{path}\" could not be loaded, so {failureConsequence}: {exception}");

                UnloadData();

                failedPath = path;
            }
        }

        private void UnloadData()
        {
            if (data == null)
            {
                // Not loaded, so exit
                return;
            }

            data.UnloadData();

            data = null;

            OnDataLoadCompleted_Sender?.Invoke(false);
        }
    }
}
