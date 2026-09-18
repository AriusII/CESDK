using System.Runtime.CompilerServices;
using CESDK.Annotations.Lua;
using CESDK.Lua.References;
using CESDK.Lua.Runtime;
using static CESDK.Lua.Interop.Api.LuaApi;

namespace CESDK.Lua.State;

// Registry references: create from the stack top, push back by slot number.
public readonly unsafe partial struct LuaState
{
    /// <summary>
    ///     Pops the value on top and stores it in the registry, returning a reference stamped with the current
    ///     <see cref="LuaRuntime.Epoch" /> (<c>luaL_ref</c>).
    /// </summary>
    /// <returns>A new, resolved reference; the caller owns it and releases it with <see cref="LuaRef.Release" />.</returns>
    /// <remarks>
    ///     Allocates the <see cref="LuaRef" /> object and, inside Lua, possibly a registry slot. Meant for values that
    ///     are pushed many times: never call it per operation.
    /// </remarks>
    [LuaStackEffect(-1)]
    public LuaRef CreateRef()
    {
        var epoch = LuaRuntime.Epoch;
        var reference = luaL_ref(Pointer, LUA_REGISTRYINDEX);
        return new LuaRef(reference, epoch);
    }

    /// <summary>
    ///     Pushes the value a reference designates (<c>lua_rawgeti</c> on the registry) when the reference is current.
    ///     Stack: +1 on <see langword="true" />, +0 on <see langword="false" />.
    /// </summary>
    /// <param name="reference">The reference; may be unresolved or stale, in which case nothing is pushed.</param>
    /// <returns><see langword="true" /> when the value was pushed.</returns>
    /// <remarks>Never raises. One volatile read, one epoch comparison and one C API call.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPushRef(LuaRef reference)
    {
        if (reference is null || !reference.TryGetCurrent(out var slot)) return false;

        _ = lua_rawgeti(Pointer, LUA_REGISTRYINDEX, slot);
        return true;
    }
}
