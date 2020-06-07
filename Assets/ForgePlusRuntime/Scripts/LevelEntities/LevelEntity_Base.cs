using System;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public abstract class LevelEntity_Base : MonoBehaviour
    {
        public LevelEntity_Level ParentLevel { get; private set; }
        public short NativeIndex { get; private set; } = -1;

        protected object NativeObject { get; private set; }

        public virtual void InitializeEntity(LevelEntity_Level parentLevel, short nativeIndex, object nativeObject)
        {
            ParentLevel = parentLevel;
            NativeIndex = nativeIndex;
            this.NativeObject = nativeObject;

            AssembleEntity();
        }

        protected virtual void AssembleEntity()
        {
            if (!ParentLevel || NativeIndex < 0 || NativeObject == null)
            {
                throw new Exception("Level Entities must be initialized before being assembled.");
            }
        }
    }
}
