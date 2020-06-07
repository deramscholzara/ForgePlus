using SimpleFileBrowser;
using System.Collections;
using UnityEngine;
using System;
using ForgePlus.DataFileIO.Extensions;

namespace ForgePlus.DataFileIO
{
    public class Button_UnloadDataFile : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI filePathDisplayText = null;

        [SerializeField]
        private DataFileTypes dataFileType = DataFileTypes.Unspecified;

        public void OnClick()
        {
            FileSettings.Instance.UnloadFile(dataFileType);

            filePathDisplayText.text = Button_SelectDataFile.defaultPathText;

            gameObject.SetActive(false);
        }
    }
}
