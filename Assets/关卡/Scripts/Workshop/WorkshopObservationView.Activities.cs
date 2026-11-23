using UnityEngine;

internal sealed partial class WorkshopObservationView
{
    private static readonly int[] CocoonReviewSlots = { 1, 0, 1 };
    private const int LeafReadyToFeed = 3;
    private const int DryDyeSelected = 4;
    private int cocoonRound;
    private int cocoonWrongSlot = -1;
    private bool loomWrong;
    private bool driedWetLeaf;
    private bool dyeWrong;

    partial void ResetChallengePlayState()
    {
        cocoonRound = 0;
        cocoonWrongSlot = -1;
        loomWrong = false;
        driedWetLeaf = false;
        dyeWrong = false;
    }

    private void DrawActivity()
    {
        if (!readOnly && DrawAdditionalSeasonGame()) return;

        switch (observation)
        {
            case WorkshopObservation.QingmingMulberry: DrawQingming(); break;
            case WorkshopObservation.GuyuLeaf: DrawGuyu(); break;
            case WorkshopObservation.MangzhongCocoon: DrawMangzhong(); break;
            case WorkshopObservation.DashuVentilation: DrawVentilation(); break;
            case WorkshopObservation.DashuReeling: DrawReeling(); break;
            case WorkshopObservation.QiufenOffering: DrawQiufen(); break;
            case WorkshopObservation.HanluLoom: DrawLoom(); break;
            case WorkshopObservation.LidongDye: DrawDye(); break;
            case WorkshopObservation.XiaoxueTailoring: DrawXiaoxue(); break;
        }
    }

    private void DrawQingming()
    {
        Art("Mulberry Garden", "WorkshopGuides/Spring_ChapterGuide", ArtBounds, true);
        var young = At(0.29f, 0.63f, 0.27f, 0.43f);
        var mature = At(0.71f, 0.63f, 0.27f, 0.43f);
        WorkshopUi.PixelCard(art, "Young Leaf Sample Paper", young, LightSurface, WorkshopUi.Bronze);
        WorkshopUi.PixelCard(art, "Mature Leaf Sample Paper", mature, LightSurface, WorkshopUi.Bronze);
        Prop("Spring_QingmingYoungMulberry", young);
        Prop("Spring_QingmingMatureMulberry", mature);
        if (stage > 0)
        {
            Tag("Young Leaf Finding", "嫩梢已展开的叶片", At(0.29f, 0.88f, 0.29f, 0.08f));
            Tag("Mature Leaf Finding", "成熟叶脉更粗", At(0.71f, 0.88f, 0.26f, 0.08f), stage == 1);
        }

        if (stage == 2)
        {
            Copy("先看蚕龄，再选叶", "这片嫩梢上的叶子已经展开，叶面柔嫩，适合这批小蚕。长大后的蚕会用不同成熟度的桑叶。");
            Restart();
            return;
        }

        Copy(stage == 1 ? "再看看芽尖和叶脉" : "哪片适合这批小蚕？",
            stage == 1
                ? "右边叶片已长大，叶脉较粗。找一片已经展开、叶面柔嫩的桑叶，不需要掐掉芽尖。"
                : "左、右都是桑叶。观察芽尖、叶面和叶脉，选一片已经展开、叶质柔嫩的给小蚕。", stage == 1);
        Hotspot("Choose Young Leaf", young, Finish);
        Hotspot("Choose Mature Leaf", mature, () => Step(1));
        Act("Select Young Leaf", "选左边的叶片", Finish, true);
        Act("Select Mature Leaf", "选右边的叶片", () => Step(1));
    }

