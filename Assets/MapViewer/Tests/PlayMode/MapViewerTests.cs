using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Maps.Tests
{
    /// <summary>End-to-end checks of the shipped prefab: public API, input actions and device control.</summary>
    public class MapViewerTests : InputTestFixture
    {
        const string PrefabPath = "Assets/MapViewer/Prefabs/MapViewer.prefab";
        const string ControlsPath = "Assets/MapViewer/Input/MapViewerControls.inputactions";

        // Unscaled seconds to hold an input; long enough to see movement at the default speeds.
        const float HoldTime = 0.4f;

        readonly List<bool> _enabledChanges = new List<bool>();
        GameObject _canvas;
        Texture2D _texture;
        Sprite _sprite;
        MapViewerSettings _settings;
        MapViewer _viewer;
        InputActionMap _mapActions;
        Gamepad _gamepad;
        Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _mouse = InputSystem.AddDevice<Mouse>();

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            _mapActions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath).FindActionMap("Map");
#else
            GameObject prefab = null;
#endif
            if (prefab == null)
                Assert.Ignore("The MapViewer prefab can only be loaded in the Editor.");

            _canvas = new GameObject("Canvas", typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            _texture = new Texture2D(256, 256);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 256f, 256f), new Vector2(0.5f, 0.5f));

            // Tests tune their own copy; changing the shared settings asset in Play Mode would persist.
            _settings = ScriptableObject.CreateInstance<MapViewerSettings>();

            GameObject instance = Object.Instantiate(prefab, _canvas.transform);
            instance.SetActive(false);

            // A fixed-size viewport keeps the tests independent of the Game view's size.
            var viewerTransform = (RectTransform)instance.transform;
            viewerTransform.anchorMin = viewerTransform.anchorMax = new Vector2(0.5f, 0.5f);
            viewerTransform.sizeDelta = new Vector2(800f, 450f);
            viewerTransform.anchoredPosition = Vector2.zero;
            _viewer = instance.GetComponent<MapViewer>();
            _viewer.Settings = _settings;
            _viewer.MapSprite = _sprite;
            _viewer.EnabledChanged += _enabledChanges.Add;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(_canvas);
            Object.Destroy(_settings);
            Object.Destroy(_sprite);
            Object.Destroy(_texture);
            _enabledChanges.Clear();
            base.TearDown();
        }

        IEnumerable<InputAction> MapActions => _mapActions.actions;

        static IEnumerator Hold() => new WaitForSecondsRealtime(HoldTime);

        Vector2 ViewportCenterOnScreen()
        {
            RectTransform viewportTransform = _viewer.View.Viewport;
            return RectTransformUtility.WorldToScreenPoint(null, viewportTransform.TransformPoint(viewportTransform.rect.center));
        }

        [UnityTest]
        public IEnumerator Enable_ShowsMapAndEnablesAllInputActions()
        {
            _viewer.Enable();
            yield return null;

            Assert.That(_viewer.gameObject.activeInHierarchy, Is.True);
            Assert.That(_viewer.IsEnabled, Is.True);
            Assert.That(_enabledChanges, Is.EqualTo(new[] { true }));
            foreach (InputAction action in MapActions)
                Assert.That(action.enabled, Is.True, action.name);
        }

        [UnityTest]
        public IEnumerator Disable_HidesMapAndDisablesAllInputActions()
        {
            _viewer.Enable();
            yield return null;

            _viewer.Disable();
            yield return null;

            Assert.That(_viewer.gameObject.activeInHierarchy, Is.False);
            Assert.That(_viewer.IsEnabled, Is.False);
            Assert.That(_enabledChanges, Is.EqualTo(new[] { true, false }));
            foreach (InputAction action in MapActions)
                Assert.That(action.enabled, Is.False, action.name);
        }

        [UnityTest]
        public IEnumerator Triggers_ZoomInAndOut()
        {
            _viewer.Enable();
            yield return null;

            Set(_gamepad.rightTrigger, 1f);
            yield return Hold();
            float zoomedIn = _viewer.Model.Zoom;
            Set(_gamepad.rightTrigger, 0f);
            Set(_gamepad.leftTrigger, 1f);
            yield return Hold();

            Assert.That(zoomedIn, Is.GreaterThan(1.05f));
            Assert.That(_viewer.Model.Zoom, Is.LessThan(zoomedIn));
        }

        [UnityTest]
        public IEnumerator LeftStickAndDpad_PanZoomedMap()
        {
            _settings.Zoom.DefaultZoom = 4f;
            _viewer.Enable();
            yield return null;

            Set(_gamepad.leftStick, Vector2.right);
            yield return Hold();
            Set(_gamepad.leftStick, Vector2.zero);
            float afterStick = _viewer.Model.Pan.x;
            Press(_gamepad.dpad.down);
            yield return Hold();

            // Pushing right reveals more of the map's right side, so the map itself moves left.
            Assert.That(afterStick, Is.LessThan(0f));
            Assert.That(_viewer.Model.Pan.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator Triggers_StillZoomWhileGameIsPaused()
        {
            Time.timeScale = 0f;
            _viewer.Enable();
            yield return null;

            Set(_gamepad.rightTrigger, 1f);
            yield return Hold();

            Assert.That(_viewer.Model.Zoom, Is.GreaterThan(1.05f));
        }

        [UnityTest]
        public IEnumerator MouseScroll_ZoomsIn()
        {
            _viewer.Enable();
            yield return null;
            Set(_mouse.position, ViewportCenterOnScreen());

            Set(_mouse.scroll, new Vector2(0f, 1f), queueEventOnly: true);
            yield return Hold();

            Assert.That(_viewer.Model.Zoom, Is.GreaterThan(1.05f));
        }

        [UnityTest]
        public IEnumerator MiddleMouseDrag_PansMap()
        {
            _settings.Zoom.DefaultZoom = 4f;
            _viewer.Enable();
            yield return null;
            Vector2 start = ViewportCenterOnScreen();
            Set(_mouse.position, start);
            yield return null;

            // Queue only, so the press is processed in the same frame the map reads it.
            Press(_mouse.middleButton, queueEventOnly: true);
            yield return null;
            Set(_mouse.position, start + new Vector2(40f, 0f), queueEventOnly: true);
            yield return null;
            Release(_mouse.middleButton, queueEventOnly: true);
            yield return null;

            Assert.That(_viewer.Model.Pan.x, Is.GreaterThan(0f));
            Assert.That(_viewer.Model.Pan.y, Is.EqualTo(0f).Within(1e-3f));
        }
    }
}
