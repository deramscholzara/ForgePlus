using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
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
                AlephOneExtensions.BuildShapeDescriptor(LevelWallCollection(), shape) :
                AlephOneExtensions.BuildShapeDescriptor(shapeDescriptor.GetCollection(), shape, shapeDescriptor.GetCLUT());
        }

        // The first wall collection of the level's environment (as the game loads them: map.cpp,
        // mark_environment_collections), or the first walls collection if it has none
        private static int LevelWallCollection()
        {
            var level = LevelEntity_Level.Instance;
            var environmentCode = level ? level.Level.static_world.environment_code : (short) 0;

            if (environmentCode >= 0 && environmentCode < map.NUMBER_OF_ENVIRONMENTS)
            {
                for (var i = 0; i < map.NUMBER_OF_ENV_COLLECTIONS; i++)
                {
                    var collection = map.Environments[environmentCode, i];
                    if (collection != cstypes.NONE && ShapesLoading.Instance.IsWallCollection(collection))
                    {
                        return collection;
                    }
                }
            }

            return shape_descriptors._collection_walls1;
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
            image.tooltip = "Click to choose a texture";
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
