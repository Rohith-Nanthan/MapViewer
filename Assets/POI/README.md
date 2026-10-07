# POI: screen and compass markers

Shows each active point of interest twice on your HUD:

- **On screen**, over the point's position. While the point is out of view, behind the camera included, its icon
  hugs the edge of your safe-area rect on the point's side.
- **On your compass**, sliding horizontally with the camera's heading, with the distance from the player under it.

Icons never leave their rect or get cut off, at any resolution, and they never trail a fast camera.

## Copy it into your project

1. Copy this `POI` folder, **with its `.meta` files**, anywhere under your project's `Assets`.
2. It needs Unity 6000.3 or newer with uGUI 2.0, which includes TextMesh Pro. Nothing else: no Input System, no
   scenes, no other assets.
3. The compass marker's distance label uses TextMesh Pro's default font. Import it with **Window > TextMeshPro >
   Import TMP Essential Resources**, or set your own font on `POI_CompassMarker`'s Distance label.
4. If your scripts are in an assembly definition, add a reference to `POI.Runtime`.

| Folder | Contents |
|---|---|
| `Runtime/` | `POI.Runtime` assembly: the `POI_UI` and `POI_Marker` components, `IPointOfInterest`, `POIRegistry`, the trackers and `POIMath`. |
| `Editor/` | `POI.Editor` assembly: the POI UI inspector, which flags setup mistakes. |
| `Prefabs/` | `POI_ScreenMarker` and `POI_CompassMarker`, the marker prefabs that are spawned per point. |

## 1. Tell the UI about your points of interest

Your POI_World implements `IPointOfInterest` and adds itself to `POIRegistry.Default` while it is active:

```csharp
using POI;
using UnityEngine;

public class POI_World : MonoBehaviour, IPointOfInterest
{
    [SerializeField] Sprite m_Icon;
    [SerializeField] Vector3 m_Position;

    bool _isActive;

    Sprite IPointOfInterest.Icon => m_Icon;
    Vector3 IPointOfInterest.Position => m_Position;

    public void Activate()
    {
        _isActive = true;
        if (isActiveAndEnabled)
            POIRegistry.Default.Add(this);
    }

    public void DeActivate()
    {
        _isActive = false;
        POIRegistry.Default.Remove(this);
    }

    void OnEnable()
    {
        if (_isActive)
            POIRegistry.Default.Add(this);
    }

    void OnDisable() => POIRegistry.Default.Remove(this);
}
```

- `Add` and `Remove` can be called again safely; they return false when there is nothing to do.
- The explicit `IPointOfInterest.` members avoid clashing with an existing `Icon` or `Position`.
- `OnEnable` and `OnDisable` hide the point while its GameObject is disabled and show it again after.
- A point destroyed without removing itself is dropped automatically.

## 2. Add POI UI to your HUD

Add **Add Component > POI > POI UI** to any object in your HUD, in the same scene or prefab as your rects. It
needs no canvas of its own. Then assign:

| Setting | What to assign |
|---|---|
| Camera | The camera everything is relative to. Uses the main camera when empty. |
| Player | Where distances are measured from. Uses the camera when empty. |
| Screen › Viewport | Your safe-area RectTransform. Screen icons are spawned inside it. |
| Screen › Marker Prefab | `POI_ScreenMarker`, or your own marker prefab. |
| Screen › Padding | Space kept from each edge, in canvas units. 16 by default. |
| Screen › Distance Format | Text for screen markers whose prefab has a Distance Label (`POI_ScreenMarker` has none). `{0} m` by default. |
| Compass › Viewport | An empty RectTransform inside your compass strip, as wide as the strip and centered on it (see below). |
| Compass › Marker Prefab | `POI_CompassMarker`, or your own marker prefab. |
| Compass › Degrees Across Viewport | How many degrees your compass strip spans across its width. 180 by default. |
| Compass › Padding | Space kept from the left and right edges. 8 by default. |
| Compass › Distance Format | Text of the distance label; `{0}` is whole meters. `{0} m` by default. |

The inspector warns about a missing viewport or prefab, a viewport outside a canvas or with a layout group, a
scene object used as a marker prefab, and an invalid distance format.

## 3. Call RefreshLayout from your screen resolution manager

```csharp
poiUI.RefreshLayout();   // when the screen resolution or safe area changes
```

Markers already follow your rects every frame. `RefreshLayout` makes the canvas catch up with the new screen and
re-reads the markers' size in the same frame. Call it as well if you resize a marker prefab at runtime. If your
manager has a UnityEvent, you can wire `POI_UI.RefreshLayout` to it in the Inspector instead.

## 4. Line the compass markers up with your NEWS compass

- Use a dedicated, empty child of your compass strip as the **Compass › Viewport**: stretched to the strip's
  width (anchors 0 to 1 horizontally) and as tall as the band where icons should sit. Nothing else should be
  parented to it or lay it out. Markers are centered vertically in it, with their distance label.
- A marker's offset from the viewport's center is `bearing / Degrees Across Viewport × width`. The bearing is the
  angle from where the camera faces, flattened onto the ground, positive to the right. Set **Degrees Across
  Viewport** to your strip's span, and drive your strip from the same camera (`POI_UI.Camera`).
- `POIMath.GetHeading`, `GetBearing` and `BearingToOffset` give exactly the numbers the markers use, if your strip
  wants them.
- `POI_UI` runs in LateUpdate at execution order 10000, after camera rigs. Update your strip at that order or later,
  so both use the same camera pose.

## Marker prefabs

Edit or duplicate `POI_ScreenMarker` and `POI_CompassMarker` to restyle them. A marker prefab needs a **POI
Marker** component pointing to its **Icon Image**, and optionally to a **Distance Label** (any TextMesh Pro text).
Its size, children included, is what is kept inside the viewport. Keep its raycast targets off, so markers never
block clicks.

One copy is spawned per active point, as a child of the viewport, and reused after the point is deactivated.
Assigning a different prefab or viewport at runtime respawns every marker.

## How the icons behave

- **Screen:** an icon sits on its point while the point is inside the rect. Outside it, the icon moves onto the
  rect's edge along the line from the rect's center towards the point. For a point behind the camera, the
  direction comes from the camera's view, not from Unity's projection, which would mirror it to the wrong side.
- **Compass:** looking up or down does not move the icons; points beyond the edges stop at the edges.
- **Fast cameras:** positions are exact for the frame being drawn, with no smoothing.
- **Overlaps:** icons of points in the same direction overlap, including on a compass edge.
