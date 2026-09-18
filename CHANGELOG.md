# Changelog

All notable changes to CESDK are documented in this file. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions
follow [Semantic Versioning](https://semver.org/).

Versions 0.1.0 to 0.2.1 were published before this file existed. Version 0.3.0 is a rewrite and is not source
compatible with them.

## [Unreleased]

## [0.3.0] - 2026-09-18

### Added

- One NuGet package that embeds the CESDK libraries, the entry point and Lua binding generators, and the analyzers.
- A generated `CESDK.CESDK.CEPluginInitialize` entry point for the class marked `[CheatEnginePlugin]`, and the plugin
  lifecycle behind it: `CheatEnginePlugin`, `PluginContext`, main thread access, and logging.
- `[LuaFunction]` and `[LuaGlobal]` bindings on top of a managed Lua 5.3 layer that binds to the Lua library Cheat
  Engine already loaded.
- Eight analyzer rules from `CESDK0001` to `CESDK2004`, with code fixes for `CESDK0001` and `CESDK1004`.
- The Cheat Engine object model in `CESDK.Engine`: borrowed and owned handles, `Address`, enums, and generated memory
  read and write wrappers.

### Changed

- The SDK targets .NET 10 and C# 14 on Windows x64, replacing the `netstandard2.0` wrapper published as 0.1.0 to 0.2.1.
- Consumers need .NET SDK 10.0.401 or later. The analyzers and generators are built against Roslyn 5.9, and older
  compilers report `CS9057` and skip them.

[Unreleased]: https://github.com/ShadowNineX/CESDK/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/ShadowNineX/CESDK/releases/tag/v0.3.0
