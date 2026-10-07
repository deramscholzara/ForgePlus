using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Something in the level that the pointer can click and drag (WorldPointer), while it's selectable
    public abstract class EditableSurface_Base : MonoBehaviour,
        ISelectable,
        IWorldPointerHandler
    {
        protected bool isSelectable = false;

        private GameObject[] faceSelectionIndicators;

        // Read from the level each time, so they follow what's painted onto the face
        public virtual LevelEntity_Light RuntimeLight
        {
            get
            {
                return null;
            }
        }

        public virtual ushort SurfaceShapeDescriptor
        {
            get
            {
                return cstypes.UNONE;
            }
        }

        protected virtual bool IsCeilingFace
        {
            get
            {
                return false;
            }
        }

        public abstract void OnValidatedPointerClick(WorldPointerEventData eventData);
        public abstract void OnValidatedBeginDrag(WorldPointerEventData eventData);
        public abstract void OnValidatedDrag(WorldPointerEventData eventData);
        public abstract void OnValidatedEndDrag(WorldPointerEventData eventData);

        public void OnWorldPointerClick(WorldPointerEventData eventData)
        {
            if (!eventData.IsDragging && isSelectable)
            {
                SelectionManager.Instance.ClickedSurface = this;

                OnValidatedPointerClick(eventData);
            }
        }

        public void OnWorldPointerBeginDrag(WorldPointerEventData eventData)
        {
            if (isSelectable)
            {
                OnValidatedBeginDrag(eventData);
            }
        }

        public void OnWorldPointerDrag(WorldPointerEventData eventData)
        {
            if (isSelectable)
            {
                OnValidatedDrag(eventData);
            }
        }

        public void OnWorldPointerEndDrag(WorldPointerEventData eventData)
        {
            if (isSelectable)
            {
                OnValidatedEndDrag(eventData);
            }
        }

        public virtual void OnDirectionalInputDown(Vector2 direction)
        {
            // Intentionally blank
        }

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }

        // Shows this face alone as selected, for what's selected through it (such as its light)
        public void DisplayFaceSelectionState(bool state)
        {
            if (faceSelectionIndicators != null)
            {
                foreach (var indicator in faceSelectionIndicators)
                {
                    GeometryUtilities.DestroySurfaceSelectionIndicator(indicator);
                }

                faceSelectionIndicators = null;
            }

            var meshFilter = GetComponent<MeshFilter>();
            if (state && meshFilter && meshFilter.sharedMesh)
            {
                faceSelectionIndicators = GeometryUtilities.FitSurfaceSelectionIndicators("Face", transform, meshFilter.sharedMesh.vertices, IsCeilingFace);
            }
        }

        protected void ToggleLightSelection()
        {
            var light = RuntimeLight;

            SelectionManager.Instance.ToggleSelectionOnFace(light);
            PaletteManager.Instance.SelectSwatchForLight(light);
        }

        // Media, Sounds and Annotations modes act on the polygon a face is in (a side's, the one it faces into)
        protected static void ClickPolygonInMode(LevelEntity_Polygon polygon)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Media:
                    ClickPolygonInMediaMode(polygon);
                    break;
                case ModeManager.PrimaryModes.Sounds:
                    ClickPolygonInSoundsMode(polygon);
                    break;
                case ModeManager.PrimaryModes.Annotations:
                    LevelEntity_Annotation.ClickPolygon(polygon);
                    break;
            }
        }

        // Paints the palette's media (or "None"), or selects the polygon's media (deselecting all, if it has none)
        private static void ClickPolygonInMediaMode(LevelEntity_Polygon polygon)
        {
            if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
            {
                if (PaletteManager.Instance.TryGetSelectedMedia(out var selectedMedia))
                {
                    polygon.SetMedia(selectedMedia);
                }

                return;
            }

            var media = polygon.Media;
            if (media == null)
            {
                SelectionManager.Instance.DeselectAll();
                PaletteManager.Instance.SelectSwatchForMedia(null);
                return;
            }

            SelectionManager.Instance.ToggleSelectionOnFace(media);
            PaletteManager.Instance.SelectSwatchForMedia(SelectionManager.Instance.GetIsSelected(media) ? media : null);
        }

        // Paints the sound chosen in each of the palette's lists (leaving the other list's), or selects the polygon
        private static void ClickPolygonInSoundsMode(LevelEntity_Polygon polygon)
        {
            if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
            {
                foreach (var kind in SoundImageEditing.Kinds)
                {
                    if (PaletteManager.Instance.TryGetSelectedSound(kind, out var index))
                    {
                        SoundImageEditing.Assign(polygon, kind, index);
                    }
                }

                return;
            }

            SelectionManager.Instance.ToggleObjectSelection(polygon, multiSelect: false);
        }

        // Nudges the surface with the directional inputs while the object is selected
        public async void InputListener(ISelectable mustBeSelectedObject)
        {
            while (Application.isPlaying && SelectionManager.Instance.GetIsSelected(mustBeSelectedObject))
            {
                var inputDirection = Vector2.zero;
                var directionalInputReceived = false;

                if (Hotkeys.WasPressed(ForgePlusInput.Editing.NudgeUp))
                {
                    inputDirection.y += 1f;
                    directionalInputReceived = true;
                }
                
                if (Hotkeys.WasPressed(ForgePlusInput.Editing.NudgeDown))
                {
                    inputDirection.y -= 1f;
                    directionalInputReceived = true;
                }

                if (Hotkeys.WasPressed(ForgePlusInput.Editing.NudgeRight))
                {
                    inputDirection.x += 1f;
                    directionalInputReceived = true;
                }
                
                if (Hotkeys.WasPressed(ForgePlusInput.Editing.NudgeLeft))
                {
                    inputDirection.x -= 1f;
                    directionalInputReceived = true;
                }

                if (directionalInputReceived)
                {
                    OnDirectionalInputDown(inputDirection);
                }

                await Awaitable.NextFrameAsync();
            }
        }
    }
}
