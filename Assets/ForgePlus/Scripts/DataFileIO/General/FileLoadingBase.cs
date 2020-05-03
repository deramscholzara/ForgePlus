using ForgePlus.ApplicationGeneral;

namespace ForgePlus.DataFileIO
{
    public abstract class FileLoadingBase<T, U, V> : OnDemandSingleton<T>
        where T : class, new()
        where U : FileDataBase<V>, new()
        where V : class, IFileLoadable, new()
    {
        public delegate void OnLoadCompleteDelegate(bool isLoaded);
        public event OnLoadCompleteDelegate OnDataLoadCompleted;

        protected abstract DataFileTypes DataFileType { get; }

        protected U data;

        public void LoadFile(bool forceReload = true)
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

            OnLoadedChanged();
            OnDataLoadCompleted?.Invoke(isLoaded: true);
        }

        public void UnloadFile()
        {
            if (data == null)
            {
                // Not loaded, so exit
                return;
            }

            data.UnloadData();

            data = null;

            OnLoadedChanged();
            OnDataLoadCompleted?.Invoke(isLoaded: false);
        }

        protected virtual void OnLoadedChanged()
        {
            // Intentionally blank
        }
    }
}
