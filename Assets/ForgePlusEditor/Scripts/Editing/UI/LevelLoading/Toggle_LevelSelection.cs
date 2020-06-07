using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.DataFileIO
{
    public class Toggle_LevelSelection : MonoBehaviour
    {
        [SerializeField]
        private Toggle toggle = null;

        [SerializeField]
        private TextMeshProUGUI label = null;

        public int LevelIndex { get; set; }

        public ToggleGroup Group
        {
            set
            {
                toggle.group = value;
            }
        }

        public string Label
        {
            set
            {
                label.text = value;
            }
        }
    }
}
