using System;
using UnityEngine;

namespace POI
{
    /// <summary>Size and safe area of the screen at one moment, as reported by <see cref="ScreenResolutionManager"/>.</summary>
    public readonly struct ScreenResolution : IEquatable<ScreenResolution>
    {
        public ScreenResolution(Vector2Int size, Rect safeArea)
        {
            Size = size;
            SafeArea = safeArea;
        }

        /// <summary>The screen as it is now.</summary>
        public static ScreenResolution Current => new ScreenResolution(new Vector2Int(Screen.width, Screen.height), Screen.safeArea);

        /// <summary>Width and height in pixels.</summary>
        public Vector2Int Size { get; }

        /// <summary>Part of the screen, in pixels, that is not covered by notches, rounded corners or system bars.</summary>
        public Rect SafeArea { get; }

        /// <summary>Width divided by height.</summary>
        public float Aspect => Size.y > 0 ? (float)Size.x / Size.y : 1f;

        public bool IsLandscape => Size.x >= Size.y;

        public bool Equals(ScreenResolution other) => Size == other.Size && SafeArea == other.SafeArea;

        public override bool Equals(object obj) => obj is ScreenResolution other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Size, SafeArea);

        public static bool operator ==(ScreenResolution left, ScreenResolution right) => left.Equals(right);

        public static bool operator !=(ScreenResolution left, ScreenResolution right) => !left.Equals(right);

        public override string ToString() => $"{Size.x}x{Size.y} (safe area {SafeArea})";
    }
}
