using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShiXu.CocoonMatch.Editor
{
    public static class CocoonMatchRefinement
    {
        [MenuItem("Tools/时序小织匠/应用54张对称进阶布局")]
        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != CocoonMatchSceneBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("请在已保存的蔟上收茧场景、编辑模式中应用调整。");
            var controller = UnityEngine.Object.FindObjectOfType<CocoonMatchController>();
            var level = controller.level;
            string backup = "CocoonMatchSupport/BeforeSymmetricRefinement";
            Directory.CreateDirectory(backup);
            if (!File.Exists(backup + "/CocoonMatch.unity")) File.Copy(scene.path, backup + "/CocoonMatch.unity");
            if (!File.Exists(backup + "/CocoonMatchLevel.asset")) File.Copy(AssetDatabase.GetAssetPath(level), backup + "/CocoonMatchLevel.asset");
            float previousGreedy = CocoonMatchDealGenerator.EstimateGreedyWinRate(level, 128);
            string difficulty = CocoonMatchDealGenerator.Generate(level);
            // Change our prefab in place and preserve its GUID and serialized references.
            string prefabPath = CocoonMatchSceneBuilder.Root + "/Prefabs/CocoonTile.prefab";
            var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view = prefabContents.GetComponent<CocoonTileView>();
                view.Rect.sizeDelta = Vector2.one * level.tileSize;
                FitIcon((RectTransform)view.icon.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefabContents); }

            controller.boardRoot.anchoredPosition = new Vector2(0, -37);
            controller.boardRoot.sizeDelta = new Vector2(760, 310);
            var surface = controller.boardRoot.parent;
            ((RectTransform)surface.Find("CollectionTray")).anchoredPosition = new Vector2(0, -246);
            ((RectTransform)surface.Find("CollectionTray")).sizeDelta = new Vector2(596, 100);
            ((RectTransform)surface.Find("TrayCount")).anchoredPosition = new Vector2(-470, -242);
            ((RectTransform)surface.Find("SolarTerm")).anchoredPosition = new Vector2(-475, 79);
            ((RectTransform)surface.Find("Rules")).anchoredPosition = new Vector2(-475, -10);
            ((RectTransform)surface.Find("CapacityHint")).anchoredPosition = new Vector2(-475, -130);
            controller.remainingText.rectTransform.anchoredPosition = new Vector2(475, 55);
            controller.clearedText.rectTransform.anchoredPosition = new Vector2(475, -47);
            ((RectTransform)controller.restartButton.transform).anchoredPosition = new Vector2(475, -143);
            controller.feedbackText.rectTransform.SetParent(surface.parent, false);
            controller.feedbackText.rectTransform.anchoredPosition = new Vector2(0, -465);
            controller.feedbackText.rectTransform.sizeDelta = new Vector2(950, 32);
            controller.feedbackText.fontSize = 19;
            controller.feedbackText.color = new Color(.85f, .82f, .63f);
            controller.feedbackText.text = "进阶局 · 先观察下层，再决定收取顺序";
            surface.parent.Find("Footer").gameObject.SetActive(false);
            var model = new CocoonMatchModel(level);
            for (int i = 0; i < controller.tiles.Count; i++)
            {
                var tile = controller.tiles[i];
                tile.Rect.anchoredPosition = level.placements[i].position;
                tile.Rect.sizeDelta = Vector2.one * level.tileSize;
                FitIcon((RectTransform)tile.icon.transform);
                tile.Configure(i, level.patterns[level.placements[i].pattern], null);
                tile.SetState(model.IsExposed(i), false, false);
                EditorUtility.SetDirty(tile);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile.Rect);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile.icon);
            }
            controller.remainingText.text = "场上剩余\n<size=34>54</size>";
            controller.clearedText.text = "已收好\n<size=27>00 / 54</size>";
            controller.trayTileSize = 72;
            var title = surface.Find("Title").GetComponent<Text>();
            title.text = "蔟上收茧";
            surface.Find("Subtitle").GetComponent<Text>().text = "层层寻茧 · 进阶挑战";
            EditorUtility.SetDirty(level);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            return $"Previous greedy baseline: {previousGreedy:P1}. {difficulty}. " + Inspect();
        }

        public static string Inspect()
        {
            var controller = UnityEngine.Object.FindObjectOfType<CocoonMatchController>();
            var data = controller.level;
            foreach (var tile in data.placements)
                if (!data.placements.Any(other => other.layer == tile.layer &&
                    Mathf.Abs(other.position.x + tile.position.x) < .01f && Mathf.Abs(other.position.y - tile.position.y) < .01f))
                    throw new InvalidOperationException("存在没有镜像对应位置的图块。");
            for (int i = 0; i < data.placements.Count; i++)
                for (int j = i + 1; j < data.placements.Count; j++)
                    if (data.placements[i].layer == data.placements[j].layer && CocoonMatchModel.Overlaps(data, i, j))
                        throw new InvalidOperationException("同层图块不应重叠。");
            return "Mirror symmetry and same-layer spacing OK. " + CocoonMatchSceneBuilder.Inspect();
        }

        private static void FitIcon(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6, 6);
            rect.offsetMax = new Vector2(-6, -6);
        }
    }
}
