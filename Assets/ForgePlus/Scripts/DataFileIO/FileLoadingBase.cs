using ForgePlus.ApplicationGeneral;

namespace ForgePlus.DataFileIO
{
    public abstract class FileLoadingBase<T, U>
        where T : FileDataBase<U>, new()
        where U : class, IFileLoadable, new()
    {
        public delegate void OnLoadCompleteDelegate(bool isLoaded);
        public OnLoadCompleteDelegate OnDataLoadComplete;

        protected abstract DataFileTypes dataFileType { get; }

        protected T data;

        public void LoadFile(bool forceReload = true)
        {
            if (!forceReload && data != null)
            {
                // Don't load if already loaded, so exit
                return;
            }

            UnloadFile();

            var path = FileSettings.Instance.GetFilePathFromPlayerPrefs(dataFileType);

            if (string.IsNullOrEmpty(path))
            {
                // No path to load from, so exit
                return;
            }

            UIBlocking.Instance.Block();

            data = new T();
            data.SetPath(path);
            data.LoadData();

            UIBlocking.Instance.Unblock();

            if (OnDataLoadComplete != null)
            {
                OnDataLoadComplete(isLoaded: true);
            }
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

            if (OnDataLoadComplete != null)
            {
                OnDataLoadComplete(isLoaded: false);
            }
        }
    }
}
