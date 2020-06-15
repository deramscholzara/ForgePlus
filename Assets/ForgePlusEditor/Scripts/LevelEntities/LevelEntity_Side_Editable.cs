#if !NO_EDITING
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using Weland.Extensions;
using System.Collections.Generic;
using UnityEngine;
using Weland;

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
                    var mesh = TopSurface.GetComponent<MeshCollider>().sharedMesh;

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
                    var mesh = MiddleSurface.GetComponent<MeshCollider>().sharedMesh;

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
                    var mesh = BottomSurface.GetComponent<MeshCollider>().sharedMesh;

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
                    Destroy(indicator);
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
                ParentLevel.Lines[NativeObject.LineIndex].Inspect();
            }
        }

        // TODO: actually set these up to use the new entity system
        public void SetOffset(DataSources dataSource, short x, short y, bool rebatch)
        {
            switch (dataSource)
            {
                case DataSources.Primary:
                    if (NativeObject.PrimaryTransferMode == 9 ||
                        NativeObject.Primary.Texture.UsesLandscapeCollection() ||
                        NativeObject.Primary.Texture.IsEmpty())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.Primary.X = x;
                    NativeObject.Primary.Y = y;

                    PrimarySurface.ApplyTextureOffset(rebatchImmediately: rebatch);

                    break;
                case DataSources.Secondary:
                    if (NativeObject.SecondaryTransferMode == 9 ||
                        NativeObject.Secondary.Texture.UsesLandscapeCollection() ||
                        NativeObject.Secondary.Texture.IsEmpty())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.Secondary.X = x;
                    NativeObject.Secondary.Y = y;

                    SecondarySurface.ApplyTextureOffset(rebatchImmediately: rebatch);

                    break;
                case DataSources.Transparent:
                    if (NativeObject.TransparentTransferMode == 9 ||
                        NativeObject.Transparent.Texture.UsesLandscapeCollection() ||
                        NativeObject.Transparent.Texture.IsEmpty())
                    {
                        // Don't adjust UVs for landscape or unassigned surfaces.
                        return;
                    }

                    NativeObject.Transparent.X = x;
                    NativeObject.Transparent.Y = y;

                    TransparentSurface.ApplyTextureOffset(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level),
                                                          rebatchImmediately: rebatch);

                    break;
                default:
                    return;
            }
        }

        public void SetShapeDescriptor(DataSources dataSource, ShapeDescriptor shapeDescriptor)
        {
            short transferMode;

            switch (dataSource)
            {
                case DataSources.Primary:
                    if (shapeDescriptor.Equals(NativeObject.Primary.Texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.Primary.Texture = shapeDescriptor;
                    transferMode = NativeObject.PrimaryTransferMode;

                    break;
                case DataSources.Secondary:
                    if (shapeDescriptor.Equals(NativeObject.Secondary.Texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.Secondary.Texture = shapeDescriptor;
                    transferMode = NativeObject.SecondaryTransferMode;

                    break;
                case DataSources.Transparent:
                    if (shapeDescriptor.Equals(NativeObject.Transparent.Texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.Transparent.Texture = shapeDescriptor;
                    transferMode = NativeObject.TransparentTransferMode;

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
                    NativeObject.PrimaryTransferMode = newTransferMode;
                    PrimarySurface.ApplyTexture();
                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    NativeObject.SecondaryTransferMode = newTransferMode;
                    SecondarySurface.ApplyTexture();
                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    NativeObject.TransparentTransferMode = newTransferMode;
                    TransparentSurface.ApplyTexture(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level));
                    break;
            }
        }

        public void SetLight(DataSources dataSource, short lightIndex)
        {
            switch (dataSource)
            {
                case DataSources.Primary:
                    if (lightIndex == NativeObject.PrimaryLightsourceIndex ||
                        NativeObject.Primary.Texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.PrimaryLightsourceIndex = lightIndex;

                    PrimarySurface.ApplyLight();

                    break;
                case DataSources.Secondary:
                    if (lightIndex == NativeObject.SecondaryLightsourceIndex ||
                        NativeObject.Secondary.Texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.SecondaryLightsourceIndex = lightIndex;

                    SecondarySurface.ApplyLight();

                    break;
                case DataSources.Transparent:
                    if (lightIndex == NativeObject.TransparentLightsourceIndex ||
                        NativeObject.Transparent.Texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.TransparentLightsourceIndex = lightIndex;

                    TransparentSurface.ApplyLight(innerLayer: !NativeObject.HasLayeredTransparentSide(ParentLevel.Level));

                    break;
                default:
                    return;
            }
        }
    }
}
#endif
