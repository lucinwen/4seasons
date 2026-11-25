using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameWorkflowMaintenance
{
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
