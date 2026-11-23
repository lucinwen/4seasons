using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShiXu.CocoonMatch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShiXu.CocoonMatch.Editor
{
    public static class CocoonMatchSceneBuilder
    {
        public const string Root = "Assets/关卡/2-夏/2-合成蚕茧";
        public const string ScenePath = Root + "/Scenes/CocoonMatch.unity";
        private static readonly Color Ink = new Color32(43, 26, 13, 255);
        private static readonly Color SoftInk = new Color32(76, 50, 32, 255);
        private static readonly Color Gold = new Color(.88f, .74f, .45f);

        [MenuItem("Tools/时序小织匠/创建蔟上收茧场景（仅首次）")]
        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请在编辑模式创建场景。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("请先保存当前场景，再创建小游戏。");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("小游戏场景已存在，请直接打开；创建器不会覆盖已有场景。");

            PrepareImporters();
            var font = WorkshopArt.PixelFont;
            if (font == null) throw new InvalidOperationException("没有找到提供的像素字体。");
            var level = MakeLevel();
            level.ValidateData();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CocoonMatch";
            var camera = new GameObject("CocoonCamera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            var cameraComponent = camera.GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(.055f, .13f, .12f);

            var canvasObject = new GameObject("CocoonCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            new GameObject("CocoonEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var controller = canvasObject.AddComponent<CocoonMatchController>();
            controller.level = level;

            var root = (RectTransform)canvasObject.transform;
            var backdrop = Image("TealBackdrop", root, null, Vector2.zero, new Vector2(6000, 6000));
            backdrop.color = new Color(.065f, .15f, .135f);
            var glow = Image("QuietPaperGlow", root, null, new Vector2(0, -20), new Vector2(1300, 730));
            glow.color = new Color(.19f, .27f, .20f, .25f);
            Label("Series", root, font, "二十四节气 · 蚕织记", new Vector2(-525, 463), new Vector2(390, 35), 20, Gold);
            Label("Chapter", root, font, "小满篇  /  休闲小戏", new Vector2(525, 463), new Vector2(390, 35), 18, Gold);

            var panelSprite = SpriteAt("UI/CocoonPanel.png");
            var shadow = Image("PanelShadow", root, panelSprite, new Vector2(0, -10), new Vector2(1516, 889));
            shadow.color = new Color(0, 0, 0, .30f);
            Image("CocoonOrnatePanel", root, panelSprite, Vector2.zero, new Vector2(1500, 880));
            var surface = Rect("GameSurface", root, Vector2.zero, new Vector2(1200, 560));
            Label("Title", surface, font, "蔟上收茧", new Vector2(0, 170), new Vector2(450, 60), 44, Ink);
            Label("Subtitle", surface, font, "层层寻茧 · 三枚成锦", new Vector2(0, 128), new Vector2(530, 30), 18, SoftInk);
            var divider = Image("TitleDivider", surface, null, new Vector2(0, 108), new Vector2(470, 1));
            divider.color = new Color(.56f, .48f, .28f, .22f);

            Label("SolarTerm", surface, font, "小满 · 蚕事", new Vector2(-450, 79), new Vector2(170, 40), 21, Ink);
            Label("Rules", surface, font, "先收上层\n\n三枚同类\n自动消除", new Vector2(-450, -10), new Vector2(160, 140), 19, SoftInk);
            Label("CapacityHint", surface, font, "七格暂存\n留意余位", new Vector2(-450, -130), new Vector2(160, 65), 17, SoftInk);
            controller.remainingText = Label("Remaining", surface, font, "场上剩余\n<size=34>54</size>", new Vector2(450, 55), new Vector2(170, 95), 20, Ink);
            controller.clearedText = Label("Cleared", surface, font, "已收好\n<size=27>00 / 54</size>", new Vector2(450, -47), new Vector2(180, 90), 18, SoftInk);
            controller.restartButton = Button("RestartButton", surface, font, "重新开始", new Vector2(450, -143), new Vector2(152, 43));

            controller.boardRoot = Rect("LayeredBoard", surface, new Vector2(0, -30), new Vector2(650, 285));
            var traySprite = SpriteAt("UI/CollectionTray.png");
            var tray = Image("CollectionTray", surface, traySprite, new Vector2(0, -222), new Vector2(596, 106));
            tray.type = UnityEngine.UI.Image.Type.Sliced;
            controller.slots = new RectTransform[CocoonMatchModel.Capacity];
            controller.slotHighlights = new Image[CocoonMatchModel.Capacity];
            for (int i = 0; i < CocoonMatchModel.Capacity; i++)
            {
                var slot = Image("Slot_" + (i + 1), tray.transform, SpriteAt("UI/TileFrame.png"), new Vector2((i - 3) * 76, 0), Vector2.one * 72);
                slot.color = new Color(.54f, .58f, .4f, .22f);
                controller.slots[i] = (RectTransform)slot.transform;
                controller.slotHighlights[i] = slot;
                Label("Position", slot.transform, font, (i + 1).ToString(), Vector2.zero, Vector2.one * 30, 16, new Color(.3f, .4f, .3f, .32f));
            }
            controller.trayText = Label("TrayCount", surface, font, "收集槽  0 / 7", new Vector2(-450, -222), new Vector2(170, 38), 19, Ink);
            controller.feedbackText = Label("Feedback", surface, font, "点击亮起的图块，收集三个相同图案", new Vector2(0, -155), new Vector2(620, 25), 16, SoftInk);
            controller.animationRoot = Rect("MovingTiles", surface, Vector2.zero, new Vector2(1200, 560));

            var template = MakeTile(controller.boardRoot, level.tileSize);
            var prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, Root + "/Prefabs/CocoonTile.prefab");
            UnityEngine.Object.DestroyImmediate(template.gameObject);
            var model = new CocoonMatchModel(level);
            for (int i = 0; i < level.placements.Count; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, controller.boardRoot);
                var view = go.GetComponent<CocoonTileView>();
                view.Rect.anchoredPosition = level.placements[i].position;
                view.Configure(i, level.patterns[level.placements[i].pattern], null);
                view.SetState(model.IsExposed(i), false, false);
                controller.tiles.Add(view);
            }
            CreateResultPanel(root, controller, font);
            Label("Footer", root, font, "收尽春蚕茧，织就四时衣", new Vector2(0, -465), new Vector2(900, 38), 19, new Color(.69f, .73f, .58f));

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("小游戏场景保存失败。");
            Selection.activeGameObject = canvasObject;
            return CocoonMatchPresentation.Apply();
        }

        private static void PrepareImporters()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = path.Contains("/Items/") ? 256 : 2048;
                importer.spriteBorder = path.EndsWith("CollectionTray.png") ? new Vector4(24, 24, 24, 24) : Vector4.zero;
                importer.SaveAndReimport();
            }
        }

        private static CocoonMatchLevel MakeLevel()
        {
            var data = ScriptableObject.CreateInstance<CocoonMatchLevel>();
            string[] files = { "WhiteCocoon", "YellowCocoon", "OrangeCocoon", "PinkCocoon", "GreenCocoon", "SilkComb", "CocoonTwigs", "MulberryBasket", "SilkwormTray" };
            string[] labels = { "雪白蚕茧", "鹅黄蚕茧", "暖橘蚕茧", "浅粉蚕茧", "青绿蚕茧", "木制丝梳", "结茧秸秆", "采桑篮", "蚕匾" };
            data.patterns = files.Select((file, i) => new CocoonPattern { label = labels[i], sprite = SpriteAt("Items/" + file + ".png") }).ToArray();
            CocoonMatchDealGenerator.Generate(data);
            AssetDatabase.CreateAsset(data, Root + "/Data/CocoonMatchLevel.asset");
            return data;
        }

        private static CocoonTileView MakeTile(Transform parent, float size)
        {
            var bg = Image("CocoonTile", parent, SpriteAt("UI/TileFrame.png"), Vector2.zero, Vector2.one * size);
            bg.raycastTarget = true;
            var outline = bg.gameObject.AddComponent<Shadow>();
            outline.effectColor = new Color(.13f, .22f, .16f, .35f);
            outline.effectDistance = new Vector2(0, -3);
            var button = bg.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.highlightedColor = new Color(1, .97f, .82f);
            colors.pressedColor = new Color(.84f, .92f, .72f);
            colors.disabledColor = Color.white;
            colors.fadeDuration = .06f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var icon = Image("Pattern", bg.transform, null, Vector2.zero, Vector2.one * (size - 15));
            var iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(6, 6);
            iconRect.offsetMax = new Vector2(-6, -6);
            icon.preserveAspect = true;
            var view = bg.gameObject.AddComponent<CocoonTileView>();
            view.frame = bg;
            view.icon = icon;
            view.button = button;
            view.group = bg.gameObject.AddComponent<CanvasGroup>();
            return view;
        }

        private static void CreateResultPanel(RectTransform root, CocoonMatchController controller, Font font)
        {
            var dimmer = Image("ResultOverlay", root, null, Vector2.zero, new Vector2(6000, 6000));
            dimmer.color = new Color(.03f, .10f, .085f, .82f);
            dimmer.raycastTarget = true;
            var panel = Image("ResultCard", dimmer.transform, SpriteAt("UI/CollectionTray.png"), Vector2.zero, new Vector2(620, 350));
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            controller.resultTitle = Label("ResultTitle", panel.transform, font, "收茧圆满", new Vector2(0, 92), new Vector2(540, 65), 39, Ink);
            controller.resultMessage = Label("ResultMessage", panel.transform, font, "", new Vector2(0, 0), new Vector2(550, 100), 23, SoftInk);
            controller.resultRestartButton = Button("ResultRestart", panel.transform, font, "再收一回", new Vector2(0, -108), new Vector2(225, 52));
            controller.resultPanel = dimmer.gameObject;
            dimmer.gameObject.SetActive(false);
        }

        internal static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static Image Image(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        internal static Text Label(string name, Transform parent, Font font, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.lineSpacing = 1.15f;
            return text;
        }

        internal static Button Button(string name, Transform parent, Font font, string label, Vector2 position, Vector2 size)
        {
            var image = Image(name, parent, SpriteAt("UI/CollectionTray.png"), position, size);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.91f, .98f, .81f);
            colors.pressedColor = new Color(.73f, .86f, .68f);
            button.colors = colors;
            Label("Label", image.transform, font, label, Vector2.zero, size - new Vector2(16, 4), 21, Ink);
            return button;
        }

        private static Sprite SpriteAt(string relative) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/" + relative);

        [MenuItem("Tools/时序小织匠/检查蔟上收茧配置")]
        public static string Inspect()
        {
            var data = AssetDatabase.LoadAssetAtPath<CocoonMatchLevel>(Root + "/Data/CocoonMatchLevel.asset");
            data.ValidateData();
            var controller = UnityEngine.Object.FindObjectOfType<CocoonMatchController>();
            if (controller == null || controller.level != data || controller.slots.Length != 7 || controller.tiles.Count != 54)
                throw new InvalidOperationException("场景配置数量不正确。");
            if (controller.tiles.Any(t => t == null || t.icon == null || t.icon.sprite == null || t.button == null || t.group == null))
                throw new InvalidOperationException("图块有缺失引用。");
            if (controller.slots.Any(s => s == null) || controller.slotHighlights.Any(s => s == null) ||
                controller.restartButton == null || controller.resultRestartButton == null || controller.resultPanel == null)
                throw new InvalidOperationException("收集槽或重开面板有缺失引用。");
            var font = controller.remainingText.font;
            string uiCharacters = "蔟上收茧二十四节气蚕织记小满篇休闲戏层寻三枚成锦先同类自动消除七格暂存留意余位重新开始场剩已好点击亮起图块个相案正在一样会排成功圆槽春结寸丝全部与工具都用尽请试着凑齐再回0123456789";
            string missing = new string(uiCharacters.Distinct().Where(c => !font.HasCharacter(c)).ToArray());
            if (missing.Length > 0) throw new InvalidOperationException("提供字体缺少界面文字：" + missing);
            var model = new CocoonMatchModel(data);
            int exposed = Enumerable.Range(0, 54).Count(model.IsExposed);
            foreach (int id in data.solution)
                if (!model.TryCollect(id, out _)) throw new InvalidOperationException("预生成的通关序列无效。");
            if (model.State != CocoonGameState.Won) throw new InvalidOperationException("通关序列未能清空牌面。");
            return $"Configuration OK: {data.placements.Count} tiles, {data.patterns.Length} patterns x 6, 3 layers, {exposed} initially exposed, 7 slots, all UI glyphs present, legal solution clears all 54. Scene: {ScenePath}";
        }
    }
}
