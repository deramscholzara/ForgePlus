using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public abstract class FileDataBase<T> where T : class, IFileLoadable, new()
    {
        private string filePath = null;
        protected T file;

        public void SetPath(string filePath)
        {
            if (this.filePath != null)
            {
                Debug.LogError("Path has already been initialized previously and will remain at initial value.");
                return;
            }

            this.filePath = filePath;
        }

        public void LoadData()
        {
            if (string.IsNullOrEmpty(this.filePath))
            {
                Debug.LogError("Path has has not yet been set, so file cannot be loaded.  You should call SetPath first.");
                return;
            }

            if (file != null)
            {
                // Already loaded, so exit
                return;
            }

            UnloadData();

            file = new T();
            file.Load(filePath);
        }

        public void UnloadData()
        {
            if (file == null)
            {
                // Not loaded, so exit
                return;
            }

            file = null;
        }
    }
}
