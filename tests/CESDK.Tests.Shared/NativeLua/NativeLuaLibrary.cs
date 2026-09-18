using CESDK.Lua.Interop.Api;

namespace CESDK.Tests.Shared.NativeLua;

/// <summary>
///     Process-wide access to a real Lua 5.3 library for <c>Category=NativeLua</c> tests and for the benchmarks.
///     The first touch locates the DLL, loads it and binds <see cref="LuaApi" />; everything after that is a field read.
/// </summary>
/// <remarks>
///     Lookup: the <see cref="PathVariable" /> environment variable when it is set (no fallback then: a CI job that
///     provisions a DLL must not silently test another one), else <see cref="DefaultPath" />. The module is never
///     unloaded.
///     This file stays free of xUnit types: a test skips with
///     <c>Assert.SkipUnless(NativeLuaLibrary.IsAvailable, NativeLuaLibrary.UnavailableReason)</c>.
/// </remarks>
internal static class NativeLuaLibrary
{
    /// <summary>Environment variable that points at a Lua 5.3 DLL of the test process architecture.</summary>
    public const string PathVariable = "CESDK_LUA53_PATH";

    /// <summary>Where a default Cheat Engine installation keeps its 64-bit Lua 5.3 DLL.</summary>
    public const string DefaultPath = @"C:\Program Files\Cheat Engine\lua53-64.dll";

    // A static readonly initializer runs once, under the runtime's type-initialization lock.
    private static readonly NativeLuaProbe SProbe =
        NativeLuaProbe.Run(Environment.GetEnvironmentVariable(PathVariable));

    /// <summary>Whether a Lua 5.3 library is loaded and <see cref="LuaApi" /> is bound to it.</summary>
    public static bool IsAvailable => SProbe.Handle != 0;

    /// <summary>Handle of the loaded module, or zero when unavailable.</summary>
    public static nint Handle => SProbe.Handle;

    /// <summary>Full path of the loaded DLL, or null when unavailable.</summary>
    public static string? LibraryPath => SProbe.LibraryPath;

    /// <summary>Why the library is unavailable, written as a test skip reason; empty when it is available.</summary>
    public static string UnavailableReason => SProbe.Reason;

    /// <summary>For callers that cannot skip (benchmarks, fixtures of other helpers).</summary>
    /// <exception cref="InvalidOperationException">
    ///     The library is unavailable; the message is <see cref="UnavailableReason" />
    ///     .
    /// </exception>
    public static void ThrowIfUnavailable()
    {
        if (!IsAvailable) throw new InvalidOperationException(UnavailableReason);
    }
}
