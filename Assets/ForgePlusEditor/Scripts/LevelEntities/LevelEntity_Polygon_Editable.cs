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
            // Geometry and Sounds modes inspect the polygon itself (with its sounds), and the others its textures
            var inspector = ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry ||
                            ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Sounds ?
                            (Inspector_Base)new Inspector_Polygon(this) :
                            new Inspector_PolygonTextures(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void SetOffset(DataSources surfaceType, short x, short y, bool rebatch)
        {
            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    if (AlephOneExtensions.IsLandscapeTransferMode(NativeObject.ceiling_transfer_mode) ||
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
                    if (AlephOneExtensions.IsLandscapeTransferMode(NativeObject.floor_transfer_mode) ||
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
                // A landscape texture is drawn as a landscape (keeping a big landscape's mode)
                newTransferMode = AlephOneExtensions.IsLandscapeTransferMode(transferMode) ? transferMode : AlephOne.map._xfer_landscape;
            }
            else if (!AlephOneExtensions.IsLandscapeTransferMode(transferMode))
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

        // The material depends on the transfer mode too (a landscape's is its own), besides how the texture moves
        public void SetTransferMode(DataSources dataSource, short transferMode)
        {
            switch (dataSource)
            {
                case DataSources.Ceiling:
                    if (transferMode == NativeObject.ceiling_transfer_mode)
                    {
                        // Transfer mode is not different, so exit
                        return;
                    }

                    NativeObject.ceiling_transfer_mode = transferMode;
                    CeilingSurface.ApplyTexture();
                    CeilingSurface.ApplyTransferMode();
                    break;
                case DataSources.Floor:
                    if (transferMode == NativeObject.floor_transfer_mode)
                    {
                        // Transfer mode is not different, so exit
                        return;
                    }

                    NativeObject.floor_transfer_mode = transferMode;
                    FloorSurface.ApplyTexture();
                    FloorSurface.ApplyTransferMode();
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
                case DataSources.Media:
                    if (lightIndex == NativeObject.media_lightsource_index)
                    {
                        // Light is not different, so exit
                        return;
                    }

                    NativeObject.media_lightsource_index = lightIndex;

                    // Without media, there's no surface to light (the light is kept for when it has one)
                    if (MediaSurface)
                    {
                        MediaSurface.ApplyLight();
                    }

                    break;
                default:
                    return;
            }
        }

        // Puts the media in the polygon (or takes it out, for null), making, retyping or removing its media surface to
        // match
        public void SetMedia(LevelEntity_Media media)
        {
            var mediaIndex = media != null ? media.NativeIndex : (short) -1;
            if (mediaIndex == NativeObject.media_index)
            {
                // Media is not different, so exit
                return;
            }

            NativeObject.media_index = mediaIndex;

            if (media == null)
            {
                if (MediaSurface)
                {
                    ParentLevel.EditableSurface_Medias.Remove(MediaSurface.GetComponent<EditableSurface_Media>());

                    // With no media, the surface destroys itself
                    MediaSurface.ApplyMedia();
                    MediaSurface = null;
                }

                return;
            }

            // The media surface is lit by the polygon's media light, which a polygon that had no media may not have
            if (!ParentLevel.Lights.ContainsKey(NativeObject.media_lightsource_index))
            {
                NativeObject.media_lightsource_index = NativeObject.floor_lightsource_index;
            }

            if (MediaSurface)
            {
                MediaSurface.ApplyMedia();
            }
            else
            {
                CreateMediaSurface();
                SelectionManager.Instance.MatchSelectabilityToMode(MediaSurface.GetComponent<EditableSurface_Media>());
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
