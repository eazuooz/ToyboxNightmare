# Changelog

## [Unreleased]

### Fixed
- `ConfigManager.AddConfig(string, string)` and `DefaultSetting` now parse and format numbers with the invariant culture. Previously `0.5` could be read as `0` (or fail) on devices whose language uses a decimal comma.
- `DefaultSoundAgentHelper.Time` no longer assigns `AudioSource.time` when no clip is set. This removes the "Attempting to set `time` on an audio source..." warning that Unity 6 logged on every `PlaySound`.

## [0.1.0] - 2026-10-08

### Added
- First packaged release.
- `Runtime/Core`: framework core compiled from source (previously a prebuilt DLL).
- `Runtime/UnityGameFramework`: Unity integration layer (components, helpers).
- Addressables-based `ResourceManager`.
