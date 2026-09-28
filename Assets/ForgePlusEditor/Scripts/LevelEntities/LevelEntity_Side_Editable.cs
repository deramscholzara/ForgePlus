#if !NO_EDITING
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

        public void Inspect()
        {
            var inspectorPrefab = ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry ?
                                  Resources.Load<Inspector_Base>("Inspectors/Inspector - Side") :
                                  Resources.Load<Inspector_Base>("Inspectors/Inspector - Side Textures");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);

            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry)
            {
                ParentLevel.Lines[NativeObject.line_index].Inspect();
            }
        }

        // TODO: actually set these up to use the new entity system
        public void SetOffset(DataSources dataSource, short x, short y, bool rebatch)
        {
            switch (dataSource)
            {
                case DataSources.Primary:
                    if (NativeObject.primary_transfer_mode == 9 ||
                        NativeObject.primary_texture.texture.UsesLandscapeCollection() ||
                        NativeObject.primary_texture.texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.primary_texture.x0 = x;
                    NativeObject.primary_texture.y0 = y;

                    PrimarySurface.ApplyTextureOffset(rebatchImmediately: rebatch);

                    break;
                case DataSources.Secondary:
                    if (NativeObject.secondary_transfer_mode == 9 ||
                        NativeObject.secondary_texture.texture.UsesLandscapeCollection() ||
                        NativeObject.secondary_texture.texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.secondary_texture.x0 = x;
                    NativeObject.secondary_texture.y0 = y;

                    SecondarySurface.ApplyTextureOffset(rebatchImmediately: rebatch);

                    break;
                case DataSources.Transparent:
                    if (NativeObject.transparent_transfer_mode == 9 ||
                        NativeObject.transparent_texture.texture.UsesLandscapeCollection() ||
                        NativeObject.transparent_texture.texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.transparent_texture.x0 = x;
                    NativeObject.transparent_texture.y0 = y;

                    TransparentSurface.ApplyTextureOffset(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level),
                                                          rebatchImmediately: rebatch);

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

            short newTransferMode = 0;
            if (shapeDescriptor.UsesLandscapeCollection())
            {
                newTransferMode = 9;
            }
            else if (transferMode != 9)
            {
                newTransferMode = transferMode;
            }

            switch (dataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    NativeObject.primary_transfer_mode = newTransferMode;
                    PrimarySurface.ApplyTexture();
                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    NativeObject.secondary_transfer_mode = newTransferMode;
                    SecondarySurface.ApplyTexture();
                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    NativeObject.transparent_transfer_mode = newTransferMode;
                    TransparentSurface.ApplyTexture(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level));
                    break;
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

                    PrimarySurface.ApplyLight();

                    break;
                case DataSources.Secondary:
                    if (lightIndex == NativeObject.secondary_lightsource_index ||
                        NativeObject.secondary_texture.texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.secondary_lightsource_index = lightIndex;

                    SecondarySurface.ApplyLight();

                    break;
                case DataSources.Transparent:
                    if (lightIndex == NativeObject.transparent_lightsource_index ||
                        NativeObject.transparent_texture.texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.transparent_lightsource_index = lightIndex;

                    TransparentSurface.ApplyLight(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level));

                    break;
                default:
                    return;
            }
        }
    }
}
#endif
