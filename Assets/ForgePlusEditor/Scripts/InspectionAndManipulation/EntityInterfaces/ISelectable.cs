namespace ForgePlus.LevelManipulation
{
    public interface ISelectable
    {
        // TODO: Set up visibility filtering (static state member+enum per relevant type?)
        void SetSelectability(bool enabled);
    }
}
