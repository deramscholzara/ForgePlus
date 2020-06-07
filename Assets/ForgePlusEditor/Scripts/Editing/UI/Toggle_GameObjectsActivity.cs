using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class Toggle_GameObjectsActivity : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] targets = new GameObject[] { };

        public void OnValueChanged(bool value)
        {
            foreach (var target in targets)
            {
                if (target)
                {
                    target.SetActive(value);
                }
                else
                {
                    Debug.LogError("This Toggle_GameObjectsActivity has a null GameObject reference!", this);
                }
            }
        }
    }
}
