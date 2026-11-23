using UnityEngine;

internal sealed partial class WorkshopObservationView
{
    private static readonly string[] OfferingNames = { "蚕花酒", "香蕉果篮", "糕点" };
    private static readonly string[] PieceNames = { "前片", "后片", "袖片" };
    private static readonly int[] ClothTargets = { 2, 0, 1 };
    private static readonly string[] ReelingActions = { "煮茧", "找丝头", "绕丝轴" };
    private static readonly string[] PositionNames = { "左", "中", "右" };

    private bool GameComplete => challengePhase == ChallengePhase.Complete;
    private int GameStep => stage >= 0 ? stage : -stage - 1;

    private bool DrawAdditionalSeasonGame()
    {
        if (!ChallengeIsActive) return false;

        switch (observation)
        {
            case WorkshopObservation.QingmingMulberry:
                DrawSpringLeafGame();
                return true;
            case WorkshopObservation.DashuReeling:
                DrawSummerReelingGame();
                return true;
            case WorkshopObservation.QiufenOffering:
                DrawAutumnOfferingGame();
                return true;
            case WorkshopObservation.XiaoxueTailoring:
                DrawWinterPatternGame();
                return true;
            default:
                return false;
        }
    }

    private void DrawSpringLeafGame()
    {
        Art("Mulberry Garden", "WorkshopGuides/Spring_ChapterGuide", ArtBounds, true);
        if (GameComplete)
        {
            WorkshopUi.PixelCard(art, "Collected Leaf Samples", new Rect(390, 268, 380, 274),
                LightSurface, WorkshopUi.Bronze);
            Prop("Spring_QingmingYoungMulberry", new Rect(438, 302, 180, 179));
            Prop("Spring_QingmingYoungMulberry", new Rect(555, 354, 164, 144));
            Tag("Picking Progress", "嫩叶 2 / 2", new Rect(452, 482, 252, 48));
            Copy("采齐了两片嫩叶", "嫩梢上已有展开的柔嫩叶。给小蚕挑叶时，要结合蚕龄看叶面和叶脉。");
            return;
        }

        int round = GameStep;
        bool youngOnLeft = round == 0;
        for (int slot = 0; slot < 2; slot++)
        {
            bool young = slot == (youngOnLeft ? 0 : 1);
            string option = young ? "Tender" : "Mature";
            var tile = At(slot == 0 ? 0.30f : 0.70f, 0.64f, 0.27f, 0.45f);
            WorkshopUi.PixelCard(art, option + " Mulberry Sample", tile, LightSurface, WorkshopUi.Bronze);
            Prop(young ? "Spring_QingmingYoungMulberry" : "Spring_QingmingMatureMulberry", tile);
            Hotspot("Pick Qingming " + option + " Hotspot", tile, () => PickSpringLeaf(young));
            Act("Pick Qingming " + option, slot == 0 ? "采左边这片" : "采右边这片", () => PickSpringLeaf(young));
        }

        Tag("Picking Progress", $"嫩叶 {round} / 2", At(0.50f, 0.16f, 0.20f, 0.083f));
        Copy(stage < 0 ? "这片的叶脉偏粗" : "找一片已经展开的嫩叶",
            stage < 0 ? "再看看芽尖旁的叶片，找叶面柔嫩、叶脉细的那一片。"
                      : "比较两边的桑叶，替这批小蚕挑两片柔嫩的叶。留住还没展开的芽尖。", stage < 0);
    }

    private void PickSpringLeaf(bool tender)
    {
        if (!tender) Step(-GameStep - 1);
        else if (GameStep == 0) Step(1);
        else Finish();
    }

