# Changelog

## [1.1.0] - 2026-04-30

### Added

- add Invoke-FetchLatestLoader and Refresh-VendoredLoader helpers
- cycle tracking mode on PageUp; bootstrap build deps from vendor

### Other

- Move game detection to data-driven games.json
- Fix install.cmd/uninstall.cmd templates for dev-tree use
- Unify installer CLI across BepInEx/MelonLoader/Cecil/ASI/REFramework/shim
- Make vendored loaders the install-time source of truth
- Add Step-SemanticVersion and Resolve-ReleaseVersion helpers
- Add camera discovery module (RTTI vtable + float classifier)
- Add AGENTS.md with shared code-quality and library API rules
- Expand submodule pointer commits in generated changelogs
- Fix /y flag detection and bundle vendored BepInEx in installers
- Use WriteAllBytes for .cmd output to avoid Defender race

## [1.0.4] - 2026-04-19

### Fixed

- install.cmd works on Program Files (x86) paths

### Other

- Add vendored MelonLoader, launcher manifest, third-party notices; bump cameraunlock-core to 4b1dcff
- Add chord hotkeys, world/local yaw toggle, vendored-loader install

## [1.0.2] - 2026-03-13

### Other

- Fix positional tracking and update README
- Add HUD marker compensation, outline camera sync, and core refactor

## [1.0.3] - 2026-03-13

### Other

- Remove isRemoteConnection parameter, apply baseline smoothing unconditionally
- Fix positional tracking and update README
- Add HUD marker compensation, outline camera sync, and core refactor

## [1.0.1] - 2026-03-10

### Other

- Add pose interpolation and spherical yaw rotation

## [1.0.0] - 2026-03-08

First release.
