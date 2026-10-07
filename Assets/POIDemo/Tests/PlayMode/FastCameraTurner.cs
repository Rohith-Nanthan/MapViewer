using UnityEngine;

namespace POI.Tests
{
    // Turns its camera quickly in LateUpdate, as camera rigs such as Cinemachine move cameras.
    sealed class FastCameraTurner : MonoBehaviour
    {
        public float DegreesPerFrame { get; set; } = 37f;

        void LateUpdate() => transform.Rotate(0f, DegreesPerFrame, 0f, Space.World);
    }
}
