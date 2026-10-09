#if !NO_EDITING
using AlephOne;
using ForgePlus.Entities.Geometry;
using ForgePlus.Extensions;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Side : LevelEntity_GeometryBase, ISelectionDisplayable, IInspectable
    {
        private readonly List<GameObject> selectionVisualizationIndicators = new List<GameObject>(4);

        public void SetSelectability(bool enabled)
        {
            // Intentionally empty - Selectability is handled in EditableSurface_Side
        }

        public void DisplaySelectionState(bool state)
        {
            if (state)
            {
                bool collectedTopSurface = false;
                Vector3 topLeftWorldPosition = Vector3.zero;
                Vector3 topRightWorldPosition = Vector3.zero;
                Vector3 bottomRightWorldPosition = Vector3.zero;
                Vector3 bottomLeftWorldPosition = Vector3.zero;
                Transform topParent = null;
                Transform bottomParent = null;

                if (TopSurface)
                {
                    var localToWorldMatrix = TopSurface.transform.localToWorldMatrix;
                    var mesh = TopSurface.GetComponent<MeshFilter>().sharedMesh;

                    topLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[1]);
                    topRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[2]);

                    topParent = TopSurface.transform;

                    bottomRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[3]);
                    bottomLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[0]);

                    bottomParent = TopSurface.transform;

                    collectedTopSurface = true;
                }

                if (MiddleSurface)
                {
                    var localToWorldMatrix = MiddleSurface.transform.localToWorldMatrix;
                    var mesh = MiddleSurface.GetComponent<MeshFilter>().sharedMesh;

                    if (!collectedTopSurface)
                    {
                        topLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[1]);
                        topRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[2]);

                        topParent = MiddleSurface.transform;

                        collectedTopSurface = true;
                    }

                    bottomRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[3]);
                    bottomLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[0]);

                    bottomParent = MiddleSurface.transform;
                }

                if (BottomSurface)
                {
                    var localToWorldMatrix = BottomSurface.transform.localToWorldMatrix;
                    var mesh = BottomSurface.GetComponent<MeshFilter>().sharedMesh;

                    if (!collectedTopSurface)
                    {
                        topLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[1]);
                        topRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[2]);

                        topParent = BottomSurface.transform;
                    }

                    bottomRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[3]);
                    bottomLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[0]);

                    bottomParent = BottomSurface.transform;
                }

                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Top-Left", topParent, topLeftWorldPosition, topRightWorldPosition, bottomLeftWorldPosition));
                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Top-Right", topParent, topRightWorldPosition, bottomRightWorldPosition, topLeftWorldPosition));
                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Bottom-Right", bottomParent, bottomRightWorldPosition, bottomLeftWorldPosition, topRightWorldPosition));
                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Bottom-Left", bottomParent, bottomLeftWorldPosition, topLeftWorldPosition, bottomRightWorldPosition));
            }
            else
            {
                foreach (var indicator in selectionVisualizationIndicators)
                {
                    GeometryUtilities.DestroySurfaceSelectionIndicator(indicator);
                }

                selectionVisualizationIndicators.Clear();
            }
        }

        // Before it's rebuilt (such as by changing a height), it's taken out of the selection, the level and its batches
        public void PrepareForDestruction()
        {
            var selectionManager = SelectionManager.Instance;

            if (selectionManager.GetIsSelected(this))
            {
                selectionManager.DeselectObject(this, multiSelect: true);
            }

            foreach (var surface in GetComponentsInChildren<EditableSurface_Side>(includeInactive: true))
            {
                ParentLevel.EditableSurface_Sides.Remove(surface);

                if (selectionManager.ClickedSurface == surface)
                {
                    selectionManager.ClickedSurface = null;
                }
            }

            foreach (var surface in GetComponentsInChildren<RuntimeSurfaceGeometry>(includeInactive: true))
            {
                surface.PrepareForDestruction();
            }

            if (NativeObject == null)
            {
                ParentLevel.PlaceholderSides.Remove(this);
            }
            else if (ParentLevel.Sides.TryGetValue(NativeIndex, out var indexedSide) && indexedSide == this)
            {
                ParentLevel.Sides.Remove(NativeIndex);
            }
        }

        // A placeholder (with no side data) is inspected as a face of its line
        public void Inspect()
        {
            Inspector_Base inspector;
            if (NativeObject == null)
            {
                inspector = new Inspector_PlaceholderSide(this);
            }
            else if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry)
            {
                inspector = new Inspector_Side(this);
            }
            else
            {
                inspector = new Inspector_SideTextures(this);
            }

            InspectorPanel.Instance.AddInspector(inspector);

            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry)
            {
                ParentLevel.Lines[ParentLineIndex].Inspect();
            }
        }

        // The game sets a control panel's primary texture from its type and state (devices.cpp: set_control_panel_texture)
        public bool PanelSetsPrimaryTexture
        {
            get
            {
                return TryGetControlPanelDefinition(out _);
            }
        }

        // Whether the primary texture is its panel type's (active or inactive)
        public bool ShowsPanelTexture
        {
            get
            {
                var texture = NativeObject.primary_texture.texture;

                return TryGetControlPanelDefinition(out var definition) &&
                       !texture.IsEmptyShapeDescriptor() &&
                       texture.GetCollection() == definition.collection &&
                       (texture.GetShape() == definition.active_shape || texture.GetShape() == definition.inactive_shape);
            }
        }

        // As the game sets it (devices.cpp: set_control_panel_texture)
        public void ApplyPanelTexture()
        {
            if (!TryGetControlPanelDefinition(out var definition))
            {
                return;
            }

            var shape = map.GET_CONTROL_PANEL_STATUS(NativeObject) ? definition.active_shape : definition.inactive_shape;
            SetShapeDescriptor(DataSources.Primary, AlephOneExtensions.BuildShapeDescriptor(definition.collection, shape));
        }

        // A computer terminal panel's permutation (devices.cpp: change_panel_state)
        public bool TryGetTerminalIndex(out short terminalIndex)
        {
            terminalIndex = cstypes.NONE;

            if (!TryGetControlPanelDefinition(out _) ||
                devices.get_panel_class(NativeObject.control_panel_type) != map._panel_is_computer_terminal)
            {
                return false;
            }

            terminalIndex = NativeObject.control_panel_permutation;

            return true;
        }

        private bool TryGetControlPanelDefinition(out control_panel_definition definition)
        {
            definition = NativeObject != null && map.SIDE_IS_CONTROL_PANEL(NativeObject) ?
                         devices.get_control_panel_definition(NativeObject.control_panel_type) :
                         null;

            return definition != null;
        }

        // For data changed elsewhere (such as by undoing). Which surfaces it has is its line's to rebuild.
        public void ApplyAllSurfaces()
        {
            ApplySurface(PrimarySurface, innerLayer: true);
            ApplySurface(SecondarySurface, innerLayer: true);
            ApplySurface(TransparentSurface, innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level));

            foreach (var surface in new[] { TopSurface, MiddleSurface, BottomSurface })
            {
                if (surface)
                {
                    surface.ApplyAmbientDelta(rebatchImmediately: false);
                }
            }
        }

        private static void ApplySurface(RuntimeSurfaceGeometry surface, bool innerLayer)
        {
            if (!surface)
            {
                return;
            }

            surface.ApplyTexture(innerLayer, rebatchImmediately: false);
            surface.ApplyTransferMode(innerLayer, rebatchImmediately: false);
            surface.ApplyTextureOffset(innerLayer, rebatchImmediately: false);
            surface.ApplyLight(innerLayer, rebatchImmediately: false);
        }

        // TODO: actually set these up to use the new entity system
        public void SetOffset(DataSources dataSource, short x, short y, bool rebatch)
        {
            switch (dataSource)
            {
                case DataSources.Primary:
                    if (AlephOneExtensions.IsLandscapeTransferMode(NativeObject.primary_transfer_mode) ||
                        NativeObject.primary_texture.texture.UsesLandscapeCollection() ||
                        NativeObject.primary_texture.texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.primary_texture.x0 = x;
                    NativeObject.primary_texture.y0 = y;

                    if (PrimarySurface)
                    {
                        PrimarySurface.ApplyTextureOffset(rebatchImmediately: rebatch);
                    }

                    break;
                case DataSources.Secondary:
                    if (AlephOneExtensions.IsLandscapeTransferMode(NativeObject.secondary_transfer_mode) ||
                        NativeObject.secondary_texture.texture.UsesLandscapeCollection() ||
                        NativeObject.secondary_texture.texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.secondary_texture.x0 = x;
                    NativeObject.secondary_texture.y0 = y;

                    if (SecondarySurface)
                    {
                        SecondarySurface.ApplyTextureOffset(rebatchImmediately: rebatch);
                    }

                    break;
                case DataSources.Transparent:
                    if (AlephOneExtensions.IsLandscapeTransferMode(NativeObject.transparent_transfer_mode) ||
                        NativeObject.transparent_texture.texture.UsesLandscapeCollection() ||
                        NativeObject.transparent_texture.texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.transparent_texture.x0 = x;
                    NativeObject.transparent_texture.y0 = y;

                    if (TransparentSurface)
                    {
                        TransparentSurface.ApplyTextureOffset(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level),
                                                             rebatchImmediately: rebatch);
                    }

                    break;
                default:
                    return;
            }
        }

        public void SetShapeDescriptor(DataSources dataSource, ushort shapeDescriptor)
        {
            short transferMode;

            switch (dataSource)
            {
                case DataSources.Primary:
                    if (shapeDescriptor.Equals(NativeObject.primary_texture.texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.primary_texture.texture = shapeDescriptor;
                    transferMode = NativeObject.primary_transfer_mode;

                    break;
                case DataSources.Secondary:
                    if (shapeDescriptor.Equals(NativeObject.secondary_texture.texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.secondary_texture.texture = shapeDescriptor;
                    transferMode = NativeObject.secondary_transfer_mode;

                    break;
                case DataSources.Transparent:
                    if (shapeDescriptor.Equals(NativeObject.transparent_texture.texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.transparent_texture.texture = shapeDescriptor;
                    transferMode = NativeObject.transparent_transfer_mode;

                    break;
                default:
                    return;
            }

            var newTransferMode = LevelEditing.TransferModeForTexture(shapeDescriptor, transferMode);

            // A surface the side doesn't draw (one the inspector edits) has nothing to update
            switch (dataSource)
            {
                case DataSources.Primary:
                    NativeObject.primary_transfer_mode = newTransferMode;

                    if (PrimarySurface)
                    {
                        PrimarySurface.ApplyTexture();
                    }

                    break;
                case DataSources.Secondary:
                    NativeObject.secondary_transfer_mode = newTransferMode;

                    if (SecondarySurface)
                    {
                        SecondarySurface.ApplyTexture();
                    }

                    break;
                case DataSources.Transparent:
                    NativeObject.transparent_transfer_mode = newTransferMode;

                    if (TransparentSurface)
                    {
                        TransparentSurface.ApplyTexture(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level));
                    }

                    break;
            }

            // Whether the line has a transparent side, or a landscape, may have changed
            LineFlagsEditing.UpdateForSide(ParentLevel.Level, NativeObject);
        }

        // The material depends on the transfer mode too (a landscape's is its own)
        public void SetTransferMode(DataSources dataSource, short transferMode)
        {
            if (transferMode == NativeObject.GetTransferMode(dataSource))
            {
                // Transfer mode is not different, so exit
                return;
            }

            switch (dataSource)
            {
                case DataSources.Primary:
                    NativeObject.primary_transfer_mode = transferMode;

                    if (PrimarySurface)
                    {
                        PrimarySurface.ApplyTexture();
                        PrimarySurface.ApplyTransferMode();
                    }

                    break;
                case DataSources.Secondary:
                    NativeObject.secondary_transfer_mode = transferMode;

                    if (SecondarySurface)
                    {
                        SecondarySurface.ApplyTexture();
                        SecondarySurface.ApplyTransferMode();
                    }

                    break;
                case DataSources.Transparent:
                    var innerLayer = !NativeObject.HasLayeredTransparentSide(ParentLevel.Level);
                    NativeObject.transparent_transfer_mode = transferMode;

                    if (TransparentSurface)
                    {
                        TransparentSurface.ApplyTexture(innerLayer);
                        TransparentSurface.ApplyTransferMode(innerLayer);
                    }

                    break;
            }

            // Whether the line has a landscape may have changed
            LineFlagsEditing.UpdateForSide(ParentLevel.Level, NativeObject);
        }

        // Every section's surface is lit with the side's ambient delta
        public void SetAmbientDelta(int ambientDelta)
        {
            if (ambientDelta == NativeObject.ambient_delta)
            {
                // Ambient delta is not different, so exit
                return;
            }

            NativeObject.ambient_delta = ambientDelta;

            foreach (var surface in new[] { TopSurface, MiddleSurface, BottomSurface })
            {
                if (surface)
                {
                    surface.ApplyAmbientDelta();
                }
            }
        }

        public void SetLight(DataSources dataSource, short lightIndex)
        {
            switch (dataSource)
            {
                case DataSources.Primary:
                    if (lightIndex == NativeObject.primary_lightsource_index ||
                        NativeObject.primary_texture.texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.primary_lightsource_index = lightIndex;

                    if (PrimarySurface)
                    {
                        PrimarySurface.ApplyLight();
                    }

                    break;
                case DataSources.Secondary:
                    if (lightIndex == NativeObject.secondary_lightsource_index ||
                        NativeObject.secondary_texture.texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.secondary_lightsource_index = lightIndex;

                    if (SecondarySurface)
                    {
                        SecondarySurface.ApplyLight();
                    }

                    break;
                case DataSources.Transparent:
                    if (lightIndex == NativeObject.transparent_lightsource_index ||
                        NativeObject.transparent_texture.texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.transparent_lightsource_index = lightIndex;

                    if (TransparentSurface)
                    {
                        TransparentSurface.ApplyLight(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level));
                    }

                    break;
                default:
                    return;
            }
        }
    }
}
#endif
