using AlephOne;
using ForgePlus.LevelManipulation;
using ForgePlus.UI;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Point : Inspector_Base<LevelEntity_Point>, ILabelDragHandler
    {
        // While its position is dragged by a label, which commits the move once the drag ends (as dragging its handle does)
        private bool isDraggingLocation;

        public Inspector_Point(LevelEntity_Point point) : base(point)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Point";
            }
        }

        [CreateProperty]
        public string Id
        {
            get
            {
                return Entity.NativeIndex.ToString();
            }
        }

        // Where it is in the level, in world units (its heights are its polygons')
        [CreateProperty]
        public int LocationX
        {
            get
            {
                return Entity.NativeObject.vertex.x;
            }
            set
            {
                Move(new world_point2d(ClampToShort(value), Entity.NativeObject.vertex.y));
            }
        }

        [CreateProperty]
        public int LocationY
        {
            get
            {
                return Entity.NativeObject.vertex.y;
            }
            set
            {
                Move(new world_point2d(Entity.NativeObject.vertex.x, ClampToShort(value)));
            }
        }

        public void BeginLabelDrag(string property)
        {
            if (property == nameof(LocationX) || property == nameof(LocationY))
            {
                PointEditing.BeginDrag();
                isDraggingLocation = true;
            }
        }

        public void EndLabelDrag(string property)
        {
            if (isDraggingLocation)
            {
                isDraggingLocation = false;
                Edit(point => PointEditing.EndDrag());
            }
        }

        private void Move(world_point2d location)
        {
            if (isDraggingLocation)
            {
                Edit(point => PointEditing.DragPoint(point, location));
            }
            else
            {
                Edit(point => PointEditing.MovePoint(point, location));
            }
        }
    }
}
