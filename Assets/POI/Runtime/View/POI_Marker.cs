using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POI
{
    /// <summary>
    /// One marker in a POI viewport: the image that shows a point of interest's icon, and optionally a label that
    /// shows its distance. Put it on the template that <see cref="POI_UI"/> copies for each point of interest; the
    /// template's size, children included, is what is kept inside the viewport.
    /// </summary>
    [AddComponentMenu("POI/POI Marker")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class POI_Marker : MonoBehaviour
    {
        [Tooltip("Image that shows the point of interest's icon.")]
        [SerializeField] Image m_Icon;

        [Tooltip("Optional text that shows the distance from the player, e.g. under a compass marker.")]
        [SerializeField] TMP_Text m_DistanceLabel;

        string _distanceFormat;
        bool _isDistanceFormatValid;
        int _shownDistance = int.MinValue;

        public RectTransform RectTransform => (RectTransform)transform;

        public Image Icon => m_Icon;

        public TMP_Text DistanceLabel => m_DistanceLabel;

        public bool IsVisible => gameObject.activeSelf;

        // The template a MarkerSet copied this marker from, so copies are only reused for the same template.
        internal POI_Marker Template { get; set; }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        /// <summary>Shows a sprite in the icon image, and hides the image while there is none.</summary>
        public void SetSprite(Sprite sprite)
        {
            if (m_Icon == null)
                return;

            if (m_Icon.sprite != sprite)
                m_Icon.sprite = sprite;

            bool hasSprite = sprite != null;
            if (m_Icon.enabled != hasSprite)
                m_Icon.enabled = hasSprite;
        }

        /// <summary>Moves the marker's pivot to a point in its parent's local space.</summary>
        public void SetLocalPosition(Vector2 position)
        {
            Transform markerTransform = transform;
            Vector3 localPosition = markerTransform.localPosition;
            var newPosition = new Vector3(position.x, position.y, localPosition.z);

            // Only touch the transform when it moves, so idle markers do not rebuild the canvas.
            if (localPosition != newPosition)
                markerTransform.localPosition = newPosition;
        }

        /// <summary>
        /// Shows a distance in the label, if there is one. The text is only rebuilt when the whole number of
        /// meters changes. An invalid format falls back to the plain number.
        /// </summary>
        /// <param name="format">Format with {0} for the distance in whole meters, e.g. "{0} m".</param>
        public void SetDistance(float meters, string format)
        {
            if (m_DistanceLabel == null)
                return;

            if (format != _distanceFormat)
            {
                _distanceFormat = format;
                _isDistanceFormatValid = POIUtil.IsValidDistanceFormat(format);
                _shownDistance = int.MinValue;
            }

            int rounded = Mathf.RoundToInt(meters);
            if (rounded == _shownDistance)
                return;

            _shownDistance = rounded;
            m_DistanceLabel.text = _isDistanceFormatValid ? string.Format(format, rounded) : rounded.ToString();
        }

        void Reset()
        {
            m_Icon = GetComponent<Image>();
            m_DistanceLabel = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
