using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Materials;
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // An inspector is the data source for its layout (under Resources/UI/Inspectors): each row, texture and flag
    // there is a template instance named after the property of the inspector it shows.
    // Inspected objects don't announce their changes, so the bindings update when RefreshValuesInInspector is called.
    // A field row is editable while its property has a setter, and grayed out until then.
    [AutoStaticsCleanup]
    public abstract partial class Inspector_Base : UIPanel, IDataSourceViewHashProvider
    {
        // So an edit refreshes every inspector showing what was edited (such as its selection inspector and its entry
        // in a list). Not readonly, so AutoStaticsCleanup makes a new one each Play session (it leaves readonly fields as
        // they are).
        private static List<Inspector_Base> loadedInspectors = new List<Inspector_Base>();

        private long version;

        protected abstract object InspectedObject { get; }

        public static void RefreshInspectorsOf(object inspectedObject)
        {
            foreach (var inspector in loadedInspectors)
            {
                if (inspector.InspectedObject == inspectedObject)
                {
                    inspector.RefreshValuesInInspector();
                }
            }
        }

        public void RefreshValuesInInspector()
        {
            version++;
        }

        public long GetViewHashCode()
        {
            return version;
        }

        protected override void OnLoaded()
        {
            Root.BindInspectorFields(this);

            loadedInspectors.Add(this);
        }

        protected override void OnUnloading()
        {
            loadedInspectors.Remove(this);
        }

        // An unassigned surface's placeholder, or the Grid texture (as the surface is drawn) for a bitmap the shapes file
        // doesn't have
        protected static UnityEngine.Texture TextureOrPlaceholder(ushort shapeDescriptor)
        {
            if (shapeDescriptor.IsEmptyShapeDescriptor())
            {
                return Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            }

            return MaterialGeneration_Geometry.GetTexture(shapeDescriptor, returnPlaceholderIfNotFound: true);
        }

        // A surface's bitmap (its shape in its collection), or -1 for no texture
        protected static int BitmapOf(ushort shapeDescriptor)
        {
            return shapeDescriptor.IsEmptyShapeDescriptor() ? -1 : shapeDescriptor.GetShape();
        }

        // Whether a surface's texture can be moved (a landscape's is fixed to the view, and a surface with no texture has
        // none)
        protected static bool IsOffsettable(ushort shapeDescriptor, short transferMode)
        {
            return !shapeDescriptor.IsEmptyShapeDescriptor() &&
                   !shapeDescriptor.UsesLandscapeCollection() &&
                   !AlephOneExtensions.IsLandscapeTransferMode(transferMode);
        }

        // The texture with another bitmap from its collection (and color table): -1 for no texture, and, for a surface
        // with no texture, from the level's wall collection
        protected static ushort WithBitmap(ushort shapeDescriptor, int bitmap)
        {
            if (bitmap < 0)
            {
                return cstypes.UNONE;
            }

            var shape = Mathf.Min(bitmap, shape_descriptors.MAXIMUM_SHAPES_PER_COLLECTION - 1);

            return shapeDescriptor.IsEmptyShapeDescriptor() ?
                AlephOneExtensions.BuildShapeDescriptor(CollectionChoices.LevelWallCollection(), shape) :
                AlephOneExtensions.BuildShapeDescriptor(shapeDescriptor.GetCollection(), shape, shapeDescriptor.GetCLUT());
        }

        // A surface's collection, as a choice of CollectionChoices ("-" for no texture)
        protected static string CollectionChoiceOf(ushort shapeDescriptor)
        {
            return shapeDescriptor.IsEmptyShapeDescriptor() ? "-" : CollectionChoices.Choice(shapeDescriptor.GetCollection());
        }

        // The texture's bitmap (and color table) from the chosen collection, if it's another one
        protected static bool TryWithCollection(ushort shapeDescriptor, string choice, out ushort changedShapeDescriptor)
        {
            changedShapeDescriptor = shapeDescriptor;

            if (shapeDescriptor.IsEmptyShapeDescriptor() ||
                !CollectionChoices.TryParse(shapeDescriptor, choice, out var collection) ||
                collection == shapeDescriptor.GetCollection())
            {
                return false;
            }

            changedShapeDescriptor = AlephOneExtensions.BuildShapeDescriptor(collection, shapeDescriptor.GetShape(), shapeDescriptor.GetCLUT());

            return true;
        }

        // A note (a label named after the inspector's string property it shows), shown while it has text
        protected void BindNote(string property)
        {
            var note = Root.Q<Label>(property);
            if (note == null)
            {
                return;
            }

            note.Bind("text", this, property);

            var shown = note.Bind("style.display", this, property);
            shown.sourceToUiConverters.AddConverter((ref string text) => new StyleEnum<DisplayStyle>(string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex));
        }

        // Clicking the texture row's texture opens a list of the textures to choose from (TexturePicker)
        protected void MakeTextureChoosable(string rowName, Func<ushort> currentTexture, Action<ushort> assign)
        {
            var image = Root.Q(rowName)?.Q<Image>(className: "fp-inspector-texture");
            if (image == null)
            {
                return;
            }

            image.AddToClassList("fp-inspector-texture--choosable");
            image.tooltip = Strings.Get(Strings.Common, "Inspector.Base.ChooseTexture.Tooltip");
            image.RegisterCallback<ClickEvent>(clickEvent =>
            {
                clickEvent.StopPropagation();
                TexturePicker.Show(image, currentTexture(), assign);
            });
        }

        // Aleph One stores most numbers as shorts, which number fields edit as ints
        protected static short ClampToShort(int value)
        {
            return (short) Mathf.Clamp(value, short.MinValue, short.MaxValue);
        }
    }

    public abstract class Inspector_Base<TEntity> : Inspector_Base where TEntity : class, IInspectable
    {
        protected Inspector_Base(TEntity entity)
        {
            Entity = entity;
        }

        protected TEntity Entity { get; private set; }

        protected override object InspectedObject
        {
            get
            {
                return Entity;
            }
        }

        // Changes the entity, then shows the change in every inspector of it
        protected void Edit(Action<TEntity> edit)
        {
            edit(Entity);

            RefreshInspectorsOf(Entity);
        }
    }
}
