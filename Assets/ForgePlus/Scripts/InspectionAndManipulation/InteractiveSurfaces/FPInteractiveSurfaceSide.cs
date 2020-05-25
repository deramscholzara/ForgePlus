using ForgePlus.ApplicationGeneral;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
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
        private struct AlignmentGroupee
        {
            public FPSide SourceSide;
            public FPSide.DataSources SourceDataSource;

            public FPSide DestinationSide;
            public FPSide.DataSources DestinationDataSource;
            public bool DestinationFlowsOutward;
            public bool DestinationIsLeftOfSource;
            public FPInteractiveSurfaceSide DestinationSurface;
        }

        private static Dialog_ObjectSelector SideDestinationSelectionDialog = null;

        public FPSide ParentFPSide = null;
        public FPSide.DataSources DataSource;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        private UVPlanarDrag uvDragPlane;
        private bool endDragShouldRemergeBatch = false;

        private List<AlignmentGroupee> alignmentGroup = new List<AlignmentGroupee>();

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

                            var destinationUVChannel = (destinationDataSource == FPSide.DataSources.Transparent && destinationIsLayered) ? 1 : 0;
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

                            AlignDestinationToSource(selectedSourceFPSide, sourceDataSource, ParentFPSide, destinationDataSource, neighborFlowsOutward, neighborIsLeft, rebatch: true);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPSide, multiSelect: false);
                        InputListener(ParentFPSide);

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

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    alignmentGroup.Clear();

                    CollectSimilarContiguousAdjacentSurfaces(ParentFPSide, DataSource);

                    for (var i = 0; i < alignmentGroup.Count; i++)
                    {
                        CollectSimilarContiguousAdjacentSurfaces(alignmentGroup[i].DestinationSide, alignmentGroup[i].DestinationDataSource);
                    }
                }
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

                foreach (var alignmentGroupee in alignmentGroup)
                {
                    AlignDestinationToSource(alignmentGroupee.SourceSide,
                                             alignmentGroupee.SourceDataSource,
                                             alignmentGroupee.DestinationSide,
                                             alignmentGroupee.DestinationDataSource,
                                             alignmentGroupee.DestinationFlowsOutward,
                                             alignmentGroupee.DestinationIsLeftOfSource,
                                             rebatch: false);
                }
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

                    foreach (var alignmentGroupee in alignmentGroup)
                    {
                        alignmentGroupee.DestinationSurface.GetComponent<RuntimeSurfaceLight>().MergeBatch();
                    }
                }

                alignmentGroup.Clear();
            }
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnDirectionalInputDown(Vector2 direction)
        {
            base.OnDirectionalInputDown(direction);

            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
                    {
                        var destinationIsLayered = ParentFPSide.WelandObject.HasLayeredTransparentSide(FPLevel.Instance.Level);
                        var destinationDataSource = DataSource;
                        var uvChannel = 0;

                        var newX = (short)(-direction.x * GeometryUtilities.UnitsPerTextureOffetNudge);
                        var newY = (short)(direction.y * GeometryUtilities.UnitsPerTextureOffetNudge);

                        if (destinationIsLayered &&
                            (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                        {
                            destinationDataSource = FPSide.DataSources.Transparent;
                            uvChannel = 1;
                        }

                        switch (destinationDataSource)
                        {
                            case FPSide.DataSources.Primary:
                                newX += (short)(Mathf.RoundToInt(ParentFPSide.WelandObject.Primary.X / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentFPSide.WelandObject.Primary.Y / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            case FPSide.DataSources.Secondary:
                                newX += (short)(Mathf.RoundToInt(ParentFPSide.WelandObject.Secondary.X / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentFPSide.WelandObject.Secondary.Y / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            case FPSide.DataSources.Transparent:
                                newX += (short)(Mathf.RoundToInt(ParentFPSide.WelandObject.Transparent.X / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentFPSide.WelandObject.Transparent.Y / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            default:
                                return;
                        }

                        ParentFPSide.SetOffset(this,
                                               destinationDataSource,
                                               uvChannel,
                                               newX,
                                               newY,
                                               rebatch: true);
                    }

                    break;
                default:
                    return;
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

        private void AlignDestinationToSource(FPSide sourceSide, FPSide.DataSources sourceDataSource, FPSide destinationSide, FPSide.DataSources destinationDataSource, bool destinationFlowsOutward, bool destinationIsLeftOfSource, bool rebatch)
        {
            short sourceX;
            short sourceY;
            short destinationHeight;
            short sourceHeight;
            FPInteractiveSurfaceSide destinationSurface;

            switch (destinationDataSource)
            {
                case FPSide.DataSources.Primary:
                    destinationHeight = destinationSide.PrimaryHighHeight;
                    destinationSurface = destinationSide.PrimarySurface;
                    break;
                case FPSide.DataSources.Secondary:
                    destinationHeight = destinationSide.SecondaryHighHeight;
                    destinationSurface = destinationSide.SecondarySurface;
                    break;
                case FPSide.DataSources.Transparent:
                    destinationHeight = destinationSide.TransparentHighHeight;
                    destinationSurface = destinationSide.TransparentSurface;
                    break;
                default:
                    return;
            }

            switch (sourceDataSource)
            {
                case FPSide.DataSources.Primary:
                    sourceX = sourceSide.WelandObject.Primary.X;
                    sourceY = sourceSide.WelandObject.Primary.Y;

                    sourceHeight = sourceSide.PrimaryHighHeight;

                    break;
                case FPSide.DataSources.Secondary:
                    sourceX = sourceSide.WelandObject.Secondary.X;
                    sourceY = sourceSide.WelandObject.Secondary.Y;

                    sourceHeight = sourceSide.SecondaryHighHeight;

                    break;
                case FPSide.DataSources.Transparent:
                    sourceX = sourceSide.WelandObject.Transparent.X;
                    sourceY = sourceSide.WelandObject.Transparent.Y;

                    sourceHeight = sourceSide.TransparentHighHeight;

                    break;
                default:
                    return;
            }

            short horizontalOffset = destinationIsLeftOfSource ?
                                     (short)-FPLevel.Instance.FPLines[destinationSide.WelandObject.LineIndex].WelandObject.Length :
                                     FPLevel.Instance.FPLines[sourceSide.WelandObject.LineIndex].WelandObject.Length;

            short newX = (short)(sourceX + horizontalOffset);
            short newY = (short)(sourceHeight - destinationHeight + sourceY);

            var destinationUVChannel = (destinationDataSource == FPSide.DataSources.Transparent && destinationSide.WelandObject.HasLayeredTransparentSide(FPLevel.Instance.Level)) ? 1 : 0;

            destinationSide.SetOffset(destinationSurface,
                                      destinationDataSource,
                                      destinationUVChannel,
                                      newX,
                                      newY,
                                      rebatch);
        }

        private void CollectSimilarContiguousAdjacentSurfaces(FPSide centralSide, FPSide.DataSources centralDataSource)
        {
            CollectSimilarContiguousAdjacentSurfaces(centralSide, centralDataSource, left: true);

            CollectSimilarContiguousAdjacentSurfaces(centralSide, centralDataSource, left: false);
        }

        private void CollectSimilarContiguousAdjacentSurfaces(FPSide centralSide, FPSide.DataSources centralDataSource, bool left)
        {
            var centralLine = FPLevel.Instance.Level.Lines[centralSide.WelandObject.LineIndex];

            var centralEndpointIndex = centralSide.WelandObject.EndpointIndex(FPLevel.Instance.Level, centralLine, left);
            var neighborLines = FPLevel.Instance.Level.EndpointLines[centralEndpointIndex];

            foreach (var neighborLine in neighborLines)
            {
                if (neighborLine == centralLine)
                {
                    continue;
                }

                var neighborFlowsOutward = neighborLine.EndpointIndexes[0] == centralEndpointIndex;
                var neighborIsClockwise = neighborFlowsOutward != left;

                var neighborSide = neighborLine.GetFPSide(FPLevel.Instance.Level, neighborIsClockwise);

                if (neighborSide == null)
                {
                    continue;
                }

                if (CheckIfSimilarAndContiguous(centralSide, centralDataSource,
                                                neighborSide, FPSide.DataSources.Primary,
                                                neighborFlowsOutward, left,
                                                out var alignmentGroupeePrimary) &&
                    !alignmentGroup.Any(surface => surface.DestinationSurface == alignmentGroupeePrimary.DestinationSurface) &&
                    alignmentGroupeePrimary.DestinationSurface != this)
                {
                    alignmentGroup.Add(alignmentGroupeePrimary);
                }

                if (CheckIfSimilarAndContiguous(centralSide, centralDataSource,
                                                neighborSide, FPSide.DataSources.Secondary,
                                                neighborFlowsOutward, left,
                                                out var alignmentGroupeeSecondary) &&
                    !alignmentGroup.Any(surface => surface.DestinationSurface == alignmentGroupeeSecondary.DestinationSurface) &&
                    alignmentGroupeeSecondary.DestinationSurface != this)
                {
                    alignmentGroup.Add(alignmentGroupeeSecondary);
                }

                if (CheckIfSimilarAndContiguous(centralSide, centralDataSource,
                                                neighborSide, FPSide.DataSources.Transparent,
                                                neighborFlowsOutward, left,
                                                out var alignmentGroupeeTransparent) &&
                    !alignmentGroup.Any(surface => surface.DestinationSurface == alignmentGroupeeTransparent.DestinationSurface) &&
                    alignmentGroupeeTransparent.DestinationSurface != this)
                {
                    alignmentGroup.Add(alignmentGroupeeTransparent);
                }
            }
        }

        private bool CheckIfSimilarAndContiguous(FPSide sourceSide, FPSide.DataSources sourceDataSource,
                                                 FPSide destinationSide, FPSide.DataSources destinationDataSource,
                                                 bool destinationFlowsOutward, bool destinationIsLeftOfSource,
                                                 out AlignmentGroupee alignmentGroupee)
        {
            short sourceLowHeight = 0;
            short sourceHighHeight = 0;
            ShapeDescriptor sourceShapeDescriptor = ShapeDescriptor.Empty;

            switch (sourceDataSource)
            {
                case FPSide.DataSources.Primary:
                    sourceLowHeight = sourceSide.PrimaryLowHeight;
                    sourceHighHeight = sourceSide.PrimaryHighHeight;
                    sourceShapeDescriptor = sourceSide.WelandObject.Primary.Texture;
                    break;
                case FPSide.DataSources.Secondary:
                    sourceLowHeight = sourceSide.SecondaryLowHeight;
                    sourceHighHeight = sourceSide.SecondaryHighHeight;
                    sourceShapeDescriptor = sourceSide.WelandObject.Secondary.Texture;
                    break;
                case FPSide.DataSources.Transparent:
                    sourceLowHeight = sourceSide.TransparentLowHeight;
                    sourceHighHeight = sourceSide.TransparentHighHeight;
                    sourceShapeDescriptor = sourceSide.WelandObject.Transparent.Texture;
                    break;
            }

            alignmentGroupee = new AlignmentGroupee();
            alignmentGroupee.SourceSide = sourceSide;
            alignmentGroupee.SourceDataSource = sourceDataSource;
            alignmentGroupee.DestinationSide = destinationSide;
            alignmentGroupee.DestinationDataSource = destinationDataSource;
            alignmentGroupee.DestinationFlowsOutward = destinationFlowsOutward;
            alignmentGroupee.DestinationIsLeftOfSource = destinationIsLeftOfSource;

            switch (destinationDataSource)
            {
                case FPSide.DataSources.Primary:
                    if (destinationSide.PrimarySurface &&
                        sourceLowHeight <= destinationSide.PrimaryHighHeight &&
                        destinationSide.PrimaryLowHeight <= sourceHighHeight &&
                        destinationSide.WelandObject.Primary.Texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.PrimarySurface;
                        alignmentGroupee.DestinationSurface.GetComponent<RuntimeSurfaceLight>().UnmergeBatch();
                        return true;
                    }

                    break;

                case FPSide.DataSources.Secondary:
                    if (destinationSide.SecondarySurface &&
                        sourceLowHeight <= destinationSide.SecondaryHighHeight &&
                        destinationSide.SecondaryLowHeight <= sourceHighHeight &&
                        destinationSide.WelandObject.Secondary.Texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.SecondarySurface;
                        alignmentGroupee.DestinationSurface.GetComponent<RuntimeSurfaceLight>().UnmergeBatch();
                        return true;
                    }

                    break;

                case FPSide.DataSources.Transparent:
                    if (destinationSide.TransparentSurface &&
                        sourceLowHeight <= destinationSide.TransparentHighHeight &&
                        destinationSide.TransparentLowHeight <= sourceHighHeight &&
                        destinationSide.WelandObject.Transparent.Texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.TransparentSurface;
                        alignmentGroupee.DestinationSurface.GetComponent<RuntimeSurfaceLight>().UnmergeBatch();
                        return true;
                    }

                    break;
            }

            alignmentGroupee = new AlignmentGroupee();
            return false;
        }
    }
}
