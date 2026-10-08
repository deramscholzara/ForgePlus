using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Materials;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Collections = ForgePlus.Inspection.CollectionChoices;
using TransferModes = ForgePlus.Inspection.TransferModeChoices;

namespace ForgePlus.Inspection
{
    // A side's or polygon's textured surface, which its inspector's rows in the scope of its name are bound to
    public abstract class SurfaceTextureView : IDataSourceViewHashProvider
    {
        private readonly Inspector_Base inspector;

        protected SurfaceTextureView(Inspector_Base inspector)
        {
            this.inspector = inspector;
        }

        // An unassigned surface's placeholder, or the Grid texture (as the surface is drawn) for a bitmap the shapes file
        // doesn't have
        [CreateProperty]
        public UnityEngine.Texture Texture
        {
            get
            {
                if (!HasTexture)
                {
                    return Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
                }

                return MaterialGeneration_Geometry.GetTexture(ShapeDescriptor, returnPlaceholderIfNotFound: true);
            }
        }

        // -1 for none; a surface with none gets one from the level's wall collection
        [CreateProperty]
        public int Bitmap
        {
            get
            {
                return HasTexture ? ShapeDescriptor.GetShape() : -1;
            }
            set
            {
                if (value < 0)
                {
                    SetShapeDescriptor(cstypes.UNONE);
                    return;
                }

                var shape = Mathf.Min(value, shape_descriptors.MAXIMUM_SHAPES_PER_COLLECTION - 1);

                SetShapeDescriptor(HasTexture ?
                    AlephOneExtensions.BuildShapeDescriptor(ShapeDescriptor.GetCollection(), shape, ShapeDescriptor.GetCLUT()) :
                    AlephOneExtensions.BuildShapeDescriptor(Collections.LevelWallCollection(), shape));
            }
        }

        // One of the level's (which the original games load), or another (which only Aleph One does); choosing another
        // keeps the texture's bitmap and color table
        [CreateProperty]
        public string Collection
        {
            get
            {
                return HasTexture ? Collections.Choice(ShapeDescriptor.GetCollection()) : "-";
            }
            set
            {
                if (HasTexture && Collections.TryParse(ShapeDescriptor, value, out var collection) && collection != ShapeDescriptor.GetCollection())
                {
                    SetShapeDescriptor(AlephOneExtensions.BuildShapeDescriptor(collection, ShapeDescriptor.GetShape(), ShapeDescriptor.GetCLUT()));
                }
            }
        }

        [CreateProperty]
        public List<string> CollectionChoices
        {
            get
            {
                return Collections.All(ShapeDescriptor);
            }
        }

        [CreateProperty]
        public List<string> CollectionAlephOneOnlyChoices
        {
            get
            {
                return Collections.AlephOneOnly(ShapeDescriptor);
            }
        }

        [CreateProperty]
        public bool IsCollectionEditable
        {
            get
            {
                return HasTexture && IsTextureAssignable;
            }
        }

        [CreateProperty]
        public string CollectionNote
        {
            get
            {
                return Collections.Note(ShapeDescriptor);
            }
        }

        // Where its texture starts
        [CreateProperty]
        public int OffsetX
        {
            get
            {
                return NativeOffsetX;
            }
            set
            {
                SetOffset(Inspector_Base.ClampToShort(value), NativeOffsetY);
            }
        }

        [CreateProperty]
        public int OffsetY
        {
            get
            {
                return NativeOffsetY;
            }
            set
            {
                SetOffset(NativeOffsetX, Inspector_Base.ClampToShort(value));
            }
        }

        // A landscape's texture is fixed to the view
        [CreateProperty]
        public bool IsOffsetEditable
        {
            get
            {
                return HasTexture &&
                       !ShapeDescriptor.UsesLandscapeCollection() &&
                       !AlephOneExtensions.IsLandscapeTransferMode(NativeTransferMode);
            }
        }

        // Each of the offset's rows
        [CreateProperty]
        public bool IsOffsetXEditable
        {
            get
            {
                return IsOffsetEditable;
            }
        }

        [CreateProperty]
        public bool IsOffsetYEditable
        {
            get
            {
                return IsOffsetEditable;
            }
        }

        [CreateProperty]
        public string TransferMode
        {
            get
            {
                return TransferModeChoice;
            }
            set
            {
                if (TransferModes.TryParse(NativeTransferMode, value, out var transferMode))
                {
                    SetTransferMode(transferMode);
                }
            }
        }

        [CreateProperty]
        public List<string> TransferModeChoices
        {
            get
            {
                return TransferModes.All(NativeTransferMode);
            }
        }

        [CreateProperty]
        public List<string> TransferModeAlephOneOnlyChoices
        {
            get
            {
                return TransferModes.AlephOneOnly(NativeTransferMode);
            }
        }

        [CreateProperty]
        public List<string> TransferModeUnavailableChoices
        {
            get
            {
                return TransferModes.Unavailable(NativeTransferMode);
            }
        }

        // A surface with no texture isn't drawn
        [CreateProperty]
        public bool IsTransferModeEditable
        {
            get
            {
                return HasTexture;
            }
        }

        [CreateProperty]
        public int LightIndex
        {
            get
            {
                return NativeLightIndex;
            }
            set
            {
                SetLight(value);
            }
        }

        [CreateProperty]
        public bool IsLightIndexEditable
        {
            get
            {
                return IsLit;
            }
        }

        protected bool HasTexture
        {
            get
            {
                return !ShapeDescriptor.IsEmptyShapeDescriptor();
            }
        }

        protected abstract ushort ShapeDescriptor { get; }

        protected abstract short NativeTransferMode { get; }

        protected abstract short NativeOffsetX { get; }

        protected abstract short NativeOffsetY { get; }

        protected abstract short NativeLightIndex { get; }

        protected abstract bool IsLit { get; }

        // Whether its texture can be chosen (rather than being set for it)
        protected virtual bool IsTextureAssignable
        {
            get
            {
                return true;
            }
        }

        protected virtual string TransferModeChoice
        {
            get
            {
                return TransferModes.Choice(NativeTransferMode);
            }
        }

        public long GetViewHashCode()
        {
            return inspector.GetViewHashCode();
        }

        // Clicking the scope's texture opens a list of the textures to choose from
        public void MakeTextureChoosable(VisualElement scope)
        {
            var image = scope?.Q(nameof(Texture))?.Q<Image>(className: "fp-inspector-texture");
            if (image == null || !IsTextureAssignable)
            {
                return;
            }

            image.AddToClassList("fp-inspector-texture--choosable");
            image.tooltip = Strings.Get(Strings.Common, "Inspector.Base.ChooseTexture.Tooltip");
            image.RegisterCallback<ClickEvent>(clickEvent =>
            {
                clickEvent.StopPropagation();
                TexturePicker.Show(image, ShapeDescriptor, SetShapeDescriptor);
            });
        }

        // As painting it from the texture palette does
        protected abstract void SetShapeDescriptor(ushort shapeDescriptor);

        protected abstract void SetOffset(short x, short y);

        protected abstract void SetTransferMode(short transferMode);

        // A negative index (no light) can't be assigned
        protected abstract void SetLight(int lightIndex);
    }
}
