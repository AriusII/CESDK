using System.Threading;
using CESDK.Lua.Runtime;
using CESDK.Lua.State;

namespace CESDK.Lua.Callbacks;

/// <summary>
///     The set of live <see cref="LuaCallback" />s of this load context, as an intrusive doubly linked list, so that
///     <see cref="LuaRuntime.Detach" /> can neutralize every closure and free every handle while the host's state can
///     still be reached. Adding and removing allocate nothing beyond the callback itself.
/// </summary>
internal static class LuaCallbackRegistry
{
    private static LuaCallback? s_head;

    /// <summary>Serializes list changes and every release: Lua calls made under it never run Lua code, so it cannot re-enter.</summary>
    internal static Lock Gate { get; } = new();

    /// <summary>Number of live callbacks; for tests and diagnostics.</summary>
    internal static int Count
    {
        get
        {
            lock (Gate)
            {
                var count = 0;
                for (var current = s_head; current is not null; current = current.Next) count++;

                return count;
            }
        }
    }

    internal static void Add(LuaCallback callback)
    {
        lock (Gate)
        {
            callback.Next = s_head;
            s_head?.Previous = callback;

            s_head = callback;
            callback.IsLinked = true;
        }
    }

    /// <summary>Unlinks under the gate held by the caller.</summary>
    internal static void RemoveUnderGate(LuaCallback callback)
    {
        if (!callback.IsLinked) return;

        if (callback.Previous is null)
            lock (Gate)
            {
                s_head = callback.Next;
            }
        else
            callback.Previous.Next = callback.Next;

        callback.Next?.Previous = callback.Previous;

        callback.Next = null;
        callback.Previous = null;
        callback.IsLinked = false;
    }

    /// <summary>
    ///     Releases every live callback with a state acquired from <paramref name="services" /> on the calling thread. When
    ///     the provider yields no state the closures cannot be neutralized, and the callbacks are abandoned instead:
    ///     marked released with their managed state kept alive, which leaks but cannot crash.
    /// </summary>
    internal static unsafe void DetachAll(LuaHostServices services)
    {
        lock (Gate)
        {
            if (s_head is null) return;

            var l = services.Provider();
            LuaState state = new(l);
            // ReleaseUnderGate unlinks the head it is called on, so s_head is re-read on every iteration and the loop
            // ends when the list is empty. Keep the explicit re-read: a "condition is always true" IDE quick-fix once
            // turned this loop into while (true), which ended every Detach with a NullReferenceException.
            for (var head = s_head; head is not null; head = s_head) head.ReleaseUnderGate(state);
        }
    }
}
