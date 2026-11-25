using UnityEngine;

internal sealed partial class WorkshopObservationView
{
    private const int DryDyeSelected = 4;
    private bool dyeWrong;

    private void DrawActivity()
    {
        if (!readOnly && DrawAdditionalSeasonGame()) return;

        switch (observation)
        {
            case WorkshopObservation.MangzhongCocoon: DrawMangzhong(); break;
            case WorkshopObservation.MangzhongCocoonHarvest: DrawCocoonHarvestRecord(); break;
            case WorkshopObservation.DashuVentilation: DrawVentilation(); break;
            case WorkshopObservation.QiufenOffering: DrawQiufen(); break;
            case WorkshopObservation.LidongDye: DrawDye(); break;
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

    private void OpenAutumnHeritage()
    {
        Close();
        WorkshopHeritageBrief.ShowSeason(WorkshopSeason.Autumn);
    }
}
