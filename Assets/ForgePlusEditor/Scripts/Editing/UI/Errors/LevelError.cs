using System;

namespace ForgePlus.UI
{
    // A problem in the open level, listed in the Errors panel until it's gone. An auto-fixable one has a fix that
    // resolves it; a standard one says how the user can fix it.
    public class LevelError
    {
        public LevelError(string description, Action fix = null)
        {
            Description = description;
            Fix = fix;
        }

        public string Description { get; }

        public Action Fix { get; }

        public bool IsAutoFixable
        {
            get
            {
                return Fix != null;
            }
        }
    }
}
