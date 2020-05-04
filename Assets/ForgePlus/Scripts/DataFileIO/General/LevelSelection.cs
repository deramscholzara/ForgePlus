using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.DataFileIO
{
    public class LevelSelection : MonoBehaviour
    {
        private const int levelsLoadedPerFrame = 8;

        [SerializeField]
        public Toggle_LevelSelection TogglePrefab;

        [SerializeField]
        public Transform TogglesParent;

        [SerializeField]
        public ToggleGroup ToggleGroup;

        [SerializeField]
        private Button LoadButton = null;

        private List<Toggle_LevelSelection> currentToggles = new List<Toggle_LevelSelection>();

        private CancellationTokenSource refreshCTS;

        private void OnEnable()
        {
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
            else
            {
                RefreshList();
            }
        }

        private void OnDisable()
        {
            MapsLoading.Instance.OnDataLoadCompleted -= OnMapsLoaded;
            MapsLoading.Instance.OnSaveCompleted -= RefreshList;
        }

        private void OnMapsLoaded(bool isLoaded)
        {
            if (isLoaded)
            {
                RefreshList();
            }
            else
            {
                ClearList();
            }
        }

        private void RefreshList()
        {
            refreshCTS?.Cancel();

            refreshCTS = new CancellationTokenSource();

            RefreshList(refreshCTS.Token);
        }

        private async void RefreshList(CancellationToken cancellationToken)
        {
            ClearList();

            await Task.Yield();

            if (!Application.isPlaying)
            {
                return;
            }

            if (MapsLoading.Instance.LevelNames.Count > 0)
            {
                LoadButton.interactable = true;

                for (var i = 0; i < MapsLoading.Instance.LevelNames.Count && MapsLoading.Instance.LevelNames.ElementAt(i) != string.Empty; i++)
                {
                    var toggle = Instantiate(TogglePrefab, TogglesParent);

                    toggle.LevelIndex = i;
                    toggle.Group = ToggleGroup;
                    toggle.Label = MapsLoading.Instance.LevelNames.ElementAt(i);

                    currentToggles.Add(toggle);

                    if (i == 0)
                    {
                        toggle.GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                    }

                    if (i < MapsLoading.Instance.LevelNames.Count - 1 &&
                        (i + 1) % levelsLoadedPerFrame == 0)
                    {
                        await Task.Yield();

                        if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                        {
                            return;
                        }
                    }
                }
            }
        }

        private void ClearList()
        {
            LoadButton.interactable = false;

            foreach (var toggle in currentToggles)
            {
                Destroy(toggle.gameObject);
            }

            currentToggles.Clear();
        }
    }
}