    private void DrawSummerReelingGame()
    {
        if (!GameComplete && GameStep < 2)
        {
            Layer("Summer_CocoonBench");
            Prop("Summer_CocoonIntact", At(0.236f, 0.746f, 0.156f, 0.208f));
            if (GameStep == 1)
            {
                Layer("Summer_OpenWindow");
                Tag("Reeling Round Progress", "通风完成 · 1 / 4", At(0.50f, 0.12f, 0.25f, 0.08f));
                Copy("木窗打开了", "让空气在工位间流通。接下来到煮茧锅，理顺三步缫丝工序。");
                Act("Dashu Move to Reeling Bench", "前往缫丝台", () => Step(2), true);
                return;
            }

            Tag("Reeling Round Progress", "准备工位 · 0 / 4", At(0.50f, 0.12f, 0.25f, 0.08f));
            Copy(stage < 0 ? "窗扇还关着" : "先给闷热的工位通风",
                stage < 0 ? "这一局先打开蔟房的窗，再去整理煮茧和卷绕的工序。"
                          : "作坊有些闷。找到画面右上方的窗扇，打开后再去煮茧。", stage < 0);
            Hotspot("Dashu Open Window Hotspot", At(0.771f, 0.304f, 0.258f, 0.331f), () => Step(1));
            Act("Dashu Open Window", "打开木窗", () => Step(1), true);
            Act("Dashu Skip Window", "直接去煮茧", () => Step(-1));
            return;
        }

        var strip = new Rect(21, 150, 1118, 291.15f);
        Art("Reeling Steps", "WorkshopObservation/Summer_ReelingSteps", strip, true);
        if (GameComplete || GameStep >= 4)
            Art("Reeling Thread Path", "WorkshopObservation/Summer_ReelingThreadPath", strip, true);
        Tag("Reeling Round Progress", GameComplete ? "完成 4 / 4" : $"窗已打开 · {GameStep - 1} / 4",
            At(0.50f, 0.145f, 0.25f, 0.078f));

        for (int i = 0; i < ReelingActions.Length; i++)
        {
            int option = i;
            float tileWidth = strip.width / ReelingActions.Length;
            var tile = new Rect(strip.x + i * tileWidth, strip.y, tileWidth, strip.height);
            bool done = GameComplete || i < GameStep - 2;
            Tag("Reeling Game Stage " + i, ReelingActions[i] + (done ? " · 完成" : ""),
                new Rect(tile.x + 53, 479, tileWidth - 106, 55));
            if (!GameComplete)
            {
                Hotspot("Dashu Reeling Hotspot " + i, tile, () => ChooseReelingStep(option));
                Act("Dashu Reeling Pick " + i, ReelingActions[i], () => ChooseReelingStep(option));
            }
        }

        if (GameComplete)
        {
            Copy("丝线已经绕上木轴", "工位通风后，你按煮茧、找丝头、绕丝轴的顺序做完了工序。平时还要按实际茧况与工位状态调整。");
            return;
        }

        Copy(stage < 0 ? "这一步还不能做" : "下一步该做什么？",
            stage < 0 ? "先让茧在水中处理好，找到丝头后再沿丝路卷绕。顺序不对可以重选。"
                      : "看看三格图和已完成的步骤，从煮茧、找丝头、绕丝轴里选下一步。", stage < 0);
    }

    private void ChooseReelingStep(int option)
    {
        int step = GameStep;
        if (option != step - 2) Step(-step - 1);
        else if (step == ReelingActions.Length + 1) Finish();
        else Step(step + 1);
    }

