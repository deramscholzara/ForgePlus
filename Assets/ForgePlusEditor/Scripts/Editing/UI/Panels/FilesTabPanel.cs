using ForgePlus.DataFileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class FilesTabPanel : UIPanel
    {
        private readonly List<DataFileViewModel> dataFiles = new List<DataFileViewModel>();
        private readonly List<string> levelNames = new List<string>();

        private ListView levelList;
        private Button loadButton;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/FilesTab";
            }
        }

        protected override void OnLoaded()
        {
            BindDataFile("maps", DataFileTypes.Maps);
            BindDataFile("shapes", DataFileTypes.Shapes);
            BindDataFile("physics", DataFileTypes.Physics);
            BindDataFile("sounds", DataFileTypes.Sounds);

            var levelItemTemplate = LoadTemplate("LevelItem");

            levelList = Root.Q<ListView>("levels");
            levelList.itemsSource = levelNames;
            levelList.makeItem = () => levelItemTemplate.Instantiate();
            levelList.bindItem = (item, index) => item.Q<Label>("label").text = levelNames[index];
            levelList.selectionChanged += selection => UpdateLoadButton();

            loadButton = Root.Find<Button>("load");
            loadButton.clicked += OnLoad;

            MapsLoading.Instance.OnDataLoadCompleted += OnMapsLoaded;
            MapsLoading.Instance.OnSaveCompleted += RefreshList;

            if (MapsLoading.Instance.LevelNames == null)
            {
                try
                {
                    MapsLoading.Instance.LoadFile();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Attempt to load file at path \"{FileSettings.Instance.GetFilePath(DataFileTypes.Maps)}\" failed with exception: {exception}");

                    FileSettings.Instance.UnloadFile(DataFileTypes.Maps);
                }
            }
        }

        protected override void OnUnloading()
        {
            MapsLoading.Instance.OnDataLoadCompleted -= OnMapsLoaded;
            MapsLoading.Instance.OnSaveCompleted -= RefreshList;

            foreach (var dataFile in dataFiles)
            {
                dataFile.Dispose();
            }

            dataFiles.Clear();
        }

        private void BindDataFile(string selectorName, DataFileTypes type)
        {
            var dataFile = new DataFileViewModel(type);
            dataFiles.Add(dataFile);

            var selector = Root.Q(selectorName);
            selector.Q<Label>("path").Bind("text", dataFile, nameof(DataFileViewModel.DisplayPath));
            selector.Find<Button>("select").clicked += dataFile.Select;

            selector.Q("unload").BindShown(dataFile, nameof(DataFileViewModel.HasPath));
            selector.Find<Button>("unload").clicked += dataFile.Unload;
        }

        private void OnMapsLoaded(bool isLoaded)
        {
            RefreshList();
        }

        // The levels up to the first unnamed one
        private void RefreshList()
        {
            var names = MapsLoading.Instance.LevelNames;

            levelNames.Clear();
            if (names != null)
            {
                levelNames.AddRange(names.TakeWhile(levelName => levelName != string.Empty));
            }

            levelList.RefreshItems();

            if (levelNames.Count > 0)
            {
                levelList.SetSelectionWithoutNotify(new[] { 0 });
            }
            else
            {
                levelList.ClearSelection();
            }

            UpdateLoadButton();
        }

        private void UpdateLoadButton()
        {
            loadButton.SetEnabled(levelList.selectedIndex >= 0);
        }

        private void OnLoad()
        {
            if (levelList.selectedIndex >= 0)
            {
                MapsLoading.Instance.OpenLevel(levelList.selectedIndex);
            }
        }
    }
}
