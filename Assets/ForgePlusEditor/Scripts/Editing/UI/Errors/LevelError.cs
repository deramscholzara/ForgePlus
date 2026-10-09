using System;
using UnityEngine;

namespace ForgePlus.UI
{
    // A problem in the open level, listed in the Errors panel until it's gone. An auto-fixable one has a fix that
    // resolves it; a standard one says how the user can fix it. A warning is listed after the errors (and can be hidden),
    // and doesn't make the errors toggle flash. One with something to show selects and frames it.
    public class LevelError
    {
        public LevelError(string description, Action fix = null, bool isWarning = false, Action<Rect> show = null)
        {
            Description = description;
            Fix = fix;
            IsWarning = isWarning;
            Show = show;
        }

        public string Description { get; }

        public Action Fix { get; }

        public bool IsWarning { get; }

        // Given the part of the view (viewport coordinates) left showing the level
        public Action<Rect> Show { get; }

        public bool IsAutoFixable
        {
            get
            {
                return Fix != null;
            }
        }
    }
}
