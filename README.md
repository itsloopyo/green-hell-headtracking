# Green Hell Head Tracking

![Green Hell running with this mod](https://raw.githubusercontent.com/itsloopyo/green-hell-headtracking/main/assets/readme-clip.gif)

An unofficial head tracking mod for Green Hell that moves the view with your head while your mouse or controller keeps aiming, driven by a webcam, phone, or any OpenTrack compatible tracker, with no VR headset required.

## Features

- Decoupled look and aim: head tracking moves the view, your mouse/controller keeps aiming
- 6DOF tracking: yaw, pitch and roll plus positional lean
- Works with any OpenTrack compatible tracker - free options available for PC, iOS and Android

## Requirements

- [Green Hell](https://store.steampowered.com/app/815370/Green_Hell/) (Steam)
- [OpenTrack](https://github.com/opentrack/opentrack) or an OpenTrack-compatible tracker (smartphone, webcam, or dedicated hardware)

## Installation

### Lopari

Download [Lopari](https://lopari.app), choose **Green Hell**, and click
**Play with head tracking**.

### Standalone Installer

1. Download the latest release from the [Releases page](https://github.com/itsloopyo/green-hell-headtracking/releases)
2. Extract the ZIP anywhere
3. Run `install.cmd`
4. Configure OpenTrack to output UDP to `127.0.0.1:4242`

The installer finds your game automatically via the Steam registry. If it can't find the game, set the `GREEN_HELL_PATH` environment variable or pass the path directly:

```
install.cmd "D:\Games\Green Hell"
```

### Manual Installation

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) v0.6.1 or later
2. Run the MelonLoader installer and select your Green Hell folder
3. Launch the game once to initialize MelonLoader, then close it
4. Copy the following DLLs to `<Green Hell>/Mods/`:
   - `GreenHellHeadTracking.dll`
   - `CameraUnlock.Core.dll`
   - `CameraUnlock.Core.Unity.dll`
   - `CameraUnlock.Core.Unity.Harmony.dll`

## Controls

Default bindings (configurable in `CameraUnlock.ini` or `Defaults.ini`):

| Action              | Nav-cluster | Chord           |
|---------------------|-------------|-----------------|
| Toggle tracking     | `End`       | `Ctrl+Shift+Y`  |
| Cycle tracking mode | `Page Up`   | `Ctrl+Shift+G`  |
| Toggle yaw mode     | `Page Down` | `Ctrl+Shift+H`  |

`Page Up` / `Ctrl+Shift+G` cycles tracking mode:

1. Normal head-tracked gameplay
2. Positional tracking disabled, rotational tracking enabled
3. Rotational tracking disabled, positional tracking enabled
4. Back to normal

The default yaw mode is **camera-local**: head yaw always pans the view horizontally on screen, even if you pitch the game camera steeply up or down. Pressing Page Down switches to **world-space** yaw, which locks horizontal head movement to gravity so the horizon stays level; this feels more natural at moderate angles but degenerates toward roll when you look straight up or down.

## Configuration

Restart the game after editing either config file. The tracking mode and yaw
hotkeys save their changes for the next launch. Toggling tracking on or off
lasts for the current session; `EnableOnStartup` controls the next launch.

Green Hell keeps camera-local yaw and its vertical lean limits (0.15 metres up,
0.05 metres down) as game defaults. Change these in `Mods/CameraUnlock.ini`;
they do not follow `Defaults.ini`.

<!-- cameraunlock:config -->
The mod reads its settings from `Mods\CameraUnlock.ini` in the game folder, and creates the file when it starts and finds none. Edit it with any text editor.

A setting set to `default` takes its value from `Defaults.ini`, which every head tracking mod that keeps its settings in `CameraUnlock.ini` reads. Head tracking mods that keep their settings in another file do not read it. Changing a setting in `Defaults.ini` changes it in every game that has it set to `default`. Writing a value in place of `default` changes that setting for this game only. When the mod saves a setting that a hotkey changed in game, it writes the new value in place of `default`, so that setting no longer follows `Defaults.ini` in this game until you set it to `default` again.

`Defaults.ini` is `%AppData%\CameraUnlock\Defaults.ini` on Windows; `$XDG_CONFIG_HOME/CameraUnlock/Defaults.ini` on Linux, or `~/.config/CameraUnlock/Defaults.ini` where `XDG_CONFIG_HOME` is not set, under Wine and Proton too; and `~/Library/Application Support/CameraUnlock/Defaults.ini` on macOS. The mod's log, where it writes one, names the file it read.

When the mod starts and finds no `Defaults.ini`, it creates one holding the built-in values, unless Windows runs the game as a packaged app, or the game runs on Linux or macOS without Wine or Proton. The mod never changes `Defaults.ini` after that. Edit it with any text editor.

On Linux and macOS without Wine or Proton, this version reads its settings and saves none: it creates no `CameraUnlock.ini` and a change made in game lasts until the game closes.

The built-in value of each setting set to `default` below:

- `UdpPort=4242`
- `EnableOnStartup=true`
- `RotationEnabled=true`
- `LocalSmoothing=0.0`
- `RemoteSmoothing=0.15`
- `PositionEnabled=true`
- `PositionLimitX=0.3`
- `PositionLimitZ=0.4`
- `PositionLimitZBack=0.1`
- `ToggleKey=End, Ctrl+Shift+Y`
- `CycleTrackingModeKey=PageUp, Ctrl+Shift+G`
- `YawModeKey=PageDown, Ctrl+Shift+H`

With every setting at its default, the file reads:

```ini
; Green Hell head tracking settings.
; Comments start with ; and go on their own line. Text after a value is part of the value.
; Hotkeys are key names such as End, PageUp or Ctrl+Shift+Y. Separate several with commas; leave empty for none.
; A setting set to default takes its value from Defaults.ini, which every head tracking mod
; that keeps its settings in CameraUnlock.ini reads: %AppData%\CameraUnlock\Defaults.ini on
; Windows, $XDG_CONFIG_HOME/CameraUnlock/Defaults.ini (normally ~/.config/CameraUnlock) on
; Linux, under Wine and Proton too, and ~/Library/Application Support/CameraUnlock/Defaults.ini
; on macOS. The log names the file it read. Change a setting in Defaults.ini to change it in
; every game that has it set to default, or write a value here instead of default to change it
; for this game only.

[CameraUnlock]
; Written by the mod. Leave this section in place.
ConfigFormat=1

[Network]
; UDP port the mod receives tracker data on (OpenTrack protocol).
UdpPort=default

[General]
; true: head tracking is on when the game starts. ToggleKey turns it on and off.
EnableOnStartup=default
; true: yaw turns around the world's up axis. false: around the camera's own up axis.
WorldSpaceYaw=false
; true: turning your head turns the view.
; Tracking mode at startup, with PositionEnabled. The mode hotkey changes both.
RotationEnabled=default

[Smoothing]
; Smoothing when the tracker runs on this PC. 0 is the least, 1 the most.
LocalSmoothing=default
; Smoothing when the tracker is another device on the network, such as a phone.
; 0 is the least, 1 the most.
RemoteSmoothing=default

[Position]
; true: moving your head moves the view.
; Tracking mode at startup, with RotationEnabled. The mode hotkey changes both.
PositionEnabled=default
; How far, in metres, leaning left or right can move the view.
PositionLimitX=default
; How far, in metres, raising your head can move the view.
PositionLimitY=0.15
; How far, in metres, lowering your head can move the view.
PositionLimitYDown=0.05
; How far, in metres, leaning forward can move the view.
PositionLimitZ=default
; How far, in metres, leaning back can move the view.
PositionLimitZBack=default

[Hotkeys]
; Turns head tracking on and off.
ToggleKey=default
; Changes the tracking mode: rotation and position, rotation only, position only.
CycleTrackingModeKey=default
; Switches yaw between the world's up axis and the camera's own (WorldSpaceYaw).
YawModeKey=default
```
<!-- /cameraunlock:config -->

## Setting Up OpenTrack

The mod listens for OpenTrack pose data on UDP port `4242` by default, on every network
interface. One datagram is six little-endian 64-bit floats in the order
`x, y, z, yaw, pitch, roll`: position in centimetres, rotation in degrees, 48
bytes in total. Anything that sends that to that port drives the view.
OpenTrack's **UDP over network** output sends exactly this, and the steps below
set it up.

1. Install [OpenTrack](https://github.com/opentrack/opentrack/releases).
2. Pick a tracker under **Input**, using the notes below.
3. Set **Output** to **UDP over network**, host `127.0.0.1`, port `4242`.
4. Press **Start**. Tracking and the game can start in either order.

### Webcam

OpenTrack ships a `neuralnet tracker` input that reads a plain webcam. Select it
under **Input**, pick your camera in its settings, and use the output settings
above. How well it tracks depends on your camera and your lighting, so try it
before buying anything.

### Phone

A phone app can reach the mod directly, with no OpenTrack on the PC, if it sends
the datagram described above. Point it at this PC's IP address (run `ipconfig`
to find it) on port `4242`. Not every phone tracker speaks this protocol, so
check yours for an OpenTrack or UDP output option first. [Headcam](https://headcam.app)
sends it, and I wrote it so decent tracking is free for anyone who already owns
a phone.

Sending direct works when the app filters its own signal on the device. The
mod's smoothing is sized to take the edge off a clean signal rather than to
rescue a noisy one, so a raw feed sent direct will jitter. If it does, point the
app at OpenTrack's **UDP over network** *input* on some other port, say 5252,
and let OpenTrack's filters and curves clean it up before its output forwards to
`127.0.0.1:4242`.

Anything arriving from outside `127.0.0.0/8` counts as a remote connection and
is smoothed with `RemoteSmoothing` rather than `LocalSmoothing`. That includes a
tracker on this very PC that sends to the machine's own LAN address, because the
mod reads the source address and not the machine.

### Headset or other hardware

If your device has an OpenTrack input driver, select it under **Input** and use
the same output settings. OpenTrack's own **Input** list is the authority on
what it can read; the mod only ever sees what OpenTrack sends.

### Centring

Centring belongs to your tracker. The mod subtracts no centre of its own: it
applies the pose it receives exactly as it arrives, so a stream of zeros holds
the view where the game itself puts it. Press the centre control in your tracker
(OpenTrack's **Center** bind, or the CENTER button in Headcam) and the tracker
zeroes its own output, which leaves the view centred with the mod doing nothing.

That is why there is no centre hotkey here and nothing to re-centre in game. Two
centres in series would drift apart, because each side re-centres at moments the
other cannot see, and you would end up pressing twice to centre once. If the
view sits off to one side, centre it in the tracker.

## Verifying Installation

1. Start OpenTrack with tracking active
2. Launch Green Hell
3. Check the MelonLoader console for:
   ```
   Green Hell Head Tracking initializing...
   Patched CameraManager.LateUpdate
   Green Hell Head Tracking initialized on port 4242
   ```
4. Move your head in-game and the camera should follow
5. If the view sits off-centre, centre it in your tracker app (opentrack Center bind, the CENTER button in Headcam)

## Troubleshooting

### Where the log is

`<Green Hell>/MelonLoader/Latest.log` is the current session's log, written fresh
on every launch. It records the port the mod listened on, which game methods it
patched, and an `OpenTrack connected` line the moment the first tracker packet
arrives. Send this file when reporting a problem. Earlier sessions are archived
under `<Green Hell>/MelonLoader/Logs/`.

### Tracking not responding

- Verify your tracker is sending to `127.0.0.1:4242`
- Look for `OpenTrack connected` in `MelonLoader/Latest.log`. If it is absent, no
  tracker packet ever reached the mod and the problem is upstream of the game.
- Check the MelonLoader console for error messages
- Press End to make sure tracking is enabled

### "Port may be in use" error

Another application is using port 4242. Close it, or check what's using the port:

```
netstat -ano | findstr 4242
```

### MelonLoader not loading

- Make sure you ran the game once after installing MelonLoader
- Check that `version.dll` exists in the game folder
- Try reinstalling MelonLoader

### Mod not appearing in console

- Verify all DLLs are in the `Mods` folder
- Check for errors in `<Green Hell>/MelonLoader/Latest.log`

### Camera jittering

- Reduce sensitivity in your tracking software
- Ensure stable lighting if using face tracking

### Tracking pauses in menus

This is intentional. Tracking resumes when you return to gameplay.

## Updating

1. Download the new release
2. Run `install.cmd` again to update the mod files

## Uninstalling

Run `uninstall.cmd` from the release folder, or remove manually:

To remove the mod only, delete these from `<Green Hell>/Mods/`:
- `GreenHellHeadTracking.dll`
- `CameraUnlock.Core.dll`
- `CameraUnlock.Core.Unity.dll`
- `CameraUnlock.Core.Unity.Harmony.dll`

For a complete removal, also delete the `MelonLoader` folder and `version.dll` from the game root, then verify game files through Steam.

## Building from Source

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (any recent version)
- [Pixi](https://pixi.sh/) task runner
- Node.js (for configuration checks)

Building uses the vendored loader and generated reference stubs. A game install
is only needed to install and run the mod.

### Build Steps

```bash
# Clone with submodules
git clone --recurse-submodules https://github.com/itsloopyo/green-hell-headtracking
cd green-hell-headtracking

# Build and install to game
pixi run install

# Or just build
pixi run build
```

### Available Commands

| Command | Description |
|---------|-------------|
| `pixi run build` | Build the mod (Release configuration) |
| `pixi run install` | Build and install to game directory |
| `pixi run uninstall` | Remove the mod from the game |
| `pixi run package` | Create release ZIP |
| `pixi run render-config` | Regenerate the default config and manifest descriptor |
| `pixi run test` | Test configuration loading, rendering and saves |
| `pixi run clean` | Clean build artifacts |
| `pixi run release` | Version bump, changelog, tag, and push |

## Community & Support

- Discord: [Loop's Head Tracking Hangout](https://discord.com/invite/dxyZdyFNT9) - setup help, bug reports, and new-release announcements
- [Lopari](https://lopari.app) - free Windows launcher with one-click install and launch for the released head-tracking mods
- [Headcam](https://headcam.app) - free app that turns your iPhone or Android phone into the head tracker

## License

MIT License - see [LICENSE](LICENSE) for details.

Third-party components bundled with or compiled into this mod are listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), each under its own licence.

## Credits

- [Creepy Jar](https://www.creepyjar.com/) - Green Hell
- [MelonLoader](https://melonwiki.xyz/) - Mod framework
- [HarmonyX](https://github.com/BepInEx/HarmonyX) - Runtime patching, loaded by MelonLoader
- [OpenTrack](https://github.com/opentrack/opentrack) - Head tracking protocol

## Disclaimer

This mod is not affiliated with, endorsed by, or supported by Creepy Jar. "Green Hell" is a trademark of Creepy Jar S.A. Use this mod at your own risk - no warranty is provided. Back up your save files before installing any mods.
