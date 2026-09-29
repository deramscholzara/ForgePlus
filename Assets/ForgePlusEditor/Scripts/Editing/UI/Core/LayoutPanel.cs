namespace ForgePlus.UI
{
    // A panel with no behavior of its own (such as help text, or options that aren't implemented yet)
    public sealed class LayoutPanel : UIPanel
    {
        private readonly string layoutPath;

        public LayoutPanel(string layoutPath)
        {
            this.layoutPath = layoutPath;
        }

        protected override string LayoutPath
        {
            get
            {
                return layoutPath;
            }
        }
    }
}
