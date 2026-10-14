// Port of Aleph One: Source_Files/Misc/Logging.h (logging errors and warnings)
//
// ForgePlus: messages are kept until ForgePlus takes them (to write them to its own log), rather than written to a log
// file.
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class Logging
    {
        private static readonly List<(bool IsError, string Message)> messages = new List<(bool IsError, string Message)>();

        public static void logError(string message)
        {
            messages.Add((true, message));
        }

        public static void logWarning(string message)
        {
            messages.Add((false, message));
        }

        // The messages logged since the last time they were taken, oldest first
        public static List<(bool IsError, string Message)> take_messages()
        {
            var taken = new List<(bool IsError, string Message)>(messages);
            messages.Clear();
            return taken;
        }
    }
}
