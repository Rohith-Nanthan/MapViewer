using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Maps.Tests
{
    /// <summary>Drives virtual devices through the shipped bindings to check what the map receives.</summary>
    public class InputSystemMapInputTests : InputTestFixture
    {
        const string k_ControlsPath = "Assets/MapViewer/Input/MapViewerControls.inputactions";

        readonly List<Object> _createdObjects = new List<Object>();
        readonly List<InputSystemMapInput> _inputs = new List<InputSystemMapInput>();
        InputActionAsset _controls;
        InputSystemMapInput _input;
        Gamepad _gamepad;
        Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _mouse = InputSystem.AddDevice<Mouse>();

            // A copy of the shipped asset tests the real bindings without mutating the asset itself.
            _controls = Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(k_ControlsPath)));
            _input = CreateInput();
        }

        public override void TearDown()
        {
            foreach (InputSystemMapInput input in _inputs)
                input.Disable();
            _inputs.Clear();
            foreach (Object createdObject in _createdObjects)
                Object.DestroyImmediate(createdObject);
            _createdObjects.Clear();
            base.TearDown();
        }

        InputSystemMapInput CreateInput()
        {
            var input = new InputSystemMapInput(Reference("Pan"), Reference("Zoom"), Reference("ZoomStep"),
                Reference("Drag"), Reference("Point"));
            _inputs.Add(input);
            return input;
        }

        InputActionReference Reference(string actionName) =>
            Track(InputActionReference.Create(_controls.FindAction($"Map/{actionName}", throwIfNotFound: true)));

        T Track<T>(T createdObject) where T : Object
        {
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        [Test]
        public void Enable_EnablesEveryMapAction()
        {
            _input.Enable();

            Assert.That(_input.IsEnabled, Is.True);
            foreach (InputAction action in _controls.FindActionMap("Map").actions)
                Assert.That(action.enabled, Is.True, action.name);
        }

        [Test]
        public void Disable_DisablesEveryMapAction()
        {
            _input.Enable();

            _input.Disable();

            Assert.That(_input.IsEnabled, Is.False);
            foreach (InputAction action in _controls.FindActionMap("Map").actions)
                Assert.That(action.enabled, Is.False, action.name);
        }

        [Test]
        public void Disable_WhenNeverEnabled_LeavesSharedActionsRunning()
        {
            // Another viewer sharing the asset has the actions running.
            InputActionMap sharedMap = _controls.FindActionMap("Map");
            sharedMap.Enable();

            _input.Disable();

            foreach (InputAction action in sharedMap.actions)
                Assert.That(action.enabled, Is.True, action.name);
            sharedMap.Disable();
        }

        [Test]
        public void Disable_WhileAnotherViewerUsesTheActions_KeepsThemRunningUntilBothAreDisabled()
        {
            InputSystemMapInput otherViewerInput = CreateInput();
            _input.Enable();
            otherViewerInput.Enable();
            InputActionMap sharedMap = _controls.FindActionMap("Map");

            _input.Disable();
            bool stillRunningForOtherViewer = sharedMap.actions.All(action => action.enabled);
            otherViewerInput.Disable();

            Assert.That(stillRunningForOtherViewer, Is.True);
            Assert.That(_input.ReadFrame().HasPointer, Is.False, "a disabled input reads nothing");
            foreach (InputAction action in sharedMap.actions)
                Assert.That(action.enabled, Is.False, action.name);
        }

        [Test]
        public void ReadFrame_WhileDisabled_ReturnsNoInput()
        {
            Set(_gamepad.rightTrigger, 1f);
            Set(_gamepad.leftStick, Vector2.right);

            MapInputFrame frame = _input.ReadFrame();

            Assert.That(frame.Zoom, Is.Zero);
            Assert.That(frame.Pan, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void RightTrigger_ZoomsIn()
        {
            _input.Enable();

            Set(_gamepad.rightTrigger, 1f);

            Assert.That(_input.ReadFrame().Zoom, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void LeftTrigger_ZoomsOut()
        {
            _input.Enable();

            Set(_gamepad.leftTrigger, 1f);

            Assert.That(_input.ReadFrame().Zoom, Is.EqualTo(-1f).Within(1e-3f));
        }

        [Test]
        public void LeftStick_Pans()
        {
            _input.Enable();

            Set(_gamepad.leftStick, new Vector2(0f, -1f));

            Assert.That(_input.ReadFrame().Pan.y, Is.EqualTo(-1f).Within(1e-3f));
        }

        [Test]
        public void Dpad_Pans()
        {
            _input.Enable();

            Press(_gamepad.dpad.left);

            Assert.That(_input.ReadFrame().Pan.x, Is.EqualTo(-1f).Within(1e-3f));
        }

        [Test]
        public void MouseScroll_ProducesZoomSteps()
        {
            _input.Enable();

            Set(_mouse.scroll, new Vector2(0f, 1f));

            Assert.That(_input.ReadFrame().ZoomSteps, Is.GreaterThan(0f));
        }

        [Test]
        public void MiddleMouseButton_StartsAndHoldsDrag()
        {
            _input.Enable();

            Press(_mouse.middleButton);
            MapInputFrame pressFrame = _input.ReadFrame();
            InputSystem.Update();
            MapInputFrame heldFrame = _input.ReadFrame();

            Assert.That(pressFrame.DragPressedThisFrame && pressFrame.DragHeld, Is.True);
            Assert.That(heldFrame.DragPressedThisFrame, Is.False);
            Assert.That(heldFrame.DragHeld, Is.True);
        }

        [Test]
        public void MousePosition_IsReportedAsPointer()
        {
            _input.Enable();

            Set(_mouse.position, new Vector2(320f, 180f));
            MapInputFrame frame = _input.ReadFrame();

            Assert.That(frame.HasPointer, Is.True);
            Assert.That(frame.PointerPosition, Is.EqualTo(new Vector2(320f, 180f)));
        }
    }
}
