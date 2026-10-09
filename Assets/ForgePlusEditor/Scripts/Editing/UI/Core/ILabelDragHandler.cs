namespace ForgePlus.UI
{
    // A data source told when one of its properties starts and stops being dragged by its row's label (LabelDragger),
    // for an edit that's only committed when the drag ends (such as a polygon's height)
    public interface ILabelDragHandler
    {
        void BeginLabelDrag(string property);
        void EndLabelDrag(string property);
    }
}
