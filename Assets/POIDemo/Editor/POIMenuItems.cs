using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace POI.Demo
{
    // Adds GameObject > UI > POI UI, which drops the demo POI UI prefab, a Screen Space - Overlay canvas, into the scene.
    static class POIMenuItems
    {
        const string k_PrefabGuid = "a958d1ea905de0345949e3994e4ab09f";

        [MenuItem("GameObject/UI/POI UI", false, 2101)]
        static void CreatePOIUI(MenuCommand command)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(k_PrefabGuid));
            if (prefab == null)
            {
                Debug.LogError("The POI UI prefab could not be found. Was it deleted?");
                return;
            }

            // The prefab is a canvas of its own, so it goes at the root unless a parent is selected.
            var parent = command.context as GameObject;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent != null ? parent.transform : null);
            if (parent == null)
                StageUtility.PlaceGameObjectInCurrentStage(instance);

            GameObjectUtility.EnsureUniqueNameForSibling(instance);
            Undo.RegisterCreatedObjectUndo(instance, "Create POI UI");
            Selection.activeGameObject = instance;
        }
    }
}
