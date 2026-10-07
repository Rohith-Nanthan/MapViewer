using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace POI.Tests
{
    // Loaded by GUID, so the tests work wherever the POI folder is placed.
    public class MarkerPrefabTests
    {
        const string k_ScreenMarkerGuid = "dd11bf0aec7a8bc4eab7c15636a95ed6";
        const string k_CompassMarkerGuid = "a0b999d44ef78bf4b8dafbe7bf6885e0";

        static POI_Marker LoadMarkerPrefab(string guid) =>
            AssetDatabase.LoadAssetAtPath<POI_Marker>(AssetDatabase.GUIDToAssetPath(guid));

        static void AssertSpawnable(POI_Marker prefab)
        {
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.gameObject.activeSelf, Is.True, "active root");
            Assert.That(prefab.IconImage, Is.Not.Null, "icon image");
            Assert.That(prefab.IconImage.raycastTarget, Is.False, "icons do not block clicks");

            RectTransform root = prefab.RectTransform;
            Assert.That(root.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(root.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(root.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        [Test]
        public void ScreenMarkerPrefab_HasAnIconAndNoDistanceLabel()
        {
            POI_Marker prefab = LoadMarkerPrefab(k_ScreenMarkerGuid);

            AssertSpawnable(prefab);
            Assert.That(prefab.DistanceLabel, Is.Null);
        }

        [Test]
        public void CompassMarkerPrefab_HasAnIconAndADistanceLabelBelowIt()
        {
            POI_Marker prefab = LoadMarkerPrefab(k_CompassMarkerGuid);

            AssertSpawnable(prefab);
            Assert.That(prefab.DistanceLabel, Is.Not.Null);
            Assert.That(prefab.DistanceLabel.transform.IsChildOf(prefab.transform), Is.True);
            Assert.That(prefab.DistanceLabel.rectTransform.anchorMax.y, Is.EqualTo(0f), "anchored to the icon's bottom");
        }

        [Test]
        public void POI_UIInspectorPropertyPaths_AllExist()
        {
            var gameObject = new GameObject("POI UI");
            try
            {
                var serializedUI = new SerializedObject(gameObject.AddComponent<POI_UI>());
                string[] paths =
                {
                    "m_Camera", "m_Player", "m_Screen", "m_Compass",
                    "m_Screen.m_Viewport", "m_Screen.m_MarkerPrefab", "m_Screen.m_DistanceFormat",
                    "m_Compass.m_Viewport", "m_Compass.m_MarkerPrefab", "m_Compass.m_DistanceFormat",
                };

                foreach (string path in paths)
                    Assert.That(serializedUI.FindProperty(path), Is.Not.Null, path);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
