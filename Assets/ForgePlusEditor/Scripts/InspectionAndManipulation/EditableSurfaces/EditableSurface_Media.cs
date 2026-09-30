using ForgePlus.Inspection;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public class EditableSurface_Media : EditableSurface_Base
    {
        public LevelEntity_Polygon Polygon = null;

        // Read from the polygon each time, so they follow what's painted onto it
        public LevelEntity_Media Media
        {
            get
            {
                return Polygon.Media;
            }
        }

        public LevelEntity_Light RuntimeLight
        {
            get
            {
                return Polygon.ParentLevel.Lights[Polygon.NativeObject.media_lightsource_index];
            }
        }

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(Polygon, multiSelect: false);
                    break;
                case ModeManager.PrimaryModes.Lights:
                    SelectionManager.Instance.ToggleObjectSelection(RuntimeLight, multiSelect: false);
                    PaletteManager.Instance.SelectSwatchForLight(RuntimeLight);
                    break;
                case ModeManager.PrimaryModes.Media:
                    ClickPolygonInMediaMode(Polygon);
                    break;
                default:
                    Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
                    return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedBeginDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedEndDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void SetSelectability(bool enabled)
        {
            base.SetSelectability(enabled);

            GetComponent<MeshCollider>().enabled = enabled;
        }
    }
}
