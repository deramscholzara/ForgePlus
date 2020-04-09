﻿using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Weland;
using UnityEngine.UI;
using ForgePlus.ShapesCollections;

namespace ForgePlus.Inspection
{
    public class InspectorFPPolygon : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_Permutation;

        public TextMeshProUGUI Value_MediaIndex;
        public TextMeshProUGUI Value_MediaLight;

        public TextMeshProUGUI Value_AmbientSound;
        public TextMeshProUGUI Value_RandomSound;

        public TextMeshProUGUI Value_Floor_Height;
        public RawImage Value_Floor_Texture;
        public TextMeshProUGUI Value_Floor_Offset;
        public TextMeshProUGUI Value_Floor_LightIndex;
        public TextMeshProUGUI Value_Floor_TransferMode;

        public TextMeshProUGUI Value_Ceiling_Height;
        public RawImage Value_Ceiling_Texture;
        public TextMeshProUGUI Value_Ceiling_Offset;
        public TextMeshProUGUI Value_Ceiling_LightIndex;
        public TextMeshProUGUI Value_Ceiling_TransferMode;

        public TextMeshProUGUI Value_VertexCount;
        public TextMeshProUGUI Value_VertexIndices;
        public TextMeshProUGUI Value_LineIndices;
        public TextMeshProUGUI Value_SideIndices;
        public TextMeshProUGUI Value_FirstObjectIndex;
        public TextMeshProUGUI Value_AdjacentPolygonIndices;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpPolygon = inspectedObject as FPPolygon;

            Value_Id.text = fpPolygon.Index.ToString();
            Value_Type.text = fpPolygon.WelandObject.Type.ToString();
            Value_Permutation.text = fpPolygon.WelandObject.Permutation.ToString();

            Value_MediaIndex.text = fpPolygon.WelandObject.MediaIndex.ToString();
            Value_MediaLight.text = fpPolygon.WelandObject.MediaLight.ToString();

            Value_AmbientSound.text = fpPolygon.WelandObject.AmbientSound.ToString();
            Value_RandomSound.text = fpPolygon.WelandObject.RandomSound.ToString();

            Value_Floor_Height.text = fpPolygon.WelandObject.FloorHeight.ToString();
            var floorTexture = WallsCollection.GetTexture(fpPolygon.WelandObject.FloorTexture);
            Value_Floor_Texture.texture = floorTexture ? floorTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Floor_Offset.text = fpPolygon.WelandObject.FloorOrigin.ToString();
            Value_Floor_LightIndex.text = fpPolygon.WelandObject.FloorLight.ToString();
            Value_Floor_TransferMode.text = fpPolygon.WelandObject.FloorTransferMode.ToString();

            Value_Ceiling_Height.text = fpPolygon.WelandObject.CeilingHeight.ToString();
            var ceilingTexture = WallsCollection.GetTexture(fpPolygon.WelandObject.CeilingTexture);
            Value_Ceiling_Texture.texture = ceilingTexture ? ceilingTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Ceiling_Offset.text = fpPolygon.WelandObject.CeilingOrigin.ToString();
            Value_Ceiling_LightIndex.text = fpPolygon.WelandObject.CeilingLight.ToString();
            Value_Ceiling_TransferMode.text = fpPolygon.WelandObject.CeilingTransferMode.ToString();

            Value_VertexCount.text = fpPolygon.WelandObject.VertexCount.ToString();

            var endpointIndices = string.Empty;
            for (var i = 0; i < fpPolygon.WelandObject.VertexCount; i++)
            {
                var index = fpPolygon.WelandObject.EndpointIndexes[i].ToString();

                if (i == 0)
                {
                    endpointIndices += index;
                }
                else
                {
                    endpointIndices += $"\n{index}";
                }
            }

            Value_VertexIndices.text = endpointIndices;

            var lineIndices = string.Empty;
            for (var i = 0; i < fpPolygon.WelandObject.VertexCount; i++)
            {
                var index = fpPolygon.WelandObject.LineIndexes[i].ToString();

                if (i == 0)
                {
                    lineIndices += index;
                }
                else
                {
                    lineIndices += $"\n{index}";
                }
            }

            Value_LineIndices.text = lineIndices;

            var sideIndices = string.Empty;
            for (var i = 0; i < fpPolygon.WelandObject.VertexCount; i++)
            {
                var index = fpPolygon.WelandObject.SideIndexes[i] < 0 ? "- no side -" : fpPolygon.WelandObject.SideIndexes[i].ToString();

                if (i == 0)
                {
                    sideIndices += index;
                }
                else
                {
                    sideIndices += $"\n{index}";
                }
            }

            Value_SideIndices.text = sideIndices;

            var adjacentPolygonIndices = string.Empty;
            for (var i = 0; i < fpPolygon.WelandObject.VertexCount; i++)
            {
                var index = fpPolygon.WelandObject.AdjacentPolygonIndexes[i] < 0 ? "- no polygon -" : fpPolygon.WelandObject.AdjacentPolygonIndexes[i].ToString();

                if (i == 0)
                {
                    adjacentPolygonIndices += index;
                }
                else
                {
                    adjacentPolygonIndices += $"\n{index}";
                }
            }

            Value_SideIndices.text = adjacentPolygonIndices;

            Value_FirstObjectIndex.text = fpPolygon.WelandObject.FirstObjectIndex.ToString();
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject (casted to FPMapObject in this case)
            throw new System.NotImplementedException();
        }
    }
}
