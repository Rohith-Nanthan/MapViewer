using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POI
{
    [AddComponentMenu("POI/POI Marker")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class POI_Marker : MonoBehaviour
    {
        public const string DefaultDistanceFormat = "{0} m";

        const int k_NoDistanceShown = int.MinValue;

        [Tooltip("Image that shows the point of interest's icon.")]
        [SerializeField] Image m_IconImage;

        [Tooltip("Optional text that shows the distance from the player, e.g. under a compass marker.")]
        [SerializeField] TMP_Text m_DistanceLabel;

        string _distanceFormat;
        bool _isDistanceFormatValid;
        int _shownDistance = k_NoDistanceShown;

        public RectTransform RectTransform => (RectTransform)transform;

        public Image IconImage
        {
            get => m_IconImage;
            set => m_IconImage = value;
        }

        public TMP_Text DistanceLabel
        {
            get => m_DistanceLabel;
            set
            {
                m_DistanceLabel = value;
                _shownDistance = k_NoDistanceShown;
            }
        }

        public bool IsVisible => gameObject.activeSelf;

        // The template a MarkerSet copied this marker from, so copies are only reused for the same template.
        internal POI_Marker Template { get; set; }

        public static bool IsValidDistanceFormat(string format)
        {
            if (format == null)
                return false;

            try
            {
                string.Format(format, 0);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public void SetIcon(Sprite sprite)
        {
            if (m_IconImage == null)
                return;

            if (m_IconImage.sprite != sprite)
                m_IconImage.sprite = sprite;

            bool hasSprite = sprite != null;
            if (m_IconImage.enabled != hasSprite)
                m_IconImage.enabled = hasSprite;
        }

        public void SetLocalPosition(Vector2 position)
        {
            Transform markerTransform = transform;
            Vector3 localPosition = markerTransform.localPosition;
            var newPosition = new Vector3(position.x, position.y, localPosition.z);

            // Only touch the transform when it moves, so idle markers do not rebuild the canvas.
            if (localPosition != newPosition)
                markerTransform.localPosition = newPosition;
        }

        // The text is rebuilt only when the whole number of meters changes. An invalid format shows the number alone.
        public void SetDistance(float meters, string format)
        {
            if (m_DistanceLabel == null)
                return;

            if (format != _distanceFormat)
            {
                _distanceFormat = format;
                _isDistanceFormatValid = IsValidDistanceFormat(format);
                _shownDistance = k_NoDistanceShown;
            }

            int roundedMeters = Mathf.RoundToInt(meters);
            if (roundedMeters == _shownDistance)
                return;

            _shownDistance = roundedMeters;
            m_DistanceLabel.text = _isDistanceFormatValid ? string.Format(format, roundedMeters) : roundedMeters.ToString();
        }

        void Reset()
        {
            m_IconImage = GetComponent<Image>();
            m_DistanceLabel = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
