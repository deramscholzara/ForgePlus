using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Palette;
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

        public abstract void OnValidatedPointerClick(WorldPointerEventData eventData);
        public abstract void OnValidatedBeginDrag(WorldPointerEventData eventData);
        public abstract void OnValidatedDrag(WorldPointerEventData eventData);
        public abstract void OnValidatedEndDrag(WorldPointerEventData eventData);

        public void OnWorldPointerClick(WorldPointerEventData eventData)
        {
            if (!eventData.IsDragging && isSelectable)
            {
                // The face a selection made by this click is on (which switching modes can select the equivalent of)
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

        // Whether the face's vertices wind the other way (as a ceiling's do)
        protected virtual bool IsCeilingFace
        {
            get
            {
                return false;
            }
        }

        private GameObject[] faceSelectionIndicators;

        // Shows this face alone as selected (its corners), for what's selected through it, such as its light
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

        // In media mode, clicking any of a polygon's surfaces (its floor, ceiling, inward-facing sides or media) paints
        // the palette's media (or "None") onto it, or selects its media, deselecting all of them if it has none
        protected static void ClickPolygonInMediaMode(LevelEntity_Polygon polygon)
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

            SelectionManager.Instance.ToggleObjectSelection(media, multiSelect: false);
            PaletteManager.Instance.SelectSwatchForMedia(SelectionManager.Instance.GetIsSelected(media) ? media : null);
        }

        // Painting gives the polygon the sound chosen in the palette (an ambient or random one, or a list's "None"; the other
        // list's sound is left as it is), and selecting selects the polygon (to inspect its sounds)
        protected static void ClickPolygonInSoundsMode(LevelEntity_Polygon polygon)
        {
            if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
            {
                foreach (var kind in new[] { SoundImageKinds.Ambient, SoundImageKinds.Random })
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
