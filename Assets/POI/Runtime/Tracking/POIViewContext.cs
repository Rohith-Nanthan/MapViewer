using System;
using UnityEngine;

namespace POI
{
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

        public Camera Camera { get; }

        public Vector3 CameraPosition { get; }

        public Vector3 CameraForward { get; }

        public Vector3 CameraUp { get; }

        public Vector3 PlayerPosition { get; }
    }
}
