// ForgePlus: not part of Aleph One.
// Unity's analyzers require every type with statics to declare whether Unity should reset them when Play mode
// starts without a domain reload. This port has no Unity dependencies, so it declares that with its own copy of
// Unity's attribute (matched by name). Its statics are constant tables and reusable buffers, apart from the loaded
// shapes and last error, which ForgePlus resets itself (ForgePlusRuntime/Scripts/DataFileIO/AlephOneStateReset.cs).
using System;

namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    internal sealed class NoAutoStaticsCleanupAttribute : Attribute
    {
    }
}
