using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POI.Samples
{
    /// <summary>
    /// Stand-in for a game's own compass: slides N, NE, E and the other directions along the POI UI's compass
    /// viewport with the camera's heading, over the same field of view as the compass markers, so the markers can
    /// be seen lining up with the directions. North is +Z. It runs as late as <see cref="POI_UI"/>, so the
    /// directions and the markers always use the same camera pose.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class CompassHeadingExample : MonoBehaviour
    {
        static readonly string[] k_Directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        [Tooltip("UI whose compass viewport, camera and field of view the directions follow.")]
        [SerializeField] POI_UI m_UI;

        [Tooltip("Text copied for each direction, placed at its height in the viewport. The template itself stays hidden.")]
        [SerializeField] TMP_Text m_DirectionTemplate;

        [Tooltip("Color of the north label, so it stands out from the other directions.")]
        [SerializeField] Color m_NorthColor = new Color(1f, 0.45f, 0.35f);

        RectTransform[] _labels;

        void Start()
        {
            RectTransform viewport = m_UI != null ? m_UI.CompassSettings.Viewport : null;
            if (viewport == null || m_DirectionTemplate == null)
            {
                Debug.LogWarning($"{nameof(CompassHeadingExample)} needs a POI UI with a compass viewport and a direction template.", this);
                enabled = false;
                return;
            }

            m_DirectionTemplate.gameObject.SetActive(false);
            CreateHeadingTick(viewport);

            _labels = new RectTransform[k_Directions.Length];
            for (int i = 0; i < k_Directions.Length; i++)
            {
                TMP_Text label = Instantiate(m_DirectionTemplate, viewport, false);
                label.name = $"Direction {k_Directions[i]}";
                label.text = k_Directions[i];
                if (i == 0)
                    label.color = m_NorthColor;

                // Drawn behind the markers, which are added to the viewport later.
                label.transform.SetSiblingIndex(0);
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
            Rect area = m_UI.CompassSettings.Viewport.rect;
            float fieldOfView = m_UI.CompassSettings.FieldOfView;
            Vector3 heading = POIUtil.GetHeading(cameraTransform.forward, cameraTransform.up, Vector3.up);

            for (int i = 0; i < _labels.Length; i++)
            {
                RectTransform label = _labels[i];
                Vector3 direction = Quaternion.Euler(0f, i * 360f / k_Directions.Length, 0f) * Vector3.forward;
                float offset = POIUtil.BearingToOffset(POIUtil.GetBearing(heading, direction, Vector3.up), fieldOfView, area.width);

                // Directions past the edges are hidden rather than piled up there, unlike the markers.
                bool isVisible = Mathf.Abs(offset) <= area.width * 0.5f - label.rect.width * 0.5f;
                if (label.gameObject.activeSelf != isVisible)
                    label.gameObject.SetActive(isVisible);

                Vector3 position = label.localPosition;
                label.localPosition = new Vector3(area.center.x + offset, position.y, position.z);
            }
        }

        // A short line at the top center marks where the camera faces.
        static void CreateHeadingTick(RectTransform viewport)
        {
            var tick = (RectTransform)new GameObject("Heading", typeof(RectTransform), typeof(Image)).transform;
            tick.SetParent(viewport, false);
            tick.anchorMin = tick.anchorMax = tick.pivot = new Vector2(0.5f, 1f);
            tick.sizeDelta = new Vector2(3f, 12f);
            tick.GetComponent<Image>().raycastTarget = false;
            tick.SetSiblingIndex(0);
        }
    }
}
