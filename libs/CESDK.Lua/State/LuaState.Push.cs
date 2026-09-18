using System;
using System.Runtime.CompilerServices;
using CESDK.Annotations.Lua;
using CESDK.Lua.Text;
using static CESDK.Lua.Interop.Api.LuaApi;

namespace CESDK.Lua.State;

// Pushes: one C API call each. None runs Lua code; the string pushes allocate inside Lua.
public readonly unsafe partial struct LuaState
{
    /// <summary>Pushes <c>nil</c>.</summary>
    [LuaStackEffect(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushNil()
    {
        lua_pushnil(Pointer);
    }

    /// <summary>Pushes an integer (<c>lua_pushinteger</c>). Lua integers are 64-bit.</summary>
    /// <param name="value">The value.</param>
    [LuaStackEffect(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushInteger(long value)
    {
        lua_pushinteger(Pointer, value);
    }

    /// <summary>Pushes a float (<c>lua_pushnumber</c>). Lua floats are doubles.</summary>
    /// <param name="value">The value.</param>
    [LuaStackEffect(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushNumber(double value)
    {
        lua_pushnumber(Pointer, value);
    }

    /// <summary>Pushes a boolean (<c>lua_pushboolean</c>).</summary>
    /// <param name="value">The value.</param>
    [LuaStackEffect(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushBoolean(bool value)
    {
        lua_pushboolean(Pointer, value ? 1 : 0);
    }

    /// <summary>
    ///     Pushes a light userdata: a bare pointer value with no identity or lifetime of its own (
    ///     <c>lua_pushlightuserdata</c>).
    /// </summary>
    /// <param name="address">The pointer value; zero is a valid (null) light userdata.</param>
    [LuaStackEffect(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushLightUserdata(nint address)
    {
        lua_pushlightuserdata(Pointer, (void*)address);
    }

    /// <summary>Pushes the globals table (<c>lua_pushglobaltable</c>), for raw access to globals that bypasses metamethods.</summary>
    [LuaStackEffect(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushGlobalTable()
    {
        _ = lua_rawgeti(Pointer, LUA_REGISTRYINDEX, LUA_RIDX_GLOBALS);
    }

    /// <summary>
    ///     Pushes a string from its bytes (<c>lua_pushlstring</c>). Lua copies the bytes; embedded NULs are kept. The
    ///     SDK's convention is UTF-8, which is what a <c>"..."u8</c> literal is.
    /// </summary>
    /// <param name="utf8">The bytes; may be empty.</param>
    /// <remarks>
    ///     Allocates inside Lua (string interning): can raise on memory exhaustion or through a failing <c>__gc</c>
    ///     finalizer.
    /// </remarks>
    [LuaStackEffect(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushString(ReadOnlySpan<byte> utf8)
    {
        // An empty span pins to null; Lua is handed a valid address anyway.
        byte empty = 0;
        fixed (byte* bytes = utf8)
        {
            _ = lua_pushlstring(Pointer, bytes is null ? &empty : bytes, (nuint)utf8.Length);
        }
    }

    /// <summary>
    ///     Pushes a string from UTF-16 text (a <see cref="string" /> converts implicitly), transcoded to UTF-8 through a
    ///     stack buffer, or a pooled array above <see cref="Utf8Scratch.StackBufferSize" /> bytes. Lone surrogates become
    ///     U+FFFD.
    /// </summary>
    /// <param name="text">The text; may be empty.</param>
    /// <remarks>
    ///     The convenience form: hot paths push <c>"..."u8</c> literals or cached UTF-8 through
    ///     <see cref="PushString(ReadOnlySpan{byte})" />. Allocates inside Lua; see there.
    /// </remarks>
    [LuaStackEffect(1)]
    [SkipLocalsInit] // Encode writes the bytes it reports before anything reads them; zeroing the 512-byte buffer first would be dead stores.
    public void PushString(ReadOnlySpan<char> text)
    {
        // Straight-line on purpose (no 'using'): nothing between Encode and Dispose can throw a managed exception, and a
        // try/finally here would put an EH region on every string push and make the method non-inlinable. A pooled
        // buffer is returned only on the normal path; abandoning it on a native raise is the safe outcome.
        Span<byte> scratch = stackalloc byte[Utf8Scratch.StackBufferSize];
        var utf8 = Utf8Scratch.Encode(text, scratch);
        PushString(utf8.Bytes);
        utf8.Dispose();
    }
}
