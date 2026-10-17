using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.Localization;
using System;
using Unity.Properties;

namespace ForgePlus.UI
{
    public class DataFileViewModel : BindableObject, IDisposable
    {
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
                return HasPath ? path : Strings.Get(Strings.Menu, type.IsFolder() ? "FilesTab.NoFolderPath" : "FilesTab.NoPath");
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

        // Unloading the maps file closes the open level, so its unsaved changes are asked about first
        public async void Unload()
        {
            if (type == DataFileTypes.Maps && !await UnsavedChanges.Confirm())
            {
                return;
            }

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
