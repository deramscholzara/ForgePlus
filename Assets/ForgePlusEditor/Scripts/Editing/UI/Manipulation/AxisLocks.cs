using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.LevelManipulation
{
    public class AxisLocks : SingletonMonoBehaviour<AxisLocks>
    {
        [SerializeField]
        private Toggle x = null;

        [SerializeField]
        private Toggle y = null;

        private bool snapToGrid = false;

        public bool XLocked { get; private set; } = false;

        public bool YLocked { get; private set; } = false;

        public bool SnapToGrid
        {
            get
            {
                if (Input.GetKey(KeyCode.LeftAlt) ||
                    Input.GetKey(KeyCode.RightAlt))
                {
                    return !snapToGrid;
                }

                return snapToGrid;
            }
        }

        public void LockX(bool state)
        {
            XLocked = state;
        }

        public void LockY(bool state)
        {
            YLocked = state;
        }

        public void SetSnapToGrid(bool state)
        {
            snapToGrid = state;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                x.isOn = !x.isOn;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                y.isOn = !y.isOn;
            }
        }
    }
}
