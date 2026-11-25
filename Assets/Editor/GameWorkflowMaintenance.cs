using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameWorkflowMaintenance
{
    public static void FinalizeSpringCleanup()
    {
        const string path = "Assets/关卡/1-春/Level1_Spring.unity";
        var scene = EditorSceneManager.OpenScene(path);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "FeedOverlayManager") continue;
            if (root.transform.childCount != 0 || root.GetComponents<Component>().Length != 1)
                throw new Exception("Retired manager contains unexpected content; inspect before removal.");
            UnityEngine.Object.DestroyImmediate(root);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Spring cleanup could not save.");
            Debug.Log("[WorkflowCleanup] Removed empty retired spring manager.");
        }
    }

    public static void ValidateScenes()
    {
        int checkedScenes = 0;
        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled) continue;
            var scene = EditorSceneManager.OpenScene(entry.path);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour == null) throw new Exception("Missing script in " + entry.path);
            checkedScenes++;
        }
        Debug.Log("[WorkflowVerify] Scene integrity PASS: " + checkedScenes + " build scenes, no missing scripts.");
    }

}
