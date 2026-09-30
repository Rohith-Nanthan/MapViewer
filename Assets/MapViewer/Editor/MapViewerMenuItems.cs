using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Maps
{
    /// <summary>Adds GameObject > UI > Map Viewer, which drops the prefab into a canvas like Unity's own UI items.</summary>
    static class MapViewerMenuItems
    {
        const string PrefabGuid = "5a1f9ce9c2052f24a82786d60a905ee0";

        [MenuItem("GameObject/UI/Map Viewer", false, 2100)]
        static void CreateMapViewer(MenuCommand command)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(PrefabGuid));
            if (prefab == null)
            {
                Debug.LogError("The Map Viewer prefab could not be found. Was it deleted?");
                return;
            }

            StageHandle stage = StageUtility.GetCurrentStageHandle();
            Transform parent = FindCanvasParent(command.context as GameObject, stage);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            GameObjectUtility.EnsureUniqueNameForSibling(instance);
            Undo.RegisterCreatedObjectUndo(instance, "Create Map Viewer");

            if (stage == StageUtility.GetMainStageHandle())
                EnsureEventSystem();

            Selection.activeGameObject = instance;
        }

        static Transform FindCanvasParent(GameObject selection, StageHandle stage)
        {
            if (selection != null && selection.GetComponentInParent<Canvas>(true) != null)
                return selection.transform;

            Canvas canvas = stage.FindComponentOfType<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : CreateCanvas().transform;
        }

        static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = LayerMask.NameToLayer("UI"),
            };
            StageUtility.PlaceGameObjectInCurrentStage(canvasObject);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            return canvas;
        }

        // The map reads its own input actions, but the canvas still needs an event system for UI clicks.
        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }
    }
}