    private void DrawGuyu()
    {
        Layer("Spring_LeafDesk");
        var dry = At(0.34f, 0.48f, 0.16f, 0.30f);
        var wet = At(0.66f, 0.48f, 0.16f, 0.30f);
        var tray = At(0.50f, 0.80f, 0.25f, 0.39f);

        if (stage == 2)
        {
            Prop("Spring_LeafOnTray", tray);
            Copy("给桑完成", "给小蚕选洁净、适熟的桑叶。雨后的湿叶先晾干，蚕座也要保持干爽。");
            Restart();
            return;
        }

        if (stage == LeafReadyToFeed)
        {
            Prop("Spring_LeafDry", driedWetLeaf ? wet : dry);
            WorkshopUi.Picture(art, "Silkworms Ready To Feed", tray, WorkshopObservationArt.Current?.SilkwormTray);
            Tag("Prepared Leaf Caption", driedWetLeaf ? "已晾干" : "叶面干爽",
                At(driedWetLeaf ? 0.66f : 0.34f, 0.69f, 0.16f, 0.073f));
            Copy("把桑叶放进蚕匾", driedWetLeaf
                ? "水珠已经晾干。点击下方的蚕匾，把这片桑叶喂给小蚕。"
                : "这片叶面没有水珠。点击下方的蚕匾，把选好的桑叶喂给小蚕。");
            Hotspot("Feed Prepared Leaf", tray, Finish);
            Act("Feed Dry Leaf", "把叶子放进蚕匾", Finish, true);
            Act("Choose Other Leaf", "重新选一片", () => Step(0));
            return;
        }

        Prop("Spring_LeafDry", dry);
        Prop("Spring_LeafWet", wet);
        Hotspot("Choose Dry Leaf", dry, () => PrepareLeaf(false));
        Hotspot("Choose Wet Leaf", wet, () => Step(1));
        if (stage == 1)
        {
            Tag("Wet Leaf Finding", "叶上有水珠", At(0.66f, 0.71f, 0.22f, 0.08f), true);
            Copy("湿叶先晾干", "这片桑叶还有水珠。可等叶面晾干再给桑，也可以回头选那片干爽的桑叶。", true);
            Act("Air Dry The Leaf", "晾干后再用", () => PrepareLeaf(true), true);
            Act("Choose Other Leaf", "重新选一片", () => Step(0));
            return;
        }

        Copy("先用哪一片？", "雨刚停，竹盘里有同批的两片桑叶。看一看叶面，选出现在可以给小蚕的那片。");
        Act("Select Left Leaf", "选左边的桑叶", () => PrepareLeaf(false));
        Act("Select Right Leaf", "选右边的桑叶", () => Step(1));
    }

    private void PrepareLeaf(bool airDried)
    {
        driedWetLeaf = airDried;
        Step(LeafReadyToFeed);
    }

    private void DrawMangzhong()
    {
        if (showBatches)
        {
            DrawSummerBatches();
            return;
        }

        Layer("Summer_CocoonBench");
        bool completed = stage == 2;
        DrawCocoonReviewTray(completed ? CocoonReviewSlots.Length : cocoonRound);

        if (completed)
        {
            Copy("三盘都已分拣", "有破口的茧已放入复查栏。南浔六月初的地方报道也记录了同期进度不同的两批蚕，可接着看。");
            Act("Compare Nanxun Batches", "看南浔的两批蚕", ToggleSummerBatches, true);
            Restart();
            return;
        }

        bool finalPlate = cocoonRound == CocoonReviewSlots.Length - 1;
        float[] centers = finalPlate ? new[] { 0.17f, 0.335f, 0.50f } : new[] { 0.236f, 0.439f };
        string[] positions = finalPlate ? new[] { "Left", "Middle", "Right" } : new[] { "Left", "Right" };
        string[] labels = finalPlate ? new[] { "左边", "中间", "右边" } : new[] { "左边", "右边" };
        for (int i = 0; i < centers.Length; i++)
        {
            int slot = i;
            string sample = slot == CocoonReviewSlots[cocoonRound] ? "Summer_CocoonReview" : "Summer_CocoonIntact";
            var bounds = finalPlate ? At(centers[i], 0.746f, 0.135f, 0.18f) : At(centers[i], 0.746f, 0.156f, 0.208f);
            Prop(sample, bounds);
            if (slot == cocoonWrongSlot)
                Tag("Inspected Intact Shell", "表面完整", At(centers[i], 0.57f, 0.15f, 0.073f), true);
            Hotspot("Choose " + positions[i] + " Cocoon", bounds, () => InspectCocoon(slot));
            Act("Select " + positions[i] + " Cocoon", "复查" + labels[i] + "的茧", () => InspectCocoon(slot));
        }

        bool incorrect = cocoonWrongSlot >= 0;
        WorkshopUi.Label(infoPanel, "Cocoon Round Progress", new Rect(27, 567, 444, 61),
            $"第 {cocoonRound + 1} / {CocoonReviewSlots.Length} 盘", 28, WorkshopUi.Faded);
        Copy(incorrect ? "这枚茧壳完整" : "哪枚茧需要复查？",
            incorrect
                ? "刚才这枚没有可见破口，留在原盘。再看其他茧的边缘和表面，挑一枚有破口的。"
                : "每盘有一枚茧壳带破口。把它挑进复查栏，再继续检查下一盘。", incorrect);
    }

