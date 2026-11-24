# Windows Build

## Current build

- Project: Sleeping Giant Game
- Unity Editor: 6000.3.9f1
- Platform: Windows 64-bit
- Scripting backend: Mono
- Startup scene: `Assets/Scenes/TestScene.unity`

## Produced build

A Windows 64-bit player was generated with Unity 6000.3.9f1 on 2026-11-23. The distributable archive is `Builds/Sleeping-Giant-Windows.zip`. Extract it and run `Sleeping Giant Game.exe`. Keep the executable beside the `Sleeping Giant Game_Data` folder, `UnityPlayer.dll`, `MonoBleedingEdge`, and the other runtime files in the archive.

The player build completed successfully, but it has not been launched on a Windows computer. The macOS cross-build logged DXC errors for some HDRP STP kernels because Unity supports Direct3D DXC compilation only from the Windows Editor. Confirm rendering on Windows. If HDRP effects are missing, rebuild from the Windows Editor using the steps below.

## Build requirements

Install Unity Editor 6000.3.9f1 with **Windows Build Support (Mono)** through Unity Hub. Open this repository with that Editor version and wait for asset import and script compilation to finish.

## Create a build

The Windows player can be built from the repository root with the Unity CLI:

```sh
unity build --target StandaloneWindows64 --output "Builds/Windows/Sleeping Giant Game.exe"
```

Or, in the Unity Editor, open **File > Build Profiles**, select **Windows**, confirm `Assets/Scenes/TestScene.unity` is enabled in the scene list, and choose **Build**. Save the output to `Builds/Windows`.

The repository ignores `Builds/`, so generated players are not committed. To share a build, zip the complete output folder. Keep the executable together with its `_Data` directory and Unity runtime files.

## Play

Extract the build folder on a Windows 64-bit PC and launch `Sleeping Giant Game.exe`.

- `W`, `A`, `S`, `D`: move
- Mouse: look around
- Left mouse button: shave
- `Space`: jump
- `Left Shift`: sprint
- `C`: crouch
- `E`: sell collected hair when standing in the selling station area
- Walk into the upgrade shop area to open it. Use its buttons to purchase upgrades and press `Escape` to close it.

The game stores its save data in Unity's platform-specific `Application.persistentDataPath`.
