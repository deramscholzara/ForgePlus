using ForgePlus.ApplicationGeneral;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;
using Weland.Extensions;

namespace ForgePlus.Entities.Geometry
{
    public class EditableSurface_Side : EditableSurface_Base
    {
        private struct AlignmentGroupee
        {
            public LevelEntity_Side SourceSide;
            public LevelEntity_Side.DataSources SourceDataSource;

            public LevelEntity_Side DestinationSide;
            public LevelEntity_Side.DataSources DestinationDataSource;
            public bool DestinationFlowsOutward;
            public bool DestinationIsLeftOfSource;
            public RuntimeSurfaceGeometry DestinationSurface;
        }

        private static Dialog_ObjectSelector SideDestinationSelectionDialog = null;

        public LevelEntity_Side ParentSide = null;
        public LevelEntity_Side.DataSources DataSource;

        // TODO: Get rid of these and just attain them on the fly instead of preloading
        //       Maybe include a reference to the context-typed RuntimeSurfaceGeometry component, to help
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public LevelEntity_Light RuntimeLight = null;
        public LevelEntity_Media Media = null;
        public LevelEntity_Platform Platform = null;

        private UVPlanarDrag uvDragPlane;

        private List<AlignmentGroupee> alignmentGroup = new List<AlignmentGroupee>();

        public async override void OnValidatedPointerClick(PointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(ParentSide, multiSelect: false);

                    break;
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        var selectedTexture = PaletteManager.Instance.GetSelectedTexture();

                        if (!selectedTexture.IsEmpty())
                        {
                            var destinationIsLayered = ParentSide.NativeObject.HasLayeredTransparentSide(LevelEntity_Level.Instance.Level);
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

                            ParentSide.SetShapeDescriptor(destinationDataSource, selectedTexture);
                        }
                    }
                    else if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing &&
                             Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        var selectedSourceObject = SelectionManager.Instance.SelectedObject;
                        var selectedSourceSide = (selectedSourceObject is LevelEntity_Side) ? selectedSourceObject as LevelEntity_Side : null;

                        if (!selectedSourceSide)
                        {
                            // There is no selection to use as a source, so exit
                            return;
                        }

                        var destinationIsSource = selectedSourceSide == ParentSide;

                        // Assign defaults for when the destination is the source (instead of a neighbor).
                        // True for both of these means that there will be no offset difference from source to destination.
                        var neighborFlowsOutward = true;
                        var neighborIsLeft = true;

