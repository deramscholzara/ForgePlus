namespace ForgePlus.Inspection
{
    // Shown while more than one thing is selected, whose inspection isn't supported yet
    public class Inspector_Multiple : Inspector_Base
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Multiple";
            }
        }

        protected override object InspectedObject
        {
            get
            {
                return null;
            }
        }
    }
}
