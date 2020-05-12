using ForgePlus.ApplicationGeneral;
using ForgePlus.Palette;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;
using Weland.Extensions;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfaceSide : FPInteractiveSurfaceBase
    {
        private static Dialog_ObjectSelector SideDestinationSelectionDialog = null;

        public FPSide ParentFPSide = null;
        public FPSide.DataSources DataSource;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        private UVPlanarDrag uvDragPlane;
        private bool endDragShouldRemergeBatch = false;

        public async override void OnValidatedPointerClick(PointerEventData eventData)
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
                                var result = await ShowLayerSourceDialog(isDestination: true);

                                if (!result.HasValue)
                                {
                                    return;
                                }

                                destinationDataSource = result.Value;
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
                                FPSide.DataSources destinationDataSource;

                                if (destinationIsLayered)
                                {
                                    var result = await ShowLayerSourceDialog(isDestination: true);

                                    if (!result.HasValue)
                                    {
                                        return;
                                    }

                                    destinationDataSource = result.Value;
                                }
                                else
                                {
                                    List<string> destinationDataSourceOptions = new List<string>();

                                    if (!ParentFPSide.WelandObject.Primary.Texture.IsEmpty())
                                    {
                                        destinationDataSourceOptions.Add(FPSide.DataSources.Primary.ToString());
                                    }

                                    if (!ParentFPSide.WelandObject.Secondary.Texture.IsEmpty())
                                    {
                                        destinationDataSourceOptions.Add(FPSide.DataSources.Secondary.ToString());
                                    }

                                    if (!ParentFPSide.WelandObject.Transparent.Texture.IsEmpty())
                                    {
                                        destinationDataSourceOptions.Add(FPSide.DataSources.Transparent.ToString());
                                    }

                                    if (destinationDataSourceOptions.Count == 0)
                                    {
                                        // The source side has no textured surfaces, so exit
                                        return;
                                    }
                                    else if (destinationDataSourceOptions.Count == 1)
                                    {
                                        destinationDataSource = (FPSide.DataSources)Enum.Parse(typeof(FPSide.DataSources), destinationDataSourceOptions[0]);
                                    }
                                    else
                                    {
                                        var result = await ShowVariableDataSourceDialog(destinationDataSourceOptions, isDestination: true);

                                        if (!result.HasValue)
                                        {
                                            return;
                                        }

                                        destinationDataSource = result.Value;
                                    }
                                }

                                var uvChannel = 0;
                                if (destinationDataSource == FPSide.DataSources.Transparent &&
                                    destinationIsLayered)
                                {
                                    uvChannel = 1;
                                }

                                List<string> sourceDataSourceOptions = new List<string>();

                                if (!selectedSourceFPSide.WelandObject.Primary.Texture.IsEmpty())
                                {
                                    sourceDataSourceOptions.Add(FPSide.DataSources.Primary.ToString());
                                }

                                if (!selectedSourceFPSide.WelandObject.Secondary.Texture.IsEmpty())
                                {
                                    sourceDataSourceOptions.Add(FPSide.DataSources.Secondary.ToString());
                                }

                                if (!selectedSourceFPSide.WelandObject.Transparent.Texture.IsEmpty())
                                {
                                    sourceDataSourceOptions.Add(FPSide.DataSources.Transparent.ToString());
                                }

                                FPSide.DataSources sourceDataSource;

                                if (sourceDataSourceOptions.Count == 0)
                                {
                                    // The source side has no textured surfaces, so exit
                                    return;
                                }
                                else if (sourceDataSourceOptions.Count == 1)
                                {
                                    sourceDataSource = (FPSide.DataSources)Enum.Parse(typeof(FPSide.DataSources), sourceDataSourceOptions[0]);
                                }
                                else
                                {
                                    var result = await ShowVariableDataSourceDialog(sourceDataSourceOptions, isDestination: false);

                                    if (!result.HasValue)
                                    {
                                        return;
                                    }

                                    sourceDataSource = result.Value;
                                }

                                short sourceX;
                                short sourceY;
                                short destinationHeight;
                                short sourceHeight;

                                switch(destinationDataSource)
                                {
                                    case FPSide.DataSources.Primary:
                                        destinationHeight = ParentFPSide.PrimaryHighHeight;
                                        break;
                                    case FPSide.DataSources.Secondary:
                                        destinationHeight = ParentFPSide.SecondaryHighHeight;
                                        break;
                                    case FPSide.DataSources.Transparent:
                                        destinationHeight = ParentFPSide.TransparentHighHeight;
                                        break;
                                    default:
                                        return;
                                }

                                switch (sourceDataSource)
                                {
                                    case FPSide.DataSources.Primary:
                                        sourceX = selectedSourceFPSide.WelandObject.Primary.X;
                                        // TODO: this needs to set the y based on vertical world distance
                                        //       instead of just matching it.
                                        //       Use this: (destinationHighHeight - sourceHighHeight) + sourceY
                                        //       Alternatively, it might be this: (sourceHighHeight - destinationHighHeight) + sourceY
                                        sourceY = selectedSourceFPSide.WelandObject.Primary.Y;

                                        sourceHeight = selectedSourceFPSide.PrimaryHighHeight;

                                        break;
                                    case FPSide.DataSources.Secondary:
                                        sourceX = selectedSourceFPSide.WelandObject.Secondary.X;
                                        // TODO: this needs to set the y based on vertical world distance
                                        //       instead of just matching it.
                                        //       Use this: (destinationHighHeight - sourceHighHeight) + sourceY
                                        //       Alternatively, it might be this: (sourceHighHeight - destinationHighHeight) + sourceY
                                        sourceY = selectedSourceFPSide.WelandObject.Secondary.Y;

                                        sourceHeight = selectedSourceFPSide.SecondaryHighHeight;

                                        break;
                                    case FPSide.DataSources.Transparent:
                                        sourceX = selectedSourceFPSide.WelandObject.Transparent.X;
                                        // TODO: this needs to set the y based on vertical world distance
                                        //       instead of just matching it.
                                        //       Use this: (destinationHighHeight - sourceHighHeight) + sourceY
                                        //       Alternatively, it might be this: (sourceHighHeight - destinationHighHeight) + sourceY
                                        sourceY = selectedSourceFPSide.WelandObject.Transparent.Y;

                                        sourceHeight = selectedSourceFPSide.TransparentHighHeight;

                                        break;
                                    default:
                                        return;
                                }

                                short horizontalOffset = neighborFlowsOutward ? (short)0 : FPLevel.Instance.FPLines[ParentFPSide.WelandObject.LineIndex].WelandObject.Length;
                                horizontalOffset += neighborIsLeft ? (short)0 : FPLevel.Instance.FPLines[selectedSourceFPSide.WelandObject.LineIndex].WelandObject.Length;

                                short newX = (short)(sourceX + horizontalOffset);
                                short newY = (short)(destinationHeight - sourceHeight + sourceY);

                                ParentFPSide.SetOffset(this,
                                                       destinationDataSource,
                                                       uvChannel,
                                                       newX,
                                                       newY,
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

                endDragShouldRemergeBatch = runtimeSurfaceLight.UnmergeBatch();

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

                if (endDragShouldRemergeBatch)
                {
                    endDragShouldRemergeBatch = false;
                    GetComponent<RuntimeSurfaceLight>().MergeBatch();
                }
            }
        }

        private async Task<FPSide.DataSources?> ShowLayerSourceDialog(bool isDestination)
        {
            if (!SideDestinationSelectionDialog)
            {
                SideDestinationSelectionDialog = Resources.Load<Dialog_ObjectSelector>($"Dialogs/Dialog - Side {(isDestination ? "Destination" : "Source")} Selection");
            }

            var dialogOptions = new List<string>()
                                {
                                    FPSide.DataSources.Primary.ToString(),
                                    FPSide.DataSources.Transparent.ToString()
                                };

            var dialogOptionLabels = new List<string>()
                                {
                                    "Inner",
                                    "Outer"
                                };

            var result = await DialogManager.Instance.DisplayQueuedDialog(SideDestinationSelectionDialog,
                                                                          dialogOptions,
                                                                          dialogOptionLabels);

            if (result == null)
            {
                return null;
            }

            return (FPSide.DataSources)Enum.Parse(typeof(FPSide.DataSources), result);
        }

        private async Task<FPSide.DataSources?> ShowVariableDataSourceDialog(List<string> dialogOptions, bool isDestination)
        {
            if (!SideDestinationSelectionDialog)
            {
                SideDestinationSelectionDialog = Resources.Load<Dialog_ObjectSelector>($"Dialogs/Dialog - Side {(isDestination ? "Destination" : "Source")} Selection");
            }

            var result = await DialogManager.Instance.DisplayQueuedDialog(SideDestinationSelectionDialog,
                                                                          dialogOptions);

            if (result == null)
            {
                return null;
            }

            return (FPSide.DataSources)Enum.Parse(typeof(FPSide.DataSources), result);
        }
    }
}
