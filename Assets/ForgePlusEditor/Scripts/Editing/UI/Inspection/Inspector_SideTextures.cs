using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using RuntimeCore.Entities.Geometry;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weland.Extensions;

namespace ForgePlus.Inspection
{
    public class Inspector_SideTextures : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;

        public RawImage Value_Primary_Texture;
        public TextMeshProUGUI Value_Primary_Offset;
        public TextMeshProUGUI Value_Primary_TransferMode;
        public TextMeshProUGUI Value_Primary_LightIndex;

        public RawImage Value_Secondary_Texture;
        public TextMeshProUGUI Value_Secondary_Offset;
        public TextMeshProUGUI Value_Secondary_TransferMode;
        public TextMeshProUGUI Value_Secondary_LightIndex;

        public RawImage Value_Transparent_Texture;
        public TextMeshProUGUI Value_Transparent_Offset;
        public TextMeshProUGUI Value_Transparent_TransferMode;
        public TextMeshProUGUI Value_Transparent_LightIndex;

        private LevelEntity_Side inspectedSide;

        private LevelEntity_Side InspectedSide
        {
            get
            {
                if (inspectedSide == null)
                {
                    inspectedSide = inspectedObject as LevelEntity_Side;
                }

                return inspectedSide;
            }
        }

        public override void RefreshValuesInInspector()
        {
            Value_Id.text =                         InspectedSide.NativeIndex.ToString();
            Value_Type.text =                       InspectedSide.NativeObject.Type.ToString();

            var hasPrimaryData =                    InspectedSide.NativeObject.Primary.Texture.IsEmpty();
            Value_Primary_Texture.texture =         hasPrimaryData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.Primary.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Primary_Offset.text =             hasPrimaryData ? $"X: {InspectedSide.NativeObject.Primary.X}\nY: {InspectedSide.NativeObject.Primary.Y}" : "X: -\nY: -";
            Value_Primary_LightIndex.text =         hasPrimaryData ? InspectedSide.NativeObject.PrimaryLightsourceIndex.ToString() : "-";
            Value_Primary_TransferMode.text =       hasPrimaryData ? InspectedSide.NativeObject.PrimaryTransferMode.ToString() : "-";

            var hasSecondaryData =                  InspectedSide.NativeObject.Secondary.Texture.IsEmpty();
            Value_Secondary_Texture.texture =       hasSecondaryData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.Secondary.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Secondary_Offset.text =           hasSecondaryData ? $"X: {InspectedSide.NativeObject.Secondary.X}\nY: {InspectedSide.NativeObject.Secondary.Y}" : "X: -\nY: -";
            Value_Secondary_LightIndex.text =       hasSecondaryData ? InspectedSide.NativeObject.SecondaryLightsourceIndex.ToString() : "-";
            Value_Secondary_TransferMode.text =     hasSecondaryData ? InspectedSide.NativeObject.SecondaryTransferMode.ToString() : "-";

            var hasTransparentData =                InspectedSide.NativeObject.Transparent.Texture.IsEmpty();
            Value_Transparent_Texture.texture =     hasTransparentData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.Transparent.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Transparent_Offset.text =         hasTransparentData ? $"X: {InspectedSide.NativeObject.Transparent.X}\nY: {InspectedSide.NativeObject.Transparent.Y}" : "X: -\nY: -";
            Value_Transparent_LightIndex.text =     hasTransparentData ? InspectedSide.NativeObject.TransparentLightsourceIndex.ToString() : "-";
            Value_Transparent_TransferMode.text =   hasTransparentData ? InspectedSide.NativeObject.TransparentTransferMode.ToString() : "-";
        }

        ////public void Set_Value_Type(Weland.SideType value)
        ////{
        ////    InspectedSide.NativeObject.Type = value;
        ////}

