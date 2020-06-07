using ForgePlus.LevelManipulation;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class Toggle_SelectPrimaryMode : MonoBehaviour
    {
        [SerializeField]
        private ModeManager.PrimaryModes Mode = ModeManager.PrimaryModes.None;

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                ModeManager.Instance.PrimaryMode = Mode;
            }
        }
    }
}
