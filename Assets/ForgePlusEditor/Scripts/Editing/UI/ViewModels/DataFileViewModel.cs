using ForgePlus.DataFileIO;
using System;
using Unity.Properties;

namespace ForgePlus.UI
{
    public class DataFileViewModel : BindableObject, IDisposable
    {
        private const string NoPathText = "select a file...";

        private readonly DataFileTypes type;
        private string path;

        public DataFileViewModel(DataFileTypes type)
        {
            this.type = type;

            FileSettings.Instance.OnPathChanged += OnPathChanged;
        }

        [CreateProperty]
        public string DisplayPath
        {
            get
            {
                return HasPath ? path : NoPathText;
            }
        }

        [CreateProperty]
        public bool HasPath
        {
            get
            {
                return !string.IsNullOrEmpty(path);
            }
        }

        public void Select()
        {
            FileSettings.Instance.ShowSelectionBrowser(type);
        }

        public void Unload()
        {
            FileSettings.Instance.UnloadFile(type);
        }

        public void Dispose()
        {
            // FileSettings may already be gone while the scene is being torn down
            var fileSettings = FileSettings.Instance;
            if (fileSettings)
            {
                fileSettings.OnPathChanged -= OnPathChanged;
            }
        }

        private void OnPathChanged(DataFileTypes changedType, string newPath)
        {
            if (changedType == type)
            {
                path = newPath;
                Notify(nameof(DisplayPath));
                Notify(nameof(HasPath));
            }
        }
    }
}
