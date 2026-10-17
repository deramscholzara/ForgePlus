#if !NO_EDITING
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // A fillable loop's preview floor (FillablePolygons): double-clicking it fills the loop, and clicking it is clicking
    // empty space (deselecting everything)
    public class FillablePolygonPreview : MonoBehaviour, IWorldPointerHandler, IWorldPointerDoubleClickHandler, IWorldPointerEmptySpace
    {
        public PolygonClosing.Loop Loop { get; set; }

        public void OnWorldPointerClick(WorldPointerEventData eventData)
        {
            SelectionManager.Instance.DeselectAll();
        }

        public void OnWorldPointerDoubleClick(WorldPointerEventData eventData)
        {
            FillablePolygons.Fill(Loop);
        }

        public void OnWorldPointerBeginDrag(WorldPointerEventData eventData)
        {
        }

        public void OnWorldPointerDrag(WorldPointerEventData eventData)
        {
        }

        public void OnWorldPointerEndDrag(WorldPointerEventData eventData)
        {
        }

        private void OnDestroy()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter && meshFilter.sharedMesh)
            {
                Destroy(meshFilter.sharedMesh);
            }
        }
    }
}
#endif
