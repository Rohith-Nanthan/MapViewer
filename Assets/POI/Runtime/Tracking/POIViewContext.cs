using System;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Where the camera and the player are this frame. <see cref="POI_UI"/> reads it once, after the camera has
    /// moved, and hands the same values to every tracker.
    /// </summary>
    public readonly struct POIViewContext
    {
        public POIViewContext(Camera camera, Vector3 playerPosition)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            Transform cameraTransform = camera.transform;
            Camera = camera;
            CameraPosition = cameraTransform.position;
            CameraForward = cameraTransform.forward;
            CameraUp = cameraTransform.up;
            PlayerPosition = playerPosition;
        }

        /// <summary>Camera the markers are relative to.</summary>
        public Camera Camera { get; }

        public Vector3 CameraPosition { get; }

        public Vector3 CameraForward { get; }

        public Vector3 CameraUp { get; }

        /// <summary>Where distances are measured from.</summary>
        public Vector3 PlayerPosition { get; }
    }
}
