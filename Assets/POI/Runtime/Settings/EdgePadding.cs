using System;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Space kept between a UI element and each edge of the rectangle it stays inside, in canvas units. Unlike
    /// <see cref="RectOffset"/>, it is a plain value, so it can have defaults in field initializers.
    /// </summary>
    [Serializable]
    public struct EdgePadding : IEquatable<EdgePadding>
    {
        [SerializeField, Min(0f)] float m_Left;
        [SerializeField, Min(0f)] float m_Right;
        [SerializeField, Min(0f)] float m_Top;
        [SerializeField, Min(0f)] float m_Bottom;

        /// <summary>The same padding on every edge.</summary>
        public EdgePadding(float all)
            : this(all, all, all, all)
        {
        }

        /// <summary>Padding per edge, in the same order as <see cref="RectOffset"/>. Negative values count as 0.</summary>
        public EdgePadding(float left, float right, float top, float bottom)
        {
            m_Left = Mathf.Max(0f, left);
            m_Right = Mathf.Max(0f, right);
            m_Top = Mathf.Max(0f, top);
            m_Bottom = Mathf.Max(0f, bottom);
        }

        public float Left => m_Left;

        public float Right => m_Right;

        public float Top => m_Top;

        public float Bottom => m_Bottom;

        public bool Equals(EdgePadding other) =>
            m_Left == other.m_Left && m_Right == other.m_Right && m_Top == other.m_Top && m_Bottom == other.m_Bottom;

        public override bool Equals(object obj) => obj is EdgePadding other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(m_Left, m_Right, m_Top, m_Bottom);

        public override string ToString() => $"(left {m_Left}, right {m_Right}, top {m_Top}, bottom {m_Bottom})";
    }
}
