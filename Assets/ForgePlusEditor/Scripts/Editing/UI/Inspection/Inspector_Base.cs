using ForgePlus.Extensions;
using ForgePlus.UI;
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
        // in a list)
        private static readonly List<Inspector_Base> loadedInspectors = new List<Inspector_Base>();

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

        protected static UnityEngine.Texture TextureOrPlaceholder(ushort shapeDescriptor)
        {
            var texture = shapeDescriptor.IsEmptyShapeDescriptor() ? null : MaterialGeneration_Geometry.GetTexture(shapeDescriptor);

            return texture ? texture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
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
