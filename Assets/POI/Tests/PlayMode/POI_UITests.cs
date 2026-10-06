using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace POI.Tests
{
    /// <summary>
    /// End-to-end checks of POI_World and POI_UI on a Screen Space - Overlay canvas: activation, both kinds of
    /// marker, keeping markers inside their viewports, and keeping up with a fast camera.
    /// </summary>
    public class POI_UITests
    {
        const int k_Padding = 16;
        const float k_ScreenIconSize = 64f;
        const float k_CompassWidth = 900f;
        const float k_CompassPadding = 8f;
        const float k_LabelWidth = 120f;

        readonly List<Object> _created = new List<Object>();
        Camera _camera;
        POI_UI _ui;
        RectTransform _screenViewport;
        Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            var texture = Track(new Texture2D(4, 4));
            _sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));

            // At the origin, facing +Z.
            _camera = Track(new GameObject("POI Test Camera")).AddComponent<Camera>();
            _camera.fieldOfView = 60f;

            // Configured while inactive, so POI_UI wakes up with its sections assigned.
            GameObject canvasObject = Track(new GameObject("POI Test Canvas", typeof(Canvas)));
            canvasObject.SetActive(false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            _screenViewport = CreateRect("Screen Viewport", canvasObject.transform, Vector2.zero);
            _screenViewport.anchorMin = Vector2.zero;
            _screenViewport.anchorMax = Vector2.one;
            POI_Marker screenTemplate = CreateMarker("Screen Marker", _screenViewport, k_ScreenIconSize, false);

            RectTransform compassViewport = CreateRect("Compass Viewport", canvasObject.transform, new Vector2(k_CompassWidth, 100f));
            compassViewport.anchorMin = compassViewport.anchorMax = new Vector2(0.5f, 1f);
            compassViewport.anchoredPosition = new Vector2(0f, -60f);
            POI_Marker compassTemplate = CreateMarker("Compass Marker", compassViewport, 48f, true);

            _ui = canvasObject.AddComponent<POI_UI>();
            _ui.Camera = _camera;
            _ui.ScreenSettings = new ScreenMarkerSettings(_screenViewport, screenTemplate)
            {
                Padding = new EdgePadding(k_Padding),
            };
            _ui.CompassSettings = new CompassMarkerSettings(compassViewport, compassTemplate)
            {
                FieldOfView = 180f,
                Padding = k_CompassPadding,
            };
            canvasObject.SetActive(true);
        }

        // Destroyed immediately, so no point of interest is still registered when the next test starts.
        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            var rectTransform = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rectTransform.SetParent(parent, false);
            rectTransform.sizeDelta = size;
            return rectTransform;
        }

        static POI_Marker CreateMarker(string name, Transform viewport, float iconSize, bool withLabel)
        {
            RectTransform icon = CreateRect(name, viewport, new Vector2(iconSize, iconSize));

            // Reset only wires the references in the Editor, so they are set explicitly in Play Mode.
            var marker = icon.gameObject.AddComponent<POI_Marker>();
            marker.Icon = icon.gameObject.AddComponent<Image>();
            if (withLabel)
            {
                RectTransform label = CreateRect("Distance", icon, new Vector2(k_LabelWidth, 24f));
                label.anchoredPosition = new Vector2(0f, -iconSize * 0.5f - 12f);
                marker.DistanceLabel = label.gameObject.AddComponent<TextMeshProUGUI>();
            }

            return marker;
        }

        POI_World CreatePoint(Vector3 position)
        {
            var poi = Track(new GameObject("POI")).AddComponent<POI_World>();
            poi.Icon = _sprite;
            poi.Position = position;
            return poi;
        }

        POI_Marker ScreenMarker(IPointOfInterest poi)
        {
            Assert.That(_ui.ScreenTracker.TryGetMarker(poi, out POI_Marker marker), Is.True, "screen marker");
            return marker;
        }

        POI_Marker CompassMarker(IPointOfInterest poi)
        {
            Assert.That(_ui.CompassTracker.TryGetMarker(poi, out POI_Marker marker), Is.True, "compass marker");
            return marker;
        }

        static Vector2 LocalPosition(POI_Marker marker) => marker.transform.localPosition;

        [UnityTest]
        public IEnumerator Activate_ShowsTheIconOnScreenAndOnTheCompass()
        {
            POI_World poi = CreatePoint(new Vector3(0f, 0f, 10f));

            poi.Activate();
            yield return null;

            POI_Marker screen = ScreenMarker(poi);
            POI_Marker compass = CompassMarker(poi);
            Assert.That(poi.IsActive, Is.True);
            Assert.That(screen.IsVisible && compass.IsVisible, Is.True);
            Assert.That(screen.Icon.sprite, Is.SameAs(_sprite));
            Assert.That(compass.Icon.sprite, Is.SameAs(_sprite));
            Assert.That(screen.transform.parent, Is.SameAs(_screenViewport));
            Assert.That(LocalPosition(screen).magnitude, Is.LessThan(1f), "straight ahead is the viewport's center");
            Assert.That(LocalPosition(compass).x, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator DeActivate_HidesBothMarkers()
        {
            POI_World poi = CreatePoint(new Vector3(0f, 0f, 10f));
            poi.Activate();
            yield return null;
            POI_Marker screen = ScreenMarker(poi);
            POI_Marker compass = CompassMarker(poi);

            poi.DeActivate();
            yield return null;

            Assert.That(poi.IsActive, Is.False);
            Assert.That(_ui.ScreenTracker.TryGetMarker(poi, out _), Is.False);
            Assert.That(_ui.CompassTracker.TryGetMarker(poi, out _), Is.False);
            Assert.That(screen.IsVisible || compass.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ActivatedWhileTheUIIsDisabled_ShowsOnceItIsEnabled()
        {
            _ui.enabled = false;
            POI_World poi = CreatePoint(new Vector3(0f, 0f, 10f));
            poi.Activate();
            yield return null;
            Assert.That(_ui.ScreenTracker.Count, Is.EqualTo(0));

            _ui.enabled = true;
            yield return null;

            Assert.That(ScreenMarker(poi).IsVisible, Is.True);
            Assert.That(CompassMarker(poi).IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator DisablingThePointsGameObject_HidesItUntilItIsEnabled()
        {
            POI_World poi = CreatePoint(new Vector3(0f, 0f, 10f));
            poi.Activate();
            yield return null;

            poi.gameObject.SetActive(false);
            yield return null;
            Assert.That(_ui.ScreenTracker.TryGetMarker(poi, out _), Is.False);
            Assert.That(poi.IsActive, Is.True, "still active, just hidden");

            poi.gameObject.SetActive(true);
            yield return null;
            Assert.That(ScreenMarker(poi).IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator DestroyingThePoint_RemovesItsMarkers()
        {
            POI_World poi = CreatePoint(new Vector3(0f, 0f, 10f));
            poi.Activate();
            yield return null;

            Object.Destroy(poi.gameObject);
            yield return null;

            Assert.That(_ui.ScreenTracker.Count, Is.EqualTo(0));
            Assert.That(_ui.CompassTracker.Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator PointBehindTheCamera_HugsTheBottomEdgeInsideThePadding()
        {
            POI_World poi = CreatePoint(new Vector3(0f, 0f, -10f));
            poi.Activate();
            yield return null;

            Rect area = _screenViewport.rect;
            POI_Marker marker = ScreenMarker(poi);
            Assert.That(LocalPosition(marker).x, Is.EqualTo(area.center.x).Within(0.5f));
            Assert.That(LocalPosition(marker).y, Is.EqualTo(area.yMin + k_Padding + k_ScreenIconSize * 0.5f).Within(0.01f));

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_screenViewport, marker.transform);
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(area.yMin + k_Padding - 0.01f));
        }

        [UnityTest]
        public IEnumerator CameraTurnedFastInLateUpdate_MarkersKeepUpInTheSameFrame()
        {
            POI_World poi = CreatePoint(new Vector3(10f, 0f, 10f));
            poi.Activate();
            _camera.gameObject.AddComponent<FastCameraTurner>();
            float maxOffset = k_CompassWidth * 0.5f - k_CompassPadding - k_LabelWidth * 0.5f;

            for (int frame = 0; frame < 12; frame++)
            {
                yield return null;

                Transform cameraTransform = _camera.transform;
                Vector3 heading = POIUtil.GetHeading(cameraTransform.forward, cameraTransform.up, Vector3.up);
                float bearing = POIUtil.GetBearing(heading, poi.Position - cameraTransform.position, Vector3.up);
                float expected = Mathf.Clamp(POIUtil.BearingToOffset(bearing, 180f, k_CompassWidth), -maxOffset, maxOffset);
                Assert.That(LocalPosition(CompassMarker(poi)).x, Is.EqualTo(expected).Within(0.01f), $"compass, frame {frame}");

                // Where the screen marker belongs for the camera's pose now, not the pose before it turned.
                POI_Marker screen = ScreenMarker(poi);
                Rect pivotBounds = POIUtil.GetPivotBounds(_screenViewport.rect, POIUtil.GetBoundsAroundPivot(screen.RectTransform),
                    _ui.ScreenSettings.Padding);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_screenViewport, POIUtil.GetScreenTarget(_camera, poi.Position),
                    null, out Vector2 target);
                Assert.That(Vector2.Distance(POIUtil.ClampTowards(pivotBounds, target), LocalPosition(screen)), Is.LessThan(0.01f),
                    $"screen, frame {frame}");
            }
        }

        [UnityTest]
        public IEnumerator CompassMarker_ShowsTheDistanceFromThePlayer()
        {
            Transform player = Track(new GameObject("Player")).transform;
            player.position = new Vector3(0f, 0f, -5f);
            _ui.Player = player;
            POI_World poi = CreatePoint(new Vector3(0f, 0f, 10f));

            poi.Activate();
            yield return null;

            Assert.That(CompassMarker(poi).DistanceLabel.text, Is.EqualTo("15 m"));
        }

        [UnityTest]
        public IEnumerator RefreshLayout_ReadsTheMarkersNewSize()
        {
            POI_World poi = CreatePoint(new Vector3(100f, 0f, 10f));
            poi.Activate();
            yield return null;
            POI_Marker marker = ScreenMarker(poi);
            float right = _screenViewport.rect.xMax - k_Padding;
            Assert.That(LocalPosition(marker).x, Is.EqualTo(right - k_ScreenIconSize * 0.5f).Within(0.01f));

            marker.RectTransform.sizeDelta = new Vector2(128f, 128f);
            _ui.RefreshLayout();
            yield return null;

            Assert.That(LocalPosition(marker).x, Is.EqualTo(right - 64f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator WithoutACamera_HidesTheMarkers()
        {
            Assume.That(Camera.main, Is.Null, "the test scene has no main camera");
            POI_World poi = CreatePoint(new Vector3(0f, 0f, 10f));
            poi.Activate();
            yield return null;

            _ui.Camera = null;
            yield return null;

            Assert.That(ScreenMarker(poi).IsVisible, Is.False);
            Assert.That(CompassMarker(poi).IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator WithoutAResolutionManager_OneIsAdded()
        {
            yield return null;

            Assert.That(_ui.ScreenResolutionManager, Is.Not.Null);
        }
    }
}