    private void InspectCocoon(int slot)
    {
        if (slot != CocoonReviewSlots[cocoonRound])
        {
            cocoonWrongSlot = slot;
            Step(1);
            return;
        }

        cocoonWrongSlot = -1;
        cocoonRound++;
        if (cocoonRound == CocoonReviewSlots.Length)
            Finish();
        else
            Step(0);
    }

    private void DrawCocoonReviewTray(int count)
    {
        var panel = WorkshopUi.PixelCard(art, "Cocoon Review Tray", new Rect(792, 436, 333, 180),
            LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(panel, "Cocoons Held For Review", new Rect(10, 9, 313, 49),
            $"待复查 {count} / {CocoonReviewSlots.Length}", 29, WorkshopUi.Green, TextAnchor.MiddleCenter);
        for (int i = 0; i < CocoonReviewSlots.Length; i++)
        {
            float x = 14f + i * 103f;
            WorkshopUi.PixelCard(panel, "Review Slot " + i, new Rect(x, 65, 94, 91), InsetPaper, WorkshopUi.Line);
            if (i < count)
                Prop("Summer_CocoonReview", new Rect(792 + x + 9, 436 + 72, 76, 67));
        }
    }

    private void DrawSummerBatches()
    {
        Layer("Summer_CocoonBench");
        var first = new Rect(89, 111, 445, 410);
        var second = new Rect(625, 111, 445, 410);
        WorkshopUi.PixelCard(art, "Early Batch Frame", first, LightSurface, WorkshopUi.Bronze);
        WorkshopUi.PixelCard(art, "Later Batch Frame", second, LightSurface, WorkshopUi.Bronze);
        Art("First Batch Cocoons", "WorkshopFinish/Cocoon", new Rect(184, 169, 255, 260));
        Prop("Spring_LeafOnTray", new Rect(691, 173, 313, 246));
        Tag("Early Batch Caption", "第一批 · 吐丝结茧", new Rect(118, 454, 387, 50));
        Tag("Later Batch Caption", "第二批 · 开始养殖", new Rect(654, 454, 387, 50));
        Copy("同在南浔，进度各有不同", "六月初的报道记录了当地具体蚕农的批次。两张画面示意同期的两种进度。");
        Act("Return To Cocoon Comparison", "返回茧况观察", ToggleSummerBatches, true);
        Act("Read Nanxun Reporting", "阅读南浔地方蚕讯", () => Application.OpenURL(
            "https://paper.xinmin.cn/html/home/2024-06/05/06/22494.html"));
    }

    private void ToggleSummerBatches()
    {
        showBatches = !showBatches;
        Render();
    }

    private void DrawVentilation()
    {
        Layer("Summer_CocoonBench");
        Prop("Summer_CocoonIntact", At(0.236f, 0.746f, 0.156f, 0.208f));
        Prop("Summer_CocoonReview", At(0.439f, 0.746f, 0.156f, 0.208f));
        if (stage == 2)
        {
            Layer("Summer_OpenWindow");
            Tag("Ventilation State", "窗已打开", At(0.77f, 0.49f, 0.19f, 0.08f));
            Copy("先透气，再看茧", "窗扇打开，空气可以流通。暑热时还要按现场的闷湿情况检查工位和这批备茧。");
            Restart();
        }
        else
        {
            Tag("Window State", "窗扇关闭", At(0.77f, 0.49f, 0.19f, 0.08f));
            Copy("作坊有些闷热", "观察蔟房上方的窗扇。打开这扇窗，再看看空气流动的示意。");
            Hotspot("Open Cocoon Room Window", At(0.771f, 0.304f, 0.258f, 0.331f), Finish);
            Act("Open Window", "打开木窗", Finish, true);
        }

        if (!readOnly || HasRecord(WorkshopObservation.DashuReeling))
            Act("Return To Reeling", "回看缫丝三步", () => Link(WorkshopObservation.DashuReeling));
    }

    private void DrawReeling()
    {
        var strip = new Rect(21, 150, 1118, 291.15f);
        Art("Reeling Steps", "WorkshopObservation/Summer_ReelingSteps", strip, true);
        if (stage >= 2)
            Art("Reeling Thread Path", "WorkshopObservation/Summer_ReelingThreadPath", strip, true);
        var steps = new[] { "煮茧", "找丝头", "绕丝轴" };
        float tileWidth = strip.width / 3f;
        for (int i = 0; i < steps.Length; i++)
        {
            var tile = new Rect(strip.x + i * tileWidth, strip.y, tileWidth, strip.height);
            int step = i;
            if (stage == step)
                Hotspot("Reeling Step " + step, tile, AdvanceReeling);
            Tag("Reeling Step Label " + step, steps[i] + (stage > i ? " · 已看" : ""),
                new Rect(tile.x + 74f, 472, tileWidth - 148f, 57));
        }

        switch (stage)
        {
            case 0:
                Copy("先看煮茧", "锅里的水让茧丝较易分离。先点左格看煮过的茧，准备寻找丝头。");
                break;
            case 1:
                Copy("找出丝头", "用索绪工具从煮过的茧上找出丝头。点中格，观察丝头从蚕茧引向丝车。");
                break;
            case 2:
                Copy("看丝线绕上木轴", "沿亮色丝线看向右格。整理并合添丝绪后，丝车稳稳卷绕，把蚕丝收在轴上。");
                break;
            default:
                Copy("一枚茧经过多道工序", "三格示意选取煮茧、索绪与绕轴三个动作。辑里湖丝的实际工序还包括添绪、烘丝等步骤。");
                Act("Read Hand Reeling Practice", "阅读辑里湖丝工艺", () => Application.OpenURL(
                    "https://www.ihchina.cn/project_details/14472/"), true);
                if (!readOnly || HasRecord(WorkshopObservation.DashuVentilation))
                    Act("Go To Summer Ventilation", "看蔟房通风", () => Link(WorkshopObservation.DashuVentilation));
                Restart();
                return;
        }
        Act("Advance Reeling", steps[stage], AdvanceReeling, true);
        if (stage > 0)
            Act("Previous Reeling Step", "返回上一步", () => Step(stage - 1));
        else if (!readOnly)
            Act("Go To Summer Ventilation", "看蔟房通风", () => Link(WorkshopObservation.DashuVentilation));
    }

    private void AdvanceReeling()
    {
        if (stage == 2)
            Finish();
        else
            Step(stage + 1);
    }

    private void DrawQiufen()
    {
        Texture("Offering Table and Tools", "WorkshopFinish/AutumnOfferingTable", ArtBounds);
        Art("Raw Silk From Summer", "WorkshopFinish/RawSilk", At(0.85f, 0.79f, 0.12f, 0.22f));
        Tag("Previous Season Material", "上一章 · 生丝", At(0.84f, 0.92f, 0.19f, 0.073f));
        if (stage == 2)
        {
            Copy("记下织坊的致谢", "这段织坊故事回顾采桑、养蚕和缫丝一路的劳动。供台和原有酬蚕任务照常，秋分观察已可在四时织录复看。");
            if (!WorkshopHeritageBrief.IsOpen)
                Act("Read Autumn Heritage", "看苏州织造资料", OpenAutumnHeritage, true);
            Restart();
            return;
        }

        Copy("读供台旁的说明", "织布前，学徒在供台边回想上一章收下的生丝，也向照料桑蚕、煮茧缫丝的人致谢。画面是织坊故事。");
        Hotspot("Read Thanks At Table", At(0.5f, 0.5f, 0.73f, 0.64f), Finish);
        Act("Read Offering Note", "读完并记入织录", Finish, true);
    }

    private void DrawLoom()
    {
        if (showSongJin)
        {
            Art("Song Brocade Diagram", "WorkshopObservation/Autumn_SongJinPattern", new Rect(72, 40, 1016, 571));
            Copy("苏州宋锦 · 显花示意", "概念图以纵向的经线和横向的彩纬表示共同显花。纱线依工艺有不同层次，画面右侧是便于读图的示意花纹。");
            Act("Return To Plain Weave", "返回基础平纹", ToggleSongJin, true);
            Act("Read Song Brocade Practice", "阅读宋锦织造技艺", () => Application.OpenURL(
                "https://www.ihchina.cn/project_details/14278/"));
            return;
        }

        Layer("Autumn_LoomCloseup");
        Layer(stage == 0 ? "Autumn_WarpLoose" : "Autumn_WarpAligned");
        if (stage == 2)
        {
            Layer("Autumn_PlainWeave");
            Copy("经线纵向，纬线横穿", "纬线从排好的经线之间横向穿过。局部小图展示素绸的基础平纹，两组线一上一下地交织。");
            Act("Look At Song Brocade", "看宋锦显花示意", ToggleSongJin, true);
            Restart();
            return;
        }

        if (stage == 0)
        {
            if (loomWrong)
                Tag("Slack Warp Blocks The Shuttle", "松线挡住了梭路", At(0.5f, 0.67f, 0.27f, 0.085f), true);
            Copy(loomWrong ? "梭路被松线挡住" : "这会儿能直接送梭吗？",
                loomWrong
                    ? "几根经线明显松垂，梭子会被挡住。先调整经线，把这几根理齐后再试。"
                    : "看看织机中央，线列里有几根经线中段弯了。决定是先送梭，还是先整理经线。", loomWrong);
            Hotspot("Align Loose Warp Hotspot", At(0.50f, 0.49f, 0.49f, 0.32f), AlignLoomWarp);
            Act("Send Shuttle Before Alignment", "现在就送梭", WrongLoomChoice);
            Act("Align Warp", "先理齐经线", AlignLoomWarp);
        }
        else
        {
            if (loomWrong)
                Tag("Warp Direction Feedback", "顺着经线无法交织", At(0.5f, 0.67f, 0.30f, 0.085f), true);
            Copy(loomWrong ? "顺着走，织不成布" : "梭子该怎么穿？",
                loomWrong
                    ? "纵向是已有的经线。要让新的丝线和它交错，换个方向再送一趟梭子。"
                    : "线列已经排齐。观察直直的经线，再选一个方向，把新的一根纬线带过去。", loomWrong);
            var across = At(0.34f, 0.89f, 0.25f, 0.085f);
            var along = At(0.66f, 0.89f, 0.25f, 0.085f);
            Tag("Transverse Shuttle Direction", "横穿线列", across);
            Tag("Longitudinal Shuttle Direction", "顺着线列", along);
            Hotspot("Send Weft Across Warp", across, Finish);
            Hotspot("Send Weft Along Warp", along, WrongLoomChoice);
            Act("Preview Weft", "横着穿过经线", Finish);
            Act("Send Shuttle Along Warp", "顺着经线纵向送梭", WrongLoomChoice);
            Act("See Loose Warp Again", "返回松线状态", () =>
            {
                loomWrong = false;
                Step(0);
            });
        }
    }

    private void WrongLoomChoice()
    {
        loomWrong = true;
        Render();
    }

    private void AlignLoomWarp()
    {
        loomWrong = false;
        Step(1);
    }

    private void ToggleSongJin()
    {
        showSongJin = !showSongJin;
        Render();
    }

    private void DrawDye()
    {
        Layer("Winter_DyeSampleDesk");
        var dry = At(0.271f, 0.648f, 0.163f, 0.196f);
        var damp = At(0.482f, 0.648f, 0.163f, 0.196f);
        var swatch = At(0.746f, 0.628f, 0.163f, 0.211f);
        Prop("Winter_DyeSampleDry", dry);
        Prop("Winter_DyeSampleDamp", damp);
        Prop(stage == 3 ? "Winter_DyedSwatch" : "Winter_PlainSwatch", swatch);

        if (stage >= 2)
            Tag("Damp Sample Condition", "有水迹 · 分开检查", At(0.48f, 0.83f, 0.23f, 0.075f));

        if (stage == 3)
        {
            Tag("Swatch Colour State", "试染后的布头", At(0.75f, 0.82f, 0.22f, 0.075f));
            Copy("先用小块布看颜色", "布头原位显示试出的颜色。进正式染布工序时，仍要从实际备好的材料中选色。");
            Restart();
            return;
        }

        if (stage == DryDyeSelected)
        {
            Tag("Dry Packet Chosen For Trial", "干爽样品", At(0.27f, 0.83f, 0.19f, 0.075f));
            Copy(dyeWrong ? "整幅布先别下染" : "在哪里先试色？", dyeWrong
                ? "样色还没确认。桌上有一小块素绸布头，先拿它试出颜色再决定。"
                : "干爽样品已选好。可以直接染整幅布，也可以先用右边的一小块素绸查看颜色。", dyeWrong);
            Hotspot("Try Dye On Lower Swatch", swatch, Finish);
            Act("Dye The Whole Piece", "直接染整幅布", WrongDyeChoice);
            Act("Test Cloth Colour", "先在小布头试色", Finish);
            return;
        }

        if (stage == 2)
        {
            Copy(dyeWrong ? "这包染材有水迹" : "用哪包染材试色？", dyeWrong
                ? "这包已经另放复查。先从另一包外观干爽的染材取样，再试颜色。"
                : "右包有水迹，已经分开放。现在选择一份可以用来试色的演示染材。", dyeWrong);
            Hotspot("Use Dry Packet", dry, () => SetDyeStage(DryDyeSelected));
            Hotspot("Use Damp Packet", damp, WrongDyeChoice);
            Act("Select Dry Dye For Trial", "用左包染材试色", () => SetDyeStage(DryDyeSelected));
            Act("Use Damp Dye For Sample", "用右包染材试色", WrongDyeChoice);
            Act("Compare Dye Packets Again", "重新看两包染材", () => SetDyeStage(0));
            return;
        }

        if (stage == 1)
            Tag("Dry Sample Condition", "布面干爽", At(0.27f, 0.83f, 0.19f, 0.075f), true);
        Copy(stage == 1 ? "这包外观干爽" : "哪包染材要另放检查？",
            stage == 1
                ? "左包布面没有可见的水迹。看看另一包的深浅变化，再选需要分开检查的那包。"
                : "桌上放着两包演示染材。仔细比较包布，找出有受潮痕迹的一包。", stage == 1);
        Hotspot("Choose Dry Dye For Review", dry, () => Step(1));
        Hotspot("Choose Damp Dye For Review", damp, () => SetDyeStage(2));
        Act("Review Left Dye", "查看左边的染材", () => Step(1));
        Act("Review Right Dye", "查看右边的染材", () => SetDyeStage(2));
    }

    private void SetDyeStage(int value)
    {
        dyeWrong = false;
        Step(value);
    }

    private void WrongDyeChoice()
    {
        dyeWrong = true;
        Render();
    }

    private void DrawXiaoxue()
    {
        Art("Winter Workshop", "WorkshopGuides/Winter_ChapterGuide", ArtBounds, true);
        var labels = new[] { "染布", "裁片", "成衣" };
        for (int i = 0; i < labels.Length; i++)
        {
            var tile = new Rect(48 + i * 365f, 260, 334, 299);
            WorkshopUi.PixelCard(art, "Winter Process Card " + i, tile, LightSurface, WorkshopUi.Bronze);
            var inside = new Rect(tile.x + 42, tile.y + 24, 250, 200);
            if (i == 0)
                Prop("Winter_DyedSwatch", inside);
            else if (i == 1)
                CutPieceIcon(inside);
            else
                WorkshopUi.Picture(art, "Winter Garment", inside,
                    WorkshopArt.Garment(WorkshopProgress.GarmentId) ??
                    WorkshopArt.Garment("1_01") ?? WorkshopArt.ChapterProduct(WorkshopSeason.Winter));
            Tag("Winter Process Label " + i, labels[i] + (stage > i ? " · 已看" : ""),
                new Rect(tile.x + 54f, tile.y + 232, 226, 50));
            if (i == stage)
                Hotspot("Winter Process " + i, tile, AdvanceXiaoxue);
        }

        switch (stage)
        {
            case 0:
                Copy("从染好的布开始", "小雪是节气。冬日工位里，先染好布，才能顺着衣样下剪。");
                break;
            case 1:
                Copy("按衣样裁出衣片", "原裁衣台会按衣样裁出前、后、袖片。观察里先认清这些衣片，再带着它们回主厅缝制。");
                break;
            case 2:
                Copy("把衣片缝起来", "沿领襟、衣身和袖子拼合缝制，一块织染好的布慢慢成了衣裳。");
                break;
            default:
                Copy("织坊里的冬章工序", "这条记录串起染布、裁片和缝衣。完成正式成衣后，可在主厅成衣展架试穿和继续回看。");
                if (WorkshopProgress.IsComplete(WorkshopSeason.Winter) && !WorkshopCraftRecordView.IsOpen)
                    Act("Open Winter Chapter Record", "查看冬章制作记录", OpenWinterChapterRecord, true);
                Restart();
                return;
        }
        Act("Advance Winter Process", "查看" + labels[stage], AdvanceXiaoxue, true);
        if (stage > 0)
            Act("Previous Winter Process", "返回上一步", () => Step(stage - 1));
    }

    private void CutPieceIcon(Rect bounds)
    {
        var tint = new Color32(240, 177, 99, 255);
        WorkshopUi.Picture(art, "Front Piece", new Rect(bounds.x, bounds.y + 17, 89, 157), WorkshopArt.CutPiece(0), tint);
        WorkshopUi.Picture(art, "Back Piece", new Rect(bounds.x + 76, bounds.y + 17, 89, 157), WorkshopArt.CutPiece(1), tint);
        WorkshopUi.Picture(art, "Sleeve Piece", new Rect(bounds.x + 161, bounds.y + 80, 89, 96), WorkshopArt.CutPiece(2), tint);
    }

    private void AdvanceXiaoxue()
    {
        if (stage == 2)
            Finish();
        else
            Step(stage + 1);
    }

    private void OpenWinterChapterRecord()
    {
        Close();
        WorkshopCraftRecordView.ShowChapterRecord(WorkshopSeason.Winter);
    }

    private void OpenAutumnHeritage()
    {
        Close();
        WorkshopHeritageBrief.ShowSeason(WorkshopSeason.Autumn);
    }
}
