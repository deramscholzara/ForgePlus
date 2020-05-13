using ForgePlus.ApplicationGeneral;
using ForgePlus.Inspection;
using ForgePlus.Palette;
using System;
using System.Collections.Generic;
using System.Linq;
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
                                    // Dialog was cancelled, so exit
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

                        if (!selectedSourceFPSide)
                        {
                            // There is no selection to use as a source, so exit
                            return;
                        }

                        var destinationIsSource = selectedSourceFPSide == ParentFPSide;

                        // Assign defaults for when the destination is the source (instead of a neighbor).
                        // True for both of these means that there will be no offset difference from source to destination.
                        var neighborFlowsOutward = true;
                        var neighborIsLeft = true;

                        if (destinationIsSource ||
                            selectedSourceFPSide.WelandObject.SideIsNeighbor(FPLevel.Instance.Level,
                                                                             ParentFPSide.WelandObject,
                                                                             out neighborFlowsOutward,
                                                                             out neighborIsLeft))
                        {
                            #region Alignment_DataSource_Destination
                            var destinationIsLayered = ParentFPSide.WelandObject.HasLayeredTransparentSide(FPLevel.Instance.Level);
                            var destinationDataSource = DataSource;

                            if (destinationIsLayered)
                            {
                                var result = await ShowLayerSourceDialog(isDestination: true);

                                if (!result.HasValue)
                                {
                                    // Dialog was cancelled, so exit
                                    return;
                                }

                                destinationDataSource = result.Value;
                            }

                            var destinationUVChannel = 0;
                            if (destinationDataSource == FPSide.DataSources.Transparent &&
                                destinationIsLayered)
                            {
                                destinationUVChannel = 1;
                            }
                            #endregion Alignment_DataSource_Destination

                            #region Alignment_DataSource_Source
                            FPSide.DataSources sourceDataSource;

                            if (destinationIsSource && destinationIsLayered)
                            {
                                sourceDataSource = destinationDataSource == FPSide.DataSources.Primary ? FPSide.DataSources.Transparent : FPSide.DataSources.Primary;
                            }
                            else
                            {
                                List<FPSide.DataSources> sourceDataSourceOptions = new List<FPSide.DataSources>();

                                foreach (var dataSource in Enum.GetValues(typeof(FPSide.DataSources)).Cast<FPSide.DataSources>())
                                {
                                    if (selectedSourceFPSide.WelandObject.HasDataSource(dataSource) &&
                                        (!destinationIsSource || dataSource != DataSource))
                                    {
                                        sourceDataSourceOptions.Add(dataSource);
                                    }
                                }

                                if (sourceDataSourceOptions.Count == 0)
                                {
                                    // The source side has no textured surfaces,
                                    // or the source is the destination and there was only one data source,
                                    // so exit
                                    return;
                                }
                                else if (sourceDataSourceOptions.Count == 1)
                                {
                                    sourceDataSource = sourceDataSourceOptions[0];
                                }
                                else
                                {
                                    var result = await ShowVariableDataSourceDialog(sourceDataSourceOptions.Select(source => source.ToString()).ToList(), isDestination: false);

                                    if (!result.HasValue)
                                    {
                                        // Dialog was cancelled, so exit
                                        return;
                                    }

                                    sourceDataSource = result.Value;
                                }
                            }
                            #endregion Alignment_DataSource_Source

                            short sourceX;
                            short sourceY;
                            short destinationHeight;
                            short sourceHeight;

                            switch (destinationDataSource)
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
                                    sourceY = selectedSourceFPSide.WelandObject.Primary.Y;

                                    sourceHeight = selectedSourceFPSide.PrimaryHighHeight;

                                    break;
                                case FPSide.DataSources.Secondary:
                                    sourceX = selectedSourceFPSide.WelandObject.Secondary.X;
                                    sourceY = selectedSourceFPSide.WelandObject.Secondary.Y;

                                    sourceHeight = selectedSourceFPSide.SecondaryHighHeight;

                                    break;
                                case FPSide.DataSources.Transparent:
                                    sourceX = selectedSourceFPSide.WelandObject.Transparent.X;
                                    sourceY = selectedSourceFPSide.WelandObject.Transparent.Y;

                                    sourceHeight = selectedSourceFPSide.TransparentHighHeight;

                                    break;
                                default:
                                    return;
                            }

                            short horizontalOffset = neighborFlowsOutward ? (short)0 : FPLevel.Instance.FPLines[ParentFPSide.WelandObject.LineIndex].WelandObject.Length;
                            if (neighborIsLeft)
                            {
                                horizontalOffset *= -1;
                            }

                            horizontalOffset += neighborIsLeft ? (short)0 : FPLevel.Instance.FPLines[selectedSourceFPSide.WelandObject.LineIndex].WelandObject.Length;

                            short newX = (short)(sourceX + horizontalOffset);
                            short newY = (short)(sourceHeight - destinationHeight + sourceY);

                            ParentFPSide.SetOffset(this,
                                                    destinationDataSource,
                                                    destinationUVChannel,
                                                    newX,
                                                    newY,
                                                    rebatch: true);
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
                    return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
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
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
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
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
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
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
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
