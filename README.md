<div align="center">

# CESDK

**Write Cheat Engine plugins as ordinary C# classes.**

[![Build](https://img.shields.io/github/actions/workflow/status/ShadowNineX/CESDK/merge.yml?branch=main&style=flat-square&logo=githubactions&logoColor=white&labelColor=24292f)](https://github.com/ShadowNineX/CESDK/actions/workflows/merge.yml)
[![NuGet](https://img.shields.io/nuget/vpre/CESDK?style=flat-square&logo=nuget&logoColor=white&labelColor=24292f&color=004880)](https://www.nuget.org/packages/CESDK)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white&labelColor=24292f)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Windows x64](https://img.shields.io/badge/platform-Windows%20x64-0078D4?style=flat-square&labelColor=24292f)](#requirements)
[![MIT license](https://img.shields.io/badge/license-MIT-6e7781?style=flat-square&labelColor=24292f)](LICENSE)

[Quick start](#quick-start) · [Why CESDK exists](#why-cesdk-exists) · [Requirements](#requirements) · [Projects](#projects) · [Contributing](#contributing)

</div>

## What CESDK is

CESDK is a .NET 10 SDK for writing Cheat Engine plugins in C#. You add one NuGet package, derive one class, and build.
The package generates the entry point Cheat Engine loads, exposes your static methods to Lua, and reports plugin
mistakes in the editor before you open Cheat Engine.

## Why CESDK exists

Cheat Engine's own C# template asks every plugin to compile over a thousand lines of interop code into itself, marshal
native structures by hand, and write the exported entry point exactly right. A wrong shape gives no error message: Cheat
Engine simply refuses to load the plugin.

CESDK moves that work into a package. The entry point and the Lua bindings are generated at compile time, without
reflection, and analyzers explain what is wrong while you type. Your plugin stays a plain class.

## Who it is for

C# developers who build plugins, tools, and automation for Cheat Engine 7.7 on Windows x64 and want typed,
compiler-checked code instead of hand-written interop. Lua remains the fastest way to script Cheat Engine without a
build step. CESDK is for plugins that deserve a real project.

## Quick start

1. Create a class library, target x64, and add the package.

   ```powershell
   dotnet new classlib -n MyPlugin
   cd MyPlugin
   dotnet add package CESDK --prerelease
   ```

   Add `<PlatformTarget>x64</PlatformTarget>` to the `PropertyGroup` of `MyPlugin.csproj` and delete `Class1.cs`.

2. Add a plugin class with one Lua function.

   ```csharp
   using CESDK.Annotations.Lua;
   using CESDK.Annotations.Plugin;
   using CESDK.Hosting.Plugin;
   using CESDK.Lua.Runtime;

   namespace MyPlugin;

   [CheatEnginePlugin("My Plugin")]
   public sealed class HelloPlugin : CheatEnginePlugin
   {
       protected override void OnEnable() => Commands.RegisterLuaFunctions(LuaRuntime.AcquireState());

       protected override void OnDisable() => Commands.UnregisterLuaFunctions(LuaRuntime.AcquireState());
   }

   internal static partial class Commands
   {
       [LuaFunction("greet")]
       public static string Greet(string name) => $"Hello, {name}!";
   }
   ```

3. Build, then keep the whole output folder together. The CESDK libraries sit next to `MyPlugin.dll`.

   ```powershell
   dotnet build -c Release
   ```

4. Start Cheat Engine, add `MyPlugin.dll` in the plugin settings, and enable it. In the Lua engine window, run
   `print(greet("world"))`.

> [!IMPORTANT]
> Cheat Engine 7.7 asks for the .NET 9 runtime, so a .NET 10 plugin needs one roll-forward setting. Start Cheat Engine
from a shell that sets it:
>
> ```powershell
> $env:DOTNET_ROLL_FORWARD = "Major"
> & "C:\Program Files\Cheat Engine\cheatengine-x86_64.exe"
> ```
>
> You can also edit `ce.runtimeconfig.json` in the Cheat Engine folder to request `10.0.0`.

The [live plugin guide](tests/CESDK.LivePlugin/README.md#run-it-in-cheat-engine) walks through the same steps with a
larger sample and the log output to expect.

## How it works

| You write                                                                      | CESDK provides                                                                                                           |
|--------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| `[CheatEnginePlugin("Name")]` on a class that derives from `CheatEnginePlugin` | The `CEPluginInitialize` entry point Cheat Engine looks up, generated into your assembly                                 |
| `[LuaFunction("name")]` on a static method                                     | A Lua global with its native thunk, registered and unregistered for you                                                  |
| `[LuaGlobal]` on a partial method                                              | A typed call into a Cheat Engine Lua function such as `readInteger`                                                      |
| A plugin that Cheat Engine would refuse                                        | An editor diagnostic from `CESDK0001` to `CESDK2004`, each with a [page that explains the fix](analyzers/docs/README.md) |
| An exception inside your plugin                                                | A logged failure instead of a crash in Cheat Engine                                                                      |

## Requirements

| Requirement   | Version                                                                                         |
|---------------|-------------------------------------------------------------------------------------------------|
| .NET SDK      | 10.0.401 or later                                                                               |
| .NET runtimes | .NET 10 `Microsoft.NETCore.App`, `Microsoft.WindowsDesktop.App`, and `Microsoft.AspNetCore.App` |
| Cheat Engine  | 7.7                                                                                             |
| Platform      | Windows, x64                                                                                    |

The analyzers and generators are built against Roslyn 5.9. An older SDK reports `CS9057` and skips them, so the entry
point is never generated.

## Projects

CESDK ships as one NuGet package built from small layered libraries.

| Project                                                                              | Role                                                                        |
|--------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| [`libs/CESDK.Annotations`](libs/CESDK.Annotations/README.md)                         | The attributes that generators and analyzers read                           |
| [`libs/CESDK.Abi`](libs/CESDK.Abi/README.md)                                         | The binary layout of Cheat Engine's plugin interface                        |
| [`libs/CESDK.Lua.Interop`](libs/CESDK.Lua.Interop/README.md)                         | The raw Lua 5.3 C API, bound to the Lua library Cheat Engine already loaded |
| [`libs/CESDK.Lua`](libs/CESDK.Lua/README.md)                                         | Managed Lua state, protected calls, marshallers, and callbacks              |
| [`libs/CESDK.Engine`](libs/CESDK.Engine/README.md)                                   | The Cheat Engine object model: handles, ownership, addresses, and enums     |
| [`libs/CESDK.Hosting`](libs/CESDK.Hosting/README.md)                                 | The plugin lifecycle, main thread access, and logging                       |
| [`source-generators`](source-generators/CESDK.SourceGenerators.EntryPoint/README.md) | The entry point and Lua binding generators                                  |
| [`analyzers`](analyzers/CESDK.Analyzers/README.md)                                   | The `CESDK` diagnostics and their code fixes                                |
| [`src/CESDK`](src/CESDK/README.md)                                                   | The single NuGet package that embeds all of the above                       |

## Contributing

```powershell
dotnet build CESDK.slnx
dotnet test --solution CESDK.slnx
dotnet pack src/CESDK -c Release
```

Warnings are errors, so the build is also the lint step. Tests tagged `Category=NativeLua` run against the Lua DLL of
Cheat Engine 7.7 kept in [`native/cheat-engine`](native/cheat-engine/README.md), so nothing has to be installed;
[`tests/CESDK.Tests.Shared`](tests/CESDK.Tests.Shared/README.md) explains how the DLL is found.
Every project has a README
that states its design and guarantees.

## License

[MIT](LICENSE). CESDK is an independent project and is not affiliated with Cheat Engine, which is licensed separately.
The repository keeps one Cheat Engine file, the Lua DLL of [`native/cheat-engine`](native/cheat-engine/README.md), as a
test fixture. It stays under Cheat Engine's terms, this license does not cover it, and the NuGet package does not
contain it.
