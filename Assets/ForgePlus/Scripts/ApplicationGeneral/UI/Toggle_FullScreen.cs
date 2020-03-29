using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_FullScreen : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            Screen.fullScreen = value;
        }
    }
}
