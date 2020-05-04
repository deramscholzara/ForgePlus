using System;

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

        public virtual void LoadFile(bool forceReload = true)
        {
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
