# Third-Party Notices

This project bundles and depends on the following third-party software.

---

## MelonLoader

- **License**: Apache-2.0
- **Copyright**: LavaGang contributors
- **Upstream**: https://github.com/LavaGang/MelonLoader
- **Usage**: Mod loader for Unity games. Bundled in release ZIP as fallback at `vendor/melonloader/MelonLoader.x64.zip` (version pinned to v0.6.x range); fetched latest within range at install time by `vendor/melonloader/fetch-latest.ps1`. Installed into the game directory by `install.cmd` only if the user does not already have a MelonLoader install. Not modified.

See `vendor/melonloader/LICENSE` (Apache-2.0 text) and `vendor/melonloader/README.md` (pinned tag + commit + SHA-256) for the bundled snapshot's full provenance.

---

## HarmonyX

- **License**: MIT
- **Copyright**: BepInEx contributors (HarmonyX fork)
- **Upstream**: https://github.com/BepInEx/HarmonyX
- **Usage**: Runtime method patching library, shipped inside MelonLoader (not separately bundled by this mod).

---

## OpenTrack

- **License**: ISC
- **Copyright**: opentrack contributors
- **Upstream**: https://github.com/opentrack/opentrack
- **Usage**: UDP tracking protocol only. No OpenTrack code is bundled or linked.

---

## Green Hell

Green Hell is the property of Creepy Jar. This mod is a fan project and is not affiliated with or endorsed by Creepy Jar. Purchase Green Hell at https://store.steampowered.com/app/815370/Green_Hell/.
