using ForgePlus.Palette;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;
using Weland.Extensions;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfaceSide : FPInteractiveSurfaceBase
    {
        public FPSide ParentFPSide = null;
        public FPSide.DataSources DataSource;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        private UVPlanarDrag uvDragPlane;

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(ParentFPSide, multiSelect: false);
                    break;
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        var selectedTexture = PaletteManager.Instance.GetSelectedTexture();

                        if (!selectedTexture.IsEmpty())
                        {
                            var destinationIsLayered = ParentFPSide.WelandObject.HasLayeredTransparentSide(FPLevel.Instance.Level);
                            var destinationDataSource = DataSource;

                            if (destinationIsLayered)
                            {
                                // TODO: should display destination picker here
                                // TODO: Figure out how to pick which of the data sources to set (a popup with available options, and cancel?)
                                //       "Choose Destination Layer" options are always:
                                //          - Inner
                                //          - Outer
                                //          - Cancel
                                // use DialogManager to display an appropriate prefab from Resources
                            }

                            ParentFPSide.SetShapeDescriptor(GetComponent<RuntimeSurfaceLight>(), destinationDataSource, selectedTexture, destinationIsLayered);
                        }
                    }
                    else if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing &&
                             Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        var selectedSourceObject = SelectionManager.Instance.SelectedObject;
                        var selectedSourceFPSide = (selectedSourceObject is FPSide) ? selectedSourceObject as FPSide : null;

                        if (selectedSourceFPSide &&
                            selectedSourceFPSide != ParentFPSide)
                        {
                            if (selectedSourceFPSide.WelandObject.SideIsNeighbor(FPLevel.Instance.Level,
                                                                                 ParentFPSide.WelandObject,
                                                                                 out var neighborFlowsOutward,
                                                                                 out var neighborIsLeft))
                            {
                                var destinationIsLayered = ParentFPSide.WelandObject.HasLayeredTransparentSide(FPLevel.Instance.Level);
                                var destinationDataSource = DataSource;

                                if (destinationIsLayered)
                                {
                                    // TODO: should display destination picker here
                                    // TODO: Figure out how to pick which of the data sources to set (a popup with available options, and cancel?)
                                    //       "Choose Destination Layer" options are always:
                                    //          - Inner
                                    //          - Outer
                                    //          - Cancel
                                    // use DialogManager to display an appropriate prefab from Resources
                                }

                                var uvChannel = 0;
                                if (destinationDataSource == FPSide.DataSources.Transparent &&
                                    destinationIsLayered)
                                {
                                    uvChannel = 1;
                                }

                                List<FPSide.DataSources> sourceDataSourceOptions = new List<FPSide.DataSources>();

                                if (!selectedSourceFPSide.WelandObject.Primary.Texture.IsEmpty())
                                {
                                    sourceDataSourceOptions.Add(FPSide.DataSources.Primary);
                                }

                                if (!selectedSourceFPSide.WelandObject.Secondary.Texture.IsEmpty())
                                {
                                    sourceDataSourceOptions.Add(FPSide.DataSources.Secondary);
                                }

                                if (!selectedSourceFPSide.WelandObject.Transparent.Texture.IsEmpty())
                                {
                                    sourceDataSourceOptions.Add(FPSide.DataSources.Transparent);
                                }

                                var sourceDataSource = DataSource;

                                if (sourceDataSourceOptions.Count > 1)
                                {
                                    // TODO: Figure out how to pick which of the data sources to align to (a popup with available options, and cancel?)
                                    //       "Choose Destination Layer" appears when there are at least two sources to choose from (non-empty ShapeDescriptor):
                                    //          - Primary
                                    //          - Secondary
                                    //          - Transparent
                                    //          - Cancel (always)
                                    // use DialogManager to display an appropriate prefab from Resources
                                }
                                else if (sourceDataSourceOptions.Count == 0)
                                {
                                    // The source side has no textured surfaces, so exit
                                    return;
                                }

                                short selectedObjectUVOffset_U;
                                short selectedObjectUVOffset_Y;

                                switch (sourceDataSource)
                                {
                                    case FPSide.DataSources.Primary:
                                        selectedObjectUVOffset_U = selectedSourceFPSide.WelandObject.Primary.X;
                                        // TODO: this needs to set the y based on vertical world distance
                                        //       instead of just matching it.
                                        //       Use this: (destinationHighHeight - sourceHighHeight) + sourceY
                                        //       Alternatively, it might be this: (sourceHighHeight - destinationHighHeight) + sourceY
                                        selectedObjectUVOffset_Y = selectedSourceFPSide.WelandObject.Primary.Y;

                                        break;
                                    case FPSide.DataSources.Secondary:
                                        selectedObjectUVOffset_U = selectedSourceFPSide.WelandObject.Secondary.X;
                                        // TODO: this needs to set the y based on vertical world distance
                                        //       instead of just matching it.
                                        //       Use this: (destinationHighHeight - sourceHighHeight) + sourceY
                                        //       Alternatively, it might be this: (sourceHighHeight - destinationHighHeight) + sourceY
                                        selectedObjectUVOffset_Y = selectedSourceFPSide.WelandObject.Secondary.Y;

                                        break;
                                    case FPSide.DataSources.Transparent:
                                        selectedObjectUVOffset_U = selectedSourceFPSide.WelandObject.Transparent.X;
                                        // TODO: this needs to set the y based on vertical world distance
                                        //       instead of just matching it.
                                        //       Use this: (destinationHighHeight - sourceHighHeight) + sourceY
                                        //       Alternatively, it might be this: (sourceHighHeight - destinationHighHeight) + sourceY
                                        selectedObjectUVOffset_Y = selectedSourceFPSide.WelandObject.Transparent.Y;

                                        break;
                                    default:
                                        return;
                                }

                                short alignmentOffset_U = neighborFlowsOutward ? (short)0 : FPLevel.Instance.FPLines[ParentFPSide.WelandObject.LineIndex].WelandObject.Length;
                                alignmentOffset_U += neighborIsLeft ? (short)0 : FPLevel.Instance.FPLines[selectedSourceFPSide.WelandObject.LineIndex].WelandObject.Length;

                                selectedObjectUVOffset_U += alignmentOffset_U;

                                ParentFPSide.SetOffset(this,
                                                       destinationDataSource,
                                                       uvChannel,
                                                       selectedObjectUVOffset_U,
                                                       selectedObjectUVOffset_Y,
                                                       rebatch: true);
                            }
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPSide, multiSelect: false);

                        if (!surfaceShapeDescriptor.IsEmpty())
                        {
                            PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor, invokeToggleEvents: false);
                        }
                    }

                    break;
                case ModeManager.PrimaryModes.Lights:
                    SelectionManager.Instance.ToggleObjectSelection(FPLight, multiSelect: false);
                    PaletteManager.Instance.SelectSwatchForLight(FPLight, invokeToggleEvents: false);
                    break;
                case ModeManager.PrimaryModes.Media:
                    if (FPMedia != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(FPMedia, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForMedia(FPMedia, invokeToggleEvents: false);
                    }

                    break;
                case ModeManager.PrimaryModes.Platforms:
                    if (FPPlatform != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(FPPlatform, multiSelect: false);
                    }

                    break;
                default:
                    Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
                    break;
            }
        }

        public override void OnValidatedBeginDrag(PointerEventData eventData)
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                var runtimeSurfaceLight = GetComponent<RuntimeSurfaceLight>();

                if (!runtimeSurfaceLight)
                {
                    // If there's no light on this surface,
                    // it's either a landscape or an unassigned surface,
                    // so don't bother with offset adjustment.
                    return;
                }

                runtimeSurfaceLight.UnmergeBatch();

                Vector2 startingUVs;

                switch (DataSource)
                {
                    case FPSide.DataSources.Primary:
                        startingUVs = new Vector2(ParentFPSide.WelandObject.Primary.X, ParentFPSide.WelandObject.Primary.Y);
                        break;
                    case FPSide.DataSources.Secondary:
                        startingUVs = new Vector2(ParentFPSide.WelandObject.Secondary.X, ParentFPSide.WelandObject.Secondary.Y);
                        break;
                    case FPSide.DataSources.Transparent:
                        startingUVs = new Vector2(ParentFPSide.WelandObject.Transparent.X, ParentFPSide.WelandObject.Transparent.Y);
                        break;
                    default:
                        return;
                }

                var startingPosition = eventData.pointerPressRaycast.worldPosition;

                var surfaceWorldNormal = eventData.pointerCurrentRaycast.worldNormal;

                var textureWorldUp = Vector3.up;

                uvDragPlane = new UVPlanarDrag(startingUVs,
                                                startingPosition,
                                                surfaceWorldNormal,
                                                textureWorldUp);
            }
        }

        public override void OnValidatedDrag(PointerEventData eventData)
        {
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                var screenPosition = new Vector3(eventData.pointerCurrentRaycast.screenPosition.x,
                                                 eventData.pointerCurrentRaycast.screenPosition.y,
                                                 0f);

                var pointerRay = Camera.main.ScreenPointToRay(screenPosition);

                var newUVOffset = uvDragPlane.UVDraggedPosition(pointerRay);

                ParentFPSide.SetOffset(this,
                                       DataSource,
                                       uvChannel: 0, // TODO: Figure out how to allow for drag-edit on overlay TransparentSides (uv channel 1)?  Hotkey when drag begins?
                                       (short)newUVOffset.x,
                                       (short)newUVOffset.y,
                                       rebatch: false);
            }
        }

        public override void OnValidatedEndDrag(PointerEventData eventData)
        {
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                uvDragPlane = null;

                GetComponent<RuntimeSurfaceLight>().MergeBatch();
            }
        }
    }
}
