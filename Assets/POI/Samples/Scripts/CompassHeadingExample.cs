using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POI.Samples
{
    // Stand-in for a game's own compass strip. North is +Z. It runs as late as POI_UI, so the directions and the
    // markers use the same camera pose.
    [DefaultExecutionOrder(10000)]
    public sealed class CompassHeadingExample : MonoBehaviour
    {
        static readonly string[] k_Directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        [Tooltip("UI whose camera and Degrees Across Viewport the directions follow.")]
        [SerializeField] POI_UI m_UI;

        [Tooltip("Compass bar the directions slide along. The UI's compass viewport is a lane inside it, as wide as it.")]
        [SerializeField] RectTransform m_CompassBar;

        [Tooltip("Text copied for each direction, placed at its height in the bar. The template itself stays hidden.")]
        [SerializeField] TMP_Text m_DirectionTemplate;

        [Tooltip("Color of the north label, so it stands out from the other directions.")]
        [SerializeField] Color m_NorthColor = new Color(1f, 0.45f, 0.35f);

        RectTransform[] _labels;

        void Start()
        {
            if (m_UI == null || m_CompassBar == null || m_DirectionTemplate == null)
            {
                Debug.LogWarning($"{nameof(CompassHeadingExample)} needs a POI UI, a compass bar and a direction template.", this);
                enabled = false;
                return;
            }

            m_DirectionTemplate.gameObject.SetActive(false);
            CreateHeadingTick(m_CompassBar);

            _labels = new RectTransform[k_Directions.Length];
            for (int i = 0; i < k_Directions.Length; i++)
            {
                TMP_Text label = Instantiate(m_DirectionTemplate, m_CompassBar, false);
                label.name = $"Direction {k_Directions[i]}";
                label.text = k_Directions[i];
                if (i == 0)
                    label.color = m_NorthColor;

                label.gameObject.SetActive(true);
                _labels[i] = label.rectTransform;
            }
        }

        void LateUpdate()
        {
            Camera viewCamera = m_UI.Camera;
            if (viewCamera == null)
                return;

            Transform cameraTransform = viewCamera.transform;
            Rect area = m_CompassBar.rect;
            float degreesAcrossWidth = m_UI.CompassSettings.DegreesAcrossViewport;
            Vector3 heading = POIMath.GetHeading(cameraTransform.forward, cameraTransform.up, Vector3.up);

            for (int i = 0; i < _labels.Length; i++)
            {
                RectTransform label = _labels[i];
                Vector3 direction = Quaternion.Euler(0f, i * 360f / k_Directions.Length, 0f) * Vector3.forward;
                float offset = POIMath.BearingToOffset(POIMath.GetBearing(heading, direction, Vector3.up), degreesAcrossWidth, area.width);

                // Directions past the edges are hidden rather than piled up there, unlike the markers.
                bool isVisible = Mathf.Abs(offset) <= area.width * 0.5f - label.rect.width * 0.5f;
                if (label.gameObject.activeSelf != isVisible)
                    label.gameObject.SetActive(isVisible);

                Vector3 position = label.localPosition;
                label.localPosition = new Vector3(area.center.x + offset, position.y, position.z);
            }
        }

        // A short line at the top center marks where the camera faces.
        static void CreateHeadingTick(RectTransform compassBar)
        {
            var tick = (RectTransform)new GameObject("Heading", typeof(RectTransform), typeof(Image)).transform;
            tick.SetParent(compassBar, false);
            tick.anchorMin = tick.anchorMax = tick.pivot = new Vector2(0.5f, 1f);
            tick.sizeDelta = new Vector2(3f, 12f);
            tick.GetComponent<Image>().raycastTarget = false;
        }
    }
}