                        if (destinationIsSource ||
                            selectedSourceSide.NativeObject.SideIsNeighbor(LevelEntity_Level.Instance.Level,
                                                                             ParentSide.NativeObject,
                                                                             out neighborFlowsOutward,
                                                                             out neighborIsLeft))
                        {
                            #region Alignment_DataSource_Destination
                            var destinationIsLayered = ParentSide.NativeObject.HasLayeredTransparentSide(LevelEntity_Level.Instance.Level);
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

                            var destinationUVChannel = (destinationDataSource == LevelEntity_Side.DataSources.Transparent && destinationIsLayered) ? 1 : 0;
                            #endregion Alignment_DataSource_Destination

                            #region Alignment_DataSource_Source
                            LevelEntity_Side.DataSources sourceDataSource;

                            if (destinationIsSource && destinationIsLayered)
                            {
                                sourceDataSource = destinationDataSource == LevelEntity_Side.DataSources.Primary ? LevelEntity_Side.DataSources.Transparent : LevelEntity_Side.DataSources.Primary;
                            }
                            else
                            {
                                List<LevelEntity_Side.DataSources> sourceDataSourceOptions = new List<LevelEntity_Side.DataSources>();

                                foreach (var dataSource in Enum.GetValues(typeof(LevelEntity_Side.DataSources)).Cast<LevelEntity_Side.DataSources>())
                                {
                                    if (selectedSourceSide.NativeObject.HasDataSource(dataSource) &&
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

                            AlignDestinationToSource(selectedSourceSide, sourceDataSource, ParentSide, destinationDataSource, neighborFlowsOutward, neighborIsLeft, rebatch: true);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(ParentSide, multiSelect: false);
                        InputListener(ParentSide);

                        if (!surfaceShapeDescriptor.IsEmpty())
                        {
                            PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor, invokeToggleEvents: false);
                        }
                    }

                    break;
                case ModeManager.PrimaryModes.Lights:
                    SelectionManager.Instance.ToggleObjectSelection(RuntimeLight, multiSelect: false);
                    PaletteManager.Instance.SelectSwatchForLight(RuntimeLight, invokeToggleEvents: false);
                    break;
                case ModeManager.PrimaryModes.Media:
                    if (Media != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Media, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForMedia(Media, invokeToggleEvents: false);
                    }

                    break;
                case ModeManager.PrimaryModes.Platforms:
                    if (Platform != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Platform, multiSelect: false);
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
                Vector2 startingUVs;

                var destinationIsLayered = ParentSide.NativeObject.HasLayeredTransparentSide(LevelEntity_Level.Instance.Level);
                var destinationDataSource = DataSource;

                if (destinationIsLayered &&
                    (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                {
                    destinationDataSource = LevelEntity_Side.DataSources.Transparent;

                    startingUVs = new Vector2(ParentSide.NativeObject.Transparent.X, ParentSide.NativeObject.Transparent.Y);
                }
                else
                {
                    switch (destinationDataSource)
                    {
                        case LevelEntity_Side.DataSources.Primary:
                            startingUVs = new Vector2(ParentSide.NativeObject.Primary.X, ParentSide.NativeObject.Primary.Y);
                            break;
                        case LevelEntity_Side.DataSources.Secondary:
                            startingUVs = new Vector2(ParentSide.NativeObject.Secondary.X, ParentSide.NativeObject.Secondary.Y);
                            break;
                        case LevelEntity_Side.DataSources.Transparent:
                            startingUVs = new Vector2(ParentSide.NativeObject.Transparent.X, ParentSide.NativeObject.Transparent.Y);
                            break;
                        default:
                            return;
                    }
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

                    CollectSimilarContiguousAdjacentSurfaces(ParentSide, destinationDataSource);

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

                ParentSide.SetOffset(DataSource,
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

                if (SurfaceBatchingManager.BatchingEnabled)
                {
                    SurfaceBatchingManager.Instance.MergeAllBatches();
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
                        var destinationIsLayered = ParentSide.NativeObject.HasLayeredTransparentSide(LevelEntity_Level.Instance.Level);
                        var destinationDataSource = DataSource;

                        var newX = (short)(-direction.x * GeometryUtilities.UnitsPerTextureOffetNudge);
                        var newY = (short)(direction.y * GeometryUtilities.UnitsPerTextureOffetNudge);

                        if (destinationIsLayered &&
                            (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                        {
                            destinationDataSource = LevelEntity_Side.DataSources.Transparent;
                        }

                        switch (destinationDataSource)
                        {
                            case LevelEntity_Side.DataSources.Primary:
                                newX += (short)(Mathf.RoundToInt(ParentSide.NativeObject.Primary.X / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentSide.NativeObject.Primary.Y / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            case LevelEntity_Side.DataSources.Secondary:
                                newX += (short)(Mathf.RoundToInt(ParentSide.NativeObject.Secondary.X / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentSide.NativeObject.Secondary.Y / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            case LevelEntity_Side.DataSources.Transparent:
                                newX += (short)(Mathf.RoundToInt(ParentSide.NativeObject.Transparent.X / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentSide.NativeObject.Transparent.Y / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            default:
                                return;
                        }

                        ParentSide.SetOffset(destinationDataSource,
                                               newX,
                                               newY,
                                               rebatch: true);
                    }

                    break;
                default:
                    return;
            }
        }

        private async Task<LevelEntity_Side.DataSources?> ShowLayerSourceDialog(bool isDestination)
        {
            if (!SideDestinationSelectionDialog)
            {
                SideDestinationSelectionDialog = Resources.Load<Dialog_ObjectSelector>($"Dialogs/Dialog - Side {(isDestination ? "Destination" : "Source")} Selection");
            }

            var dialogOptions = new List<string>()
                                {
                                    LevelEntity_Side.DataSources.Primary.ToString(),
                                    LevelEntity_Side.DataSources.Transparent.ToString()
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

            return (LevelEntity_Side.DataSources)Enum.Parse(typeof(LevelEntity_Side.DataSources), result);
        }

        private async Task<LevelEntity_Side.DataSources?> ShowVariableDataSourceDialog(List<string> dialogOptions, bool isDestination)
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

            return (LevelEntity_Side.DataSources)Enum.Parse(typeof(LevelEntity_Side.DataSources), result);
        }

        private void AlignDestinationToSource(LevelEntity_Side sourceSide, LevelEntity_Side.DataSources sourceDataSource, LevelEntity_Side destinationSide, LevelEntity_Side.DataSources destinationDataSource, bool destinationFlowsOutward, bool destinationIsLeftOfSource, bool rebatch)
        {
            short sourceX;
            short sourceY;
            short destinationHeight;
            short sourceHeight;

            switch (destinationDataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    destinationHeight = destinationSide.PrimaryHighElevation;
                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    destinationHeight = destinationSide.SecondaryHighElevation;
                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    destinationHeight = destinationSide.TransparentHighElevation;
                    break;
                default:
                    return;
            }

            switch (sourceDataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    sourceX = sourceSide.NativeObject.Primary.X;
                    sourceY = sourceSide.NativeObject.Primary.Y;

                    sourceHeight = sourceSide.PrimaryHighElevation;

                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    sourceX = sourceSide.NativeObject.Secondary.X;
                    sourceY = sourceSide.NativeObject.Secondary.Y;

                    sourceHeight = sourceSide.SecondaryHighElevation;

                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    sourceX = sourceSide.NativeObject.Transparent.X;
                    sourceY = sourceSide.NativeObject.Transparent.Y;

                    sourceHeight = sourceSide.TransparentHighElevation;

                    break;
                default:
                    return;
            }

            short horizontalOffset = destinationIsLeftOfSource ?
                                     (short)-LevelEntity_Level.Instance.Lines[destinationSide.NativeObject.LineIndex].NativeObject.Length :
                                     LevelEntity_Level.Instance.Lines[sourceSide.NativeObject.LineIndex].NativeObject.Length;

            short newX = (short)(sourceX + horizontalOffset);
            short newY = (short)(sourceHeight - destinationHeight + sourceY);

            destinationSide.SetOffset(destinationDataSource,
                                      newX,
                                      newY,
                                      rebatch);
        }

        private void CollectSimilarContiguousAdjacentSurfaces(LevelEntity_Side centralSide, LevelEntity_Side.DataSources centralDataSource)
        {
            CollectSimilarContiguousAdjacentSurfaces(centralSide, centralDataSource, left: true);

            CollectSimilarContiguousAdjacentSurfaces(centralSide, centralDataSource, left: false);
        }

        private void CollectSimilarContiguousAdjacentSurfaces(LevelEntity_Side centralSide, LevelEntity_Side.DataSources centralDataSource, bool left)
        {
            var centralLine = LevelEntity_Level.Instance.Level.Lines[centralSide.NativeObject.LineIndex];

            var centralEndpointIndex = centralSide.NativeObject.EndpointIndex(LevelEntity_Level.Instance.Level, centralLine, left);
            var neighborLines = LevelEntity_Level.Instance.Level.EndpointLines[centralEndpointIndex];

            foreach (var neighborLine in neighborLines)
            {
                if (neighborLine == centralLine)
                {
                    continue;
                }

                var neighborFlowsOutward = neighborLine.EndpointIndexes[0] == centralEndpointIndex;
                var neighborIsClockwise = neighborFlowsOutward != left;

                var neighborSide = neighborLine.GetRuntimeSide(LevelEntity_Level.Instance.Level, neighborIsClockwise);

                if (neighborSide == null)
                {
                    continue;
                }

                if (CheckIfSimilarAndContiguous(centralSide, centralDataSource,
                                                neighborSide, LevelEntity_Side.DataSources.Primary,
                                                neighborFlowsOutward, left,
                                                out var alignmentGroupeePrimary) &&
                    !alignmentGroup.Any(surface => surface.DestinationSurface == alignmentGroupeePrimary.DestinationSurface) &&
                    alignmentGroupeePrimary.DestinationSurface != this)
                {
                    alignmentGroup.Add(alignmentGroupeePrimary);
                }

                if (CheckIfSimilarAndContiguous(centralSide, centralDataSource,
                                                neighborSide, LevelEntity_Side.DataSources.Secondary,
                                                neighborFlowsOutward, left,
                                                out var alignmentGroupeeSecondary) &&
                    !alignmentGroup.Any(surface => surface.DestinationSurface == alignmentGroupeeSecondary.DestinationSurface) &&
                    alignmentGroupeeSecondary.DestinationSurface != this)
                {
                    alignmentGroup.Add(alignmentGroupeeSecondary);
                }

                if (CheckIfSimilarAndContiguous(centralSide, centralDataSource,
                                                neighborSide, LevelEntity_Side.DataSources.Transparent,
                                                neighborFlowsOutward, left,
                                                out var alignmentGroupeeTransparent) &&
                    !alignmentGroup.Any(surface => surface.DestinationSurface == alignmentGroupeeTransparent.DestinationSurface) &&
                    alignmentGroupeeTransparent.DestinationSurface != this)
                {
                    alignmentGroup.Add(alignmentGroupeeTransparent);
                }
            }
        }

        private bool CheckIfSimilarAndContiguous(LevelEntity_Side sourceSide, LevelEntity_Side.DataSources sourceDataSource,
                                                 LevelEntity_Side destinationSide, LevelEntity_Side.DataSources destinationDataSource,
                                                 bool destinationFlowsOutward, bool destinationIsLeftOfSource,
                                                 out AlignmentGroupee alignmentGroupee)
        {
            short sourceLowHeight = 0;
            short sourceHighHeight = 0;
            ShapeDescriptor sourceShapeDescriptor = ShapeDescriptor.Empty;

            switch (sourceDataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    sourceLowHeight = sourceSide.PrimaryLowElevation;
                    sourceHighHeight = sourceSide.PrimaryHighElevation;
                    sourceShapeDescriptor = sourceSide.NativeObject.Primary.Texture;
                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    sourceLowHeight = sourceSide.SecondaryLowElevation;
                    sourceHighHeight = sourceSide.SecondaryHighElevation;
                    sourceShapeDescriptor = sourceSide.NativeObject.Secondary.Texture;
                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    sourceLowHeight = sourceSide.TransparentLowElevation;
                    sourceHighHeight = sourceSide.TransparentHighElevation;
                    sourceShapeDescriptor = sourceSide.NativeObject.Transparent.Texture;
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
                case LevelEntity_Side.DataSources.Primary:
                    if (destinationSide.PrimarySurface &&
                        sourceLowHeight <= destinationSide.PrimaryHighElevation &&
                        destinationSide.PrimaryLowElevation <= sourceHighHeight &&
                        destinationSide.NativeObject.Primary.Texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.PrimarySurface;
                        return true;
                    }

                    break;

                case LevelEntity_Side.DataSources.Secondary:
                    if (destinationSide.SecondarySurface &&
                        sourceLowHeight <= destinationSide.SecondaryHighElevation &&
                        destinationSide.SecondaryLowElevation <= sourceHighHeight &&
                        destinationSide.NativeObject.Secondary.Texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.SecondarySurface;
                        return true;
                    }

                    break;

                case LevelEntity_Side.DataSources.Transparent:
                    if (destinationSide.TransparentSurface &&
                        sourceLowHeight <= destinationSide.TransparentHighElevation &&
                        destinationSide.TransparentLowElevation <= sourceHighHeight &&
                        destinationSide.NativeObject.Transparent.Texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.TransparentSurface;
                        return true;
                    }

                    break;
            }

            alignmentGroupee = new AlignmentGroupee();
            return false;
        }
    }
}
