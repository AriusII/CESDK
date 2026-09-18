using System.Runtime.CompilerServices;

// Every unmanaged signature this assembly calls through (the Lua C API forwarders of CESDK.Lua.Interop, the two host
// function pointers of LuaHostBinding, the lua_CFunction wrapped by LuaNativeFunction) is blittable by construction.
// With runtime marshalling disabled that is a compile-time guarantee: a bool, string, ref or out in one of them no
// longer compiles, so no hidden marshalling stub and no hidden allocation can appear on a call path.
[assembly: DisableRuntimeMarshalling]
