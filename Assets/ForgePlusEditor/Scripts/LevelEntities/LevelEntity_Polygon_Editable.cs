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
    public partial class LevelEntity_Polygon : LevelEntity_GeometryBase, ISelectionDisplayable, IInspectable
    {
        private List<GameObject> selectionVisualizationIndicators = new List<GameObject>(16);

        public void SetSelectability(bool enabled)
        {
            // Intentionally empty - Selectability is handled in EditableSurface_Polygon
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
                    GeometryUtilities.DestroySurfaceSelectionIndicator(indicator);
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

        public void SetOffset(DataSources surfaceType, short x, short y, bool rebatch)
        {
            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    if (NativeObject.ceiling_transfer_mode == 9 ||
                        NativeObject.ceiling_texture.UsesLandscapeCollection() ||
                        NativeObject.ceiling_texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape surfaces.
                        return;
                    }

                    NativeObject.ceiling_origin.x = x;
                    NativeObject.ceiling_origin.y = y;

                    CeilingSurface.ApplyTextureOffset(rebatchImmediately: rebatch);

                    break;
                case DataSources.Floor:
                    if (NativeObject.floor_transfer_mode == 9 ||
                        NativeObject.floor_texture.UsesLandscapeCollection() ||
                        NativeObject.floor_texture.IsEmptyShapeDescriptor())
                    {
                        // Don't adjust UVs for landscape surfaces.
                        return;
                    }

                    NativeObject.floor_origin.x = x;
                    NativeObject.floor_origin.y = y;
                    
                    FloorSurface.ApplyTextureOffset(rebatchImmediately: rebatch);

                    break;
                default:
                    return;
            }
        }

        public void SetShapeDescriptor(DataSources surfaceType, ushort shapeDescriptor)
        {
            short transferMode;

            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    if (shapeDescriptor.Equals(NativeObject.ceiling_texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.ceiling_texture = shapeDescriptor;
                    transferMode = NativeObject.ceiling_transfer_mode;

                    break;
                case DataSources.Floor:
                    if (shapeDescriptor.Equals(NativeObject.floor_texture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    NativeObject.floor_texture = shapeDescriptor;
                    transferMode = NativeObject.floor_transfer_mode;

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
                    NativeObject.ceiling_transfer_mode = newTransferMode;
                    CeilingSurface.ApplyTexture();
                    break;
                case DataSources.Floor:
                    NativeObject.floor_transfer_mode = newTransferMode;
                    FloorSurface.ApplyTexture();
                    break;
            }
        }

        public void SetLight(DataSources dataSource, short lightIndex)
        {
            switch (dataSource)
            {
                case DataSources.Ceiling:
                    if (lightIndex == NativeObject.ceiling_lightsource_index ||
                        NativeObject.ceiling_texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.ceiling_lightsource_index = lightIndex;

                    CeilingSurface.ApplyLight();

                    break;
                case DataSources.Floor:
                    if (lightIndex == NativeObject.floor_lightsource_index ||
                        NativeObject.floor_texture.UsesLandscapeCollection())
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.floor_lightsource_index = lightIndex;

                    FloorSurface.ApplyLight();

                    break;
                default:
                    return;
            }
        }

        private void CreateSelectionIndicators(RuntimeSurfaceGeometry surface, bool isfloor)
        {
            var vertices = surface.GetComponent<MeshFilter>().sharedMesh.vertices;

            selectionVisualizationIndicators.AddRange(GeometryUtilities.FitSurfaceSelectionIndicators("Vertex", surface.transform, vertices, isCeiling: !isfloor));
        }
    }
}
#endif
