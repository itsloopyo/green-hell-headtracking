# melonloader (vendored)

This directory contains a bundled copy of the upstream mod loader. It is the install-time
source of truth: install.cmd extracts directly from here and never reaches out to the network.
Refresh manually with `pixi run update-deps`, then commit.

## Snapshot

- Asset: `MelonLoader.x64.zip`
- Tag: `v0.6.6`
- Commit: `1119a286590ca78bb4a1217bbd23279e0daac9d3`
- Upstream URL: https://github.com/LavaGang/MelonLoader/releases/download/v0.6.6/MelonLoader.x64.zip
- SHA-256: `687b82605606e941cefdc007b880b720922cc319bb70270064590d4038c3c0db`
- Fetched at: 2026-04-30T21:06:46.1740711+01:00
- Source: github

Do not edit this directory by hand. Run ``pixi run package`` (or CI release) to refresh.
