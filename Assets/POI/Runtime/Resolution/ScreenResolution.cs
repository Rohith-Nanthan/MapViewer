using System;
using UnityEngine;

namespace POI
{
    public readonly struct ScreenResolution : IEquatable<ScreenResolution>
    {
        public ScreenResolution(Vector2Int size, Rect safeArea)
        {
            Size = size;
            SafeArea = safeArea;
        }

        public static ScreenResolution Current => new ScreenResolution(new Vector2Int(Screen.width, Screen.height), Screen.safeArea);

        public Vector2Int Size { get; }

        public Rect SafeArea { get; }

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
