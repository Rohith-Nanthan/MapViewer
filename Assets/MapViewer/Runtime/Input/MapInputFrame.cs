using UnityEngine;

namespace Maps
{
    /// <summary>One frame of map input, independent of the devices that produced it.</summary>
    public struct MapInputFrame
    {
        /// <summary>Directional pan in [-1, 1] per axis, e.g. a stick or D-pad. Applied as a velocity.</summary>
        public Vector2 Pan;

        /// <summary>Continuous zoom in [-1, 1], e.g. the triggers. Positive zooms in. Applied as a rate.</summary>
        public float Zoom;

        /// <summary>Discrete zoom steps this frame, e.g. scroll-wheel notches. Positive zooms in.</summary>
        public float ZoomSteps;

        /// <summary>The drag button went down this frame.</summary>
        public bool DragPressedThisFrame;

        /// <summary>The drag button is held down.</summary>
        public bool DragHeld;

        /// <summary>A pointer is available, so <see cref="PointerPosition"/> is meaningful.</summary>
        public bool HasPointer;

        /// <summary>Pointer position in screen pixels.</summary>
        public Vector2 PointerPosition;
    }
}
