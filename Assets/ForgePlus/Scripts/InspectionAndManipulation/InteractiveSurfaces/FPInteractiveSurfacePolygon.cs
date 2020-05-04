﻿using ForgePlus.Palette;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfacePolygon : FPInteractiveSurfaceBase
    {
        public FPPolygon ParentFPPolygon = null;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 && !eventData.dragging && isSelectable)
            {
                switch (ModeManager.Instance.PrimaryMode)
                {
                    case ModeManager.PrimaryModes.Geometry:
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);
                        break;
                    case ModeManager.PrimaryModes.Textures:
                        if ((ushort)surfaceShapeDescriptor != (ushort)ShapeDescriptor.Empty)
                        {
                            SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);
                            PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor, invokeToggleEvents: false);
                        }

                        break;
                    case ModeManager.PrimaryModes.Lights:
                        SelectionManager.Instance.ToggleObjectSelection(FPLight, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForLight(FPLight, invokeToggleEvents: false);
                        break;
                    case ModeManager.PrimaryModes.Media:
                        if (FPMedia != null)
                        {
                            SelectionManager.Instance.ToggleObjectSelection(FPMedia, multiSelect: false);
                            PaletteManager.Instance.SelectSwatchForMedia(FPMedia, invokeToggleEvents: false);
                        }

                        break;
                    case ModeManager.PrimaryModes.Platforms:
                        if (FPPlatform != null)
                        {
                            SelectionManager.Instance.ToggleObjectSelection(FPPlatform, multiSelect: false);
                        }

                        break;
                    default:
                        Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
                        break;
                }
            }
        }
    }
}
