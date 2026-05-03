# Changelog

## [1.1.2] - 2026-05-03

### Other

- Verify existing BepInEx loader arch and replace on mismatch
- Fall back to dev-tree vendor path in BepInEx install body

## [1.1.1] - 2026-05-03

### Other

- Add DX11 overlay header for crosshair rendering
- Update PositionInterpolator tests for bounded extrapolation
- Skip vendor refresh when SHA-256 matches existing copy
- Fix degenerate-input bugs in scanners, projection, and color parser
- Add yaw-mode key and WorldSpaceYaw config options
- Quote /y flag detection and add shared install/uninstall bodies
- Add DevDeploy module with Cecil dev-install orchestrator
- Auto-refresh cameraunlock-core submodule in Copy-SharedBundle
- Add install bodies and dev-deploy orchestrators for non-Cecil frameworks
- Resolve exe relpath from games.json in ASI/shim dev-deploy
- Add automatic port retry to C++ UdpReceiver
- Take BuildOutputPath in dev-deploy and add loader/config auto-install

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
