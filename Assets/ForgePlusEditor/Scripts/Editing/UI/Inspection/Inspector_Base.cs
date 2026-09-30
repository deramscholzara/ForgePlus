using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Materials;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // An inspector is the data source for its layout (under Resources/UI/Inspectors): each row, texture and flag
    // there is a template instance named after the property of the inspector it shows.
    // Inspected objects don't announce their changes, so the bindings update when RefreshValuesInInspector is called.
    public abstract class Inspector_Base : UIPanel, IDataSourceViewHashProvider
    {
        private long version;

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
        }

        protected static UnityEngine.Texture TextureOrPlaceholder(ushort shapeDescriptor)
        {
            var texture = shapeDescriptor.IsEmptyShapeDescriptor() ? null : MaterialGeneration_Geometry.GetTexture(shapeDescriptor);

            return texture ? texture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
        }
    }

    public abstract class Inspector_Base<TEntity> : Inspector_Base where TEntity : class, IInspectable
    {
        protected Inspector_Base(TEntity entity)
        {
            Entity = entity;
        }

        protected TEntity Entity { get; private set; }
    }
}
