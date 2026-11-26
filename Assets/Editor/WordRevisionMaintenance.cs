using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WordRevisionMaintenance
{
    public static void FinalizeAutumnIntegration()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var autumn = EditorSceneManager.OpenScene("Assets/关卡/3-秋/Level3_Autumn.unity");
        if (!EditorSceneManager.SaveScene(autumn)) throw new Exception("Could not round-trip the imported autumn scene.");
        Debug.Log("[AutumnIntegration] Imported autumn scene round-tripped through Unity serialization.");
        Validate();
    }

    public static void PrepareArtAndValidate()
    {
        const string folder = "Assets/Resources/WordRevision/";
        foreach (string name in new[] { "StatueCard", "StatueIcon" })
        {
            string path = folder + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        const string atlasPath = folder + "TaskIcons.png";
        AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
        var atlasImporter = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
        atlasImporter.filterMode = FilterMode.Point;
        atlasImporter.alphaIsTransparency = true;
        atlasImporter.textureCompression = TextureImporterCompression.Uncompressed;
        atlasImporter.SaveAndReimport();
        var art = Resources.Load<WorkshopArtData>("WorkshopArt");
        art.itemCards[(int)WorkshopItem.Statue] = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "StatueCard.png");
        art.taskIconAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        var autumn = Resources.Load<AutumnItemIcons>("AutumnItemIcons");
        autumn.statue = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "StatueIcon.png");
        EditorUtility.SetDirty(art);
        EditorUtility.SetDirty(autumn);
        AssetDatabase.SaveAssets();
        AlignAutumnDyeStations();
        Validate();
    }

    private static void AlignAutumnDyeStations()
    {
        var scene = EditorSceneManager.OpenScene("Assets/关卡/3-秋/Level3_Autumn.unity");
        string[] props = { "4-挖掘染料锄头", "4-研磨棒", "4-染料空", "4-染料空缸" };
        foreach (string name in props)
        {
            var part = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(item => item.name == name);
            var sprite = part.GetComponent<SpriteRenderer>();
            if (sprite == null) throw new Exception("Dye station has no sprite: " + name);
            part.position += new Vector3(0, -2.5f - sprite.bounds.min.y, 0);
        }
        var garden = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(item => item.name == "染料园触点");
        garden.position = new Vector3(garden.position.x, -2.5f, garden.position.z);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save autumn station alignment.");
    }

    public static void Validate()
    {
        foreach (WorkshopSeason season in Enum.GetValues(typeof(WorkshopSeason)))
        {
            var frame = Resources.Load<Sprite>("WorkshopChapterFrames/Blank/" + season + "_Frame");
            if (frame == null) throw new Exception("Chapter frame is missing or not imported as a sprite: " + season);
        }
        WorkshopWordChecks.Verify();
        WorkshopRewardChecks.Verify();
        GameWorkflowMaintenance.ValidateScenes();
        var autumn = EditorSceneManager.OpenScene("Assets/关卡/3-秋/Level3_Autumn.unity");
        foreach (string name in new[] { "染料园触点", "4-挖掘染料锄头", "4-研磨棒", "4-染料空", "4-染料空缸" })
            if (!autumn.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Any(part => part.name == name && part.gameObject.activeSelf))
                throw new Exception("Autumn workstation missing or inactive: " + name);
        Debug.Log("[WordRevision] VALIDATE PASS (no player build).");
    }

    public static void Install()
    {
        const string autumnPath = "Assets/关卡/3-秋/Level3_Autumn.unity";
        const string winterPath = "Assets/关卡/4-冬/Level4_Winter.unity";
        var winter = EditorSceneManager.OpenScene(winterPath);
        var autumn = EditorSceneManager.OpenScene(autumnPath, OpenSceneMode.Additive);
        string[] names = { "染料园触点", "4-挖掘染料锄头", "4-研磨棒", "4-染料空", "4-染料空缸" };
        Vector3[] positions = {
            new Vector3(5.8f, -3.4f, 0), new Vector3(4.7f, -3.45f, 0),
            new Vector3(7.5f, -3.45f, 0), new Vector3(9.1f, -3.55f, 0), new Vector3(11.1f, -3.4f, 0)
        };
        for (int i = 0; i < names.Length; i++)
        {
            var source = winter.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(part => part.name == names[i]);
            if (source == null) throw new Exception("Missing migration source " + names[i]);
            var existing = autumn.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(part => part.name == names[i]);
            if (existing == null)
            {
                var copy = UnityEngine.Object.Instantiate(source.gameObject);
                copy.name = names[i];
                copy.transform.SetParent(null, true);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, autumn);
                copy.transform.position = positions[i];
                copy.SetActive(true);
                foreach (var col in copy.GetComponents<Collider2D>()) col.isTrigger = true;
                if (i != 0 && copy.GetComponent<Collider2D>() == null) copy.AddComponent<BoxCollider2D>().isTrigger = true;
            }
            // Preserve the old objects and all GUID links as inactive scene references.
            source.gameObject.SetActive(false);
        }
        if (!EditorSceneManager.SaveScene(autumn) || !EditorSceneManager.SaveScene(winter))
            throw new Exception("Could not save migrated seasonal stations.");
        Debug.Log("[WordRevision] Installed autumn dye stations; winter now contains tailoring only.");
    }

    public static void ValidateAndBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode before building.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Validate();
        string[] arguments = Environment.GetCommandLineArgs();
        int outputOption = Array.IndexOf(arguments, "-wordDemoOutput");
        string output = Path.GetFullPath(outputOption >= 0 && outputOption + 1 < arguments.Length
            ? arguments[outputOption + 1] : "../Outputs/WordSimplified_20261126");
        Directory.CreateDirectory(output);
        string company = PlayerSettings.companyName, product = PlayerSettings.productName;
        try
        {
            PlayerSettings.companyName = "Jiangnan Studio QA";
            PlayerSettings.productName = "四时织坊_Word简化试玩";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(output, "四时织坊_Word简化试玩.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            File.WriteAllText("../WordRevision/build-result.json", "{\"result\":\"" + report.summary.result + "\",\"errors\":" + report.summary.totalErrors + ",\"warnings\":" + report.summary.totalWarnings + "}");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed");
            Debug.Log("[WordRevision] BUILD PASS " + output);
        }
        finally { PlayerSettings.companyName = company; PlayerSettings.productName = product; AssetDatabase.SaveAssets(); }
    }

    public static void Inspect()
    {
        string[] paths = {
            "Assets/关卡/1-春/Level1_Spring.unity",
            "Assets/关卡/3-秋/Level3_Autumn.unity",
            "Assets/关卡/4-冬/Level4_Winter.unity"
        };
        var lines = new System.Collections.Generic.List<string>();
        foreach (string path in paths)
        {
            var scene = EditorSceneManager.OpenScene(path);
            lines.Add(path);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var part in root.GetComponentsInChildren<Transform>(true))
                {
                    var sr = part.GetComponent<SpriteRenderer>();
                    if (sr == null && part.name != "Player") continue;
                    lines.Add(part.name + " | " + part.position + " | " + (sr != null ? sr.bounds.ToString() : "player"));
                }
        }
        File.WriteAllLines("../WordRevision/scene-inspection.txt", lines);
        Debug.Log("[WordRevision] Inspection complete.");
    }
}
