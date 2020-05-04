using ForgePlus.LevelManipulation;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class Toggle_SelectSecondaryMode : MonoBehaviour
    {
        [SerializeField]
        public ModeManager.SecondaryModes Mode = ModeManager.SecondaryModes.None;

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                ModeManager.Instance.SecondaryMode = Mode;
            }
        }
    }
}
