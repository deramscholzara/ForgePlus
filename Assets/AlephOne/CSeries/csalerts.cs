// Port of Aleph One: Source_Files/CSeries/csalerts.h (assertions and fatal alerts)
using System;

namespace AlephOne
{
    // Thrown where Aleph One halts
    public class AlephOneAssertion : Exception
    {
        public AlephOneAssertion(string message) : base(message) { }
    }

    public static class csalerts
    {
        public static void assert(bool expr)
        {
            if (!expr)
            {
                throw new AlephOneAssertion("assertion failed");
            }
        }

        public static void vassert(bool expr, string diag)
        {
            if (!expr)
            {
                throw new AlephOneAssertion(diag);
            }
        }

        // In place of vassert(expr, csprintf(temporary, format, ...)): the diagnostic is only formatted
        // when the assertion fails, as formatting it up front would allocate on every call
        public static void vassert<T0>(bool expr, string format, T0 arg0)
        {
            if (!expr)
            {
                throw new AlephOneAssertion(string.Format(format, arg0));
            }
        }

        public static void vassert<T0, T1>(bool expr, string format, T0 arg0, T1 arg1)
        {
            if (!expr)
            {
                throw new AlephOneAssertion(string.Format(format, arg0, arg1));
            }
        }

        public static void vhalt(string diag)
        {
            throw new AlephOneAssertion(diag);
        }

        public static void alert_corrupted_map(int error = -1)
        {
            vhalt($"This map is corrupted (error 0x{error:x4})");
        }
    }
}
