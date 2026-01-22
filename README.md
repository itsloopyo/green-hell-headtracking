# Green Hell Head Tracking

![Mod GIF](assets/readme-clip.gif)

An **unofficial** MelonLoader mod that adds head tracking support to Green Hell. Move your head naturally to look around the jungle while your mouse controls where you're aiming.

## Features

- **6DOF head tracking**: Yaw, pitch, roll rotation plus positional tracking (lean in/out/side-to-side) via OpenTrack UDP protocol
- **Decoupled look + aim**: Look around freely with your head while your aim stays independent
- **Aim reticle**: Shows where your mouse is aiming when head tracking moves the camera

## Requirements

- [Green Hell](https://store.steampowered.com/app/815370/Green_Hell/) (Steam version)
- [OpenTrack](https://github.com/opentrack/opentrack) or an OpenTrack-compatible head tracking app (smartphone, webcam, or dedicated hardware)

## Installation

1. Download the latest release from the [Releases page](https://github.com/itsloopyo/green-hell-headtracking/releases)
2. Extract the ZIP anywhere
3. Run `install.cmd`
4. Configure OpenTrack to output UDP to `127.0.0.1:4242`

The installer automatically finds your game via the Steam registry and common install locations. If it can't find the game:
- Set the `GREEN_HELL_PATH` environment variable to your game folder
- Or run: `install.cmd "D:\Games\Green Hell"`

### Manual Installation

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) v0.6.1 or later
2. Run the MelonLoader installer and select your Green Hell installation folder
3. Launch Green Hell once to initialize MelonLoader, then close it
4. Extract the following files to `<Green Hell>/Mods/`:
   - `GreenHellHeadTracking.dll`
   - `CameraUnlock.Core.dll`
   - `CameraUnlock.Core.Unity.dll`
   - `CameraUnlock.Core.Unity.Harmony.dll`

## Controls

| Key | Action |
|-----|--------|
| **Home** | Recenter head tracking |
| **End** | Toggle head tracking on/off |
| **Page Up** | Toggle positional tracking on/off |

## Building from Source

### Prerequisites

- [.NET SDK 6.0+](https://dotnet.microsoft.com/download)
- [Pixi](https://pixi.sh/) package manager
- Green Hell installed (for game assemblies)

### Build Steps

```bash
# Clone with submodules
git clone --recursive https://github.com/itsloopyo/green-hell-headtracking
cd green-hell-headtracking

# Restore and build
pixi run build

# Build and install to game (installs MelonLoader if needed)
pixi run install
```

### Available Commands

| Command | Description |
|---------|-------------|
| `pixi run build` | Build the mod in Release configuration |
| `pixi run install` | Build and install to Green Hell |
| `pixi run package` | Create release ZIP |
| `pixi run uninstall` | Remove the mod from the game |
| `pixi run clean` | Clean build artifacts |
| `pixi run release` | Version bump, changelog, tag, and push |

## OpenTrack Setup

1. Install [OpenTrack](https://github.com/opentrack/opentrack/releases) and configure any compatible tracker as input (smartphone apps, webcam-based tracking, dedicated hardware, etc.)
2. Set output to **UDP over network**
3. Configure remote IP: `127.0.0.1` and port: `4242`
4. Start tracking before launching the game

### Phone App Setup

This mod includes built-in smoothing to handle network jitter, so if your tracking app already provides a filtered signal, you can send directly from your phone to the mod on port 4242 without needing OpenTrack on PC.

1. Install an OpenTrack-compatible head tracking app from your phone's app store
2. Configure your phone app to send to your PC's IP address on port 4242 (run `ipconfig` to find it, e.g. `192.168.1.100`)
3. Set the protocol to OpenTrack/UDP
4. Start tracking

**With OpenTrack (optional):** If you experience jerky motion, want curve mapping, or want a visual preview, route through OpenTrack instead. The mod already listens on port 4242, so OpenTrack's input must use a different port:
1. In OpenTrack, set Input to **UDP over network** on port **5252** (or any port other than 4242)
2. Set Output to **UDP over network** at `127.0.0.1:4242`
3. In your phone app, send to your PC's IP on port **5252** (matching OpenTrack's input port)
4. Make sure port 5252 is open in your PC's firewall for incoming UDP traffic

## Verifying Installation

1. Start OpenTrack and enable tracking
2. Launch Green Hell
3. Check the MelonLoader console for:
   ```
   Green Hell Head Tracking initializing...
   Patched CameraManager.LateUpdate
   Green Hell Head Tracking initialized on port 4242
   ```
4. In-game, move your head - the camera should follow
5. Press **Home** to recenter if needed

## Troubleshooting

**Tracking not working**
- Verify your tracker is sending to `127.0.0.1:4242`
- Check MelonLoader console for error messages
- Press `End` to ensure tracking is enabled

**"Port may be in use" error**
- Another application is using port 4242. Close the conflicting application, or check: `netstat -ano | findstr 4242`

**MelonLoader not loading**
- Ensure you ran the game once after installing MelonLoader
- Check that `version.dll` exists in the game folder
- Try reinstalling MelonLoader

**Mod not appearing in console**
- Verify all DLL files are in the `Mods` folder
- Check that file names are correct (case-sensitive)
- Look for errors in MelonLoader logs at `<Green Hell>/MelonLoader/Logs/`

**Camera jittering**
- Reduce sensitivity in your tracking software
- Ensure stable lighting for face tracking solutions

**Tracking disabled in menus**
- This is intentional - tracking resumes when returning to gameplay

## Uninstalling

Run `uninstall.cmd` from the release folder, or manually:

**Remove mod only** — delete from `<Green Hell>/Mods/`:
- `GreenHellHeadTracking.dll`, `GreenHellHeadTracking.pdb`
- `CameraUnlock.Core.dll`, `CameraUnlock.Core.Unity.dll`, `CameraUnlock.Core.Unity.Harmony.dll`

**Complete removal** — also delete the `MelonLoader` folder and `version.dll` from the game root, then verify game files via Steam.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- [Creepy Jar](https://www.creepyjar.com/) - Green Hell
- [MelonLoader](https://melonwiki.xyz/) - Mod framework
- [Harmony](https://github.com/pardeike/Harmony) - Runtime patching
- [OpenTrack](https://github.com/opentrack/opentrack) - Head tracking protocol
