using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Localization;
using ForgePlus.Palette;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

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

        public LevelEntity_Side ParentSide = null;
        public LevelEntity_Side.DataSources DataSource;

        public LevelEntity_Platform Platform = null;

        // Read from the side each time, so they follow what's painted onto it (a surface with no side data has no
        // texture or light)
        public ushort SurfaceShapeDescriptor
        {
            get
            {
                return ParentSide.NativeObject != null ? ParentSide.NativeObject.GetTexture(DataSource).texture : cstypes.UNONE;
            }
        }

        public LevelEntity_Light RuntimeLight
        {
            get
            {
                return ParentSide.NativeObject != null ? ParentSide.ParentLevel.Lights[ParentSide.NativeObject.GetLightsourceIndex(DataSource)] : null;
            }
        }

        private UVPlanarDrag uvDragPlane;

        private List<AlignmentGroupee> alignmentGroup = new List<AlignmentGroupee>();

        public async override void OnValidatedPointerClick(WorldPointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(ParentSide, multiSelect: false);

                    break;
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        // A surface with no side data has nowhere to store a texture
                        if (ParentSide.NativeObject != null &&
                            PaletteManager.Instance.TryGetSelectedTexture(out var selectedTexture))
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
                             ForgePlusInput.Editing.AlignToSelection.IsPressed())
                    {
                        var selectedSourceObject = SelectionManager.Instance.SelectedObject;
                        var selectedSourceSide = (selectedSourceObject is LevelEntity_Side) ? selectedSourceObject as LevelEntity_Side : null;

                        if (!selectedSourceSide)
                        {
                            // There is no selection to use as a source, so exit
                            return;
                        }

                        if (selectedSourceSide.NativeObject == null || ParentSide.NativeObject == null)
                        {
                            // A surface with no side data has no texture to align, so exit
                            return;
                        }

                        var destinationIsSource = selectedSourceSide == ParentSide;

                        // Assign defaults for when the destination is the source (instead of a neighbor).
                        // True for both of these means that there will be no offset difference from source to destination.
                        var neighborFlowsOutward = true;
                        var neighborIsLeft = true;

                        if (destinationIsSource ||
                            selectedSourceSide.NativeObject.SideIsNeighbor(LevelEntity_Level.Instance,
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

                        // An unassigned surface selects "None"
                        PaletteManager.Instance.SelectSwatchForTexture(SurfaceShapeDescriptor);
                    }

                    break;
                case ModeManager.PrimaryModes.Lights:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        var selectedLight = PaletteManager.Instance.GetSelectedLight();

                        if (selectedLight != null)
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

                            ParentSide.SetLight(destinationDataSource, selectedLight.NativeIndex);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(RuntimeLight, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForLight(RuntimeLight);
                    }

                    break;
                case ModeManager.PrimaryModes.Media:
                    // A side is the polygon's it faces into
                    if (ParentSide.FacingPolygon)
                    {
                        ClickPolygonInMediaMode(ParentSide.FacingPolygon);
                    }

                    break;
                case ModeManager.PrimaryModes.Sounds:
                    // A side is the polygon's it faces into
                    if (ParentSide.FacingPolygon)
                    {
                        ClickPolygonInSoundsMode(ParentSide.FacingPolygon);
                    }

                    break;
                case ModeManager.PrimaryModes.Platforms:
                    // Any face of a platform (either surface of one that goes both ways, or the sides that move with it)
                    // selects it, or deselects it if it's selected
                    if (Platform != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Platform.SelectablePlatform, multiSelect: false);
                    }

                    break;
                case ModeManager.PrimaryModes.Annotations:
                    // A side is the polygon's it faces into
                    if (ParentSide.FacingPolygon)
                    {
                        LevelEntity_Annotation.ClickPolygon(ParentSide.FacingPolygon);
                    }

                    break;
                case ModeManager.PrimaryModes.Terminals:
                    // A computer terminal panel previews its terminal, as the game shows it when it's used
                    if (ParentSide.TryGetTerminalIndex(out var terminalIndex))
                    {
                        ForgePlusUI.Instance.ShowTerminal(terminalIndex);
                    }

                    return;
                default:
                    Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
                    return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedBeginDrag(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                Vector2 startingUVs;

                var destinationIsLayered = ParentSide.NativeObject.HasLayeredTransparentSide(LevelEntity_Level.Instance.Level);
                var destinationDataSource = DataSource;

                if (destinationIsLayered &&
                    ForgePlusInput.Editing.TargetOuterLayer.IsPressed())
                {
                    destinationDataSource = LevelEntity_Side.DataSources.Transparent;

                    startingUVs = new Vector2(ParentSide.NativeObject.transparent_texture.x0, ParentSide.NativeObject.transparent_texture.y0);
                }
                else
                {
                    switch (destinationDataSource)
                    {
                        case LevelEntity_Side.DataSources.Primary:
                            startingUVs = new Vector2(ParentSide.NativeObject.primary_texture.x0, ParentSide.NativeObject.primary_texture.y0);
                            break;
                        case LevelEntity_Side.DataSources.Secondary:
                            startingUVs = new Vector2(ParentSide.NativeObject.secondary_texture.x0, ParentSide.NativeObject.secondary_texture.y0);
                            break;
                        case LevelEntity_Side.DataSources.Transparent:
                            startingUVs = new Vector2(ParentSide.NativeObject.transparent_texture.x0, ParentSide.NativeObject.transparent_texture.y0);
                            break;
                        default:
                            return;
                    }
                }

                var startingPosition = eventData.PressWorldPosition;

                var surfaceWorldNormal = eventData.PressWorldNormal;
                var textureWorldUp = Vector3.up;

                uvDragPlane = new UVPlanarDrag(startingUVs,
                                               startingPosition,
                                               surfaceWorldNormal,
                                               textureWorldUp);

                if (ForgePlusInput.Editing.AlignContiguous.IsPressed())
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

        public override void OnValidatedDrag(WorldPointerEventData eventData)
        {
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                var screenPosition = new Vector3(eventData.Position.x,
                                                 eventData.Position.y,
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

        public override void OnValidatedEndDrag(WorldPointerEventData eventData)
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
                            ForgePlusInput.Editing.TargetOuterLayer.IsPressed())
                        {
                            destinationDataSource = LevelEntity_Side.DataSources.Transparent;
                        }

                        switch (destinationDataSource)
                        {
                            case LevelEntity_Side.DataSources.Primary:
                                newX += (short)(Mathf.RoundToInt(ParentSide.NativeObject.primary_texture.x0 / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentSide.NativeObject.primary_texture.y0 / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            case LevelEntity_Side.DataSources.Secondary:
                                newX += (short)(Mathf.RoundToInt(ParentSide.NativeObject.secondary_texture.x0 / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentSide.NativeObject.secondary_texture.y0 / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                                break;
                            case LevelEntity_Side.DataSources.Transparent:
                                newX += (short)(Mathf.RoundToInt(ParentSide.NativeObject.transparent_texture.x0 / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                                newY += (short)(Mathf.RoundToInt(ParentSide.NativeObject.transparent_texture.y0 / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

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

        private static string DataSourceDialogTitle(bool isDestination)
        {
            return isDestination ? Strings.Get(Strings.Common, "Surface.Side.DataSourceDialog.Title.Destination") : Strings.Get(Strings.Common, "Surface.Side.DataSourceDialog.Title.Source");
        }

        private static string DataSourceLabel(string dataSource)
        {
            switch (dataSource)
            {
                case nameof(LevelEntity_Side.DataSources.Primary):
                    return Strings.Get(Strings.Common, "Surface.Side.DataSourceDialog.Source.Primary");
                case nameof(LevelEntity_Side.DataSources.Secondary):
                    return Strings.Get(Strings.Common, "Surface.Side.DataSourceDialog.Source.Secondary");
                case nameof(LevelEntity_Side.DataSources.Transparent):
                    return Strings.Get(Strings.Common, "Surface.Side.DataSourceDialog.Source.Transparent");
                default:
                    return dataSource;
            }
        }

        private async Task<LevelEntity_Side.DataSources?> ShowLayerSourceDialog(bool isDestination)
        {
            var dialogOptions = new List<string>()
                                {
                                    LevelEntity_Side.DataSources.Primary.ToString(),
                                    LevelEntity_Side.DataSources.Transparent.ToString()
                                };

            var dialogOptionLabels = new List<string>()
                                {
                                    Strings.Get(Strings.Common, "Surface.Side.DataSourceDialog.Layer.Inner"),
                                    Strings.Get(Strings.Common, "Surface.Side.DataSourceDialog.Layer.Outer")
                                };

            var result = await DialogManager.Instance.DisplayQueuedDialog(DataSourceDialogTitle(isDestination),
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
            var dialogOptionLabels = dialogOptions.Select(DataSourceLabel).ToList();

            var result = await DialogManager.Instance.DisplayQueuedDialog(DataSourceDialogTitle(isDestination),
                                                                          dialogOptions,
                                                                          dialogOptionLabels);

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
                    sourceX = sourceSide.NativeObject.primary_texture.x0;
                    sourceY = sourceSide.NativeObject.primary_texture.y0;

                    sourceHeight = sourceSide.PrimaryHighElevation;

                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    sourceX = sourceSide.NativeObject.secondary_texture.x0;
                    sourceY = sourceSide.NativeObject.secondary_texture.y0;

                    sourceHeight = sourceSide.SecondaryHighElevation;

                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    sourceX = sourceSide.NativeObject.transparent_texture.x0;
                    sourceY = sourceSide.NativeObject.transparent_texture.y0;

                    sourceHeight = sourceSide.TransparentHighElevation;

                    break;
                default:
                    return;
            }

            short horizontalOffset = destinationIsLeftOfSource ?
                                     (short)-LevelEntity_Level.Instance.Lines[destinationSide.NativeObject.line_index].NativeObject.length :
                                     LevelEntity_Level.Instance.Lines[sourceSide.NativeObject.line_index].NativeObject.length;

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
            var centralLine = LevelEntity_Level.Instance.Level.LineList[centralSide.NativeObject.line_index];

            var centralEndpointIndex = centralSide.NativeObject.EndpointIndex(centralLine, left);
            var neighborLineIndexes = LevelEntity_Level.Instance.EndpointLines[centralEndpointIndex];

            foreach (var neighborLineIndex in neighborLineIndexes)
            {
                var neighborLine = LevelEntity_Level.Instance.Level.LineList[neighborLineIndex];

                if (neighborLine == centralLine)
                {
                    continue;
                }

                var neighborFlowsOutward = neighborLine.endpoint_indexes[0] == centralEndpointIndex;
                var neighborIsClockwise = neighborFlowsOutward != left;

                var neighborSide = neighborLine.GetRuntimeSide(neighborIsClockwise);

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
            ushort sourceShapeDescriptor = cstypes.UNONE;

            switch (sourceDataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    sourceLowHeight = sourceSide.PrimaryLowElevation;
                    sourceHighHeight = sourceSide.PrimaryHighElevation;
                    sourceShapeDescriptor = sourceSide.NativeObject.primary_texture.texture;
                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    sourceLowHeight = sourceSide.SecondaryLowElevation;
                    sourceHighHeight = sourceSide.SecondaryHighElevation;
                    sourceShapeDescriptor = sourceSide.NativeObject.secondary_texture.texture;
                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    sourceLowHeight = sourceSide.TransparentLowElevation;
                    sourceHighHeight = sourceSide.TransparentHighElevation;
                    sourceShapeDescriptor = sourceSide.NativeObject.transparent_texture.texture;
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
                        destinationSide.NativeObject.primary_texture.texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.PrimarySurface;
                        return true;
                    }

                    break;

                case LevelEntity_Side.DataSources.Secondary:
                    if (destinationSide.SecondarySurface &&
                        sourceLowHeight <= destinationSide.SecondaryHighElevation &&
                        destinationSide.SecondaryLowElevation <= sourceHighHeight &&
                        destinationSide.NativeObject.secondary_texture.texture.Equals(sourceShapeDescriptor))
                    {
                        alignmentGroupee.DestinationSurface = destinationSide.SecondarySurface;
                        return true;
                    }

                    break;

                case LevelEntity_Side.DataSources.Transparent:
                    if (destinationSide.TransparentSurface &&
                        sourceLowHeight <= destinationSide.TransparentHighElevation &&
                        destinationSide.TransparentLowElevation <= sourceHighHeight &&
                        destinationSide.NativeObject.transparent_texture.texture.Equals(sourceShapeDescriptor))
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
