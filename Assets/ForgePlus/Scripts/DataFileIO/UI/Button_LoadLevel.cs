using ForgePlus.DataFileIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.LevelLoading
{
    public class Button_LoadLevel : MonoBehaviour
    {
        public ToggleGroup ToggleGroup;

        public void OnClick()
        {
            MapsLoading.Instance.OpenLevel(ToggleGroup.GetFirstActiveToggle().GetComponent<Toggle_LevelSelection>().LevelIndex);
        }
    }
}
