# POI demo

Demo scene, example scripts and tests for the screen and compass markers in `Assets/POI`. This folder is for
this repository only; to use the markers elsewhere, copy just `Assets/POI` (see its README).

## Quick start

Open `Scenes/POIDemo.unity` and press Play. The tower is ahead, the shop is just off screen to the left, and the
danger and camp points are behind the camera, so every case shows straight away. The screen viewport is tinted
blue and the compass bar is dark grey.

| Action | Keyboard & mouse | Gamepad |
|---|---|---|
| Move | WASD or arrow keys | Left stick |
| Down / up | Q / E | Left / right shoulder |
| Look | Hold right mouse | Right stick |
| Move fast | Hold Shift | Press left stick |
| Spin a full turn every second | Space | Y / Triangle |
| Toggle a point | 1-4 | |
| Toggle all points | 0 | X / Square |

Sprint and spin to see the markers keep up with a fast camera, and resize the Game view while playing: the
Console logs each resolution change, and the manager refreshes the POI UI.

## What is here

| Path | Contents |
|---|---|
| `Scripts/POI_World.cs` | Reference point of interest: `Activate` / `DeActivate`, the pattern in the POI README. |
| `Scripts/ScreenResolutionManager.cs` | Raises an event when the screen's size or safe area changes; wired in the scene to `POI_UI.RefreshLayout`. |
| `Scripts/CompassHeadingExample.cs` | Stand-in NEWS strip, lined up with the compass markers. |
| `Scripts/FlyCameraExample.cs`, `POIDemoExample.cs` | Fly camera, and number keys that activate and deactivate points. |
| `Prefabs/POI_UI.prefab` | A complete HUD for the demo: overlay canvas, safe-area viewport, compass bar with a marker lane. **GameObject > UI > POI UI** adds it. |
| `Scenes`, `Sprites`, `Materials` | The demo scene and its assets. The materials are URP. |
| `Tests/` | EditMode and PlayMode tests for both folders. |

The demo needs the Input System. Its components are under **Add Component > POI Demo**.

## Tests

Run them from **Window > General > Test Runner**, or headless with the Unity CLI while the project is closed in
the Editor:

```bash
unity test . --mode EditMode
```

```bash
unity test . --mode PlayMode
```

The EditMode tests cover the math (projection, behind-camera directions, edge clamping, padding, bearings), the
registry, marker spawning and reuse, both trackers driven with an 800x600 camera, respawning when a prefab or
viewport changes, the marker prefabs themselves, and the resolution tracker. The PlayMode tests run POI_World and
POI_UI end to end, including a point destroyed without removing itself and a camera turned 37 degrees per frame in
LateUpdate.
