using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.DataFileIO
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
