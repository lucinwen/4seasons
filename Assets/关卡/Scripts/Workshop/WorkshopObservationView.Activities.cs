using UnityEngine;

internal sealed partial class WorkshopObservationView
{
    private const int DryDyeSelected = 4;
    private bool loomWrong;
    private bool dyeWrong;

    private void DrawActivity()
    {
        if (!readOnly && DrawAdditionalSeasonGame()) return;

        switch (observation)
        {
            case WorkshopObservation.QingmingMulberry: DrawQingming(); break;
            case WorkshopObservation.MangzhongCocoon: DrawMangzhong(); break;
            case WorkshopObservation.DashuVentilation: DrawVentilation(); break;
            case WorkshopObservation.QiufenOffering: DrawQiufen(); break;
            case WorkshopObservation.HanluLoom: DrawLoom(); break;
            case WorkshopObservation.LidongDye: DrawDye(); break;
            case WorkshopObservation.XiaoxueTailoring: DrawXiaoxue(); break;
            case WorkshopObservation.DaxueSewing:
            case WorkshopObservation.DongzhiFitting:
                DrawGarmentArchive();
                break;
        }
    }

    private void DrawGarmentArchive()
    {
        bool sewing = observation == WorkshopObservation.DaxueSewing;
        Layer("Winter_DyeSampleDesk");
        Art("Archived Garment Workshop", sewing ? "WorkshopGameArt/DaxueSewingTable" : "WorkshopGameArt/DongzhiFittingRoom", ArtBounds, true);
        var frame = WorkshopUi.WorkbenchCard(art, "Garment Folio", new Rect(177, 53, 806, 544), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Picture(frame, "Finished Silk Garment", new Rect(50, 45, 374, 452),
            WorkshopArt.Garment(WorkshopProgress.SeasonalGarmentId));
        var work = OrderWork(observation) ?? BestWork(observation, 1) ?? BestWork(observation, 0);
        WorkshopUi.Label(frame, "Garment Result", new Rect(420, 156, 330, 116), work?.description ?? "主厅成衣工位", 29, WorkshopUi.Green);
        DrawWorkSample(frame, new Rect(444, 282, 264, 175), work?.sample);
        Copy(sewing ? "夹衣缝样" : "冬至验衣签", sewing
            ? "小雪裁片和工坊素里绢合好，留口翻正，补针后送去试衣。"
            : "看叠穿后的袖根余量和衣襟。合襟、抬臂都顺利，才交成衣。");
        if (!HasRecord(observation)) return;
        Act("Replay Garment Workbench", "玩挑战单", () =>
        {
            readOnly = false;
            PrepareChallenge(1);
            Render();
        }, true);
    }

    private void DrawQingming()
    {
        Art("Mulberry Garden", "WorkshopGuides/Spring_ChapterGuide", ArtBounds, true);
        var young = At(0.29f, 0.59f, 0.27f, 0.43f);
        var damaged = At(0.71f, 0.59f, 0.27f, 0.43f);
        WorkshopUi.PixelCard(art, "Young Leaf Sample Paper", young, LightSurface, WorkshopUi.Bronze);
        WorkshopUi.PixelCard(art, "Damaged Leaf Sample Paper", damaged, LightSurface, WorkshopUi.Bronze);
        Prop("Spring_QingmingYoungMulberry", young);
        Prop("Spring_QingmingDamagedMulberry", damaged);
        Tag("Young Leaf Finding", "嫩叶收进篮", At(0.29f, 0.84f, 0.26f, 0.08f));
        Tag("Damaged Leaf Finding", "黄褐破叶跳过", At(0.71f, 0.84f, 0.26f, 0.08f));
        Copy("清明移篮接桑", "左右移动篮子接住嫩叶。桑叶交给师傅照料小蚕，再为下一批备好蚕种。");
    }

    private void DrawMangzhong()
    {
        Layer("Summer_CocoonBench");
        var work = WorkshopUpstreamWork(WorkshopObservation.MangzhongCocoon, 0);
        DrawWorkSample(art, new Rect(180, 210, 800, 230), work?.sample);
        Copy("芒种 · 丝路已接通", "在蚕茧簇架转动线段，把丝头引到丝轴。丝样已保存，秋季可继续借光验丝。");
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

        if (!readOnly || HasRecord(WorkshopObservation.MangzhongCocoon))
            Act("Return To Thread Record", "回看芒种丝路", () => Link(WorkshopObservation.MangzhongCocoon));
    }

    private void DrawQiufen()
    {
        Texture("Offering Table and Tools", "WorkshopFinish/AutumnOfferingTable", ArtBounds);
        var panel = WorkshopUi.PixelCard(art, "Accepted Silk Card", new Rect(184, 381, 792, 142),
            LightSurface, WorkshopUi.Bronze);
        var work = WorkshopUpstreamWork(WorkshopObservation.QiufenOffering, 0);
        DrawWorkSample(panel, new Rect(24, 13, 744, 115), work?.sample);
        Copy("秋分借光验丝", "白昼渐短，织坊在夜织前借光查接头。验过的生丝收进柜子，留给寒露理经走梭。");
        Act("Read Autumn Heritage", "看苏州织造资料", OpenAutumnHeritage, true);
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
