using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public class Button_Quit : MonoBehaviour
    {
        public void OnClick()
        {
            // TODO: Check if save is necessary

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