    private void DrawAutumnOfferingGame()
    {
        Texture("Offering Table and Tools", "WorkshopFinish/AutumnOfferingTable", ArtBounds);
        var icons = AutumnItemIcons.Load();
        var offerings = new[] { WorkshopObservationArt.Current?.OfferingWine, icons?.banana, icons?.cake };
        int placed = GameComplete ? OfferingNames.Length : GameStep;
        for (int slot = 0; slot < OfferingNames.Length; slot++)
        {
            var seat = new Rect(55 + slot * 368f, 111, 314, 242);
            WorkshopUi.PixelCard(art, "Offering Seat " + slot, seat, LightSurface, WorkshopUi.Bronze);
            if (slot < placed)
                WorkshopUi.Picture(art, "Placed Offering " + slot, new Rect(seat.x + 69, seat.y + 14, 176, 153), offerings[slot]);
            Tag("Offering Seat Label " + slot,
                OfferingNames[slot] + (slot < placed ? " · 已摆好" : slot == placed ? " · 当前空位" : ""),
                new Rect(seat.x + 12, seat.y + 173, 290, 50));
        }

        if (GameComplete)
        {
            Copy("供台摆好了", "织坊故事用这些物件表达对养蚕、缫丝和织造劳动的谢意。");
            return;
        }

        for (int i = 0; i < OfferingNames.Length; i++)
        {
            int option = (i + 1) % OfferingNames.Length;
            var tray = new Rect(100 + i * 365f, 420, 226, 162);
            WorkshopUi.PixelCard(art, "Offering Choice " + option, tray, LightSurface, WorkshopUi.Bronze);
            WorkshopUi.Picture(art, "Offering Option " + option, new Rect(tray.x + 17, tray.y + 14, 192, 136), offerings[option]);
            Hotspot("Qiufen Offering Hotspot " + option, tray, () => PlaceOffering(option));
            Act("Qiufen Offering Pick " + option, "摆上" + OfferingNames[option], () => PlaceOffering(option));
        }

        Copy(stage < 0 ? "这个物件摆错了位置" : $"供位 {placed} / {OfferingNames.Length}",
            $"按织坊的供台示意摆放。当前空位标着「{OfferingNames[placed]}」。从下方挑选相应的物件。", stage < 0);
    }

    private void PlaceOffering(int option)
    {
        int step = GameStep;
        if (option != step) Step(-step - 1);
        else if (step + 1 == OfferingNames.Length) Finish();
        else Step(step + 1);
    }

    private void DrawWinterPatternGame()
    {
        Art("Winter Workshop", "WorkshopGuides/Winter_ChapterGuide", ArtBounds, true);
        if (GameComplete)
        {
            for (int i = 0; i < PieceNames.Length; i++)
                DrawPatternTile(new Rect(67 + i * 363f, 242, 295, 278), i, "Matched Pattern " + i, PieceNames[i]);
            Copy("三片都配上了", "前片、后片和袖片配齐后，就能依衣样缝制。各地工坊会按实际款式处理裁片。");
            return;
        }

        int step = GameStep;
        int target = ClothTargets[step];
        DrawPatternTile(new Rect(428, 55, 304, 255), target, "Target Pattern", "样板");
        for (int slot = 0; slot < PieceNames.Length; slot++)
        {
            int option = (slot + step) % PieceNames.Length;
            var tile = new Rect(80 + slot * 366f, 344, 268, 250);
            DrawPatternTile(tile, option, "Cloth Option " + option, PositionNames[slot]);
            Hotspot("Xiaoxue Pattern Hotspot " + option, tile, () => PickPattern(option));
            Act("Xiaoxue Pattern Pick " + option, "配" + PositionNames[slot] + "边衣片", () => PickPattern(option));
        }
        Copy(stage < 0 ? "这片的轮廓对不上" : $"已配 {step} / {PieceNames.Length}",
            "上方是当前样板。找下方轮廓相同的衣片，三种衣片配齐即可过关。", stage < 0);
    }

    private void DrawPatternTile(Rect bounds, int piece, string name, string caption)
    {
        WorkshopUi.PixelCard(art, name, bounds, LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Picture(art, name + " Sprite", new Rect(bounds.x + 38, bounds.y + 12,
            bounds.width - 76, bounds.height - 75), WorkshopArt.CutPiece(piece), new Color32(216, 159, 103, 255));
        Tag(name + " Label", caption,
            new Rect(bounds.x + 12, bounds.yMax - 53, bounds.width - 24, 43));
    }

    private void PickPattern(int option)
    {
        int step = GameStep;
        if (option != ClothTargets[step]) Step(-step - 1);
        else if (step + 1 == ClothTargets.Length) Finish();
        else Step(step + 1);
    }
}
