namespace ForgePlus.Inspection
{
    public interface IInspectable
    {
        // TODO: Add this so it must be implemented in all inspectables
        ////event Action<T> InspectablePropertiesChanged;

        void Inspect();
    }
}
