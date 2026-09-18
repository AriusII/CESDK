using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using CESDK.Lua.References;
using CESDK.Lua.Runtime;
using CESDK.Lua.State;
using static CESDK.Lua.Interop.Api.LuaApi;

namespace CESDK.Lua.CompilerServices;

/// <summary>
///     Generator-facing: pushes a global function through a lazily resolved, epoch-checked <see cref="LuaRef" />, so that
///     a bound global costs one <c>lua_rawgeti</c> per call after the first. Not meant to be called by hand.
/// </summary>
/// <remarks>
///     <para>
///         <b>Hot path</b> (<see cref="TryPush" />): one volatile read of the reference, one epoch comparison, one
///         <c>lua_rawgeti</c>, whose returned type tag is checked for free. <b>Cold path</b> (first use, or the epoch has
///         advanced since the reference was resolved): a protected read of the global (
///         <see cref="LuaState.TryGetGlobal" />),
///         a type check (the value must be a function) and a <c>luaL_ref</c>, under a lock so that two threads resolving
///         the
///         same global do not both take a slot. A stale slot from a previous epoch is never released: its registry may be
///         gone or reused, so it is simply forgotten (one slot per epoch per global, in the rare case that the host
///         re-attaches
///         without resetting its state).
///     </para>
///     <para>
///         A cached reference binds to the function value at resolve time: a script that later replaces the global is not
///         seen until the next epoch. That is the intended trade.
///     </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class LuaGlobalFunctions
{
    private static readonly Lock SResolveGate = new();

    /// <summary>
    ///     Pushes the global function <paramref name="name" />, resolving and caching it in <paramref name="cache" /> on
    ///     first use and whenever the cached slot is stale. Stack: +1 on <see langword="true" />; +0 on
    ///     <see langword="false" />.
    /// </summary>
    /// <param name="state">The state to push on; the calling thread's.</param>
    /// <param name="cache">
    ///     The reference that caches the resolution; a <c>static readonly</c> field created with
    ///     <c>new LuaRef()</c>.
    /// </param>
    /// <param name="name">The global name, UTF-8; a <c>"..."u8</c> literal.</param>
    /// <returns>
    ///     <see langword="false" /> when the global is undefined, is not a function, or reading it raised; the stack is
    ///     unchanged then.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryPush(LuaState state, LuaRef cache, ReadOnlySpan<byte> name)
    {
        if (!cache.TryGetCurrent(out var slot)) return Resolve(state, cache, name);
        if (lua_rawgeti(state.Pointer, LUA_REGISTRYINDEX, slot) == LUA_TFUNCTION) return true;

        lua_settop(state.Pointer, -2);

        return Resolve(state, cache, name);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Resolve(LuaState state, LuaRef cache, ReadOnlySpan<byte> name)
    {
        var top = state.Top;
        lock (SResolveGate)
        {
            // Another thread may have resolved it while this one waited for the gate.
            if (cache.TryGetCurrent(out var slot))
            {
                if (lua_rawgeti(state.Pointer, LUA_REGISTRYINDEX, slot) == LUA_TFUNCTION) return true;

                state.SetTop(top);
            }

            var epoch = LuaRuntime.Epoch;
            var status = state.TryGetGlobal(name);
            if (!status.IsOk || !state.IsFunction(-1))
            {
                state.SetTop(top);
                return false;
            }

            state.PushValue(-1);
            var reference = luaL_ref(state.Pointer, LUA_REGISTRYINDEX);
            cache.Rebind(reference, epoch);
            return true;
        }
    }
}
