# Changelog

## [Unreleased]

### Added
- `Editor`: custom inspectors for all framework components and the `Game Framework` menu, ported from Unity Game Framework `Scripts/Editor` (Inspector, Misc) and adapted to this edition.
  - Helper type dropdowns, procedure checkboxes and runtime state (entities, UI groups, object pools, reference pools, …) in the Inspector instead of raw serialized field names.
  - `Game Framework/Log Scripting Define Symbols`, `Open Folder`, `Scenes in Build Settings` menus. Scripting define symbols use the `NamedBuildTarget` API.
  - Log redirection (double-click a `Log.Info` entry to open the caller instead of `DefaultLogHelper.cs`) re-enabled for Unity 6.
- `Editor/Addressables`: `Game Framework/Addressables` menu (open Groups window, switch Play Mode Script to *Use Asset Database*, build / clean content, validate addresses) and an Addressables-aware `ResourceComponent` inspector.

### Not ported
- The original AssetBundle tools (Resource Editor, Resource Builder, Resource Analyzer, Resource Pack Builder, Resource Sync Tools) are intentionally left out. This edition loads assets through Addressables, so their output can not be read at runtime.

### Fixed
- `LogRedirection`: the `OnOpenAsset` callback takes `EntityId` on Unity 6000.3 and newer. With the old `int` signature the editor logged an error every time an asset was opened.
- `ConfigManager.AddConfig(string, string)` and `DefaultSetting` now parse and format numbers with the invariant culture. Previously `0.5` could be read as `0` (or fail) on devices whose language uses a decimal comma.
- `DefaultSoundAgentHelper.Time` no longer assigns `AudioSource.time` when no clip is set. This removes the "Attempting to set `time` on an audio source..." warning that Unity 6 logged on every `PlaySound`.

## [0.1.0] - 2026-10-08

### Added
- Framework core as `Assets/Plugins/GameFramework.dll`, built from the separate Game Framework C# project.
- Unity Game Framework (components, helpers) in `Assets/Scripts`.
- Addressables-based `ResourceManager`.
