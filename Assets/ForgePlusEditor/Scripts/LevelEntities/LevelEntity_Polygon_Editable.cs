#if !NO_EDITING
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using System.Collections.Generic;
using UnityEngine;
using Weland;
using Weland.Extensions;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Polygon : LevelEntity_GeometryBase, ISelectionDisplayable, IInspectable
    {
        private List<GameObject> selectionVisualizationIndicators = new List<GameObject>(16);

        public void SetSelectability(bool enabled)
        {
            // Intentionally empty - Selectability is handled in FPSurfacePolygon & the availability of SwitchFPLight buttons
        }

        public void DisplaySelectionState(bool state)
        {
            if (state)
            {
                CreateSelectionIndicators(CeilingSurface, isfloor: false);
                CreateSelectionIndicators(FloorSurface, isfloor: true);
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
                                  Resources.Load<Inspector_Base>("Inspectors/Inspector - Polygon") :
                                  Resources.Load<Inspector_Base>("Inspectors/Inspector - Polygon Textures");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        // TODO: actually set these up to use the new entity system
        public void SetOffset(DataSources surfaceType, short x, short y, bool rebatch)
        {
            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    if (NativeObject.CeilingTransferMode == 9 ||
                        NativeObject.CeilingTexture.UsesLandscapeCollection() ||
                        NativeObject.CeilingTexture.IsEmpty())
                    {
                        // Don't adjust UVs for landscape surfaces.
                        return;
                    }

                    NativeObject.CeilingOrigin.X = x;
                    NativeObject.CeilingOrigin.Y = y;

                    CeilingSurface.ApplyTexture(rebatchImmediately: rebatch);

                    break;
                case DataSources.Floor:
                    if (NativeObject.FloorTransferMode == 9 ||
                        NativeObject.FloorTexture.UsesLandscapeCollection() ||
                        NativeObject.FloorTexture.IsEmpty())
                    {
                        // Don't adjust UVs for landscape surfaces.
                        return;
                    }

                    NativeObject.FloorOrigin.X = x;
                    NativeObject.FloorOrigin.Y = y;
                    
                    FloorSurface.ApplyTexture(rebatchImmediately: rebatch);

                    break;
                default:
                    return;
            }
        }

        public void SetShapeDescriptor(DataSources surfaceType, ShapeDescriptor shapeDescriptor)
        {
            short transferMode;

            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    if (shapeDescriptor.Equals(NativeObject.CeilingTexture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    MaterialGeneration_Geometry.DecrementTextureUsage(NativeObject.CeilingTexture);

                    NativeObject.CeilingTexture = shapeDescriptor;
                    transferMode = NativeObject.CeilingTransferMode;

                    break;
                case DataSources.Floor:
                    if (shapeDescriptor.Equals(NativeObject.FloorTexture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    MaterialGeneration_Geometry.DecrementTextureUsage(NativeObject.FloorTexture);

                    NativeObject.FloorTexture = shapeDescriptor;
                    transferMode = NativeObject.FloorTransferMode;

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

            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    NativeObject.CeilingTransferMode = newTransferMode;
                    CeilingSurface.ApplyTexture();
                    break;
                case DataSources.Floor:
                    NativeObject.FloorTransferMode = newTransferMode;
                    FloorSurface.ApplyTexture();
                    break;
            }
        }

        private void CreateSelectionIndicators(RuntimeSurfaceGeometry surface, bool isfloor)
        {
            var vertices = surface.GetComponent<MeshCollider>().sharedMesh.vertices;

            var localToWorldMatrix = surface.transform.localToWorldMatrix;

            for (var i = 0; i < vertices.Length; i++)
            {
                var currentVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i]);
                Vector3 previousVertexWorldPosition;
                Vector3 nextVertexWorldPosition;

                if (isfloor)
                {
                    previousVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i >= 1 ? i - 1 : vertices.Length - 1]);
                    nextVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i < vertices.Length - 1 ? i + 1 : 0]);
                }
                else
                {
                    previousVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i < vertices.Length - 1 ? i + 1 : 0]);
                    nextVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i >= 1 ? i - 1 : vertices.Length - 1]);
                }

                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator($"Vertex ({i})", surface.transform, currentVertexWorldPosition, nextVertexWorldPosition, previousVertexWorldPosition));
            }
        }
    }
}
#endif
