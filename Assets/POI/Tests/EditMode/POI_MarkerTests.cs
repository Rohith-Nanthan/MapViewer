using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POI.Tests
{
    public class POI_MarkerTests
    {
        GameObject _root;
        POI_Marker _marker;
        Image _icon;
        TMP_Text _label;
        Texture2D _texture;
        Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Marker", typeof(RectTransform), typeof(Image));
            _icon = _root.GetComponent<Image>();
            var label = new GameObject("Distance", typeof(RectTransform));
            label.transform.SetParent(_root.transform, false);
            _label = label.AddComponent<TextMeshProUGUI>();

            // Reset wires the icon and the label, as when the component is added in the Inspector.
            _marker = _root.AddComponent<POI_Marker>();
            _texture = new Texture2D(4, 4);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void Reset_FindsIconAndDistanceLabel()
        {
            Assert.That(_marker.Icon, Is.SameAs(_icon));
            Assert.That(_marker.DistanceLabel, Is.SameAs(_label));
        }

        [Test]
        public void SetSprite_ShowsSpriteAndHidesImageWithoutOne()
        {
            _marker.SetSprite(_sprite);
            Assert.That(_icon.sprite, Is.SameAs(_sprite));
            Assert.That(_icon.enabled, Is.True);

            _marker.SetSprite(null);
            Assert.That(_icon.enabled, Is.False);
        }

        [Test]
        public void SetDistance_ShowsWholeMetersInFormat()
        {
            _marker.SetDistance(12.4f, "{0} m");
            Assert.That(_label.text, Is.EqualTo("12 m"));

            _marker.SetDistance(12.6f, "{0}m away");
            Assert.That(_label.text, Is.EqualTo("13m away"));
        }

        [Test]
        public void SetDistance_InvalidFormat_ShowsPlainNumber()
        {
            _marker.SetDistance(7f, "{1} m");

            Assert.That(_label.text, Is.EqualTo("7"));
        }

        [Test]
        public void IsValidDistanceFormat_AcceptsFormatsWithOneArgument()
        {
            Assert.That(POI_Marker.IsValidDistanceFormat(POI_Marker.DefaultDistanceFormat), Is.True);
            Assert.That(POI_Marker.IsValidDistanceFormat("{0:N0}m"), Is.True);
            Assert.That(POI_Marker.IsValidDistanceFormat(string.Empty), Is.True);
            Assert.That(POI_Marker.IsValidDistanceFormat("{1} m"), Is.False);
            Assert.That(POI_Marker.IsValidDistanceFormat("{0 m"), Is.False);
            Assert.That(POI_Marker.IsValidDistanceFormat(null), Is.False);
        }

        [Test]
        public void SetLocalPosition_MovesPivotAndKeepsDepth()
        {
            _root.transform.localPosition = new Vector3(0f, 0f, 5f);

            _marker.SetLocalPosition(new Vector2(10f, -20f));

            Assert.That(_root.transform.localPosition, Is.EqualTo(new Vector3(10f, -20f, 5f)));
        }
    }
}
