using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ShiXu.CocoonMatch.Editor.CocoonMatchSceneBuilder;

namespace ShiXu.CocoonMatch.Editor
{
    // Applies only to the standalone CocoonMatch scene and its own copied assets.
    public static class CocoonMatchPresentation
    {
        public static readonly Color Ink = new Color32(43, 26, 13, 255);
        public static readonly Color SoftInk = new Color32(64, 39, 22, 255);
        public const string Introduction = "蚕茧已经上簇5天，是时候收茧了。点击收集三枚相同碎片，即可获得。";
        public const string SolarIntroduction = "公历六月五至六日，夏季的第三个节气。正值芒作物成熟，是采收蚕茧的好时节。";

        [MenuItem("Tools/时序小织匠/应用芒种文案与开始弹窗")]
        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("请在已保存的蔟上收茧场景、编辑模式下应用界面修改。");
            var controller = UnityEngine.Object.FindObjectOfType<CocoonMatchController>();
            if (controller == null) throw new InvalidOperationException("小游戏控制器缺失。");
            var root = (RectTransform)controller.transform;
            var surface = (RectTransform)controller.boardRoot.parent;
            var font = controller.remainingText.font;
            var level = controller.level;
            var summer = PrepareSummerPanel();

            string difficulty = CocoonMatchDealGenerator.Generate(level);
            AssignHarvestMessages(level);
            Hide(root, "Series", "Chapter", "Footer", "TealBackdrop", "QuietPaperGlow", "Feedback");
            Hide(surface, "Title", "Subtitle", "TitleDivider", "Rules", "CapacityHint", "TrayCount");
            if (controller.feedbackText != null) { controller.feedbackText.text = ""; controller.feedbackText.gameObject.SetActive(false); }
            foreach (var slot in controller.slots) Hide(slot, "Position");

            // A transparent outer canvas lets a later environment scene show through.
            // Keep the user's ornate game popup itself and the card/tray frames.
            var workshop = root.Find("WorkshopVisuals") as RectTransform;
            if (workshop == null)
            {
                workshop = Rect("WorkshopVisuals", root, Vector2.zero, new Vector2(1600, 1000));
                workshop.SetAsFirstSibling();
                root.Find("PanelShadow").SetParent(workshop, false);
                root.Find("CocoonOrnatePanel").SetParent(workshop, false);
                surface.SetParent(workshop, false);
            }
            controller.gameSurface = workshop.gameObject;
            var camera = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
            if (camera != null) camera.backgroundColor = Color.clear;

            var solar = surface.Find("SolarTerm").GetComponent<Text>();
            solar.text = "芒种";
            solar.fontSize = 27;
            solar.rectTransform.anchoredPosition = new Vector2(-434, 70);
            // At 1600x1000 the text starts at x=301, safely inside the gold line (~278).
            // Deliberate line breaks keep phrases legible inside this narrow side column.
            var introduction = GetLabel("SolarIntroduction", surface, font,
                "公历六月五至\n六日，夏季的\n第三个节气。\n正值芒作物\n成熟，是采收\n蚕茧的好时节。",
                new Vector2(-434, -59), new Vector2(130, 216), 18, SoftInk);
            introduction.alignment = TextAnchor.UpperLeft;
            introduction.lineSpacing = 1.3f;

            controller.boardRoot.anchoredPosition = new Vector2(0, -37);
            controller.boardRoot.sizeDelta = new Vector2(760, 310);
            var tray = (RectTransform)surface.Find("CollectionTray");
            tray.anchoredPosition = new Vector2(0, -246);
            tray.sizeDelta = new Vector2(596, 100);
            controller.remainingText.rectTransform.anchoredPosition = new Vector2(475, 55);
            controller.clearedText.rectTransform.anchoredPosition = new Vector2(475, -47);
            ((RectTransform)controller.restartButton.transform).anchoredPosition = new Vector2(475, -143);
            controller.remainingText.text = "场上剩余\n<size=34>54</size>";
            controller.clearedText.text = "已收好\n<size=27>00 / 54</size>";