        ////public void Set_Value_Primary_Offset_X(short value)
        ////{
        ////    InspectedSide.SetOffset(InspectedSide.PrimarySurface.GetComponent<FPInteractiveSurfaceSide>(),
        ////                            LevelEntity_Side.DataSources.Primary,
        ////                            x: value,
        ////                            InspectedSide.NativeObject.Primary.Y,
        ////                            rebatch: true);
        ////}

        ////public void Set_Value_Primary_Offset_Y(short value)
        ////{
        ////    InspectedSide.SetOffset(InspectedSide.PrimarySurface.GetComponent<FPInteractiveSurfaceSide>(),
        ////                            LevelEntity_Side.DataSources.Primary,
        ////                            InspectedSide.NativeObject.Primary.X,
        ////                            y: value,
        ////                            rebatch: true);
        ////}

        ////public void Set_Value_Primary_TransferMode(TransferModes value)
        ////{
        ////    inspectedSide.SetTransferMode(InspectedSide.PrimarySurface.GetComponent<RuntimeSurfaceLight>(),
        ////                                  LevelEntity_Side.DataSources.Primary,
        ////                                  value,
        ////                                  InspectedSide.NativeObject.Primary.Texture);
        ////}

        ////public void Set_Value_Primary_LightIndex(short value)
        ////{
        ////    // TODO
        ////}

        ////public void Set_Value_Secondary_Offset_X(short value)
        ////{
        ////    InspectedSide.SetOffset(InspectedSide.SecondarySurface.GetComponent<FPInteractiveSurfaceSide>(),
        ////                            LevelEntity_Side.DataSources.Secondary,
        ////                            x: value,
        ////                            InspectedSide.NativeObject.Secondary.Y,
        ////                            rebatch: true);
        ////}

        ////public void Set_Value_Secondary_Offset_Y(short value)
        ////{
        ////    InspectedSide.SetOffset(InspectedSide.SecondarySurface.GetComponent<FPInteractiveSurfaceSide>(),
        ////                            LevelEntity_Side.DataSources.Secondary,
        ////                            InspectedSide.NativeObject.Secondary.X,
        ////                            y: value,
        ////                            rebatch: true);
        ////}

        ////public void Set_Value_Secondary_TransferMode(TransferModes value)
        ////{
        ////    inspectedSide.SetTransferMode(InspectedSide.PrimarySurface.GetComponent<RuntimeSurfaceLight>(),
        ////                                  LevelEntity_Side.DataSources.Primary,
        ////                                  value,
        ////                                  InspectedSide.NativeObject.Primary.Texture);
        ////}

        ////public void Set_Value_Secondary_LightIndex(short value)
        ////{
        ////    // TODO
        ////}

        ////public void Set_Value_Transparent_Offset_X(short value)
        ////{
        ////    InspectedSide.SetOffset(InspectedSide.TransparentSurface.GetComponent<FPInteractiveSurfaceSide>(),
        ////                            LevelEntity_Side.DataSources.Transparent,
        ////                            x: value,
        ////                            InspectedSide.NativeObject.Transparent.Y,
        ////                            rebatch: true);
        ////}

        ////public void Set_Value_Transparent_Offset_Y(short value)
        ////{
        ////    InspectedSide.SetOffset(InspectedSide.TransparentSurface.GetComponent<FPInteractiveSurfaceSide>(),
        ////                            LevelEntity_Side.DataSources.Transparent,
        ////                            InspectedSide.NativeObject.Transparent.X,
        ////                            y: value,
        ////                            rebatch: true);
        ////}

        ////public void Set_Value_Transparent_TransferMode(TransferModes value)
        ////{
        ////    inspectedSide.SetTransferMode(InspectedSide.PrimarySurface.GetComponent<RuntimeSurfaceLight>(),
        ////                                  LevelEntity_Side.DataSources.Primary,
        ////                                  value,
        ////                                  InspectedSide.NativeObject.Primary.Texture);
        ////}

        ////public void Set_Value_Transparent_LightIndex(short value)
        ////{
        ////    // TODO
        ////}
    }
}