            controller.harvestText = GetLabel("HarvestNotice", surface, font, "",
                new Vector2(0, 146), new Vector2(1020, 38), 22, Ink);
            controller.harvestText.horizontalOverflow = HorizontalWrapMode.Overflow;
            controller.harvestText.resizeTextForBestFit = true;
            controller.harvestText.resizeTextMinSize = 20;
            controller.harvestText.resizeTextMaxSize = 22;
            controller.harvestText.transform.SetAsLastSibling();
            controller.harvestGroup = controller.harvestText.GetComponent<CanvasGroup>();
            if (controller.harvestGroup == null) controller.harvestGroup = controller.harvestText.gameObject.AddComponent<CanvasGroup>();
            controller.harvestGroup.blocksRaycasts = false;
            controller.harvestGroup.interactable = false;
            controller.harvestGroup.alpha = 0;

            if (root.Find("StartOverlay") == null)
            {
                var overlay = Modal("StartOverlay", root);
                var card = Panel("StartCard", overlay.transform, summer, new Vector2(640, 360));
                Label("StartTitle", card.transform, font, "蔟上收茧", new Vector2(0, 105), new Vector2(500, 62), 40, Ink);
                var body = Label("StartIntroduction", card.transform, font, Introduction,
                    new Vector2(0, 7), new Vector2(500, 115), 23, SoftInk);
                body.lineSpacing = 1.4f;
                controller.startButton = Button("StartButton", card.transform, font, "开始游戏", new Vector2(0, -105), new Vector2(230, 56));
                var buttonLabel = controller.startButton.GetComponentInChildren<Text>();
                buttonLabel.rectTransform.anchoredPosition = new Vector2(22, 0);
                buttonLabel.rectTransform.sizeDelta = new Vector2(160, 50);
                var icon = Image("CocoonIcon", controller.startButton.transform, level.patterns[0].sprite, new Vector2(-74, 0), new Vector2(36, 36));
                icon.preserveAspect = true;
                controller.startPanel = overlay.gameObject;
            }
            else
            {
                controller.startPanel = root.Find("StartOverlay").gameObject;
                controller.startButton = controller.startPanel.GetComponentInChildren<Button>(true);
            }
            if (root.Find("ClosedOverlay") == null)
            {
                var overlay = Modal("ClosedOverlay", root);
                var card = Panel("ClosedCard", overlay.transform, summer, new Vector2(580, 300));
                Label("ClosedTitle", card.transform, font, "已离开拾茧草堂", new Vector2(0, 65), new Vector2(510, 60), 34, Ink);
                controller.returnButton = Button("ReturnButton", card.transform, font, "返回拾茧草堂", new Vector2(0, -55), new Vector2(250, 56));
                controller.closedPanel = overlay.gameObject;
            }
            else
            {
                controller.closedPanel = root.Find("ClosedOverlay").gameObject;
                controller.returnButton = controller.closedPanel.GetComponentInChildren<Button>(true);
            }
            controller.resultRestartButton.GetComponentInChildren<Text>(true).text = "离开拾茧草堂";
            ((RectTransform)controller.resultRestartButton.transform).sizeDelta = new Vector2(250, 56);
            controller.resultRestartButton.GetComponentInChildren<Text>(true).rectTransform.sizeDelta = new Vector2(234, 52);
            controller.closedPanel.transform.Find("ClosedCard/ClosedTitle").GetComponent<Text>().text = "已离开拾茧草堂";
            controller.returnButton.GetComponentInChildren<Text>(true).text = "返回拾茧草堂";
            var resultCard = controller.resultTitle.transform.parent.GetComponent<Image>();
            resultCard.sprite = summer;
            resultCard.type = UnityEngine.UI.Image.Type.Sliced;

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                text.color = text.name.Contains("Title") || text.name == "SolarTerm" || text.name == "Label" || text == controller.remainingText ? Ink : SoftInk;
                text.fontStyle = FontStyle.Normal;
                // Preserve the pixel font's native stroke weight without synthetic bold or outlines.
                var stroke = text.GetComponent<Outline>();
                if (stroke != null) stroke.enabled = false;
            }
            controller.harvestText.color = Ink;
            var model = new CocoonMatchModel(level);
            for (int i = 0; i < controller.tiles.Count; i++)
            {
                var tile = controller.tiles[i];
                tile.Rect.anchoredPosition = level.placements[i].position;
                tile.Rect.sizeDelta = Vector2.one * level.tileSize;
                tile.Configure(i, level.patterns[level.placements[i].pattern], null);
                tile.SetState(model.IsExposed(i), false, true);
                EditorUtility.SetDirty(tile);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile.icon);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile.button);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile.Rect);
            }
            controller.startPanel.SetActive(true);
            controller.resultPanel.SetActive(false);
            controller.closedPanel.SetActive(false);
            controller.trayTileSize = 72;
            EditorUtility.SetDirty(level);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("保存小游戏场景失败。");
            return difficulty + ". " + Inspect();
        }

        private static Sprite PrepareSummerPanel()
        {
            string path = Root + "/Art/UI/SummerPanel.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("缺少独立复制的夏季弹窗图片。");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(40, 40, 40, 40);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AssignHarvestMessages(CocoonMatchLevel level)
        {
            // Stable sprite identity, independent of pattern-array order or deal order.
            foreach (var pattern in level.patterns)
            {
                switch (pattern.sprite.name)
                {
                    case "WhiteCocoon": pattern.harvestMessage = "获得白色蚕茧，常见家养蚕茧，便于染色织布"; break;
                    case "YellowCocoon": pattern.harvestMessage = "获得稀有金色蚕茧~天然含金黄素，自带抑菌效果"; break;
                    case "PinkCocoon": pattern.harvestMessage = "获得粉色蚕茧，含桑叶类胡萝卜素，色彩柔和"; break;
                    case "OrangeCocoon": pattern.harvestMessage = "获得橙色蚕茧，天然彩蚕，色素多存与外层丝胶之中"; break;
                    case "GreenCocoon": pattern.harvestMessage = "获得珍稀青色蚕茧~色素深入丝素，耐煮不易褪色"; break;
                    case "SilkComb": pattern.harvestMessage = "获得收茧梳，轻轻使用避免戳破茧壳！"; break;
                    case "SilkwormTray": pattern.harvestMessage = "收获茧匾，摊开蚕茧摆放便于风干"; break;
                    case "MulberryBasket": pattern.harvestMessage = "收获竹篓，传统采茧容器，透气保险~"; break;
                    case "CocoonTwigs": pattern.harvestMessage = "收获收茧枝架，轻取蚕茧，保留完整茧壳"; break;
                    default: throw new InvalidOperationException("图案未配置收获文案：" + pattern.sprite.name);
                }
            }
        }

        private static Image Modal(string name, Transform root)
        {
            var overlay = Image(name, root, null, Vector2.zero, new Vector2(6000, 6000));
            overlay.color = new Color(.12f, .08f, .04f, .48f);
            overlay.raycastTarget = true;
            return overlay;
        }

        private static Image Panel(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            var panel = Image(name, parent, sprite, Vector2.zero, size);
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            panel.raycastTarget = true;
            return panel;
        }

        private static Text GetLabel(string name, Transform parent, Font font, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var child = parent.Find(name);
            if (child == null) return Label(name, parent, font, value, position, size, fontSize, color);
            var text = child.GetComponent<Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            text.color = color;
            return text;
        }

        private static void Hide(Transform parent, params string[] names)
        {
            foreach (var name in names)
            {
                var child = parent.Find(name);
                if (child == null) continue;
                var text = child.GetComponent<Text>();
                if (text != null) text.text = "";
                child.gameObject.SetActive(false);
            }
        }

        public static string Inspect()
        {
            var c = UnityEngine.Object.FindObjectOfType<CocoonMatchController>();
            if (c.startButton == null || c.startPanel == null || c.closedPanel == null || c.returnButton == null || c.harvestText == null || c.harvestGroup == null || c.gameSurface == null)
                throw new InvalidOperationException("开始、收获提示或退出界面存在缺失引用。");
            if (c.level.patterns.Any(p => string.IsNullOrWhiteSpace(p.harvestMessage) || p.harvestMessage.Contains("（") || p.harvestMessage.Contains("）")))
                throw new InvalidOperationException("图案文案缺失或含有不应显示的括号。");
            string characters = Introduction + SolarIntroduction + string.Concat(c.level.patterns.Select(p => p.harvestMessage)) + "芒种开始游戏离开拾茧草堂已返回";
            string missing = new string(characters.Distinct().Where(ch => !c.remainingText.font.HasCharacter(ch)).ToArray());
            if (missing.Length > 0) throw new InvalidOperationException("像素字体缺字：" + missing);
            return "Start/exit bindings and all 9 harvest messages OK; glyphs OK. " + CocoonMatchRefinement.Inspect();
        }
    }
}
