using System.Collections;
using JiangnanDye;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private enum DongzhiFittingStep { LiftArm, FindPull, ReleaseSeam, FitClasp, Deliver }

    private static readonly Color DongzhiGreen = new Color32(57, 101, 81, 255);
    private static readonly Color DongzhiDye = new Color32(224, 178, 99, 255);
    private static readonly Color DongzhiLoose = new Color32(221, 202, 151, 255);
    private static readonly Rect DongzhiCoat = new Rect(385, 230, 362, 418);
    private static readonly Rect DongzhiSleeveGrip = new Rect(384, 338, 124, 153);
    private static readonly Rect DongzhiSeamGrip = new Rect(479, 284, 99, 95);
    private static readonly Vector2 DongzhiShoulder = new Vector2(502, 293);
    private static readonly Rect DongzhiCard = new Rect(817, 127, 301, 452);
    private static readonly Vector2 DongzhiChestStrap = new Vector2(546, 344);
    private static readonly Vector2 DongzhiClaspRailStart = new Vector2(595, 361);
    private static readonly Vector2 DongzhiClaspRailEnd = new Vector2(678, 417);
    private static readonly Rect DongzhiClaspArtwork = new Rect(.402f, .156f, .205f, .053f);
    private static readonly Rect DongzhiClaspKnot = new Rect(.548f, .157f, .059f, .051f);
    private const float DongzhiSeamSpacing = 73f;
    private static readonly Vector2 DongzhiSeamPin = new Vector2(71, 140);

    private DongzhiFittingStep fittingStep;
    private int fittingEase;
    private int fittingLetOut;
    private int fittingCorrections;
    private int fittingClasp;
    private int fittingLastClasp;
    private bool fittingGotArmPass;
    private bool fittingInitialCue;
    private bool fittingPlayingPose;
    private bool fittingDraggingArm;
    private float fittingArmPose;
    private float fittingSleeveDragFromY;
    private string fittingNotice;
    private RectTransform fittingSleeve;
    private CanvasGroup fittingPullMarks;
    private RectTransform fittingLiftCue;
    private Image fittingArmFill;
    private Text fittingArmCaption;
    private RectTransform fittingSeamFlap;
    private RectTransform fittingSeamCue;
    private float fittingSeamDragFromX;
    private int fittingSeamDetent;
    private bool fittingDraggingSeam;
    private RectTransform fittingLeftFront;
    private RectTransform fittingRightFront;
    private Image fittingModelLining;
    private CanvasGroup fittingClaspCreases;
    private Text fittingClaspStatus;
    private const int DongzhiClaspSlotCount = 3;
    private const float DongzhiClaspSnapDistance = 26f;
    private RawImage fittingClaspSource;
    private RectTransform fittingClaspGhost;
    private CanvasGroup fittingClaspCue;

    private int FittingTarget => ChallengeVariant == 0 ? 1 : 2;

    // The order's lining is separate from the extra cloth reserved for the facing.
    // A small amount can always be let out at the existing underarm seam.
    private int FittingReleaseAvailable => Mathf.Min(2 - fittingEase,
        WorkshopProgress.GarmentLiningAllowance >= 2 ? 2 : 1);

    // A wider cut has more overlap to use. A thick inner layer consumes that overlap.
    private int FittingMatchingClasp => Mathf.Clamp(FittingTarget - (WorkshopProgress.GarmentSize == 2 ? 1 : 0), 0, 2);

    private void ResetFittingChallenge()
    {
        if (observation != WorkshopObservation.DongzhiFitting) return;
        fittingStep = DongzhiFittingStep.LiftArm;
        fittingEase = Mathf.Clamp(WorkshopProgress.GarmentSleeveAllowance, 0, 2);
        fittingLetOut = fittingCorrections = 0;
        fittingClasp = fittingLastClasp = -1;
        fittingGotArmPass = fittingInitialCue = fittingPlayingPose = fittingDraggingArm = false;
        fittingDraggingSeam = false;
        fittingArmPose = 0f;
        fittingNotice = null;
    }

    private void DrawDongzhiFitting()
    {
        var room = GameArtSprite("DongzhiFittingRoom");
        if (room == null) room = Resources.Load<Sprite>("WorkshopFinish/WardrobeWinterCourtyard");
        WorkshopUi.Picture(art, "Dongzhi Fitting Room", ArtBounds, room, stretch: true);
        WorkshopUi.Fill(art, "Fitting Room Shade", ArtBounds, new Color32(36, 35, 32, 31));
        if (room == null || room.name == "WardrobeWinterCourtyard")
            WorkshopUi.Picture(art, "Dongzhi Fitting Stage", new Rect(322, 47, 480, 594),
                Resources.Load<Sprite>("WorkshopFinish/FittingStage"), stretch: true);

        fittingSleeve = fittingLiftCue = fittingClaspGhost = null;
        fittingClaspCue = null;
        fittingSeamFlap = fittingSeamCue = fittingLeftFront = fittingRightFront = null;
        fittingPullMarks = fittingClaspCreases = null;
        fittingModelLining = null;
        fittingClaspStatus = null;
        fittingClaspSource = null;
        fittingArmFill = null;
        fittingArmCaption = null;
        DrawDongzhiWorkOrder();
        DrawDongzhiModel();
        if (readOnly || GameComplete)
        {
            DrawDongzhiPass();
            Copy("夹衣已验收", "袖子抬得起，前襟合得住。夹衣可在主厅换装。");
            return;
        }

        switch (fittingStep)
        {
            case DongzhiFittingStep.LiftArm: DrawDongzhiLift(); break;
            case DongzhiFittingStep.FindPull: DrawDongzhiFindPull(); break;
            case DongzhiFittingStep.ReleaseSeam: DrawDongzhiRelease(); break;
            case DongzhiFittingStep.FitClasp: DrawDongzhiClasp(); break;
            case DongzhiFittingStep.Deliver: DrawDongzhiPass(); break;
        }
    }

    private void DrawDongzhiWorkOrder()
    {
        var order = WorkshopUi.PixelCard(art, "Dongzhi Work Order", new Rect(53, 132, 262, 418), LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(order, "Dongzhi Work Order Name", new Rect(16, 14, 230, 44), "冬至夹衣", 28,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        WorkshopUi.Label(order, "Dongzhi Layers", new Rect(16, 69, 230, 36), ChallengeVariant == 0 ? "薄内搭" : "厚内搭", 23,
            WorkshopUi.Faded, TextAnchor.MiddleCenter);
        var tile = GameArtSprite("CuttingCloth");
        int layers = ChallengeVariant == 0 ? 1 : 2;
        for (int i = 0; i < layers; i++)
            GameArtSurface(order, "Inner Layer " + i, new Rect(57 + i * 18, 103 + i * 18, 148, 51), tile,
                i == 0 ? DongzhiLoose : new Color32(204, 177, 131, 255));
        string loom = WorkshopUpstreamWork(WorkshopObservation.HanluLoom, ChallengeVariant)?.sample;
        if (DrawWorkSample(order, new Rect(22, 190, 218, 53), loom))
            WorkshopUi.Label(order, "Upstream Woven Pattern", new Rect(14, 246, 234, 31), "寒露纹样", 21,
                WorkshopUi.Faded, TextAnchor.MiddleCenter);
        else
            GameArtSurface(order, "Fallback Fabric Swatch", new Rect(32, 184, 198, 79), tile, DongzhiClothTint());

        if (DrawWorkSample(order, new Rect(26, 298, 210, 73), WorkshopProgress.GarmentCutSample))
            WorkshopUi.Label(order, "Upstream Xiaoxue Cuts", new Rect(17, 374, 228, 29), "小雪裁片", 21,
                WorkshopUi.Faded, TextAnchor.MiddleCenter);
        else
            WorkshopUi.Picture(order, "Fallback Cut Panel", new Rect(66, 285, 134, 99), WorkshopArt.CutPiece(0), DongzhiClothTint());
    }

    private void DrawDongzhiModel()
    {
        var hero = Resources.Load<Sprite>("WorkshopFinish/FittingHero");
        WorkshopUi.Picture(art, "Dongzhi Fitting Guest", new Rect(477, 56, 191, 399), hero);
        var garment = WorkshopArt.Garment("4_02") ?? Resources.Load<Sprite>("WorkshopFinish/SilkVariants/4_02");
        if (garment == null) return;

        // Slice the existing opaque artwork at the sleeve seam. In the neutral position the
        // pieces meet exactly, and the left sleeve can move independently during a fit test.
        var top = new Rect(0, 0, 1, .13f);
        var shoulderBand = new Rect(0, .13f, .325f, .112f);
        var upperBand = new Rect(0, .242f, .322f, .132f);
        var elbowBand = new Rect(0, .374f, .302f, .126f);
        var cuffBand = new Rect(0, .50f, .27f, .123f);
        bool fittingFront = !readOnly && !GameComplete && fittingStep == DongzhiFittingStep.FitClasp;
        if (fittingFront)
        {
            fittingModelLining = GameArtSurface(art, "Lining Visible at Front", new Rect(), GameArtSprite("CuttingCloth"), DongzhiLoose);
        }
        DrawDongzhiCoatSlice(art, "Collar", garment, top, Vector2.zero);
        DrawDongzhiCoatSlice(art, "Hem", garment, new Rect(0, .623f, 1, .377f), Vector2.zero);
        fittingSleeve = WorkshopUi.Rect(art, "Animated Left Sleeve", new Rect(DongzhiShoulder.x, DongzhiShoulder.y, 1, 1));
        if (fittingFront)
        {
            fittingLeftFront = WorkshopUi.Rect(art, "Moving Left Front", new Rect());
            fittingRightFront = WorkshopUi.Rect(art, "Moving Right Front", new Rect());
        }
        DrawDongzhiSleeveBand(garment, shoulderBand, "Shoulder");
        DrawDongzhiSleeveBand(garment, upperBand, "Upper Arm");
        DrawDongzhiSleeveBand(garment, elbowBand, "Elbow");
        DrawDongzhiSleeveBand(garment, cuffBand, "Cuff");
        ApplyDongzhiPose(fittingArmPose);

        if (!readOnly && !GameComplete && fittingStep <= DongzhiFittingStep.ReleaseSeam)
        {
            var folds = WorkshopUi.Rect(art, "Sleeve Tension Lines", new Rect());
            fittingPullMarks = folds.gameObject.AddComponent<CanvasGroup>();
            fittingPullMarks.blocksRaycasts = false;
            for (int i = 0; i < 3; i++)
            {
                var start = new Vector2(505 - i * 13, 309 + i * 15);
                DrawCueStroke(folds, start, start + new Vector2(48 - i * 7, 13 + i * 3), 6f, GuideGold);
            }
            fittingPullMarks.alpha = fittingStep == DongzhiFittingStep.LiftArm ? 0f : 1f;
        }
    }

    private void DrawDongzhiSleeveBand(Sprite garment, Rect band, string part)
    {
        DrawDongzhiCoatSlice(fittingSleeve, "Moving " + part, garment, band, -DongzhiShoulder);
        if (fittingLeftFront == null)
        {
            DrawDongzhiCoatSlice(art, "Garment " + part, garment,
                new Rect(band.width, band.y, 1f - band.width, band.height), Vector2.zero);
            return;
        }
        // Leave the second original cord out while the player positions its loose end.
        const float cordTop = .255f;
        const float cordBottom = .298f;
        if (band.y >= cordBottom || band.yMax <= cordTop)
        {
            DrawDongzhiFrontSlice(garment, band.width, band.y, band.height, part);
            return;
        }
        DrawDongzhiFrontSlice(garment, band.width, band.y, cordTop - band.y, part + " above clasp");
        DrawDongzhiCoatSlice(fittingLeftFront, part + " left of clasp", garment,
            new Rect(band.width, cordTop, .396f - band.width, cordBottom - cordTop), Vector2.zero);
        DrawDongzhiCoatSlice(fittingRightFront, part + " right of clasp", garment,
            new Rect(.612f, cordTop, .388f, cordBottom - cordTop), Vector2.zero);
        DrawDongzhiFrontSlice(garment, band.width, cordBottom, band.yMax - cordBottom, part + " below clasp");
        DrawDongzhiPlacketPatch(fittingLeftFront, garment, new Rect(.394f, cordTop, .106f, cordBottom - cordTop),
            new Rect(.392f, .305f, .108f, cordBottom - cordTop));
        DrawDongzhiPlacketPatch(fittingRightFront, garment, new Rect(.5f, cordTop, .112f, cordBottom - cordTop),
            new Rect(.5f, .305f, .112f, cordBottom - cordTop));
    }

    private void DrawDongzhiPlacketPatch(Transform parent, Sprite garment, Rect part, Rect sample)
    {
        var bounds = new Rect(DongzhiCoat.x + part.x * DongzhiCoat.width, DongzhiCoat.y + part.y * DongzhiCoat.height,
            part.width * DongzhiCoat.width, part.height * DongzhiCoat.height);
        DrawWorkbenchSpriteCrop(parent, "Plain Placket Fabric", bounds, garment, sample, DongzhiClothTint());
    }

    private void DrawDongzhiFrontSlice(Sprite garment, float sleeveEdge, float top, float height, string name)
    {
        DrawDongzhiCoatSlice(fittingLeftFront, name, garment, new Rect(sleeveEdge, top, .5f - sleeveEdge, height), Vector2.zero);
        DrawDongzhiCoatSlice(fittingRightFront, name, garment, new Rect(.5f, top, .5f, height), Vector2.zero);
    }

    private void DrawDongzhiCoatSlice(Transform parent, string name, Sprite garment, Rect part, Vector2 offset)
    {
        var bounds = new Rect(DongzhiCoat.x + part.x * DongzhiCoat.width + offset.x,
            DongzhiCoat.y + part.y * DongzhiCoat.height + offset.y, part.width * DongzhiCoat.width,
            part.height * DongzhiCoat.height);
        DrawWorkbenchSpriteCrop(parent, name, bounds, garment, part, DongzhiClothTint());
    }

    private static RawImage DrawWorkbenchSpriteCrop(Transform parent, string name, Rect bounds,
        Sprite garment, Rect part, Color tint)
    {
        var image = WorkshopUi.Rect(parent, name, bounds).gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.color = tint;
        if (garment == null) return image;
        var slice = garment.rect;
        var texture = garment.texture;
        image.texture = texture;
        image.uvRect = new Rect((slice.x + part.x * slice.width) / texture.width,
            (slice.y + (1f - part.yMax) * slice.height) / texture.height,
            part.width * slice.width / texture.width, part.height * slice.height / texture.height);
        return image;
    }

    private static Color DongzhiClothTint()
    {
        string name = WorkshopProgress.DyedColor;
        foreach (var dye in DyeColorUtil.AllColors)
        {
            string label = DyeColorUtil.ChineseName(dye);
            if (label != name && label + "布" != name) continue;
            if (dye == DyeColor.Yellow) return Color.white;
            return Color.Lerp(Color.white, DyeColorUtil.Tint(dye), .44f);
        }
        return Color.white;
    }

    private RectTransform DongzhiStageCard(string title)
    {
        var card = WorkshopUi.PixelCard(art, "Dongzhi Fit Controls", DongzhiCard, LightSurface, WorkshopUi.Bronze);
        WorkshopUi.Label(card, "Fitting Stage Caption", new Rect(15, 16, 271, 45), title, 30, WorkshopUi.Ink,
            TextAnchor.MiddleCenter);
        return card;
    }

    private void DrawDongzhiLift()
    {
        var card = DongzhiStageCard("① 抬手试袖");
        var sleeve = WorkshopArt.CutPiece(2);
        WorkshopUi.Picture(card, "Fitting Sleeve Swatch", new Rect(77, 68, 150, 116), sleeve,
            DongzhiClothTint());
        DrawDongzhiReachMeter(card, 220f);
        fittingArmCaption = WorkshopUi.Label(card, "Dongzhi Arm Result", new Rect(21, 245, 259, 52), "抬手试袖", 23,
            WorkshopUi.Faded, TextAnchor.MiddleCenter);
        if (!string.IsNullOrEmpty(fittingNotice))
            WorkshopUi.Label(card, "Dongzhi Fit Notice", new Rect(21, 307, 259, 118), fittingNotice, 22,
                WorkshopUi.Green, TextAnchor.MiddleCenter);

        Copy("抬手试袖", ChallengeVariant == 0 ? "冬至穿夹衣，活动时袖根需要余量。" : "添了厚内搭，袖根需要留更多余量。");
        Act("Dongzhi Raise Arm", "抬起手臂", TryDongzhiArmTap, true);
        var lift = DongzhiButton(art, "Dongzhi Lift Sleeve", DongzhiSleeveGrip, "", TryDongzhiArmTap, true, true);
        var gesture = lift.gameObject.AddComponent<EventTrigger>();
        BindWinterEvent(gesture, EventTriggerType.BeginDrag, evt => BeginDongzhiLift((PointerEventData)evt));
        BindWinterEvent(gesture, EventTriggerType.Drag, evt => DragDongzhiLift((PointerEventData)evt));
        BindWinterEvent(gesture, EventTriggerType.EndDrag, evt => DropDongzhiLift((PointerEventData)evt));
        if (!fittingInitialCue || fittingLetOut > 0)
        {
            VisualStep((RectTransform)lift.transform);
            fittingLiftCue = ShowGesture(art, new Vector2(435, 426), new Vector2(375, 273), WorkGesture.Drag);
        }
    }

    private void DrawDongzhiReachMeter(RectTransform card, float y)
    {
        WorkshopUi.Fill(card, "Reach Track", new Rect(32, y, 237, 18), new Color32(214, 202, 174, 255));
        fittingArmFill = WorkshopUi.Fill(card, "Reach Fill", new Rect(32, y, 0, 18), DongzhiGreen);
        WorkshopUi.Fill(card, "Required Reach", new Rect(250, y - 4, 4, 26), WorkshopUi.Bronze);
    }

    private Button DongzhiButton(RectTransform parent, string name, Rect bounds, string label, System.Action action,
        bool primary = false, bool transparent = false)
    {
        var button = transparent
            ? WorkshopUi.Button(parent, name, bounds, label, action, Color.clear)
            : WorkshopUi.PixelButton(parent, name, bounds, label, action, primary, 23, DongzhiGreen);
        RegisterButton(button);
        return button;
    }

    private void BeginDongzhiLift(PointerEventData pointer)
    {
        if (fittingStep != DongzhiFittingStep.LiftArm || fittingPlayingPose || pointer.button != PointerEventData.InputButton.Left) return;
        pointer.eligibleForClick = false;
        fittingSleeveDragFromY = DongzhiPointerOnArt(pointer, pointer.pressPosition).y;
        fittingDraggingArm = fittingInitialCue = true;
        if (fittingLiftCue != null) fittingLiftCue.gameObject.SetActive(false);
        DragDongzhiLift(pointer);
    }

    private void DragDongzhiLift(PointerEventData pointer)
    {
        if (!fittingDraggingArm) return;
        float travel = DongzhiTravel(pointer);
        float limit = DongzhiMaximumLift(fittingEase);
        ApplyDongzhiPose(Mathf.Min(travel, limit));
        ApplyDongzhiTension(Mathf.Clamp01((travel - limit) * 5f));
    }

    private void DropDongzhiLift(PointerEventData pointer)
    {
        if (!fittingDraggingArm) return;
        fittingDraggingArm = false;
        if (DongzhiTravel(pointer) < .82f)
        {
            ApplyDongzhiPose(0f);
            ApplyDongzhiTension(0f);
            if (fittingArmCaption != null) fittingArmCaption.text = "再向上抬一些";
            if (fittingLiftCue != null) fittingLiftCue.gameObject.SetActive(true);
            return;
        }
        FinishDongzhiLift();
    }

    private float DongzhiTravel(PointerEventData pointer)
    {
        return Mathf.Clamp01((fittingSleeveDragFromY - DongzhiPointerOnArt(pointer, pointer.position).y) / 153f);
    }

    private Vector2 DongzhiPointerOnArt(PointerEventData pointer, Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(art, screenPoint, pointer.pressEventCamera, out var point);
        return new Vector2(point.x - art.rect.xMin, art.rect.yMax - point.y);
    }

    private float DongzhiMaximumLift(float ease) => Mathf.Lerp(1f, .4f, Mathf.Clamp01((FittingTarget - ease) / 2f));

    private void ApplyDongzhiTension(float tension)
    {
        if (fittingPullMarks != null) fittingPullMarks.alpha = tension;
    }

    private void ApplyDongzhiPose(float lift)
    {
        fittingArmPose = lift;
        if (fittingSleeve != null) fittingSleeve.localEulerAngles = new Vector3(0, 0, -109f * lift);
        if (fittingArmFill != null)
        {
            fittingArmFill.rectTransform.sizeDelta = new Vector2(lift * 237f, 18f);
            fittingArmFill.color = lift >= .9f ? DongzhiGreen : DongzhiDye;
        }
        if (fittingArmCaption != null) fittingArmCaption.text = lift >= .92f ? "抬手到位" : "抬手试袖";
    }

    private void TryDongzhiArmTap()
    {
        if (fittingStep != DongzhiFittingStep.LiftArm || fittingPlayingPose || fittingDraggingArm) return;
        fittingInitialCue = fittingPlayingPose = true;
        if (fittingLiftCue != null) fittingLiftCue.gameObject.SetActive(false);
        StartCoroutine(AnimateDongzhiLift(art, fittingArmPose, DongzhiMaximumLift(fittingEase)));
    }

    private IEnumerator AnimateDongzhiLift(RectTransform scene, float from, float to)
    {
        const float duration = .27f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            if (scene == null || !scene.gameObject.activeInHierarchy) { fittingPlayingPose = false; yield break; }
            float attempt = Mathf.SmoothStep(from, 1f, elapsed / duration);
            ApplyDongzhiPose(Mathf.Min(attempt, to));
            ApplyDongzhiTension(Mathf.Clamp01((attempt - to) * 5f));
            yield return null;
        }
        if (scene == null || !scene.gameObject.activeInHierarchy) { fittingPlayingPose = false; yield break; }
        ApplyDongzhiPose(to);
        fittingPlayingPose = false;
        FinishDongzhiLift();
    }

    private void FinishDongzhiLift()
    {
        if (fittingStep != DongzhiFittingStep.LiftArm) return;
        fittingGotArmPass = fittingEase >= FittingTarget;
        fittingStep = fittingGotArmPass ? DongzhiFittingStep.FitClasp : DongzhiFittingStep.FindPull;
        fittingNotice = fittingGotArmPass ? "抬手通过" : "袖根绷住了";
        if (fittingGotArmPass)
        {
            fittingArmPose = 0f;
            WorkshopAudioFeedback.Progress();
        }
        else WorkshopAudioFeedback.Notice();
        Render();
    }

    private void DrawDongzhiFindPull()
    {
        var card = DongzhiStageCard("袖根有拉痕");
        WorkshopUi.Picture(card, "Underarm Cloth", new Rect(61, 79, 179, 142), WorkshopArt.CutPiece(2), DongzhiClothTint());
        WorkshopUi.Label(card, "Dongzhi Fit Notice", new Rect(23, 232, 255, 74), "袖根绷紧", 25,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);
        Copy("检查拉痕", "袖根被拉紧了。", true);
        Act("Dongzhi Find Underarm", "检查袖根", InspectDongzhiPull, true);
        var inspect = DongzhiButton(art, "Dongzhi Underarm Pull Marks", DongzhiSeamGrip, "", InspectDongzhiPull, true, true);
        VisualStep((RectTransform)inspect.transform);
        ShowGesture(art, DongzhiSeamGrip.center, DongzhiSeamGrip.center, WorkGesture.Tap);
    }

    private void InspectDongzhiPull()
    {
        if (fittingStep != DongzhiFittingStep.FindPull) return;
        fittingStep = DongzhiFittingStep.ReleaseSeam;
        WorkshopAudioFeedback.Fabric();
        Render();
    }

    private void DrawDongzhiRelease()
    {
        var card = DongzhiStageCard("放松袖根");
        var texture = GameArtSprite("CuttingCloth");
        int available = FittingReleaseAvailable;
        var reserve = new Rect(29, 94, 103 + DongzhiSeamSpacing * available, 95);
        GameArtSurface(card, "Reserve Cloth for Alterations", reserve, texture, DongzhiLoose);
        for (int amount = 0; amount <= available; amount++)
        {
            float x = DongzhiSeamPin.x + amount * DongzhiSeamSpacing;
            DrawCueStroke(card, new Vector2(x, 104), new Vector2(x, 178), 3, WorkshopUi.Bronze);
            if (amount == 0) continue;
            int detent = amount;
            var stop = DongzhiButton(card, "Dongzhi Release " + amount, new Rect(x - 23, 113, 46, 57), "", () => ReleaseDongzhiSeam(detent), transparent: true);
            DrawCueStroke(stop.transform, new Vector2(16, 29), new Vector2(30, 29), 5,
                amount == 1 ? DongzhiGreen : WorkshopUi.Faded);
        }
        fittingSeamFlap = WorkshopUi.Rect(card, "Moving Sleeve Seam", new Rect(26, 89, 84, 105));
        GameArtSurface(fittingSeamFlap, "Outer Sleeve Cloth", new Rect(0, 0, 72, 105), texture, DongzhiDye);
        DrawCueStroke(fittingSeamFlap, new Vector2(47, 17), new Vector2(47, 87), 4, DongzhiGreen);
        var pinch = DongzhiButton(fittingSeamFlap, "Dongzhi Drag Seam", new Rect(18, 16, 55, 76), "", null, transparent: true);
        var garment = WorkshopArt.Garment("4_02") ?? Resources.Load<Sprite>("WorkshopFinish/SilkVariants/4_02");
        DrawWorkbenchSpriteCrop(fittingSeamFlap, "Seam Pin", new Rect(32, 29, 36, 44), garment,
            new Rect(.401f, .152f, .112f, .062f), Color.white);
        var trigger = pinch.gameObject.AddComponent<EventTrigger>();
        BindWinterEvent(trigger, EventTriggerType.BeginDrag, evt => BeginDongzhiSeam((PointerEventData)evt));
        BindWinterEvent(trigger, EventTriggerType.Drag, evt => DragDongzhiSeam((PointerEventData)evt));
        BindWinterEvent(trigger, EventTriggerType.EndDrag, evt => DropDongzhiSeam((PointerEventData)evt));
        DrawDongzhiReachMeter(card, 252f);
        ApplyDongzhiSeamPreview(0f);
        var start = DongzhiCard.position + DongzhiSeamPin;
        VisualStep((RectTransform)pinch.transform);
        DrawCueStroke(card, DongzhiSeamPin + new Vector2(26, 0),
            DongzhiSeamPin + new Vector2(DongzhiSeamSpacing - 9, 0), 3, WorkshopUi.Cream);
        fittingSeamCue = ShowGesture(art, start, start + Vector2.right * DongzhiSeamSpacing, WorkGesture.Drag);
        Copy("放松袖根", "往外拉一点，看袖子能抬多高。");
    }

    private void BeginDongzhiSeam(PointerEventData pointer)
    {
        if (fittingStep != DongzhiFittingStep.ReleaseSeam || pointer.button != PointerEventData.InputButton.Left) return;
        fittingDraggingSeam = true;
        fittingSeamDragFromX = DongzhiPointerOnArt(pointer, pointer.pressPosition).x;
        fittingSeamDetent = 0;
        if (fittingSeamCue != null) fittingSeamCue.gameObject.SetActive(false);
        DragDongzhiSeam(pointer);
    }

    private float DongzhiSeamTravel(PointerEventData pointer) => Mathf.Clamp(
        (DongzhiPointerOnArt(pointer, pointer.position).x - fittingSeamDragFromX) / DongzhiSeamSpacing, 0, FittingReleaseAvailable);

    private void DragDongzhiSeam(PointerEventData pointer)
    {
        if (!fittingDraggingSeam) return;
        float travel = DongzhiSeamTravel(pointer);
        ApplyDongzhiSeamPreview(travel);
        int detent = Mathf.RoundToInt(travel);
        if (detent == fittingSeamDetent) return;
        fittingSeamDetent = detent;
        WorkshopAudioFeedback.Fabric();
    }

    private void DropDongzhiSeam(PointerEventData pointer)
    {
        if (!fittingDraggingSeam) return;
        fittingDraggingSeam = false;
        int amount = Mathf.RoundToInt(DongzhiSeamTravel(pointer));
        if (amount > 0)
        {
            ReleaseDongzhiSeam(amount);
            return;
        }
        ApplyDongzhiSeamPreview(0f);
        if (fittingSeamCue != null) fittingSeamCue.gameObject.SetActive(true);
    }

    private void ApplyDongzhiSeamPreview(float amount)
    {
        fittingSeamFlap.anchoredPosition = new Vector2(26 + amount * DongzhiSeamSpacing, -89f);
        float reach = DongzhiMaximumLift(fittingEase + amount);
        ApplyDongzhiPose(reach);
        ApplyDongzhiTension(Mathf.Clamp01((1f - reach) * 4f));
    }

    private void ReleaseDongzhiSeam(int amount)
    {
        if (fittingStep != DongzhiFittingStep.ReleaseSeam || amount < 1 || amount > FittingReleaseAvailable) return;
        fittingEase += amount;
        fittingLetOut += amount;
        fittingCorrections++;
        fittingStep = DongzhiFittingStep.LiftArm;
        fittingArmPose = 0f;
        fittingNotice = "再抬手检查";
        WorkshopAudioFeedback.Fabric();
        Render();
    }

    private void DrawDongzhiClasp()
    {
        var card = DongzhiStageCard("② 合襟");
        WorkshopUi.Label(card, "Lift Test Passed", new Rect(23, 80, 255, 47), "✓ 抬手通过", 25, DongzhiGreen,
            TextAnchor.MiddleCenter);
        fittingClaspStatus = WorkshopUi.Label(card, "Dongzhi Clasp Feedback", new Rect(23, 144, 255, 100), "", 27,
            WorkshopUi.Ink, TextAnchor.MiddleCenter);

        var garment = WorkshopArt.Garment("4_02") ?? Resources.Load<Sprite>("WorkshopFinish/SilkVariants/4_02");
        var creases = WorkshopUi.Rect(art, "Fabric gathering beside the clasp", new Rect());
        fittingClaspCreases = creases.gameObject.AddComponent<CanvasGroup>();
        fittingClaspCreases.blocksRaycasts = false;
        for (int i = 0; i < 3; i++)
        {
            DrawCueStroke(creases, new Vector2(508 + i * 3, 337 + i * 22),
                new Vector2(551, 367 + i * 8), 4f, ErrorInk);
            DrawCueStroke(creases, new Vector2(648 - i * 3, 344 + i * 22),
                new Vector2(591, 368 + i * 8), 4f, ErrorInk);
        }
        fittingClaspSource = DrawWorkbenchSpriteCrop(art, "Loose Gold Cord on the Coat", new Rect(), garment,
            DongzhiClaspArtwork, Color.white);
        PlaceDongzhiKnot(fittingClaspSource.rectTransform, DongzhiChestStrap, 78, 32);
        var drag = DongzhiButton(art, "Dongzhi Drag Clasp", SewingTouchArea(DongzhiChestStrap, 42, 34), "",
            null, transparent: true);
        var hook = drag.gameObject.AddComponent<EventTrigger>();
        BindWinterEvent(hook, EventTriggerType.BeginDrag, evt => BeginDongzhiClasp((PointerEventData)evt));
        BindWinterEvent(hook, EventTriggerType.Drag, evt => DragDongzhiClasp((PointerEventData)evt));
        BindWinterEvent(hook, EventTriggerType.EndDrag, evt => DropDongzhiClasp((PointerEventData)evt));

        DrawCueStroke(art, DongzhiClaspRailStart, DongzhiClaspRailEnd, 6f, DongzhiLoose);
        for (int i = 0; i < DongzhiClaspSlotCount; i++)
        {
            int slot = i;
            Vector2 at = DongzhiClaspPosition(i);
            var target = DongzhiButton(art, "Dongzhi Clasp Slot " + (i + 1), SewingTouchArea(at, 25, 21), "",
                () => TryDongzhiClasp(slot), transparent: true);
            var loop = WorkshopUi.PixelCard(target.transform, "Button Loop on the Coat", new Rect(9, 5, 32, 32),
                DongzhiGreen, WorkshopUi.Ink);
            DrawWorkbenchSpriteCrop(loop, "Silk Knot", new Rect(4, 4, 24, 24), garment, DongzhiClaspKnot, Color.white);
            if (fittingLastClasp == i) VisualStep((RectTransform)target.transform, ErrorInk);
            Act("Dongzhi Clasp Alternative " + i, "试第 " + (i + 1) + " 个扣眼", () => TryDongzhiClasp(slot));
        }

        fittingClaspGhost = WorkshopUi.Rect(art, "Clasp in Hand", new Rect());
        DrawWorkbenchSpriteCrop(fittingClaspGhost, "Moved Gold Cord Clasp", new Rect(0, 0, 70, 32), garment,
            DongzhiClaspArtwork, Color.white);
        fittingClaspGhost.gameObject.SetActive(false);
        VisualStep((RectTransform)drag.transform);
        preferredAction = drag;
        if (fittingLastClasp < 0)
            fittingClaspCue = ShowGesture(art, DongzhiChestStrap, DongzhiClaspRailEnd, WorkGesture.Drag).GetComponent<CanvasGroup>();
        ApplyDongzhiClaspPreview(DongzhiRestingClasp());
        Copy("合襟对扣", "在衣襟上拉盘扣，松手前可以来回试。", fittingLastClasp >= 0);
    }

    private static Vector2 DongzhiClaspPosition(int index) => Vector2.Lerp(DongzhiClaspRailStart, DongzhiClaspRailEnd, index * .5f);

    private float DongzhiRestingClasp() => fittingLastClasp < 0 ? FittingMatchingClasp + 1f : fittingLastClasp;

    private void ApplyDongzhiClaspPreview(float position)
    {
        float error = position - FittingMatchingClasp;
        float slack = Mathf.Max(0f, error - .29f);
        float tension = Mathf.Max(0f, -error - .29f);
        float opening = Mathf.Min(slack * 26f, 35f);
        float overlap = Mathf.Min(tension * 17f, 25f);
        float modelOffset = opening - overlap;
        fittingClaspCreases.alpha = Mathf.Clamp01(tension * 1.8f);
        bool restsFlat = Mathf.Abs(error) <= .29f;
        fittingClaspStatus.text = restsFlat ? "✓ 衣襟贴平" : error > 0 ? "衣襟留缝" : "衣襟起皱";
        fittingClaspStatus.color = restsFlat ? DongzhiGreen : ErrorInk;
        if (fittingModelLining == null) return;
        fittingLeftFront.anchoredPosition = new Vector2(-modelOffset, 0f);
        fittingRightFront.anchoredPosition = new Vector2(modelOffset, 0f);
        fittingSleeve.anchoredPosition = new Vector2(DongzhiShoulder.x - modelOffset, -DongzhiShoulder.y);
        SetDongzhiSurface(fittingModelLining, new Rect(DongzhiCoat.center.x - 3f - opening,
            DongzhiCoat.y + DongzhiCoat.height * .13f, Mathf.Max(18f, 2f * opening + 6f), DongzhiCoat.height * .493f));
    }

    private static void SetDongzhiSurface(Image panel, Rect bounds)
    {
        panel.rectTransform.anchoredPosition = new Vector2(bounds.x, -bounds.y);
        panel.rectTransform.sizeDelta = bounds.size;
    }

    private static void PlaceDongzhiKnot(RectTransform knot, Vector2 at, float width, float height)
    {
        knot.sizeDelta = new Vector2(width, height);
        knot.anchoredPosition = new Vector2(at.x - width * .5f, -at.y + height * .5f);
    }

    private void BeginDongzhiClasp(PointerEventData pointer)
    {
        if (fittingStep != DongzhiFittingStep.FitClasp || pointer.button != PointerEventData.InputButton.Left) return;
        if (fittingClaspCue != null) fittingClaspCue.alpha = 0f;
        fittingClaspSource.enabled = false;
        fittingClaspGhost.gameObject.SetActive(true);
        WorkshopAudioFeedback.Fabric();
        DragDongzhiClasp(pointer);
    }

    private void DragDongzhiClasp(PointerEventData pointer)
    {
        if (fittingClaspGhost == null || !fittingClaspGhost.gameObject.activeSelf) return;
        var at = DongzhiPointerOnArt(pointer, pointer.position);
        float travel = DongzhiClaspTravel(at);
        int slot = Mathf.Clamp(Mathf.RoundToInt(travel), 0, DongzhiClaspSlotCount - 1);
        if (Vector2.Distance(at, DongzhiClaspPosition(slot)) <= DongzhiClaspSnapDistance)
        {
            at = DongzhiClaspPosition(slot);
            travel = slot;
        }
        PlaceDongzhiKnot(fittingClaspGhost, at, 70, 32);
        ApplyDongzhiClaspPreview(Mathf.Clamp(travel, -.55f, 2.55f));
    }

    private void DropDongzhiClasp(PointerEventData pointer)
    {
        if (fittingClaspGhost == null || !fittingClaspGhost.gameObject.activeSelf) return;
        fittingClaspGhost.gameObject.SetActive(false);
        fittingClaspSource.enabled = true;
        var at = DongzhiPointerOnArt(pointer, pointer.position);
        int slot = Mathf.Clamp(Mathf.RoundToInt(DongzhiClaspTravel(at)), 0, DongzhiClaspSlotCount - 1);
        if (Vector2.Distance(at, DongzhiClaspPosition(slot)) <= DongzhiClaspSnapDistance)
        {
            TryDongzhiClasp(slot);
            return;
        }
        ApplyDongzhiClaspPreview(DongzhiRestingClasp());
        if (fittingClaspCue != null) fittingClaspCue.alpha = 1f;
    }

    private static float DongzhiClaspTravel(Vector2 point)
    {
        Vector2 rail = DongzhiClaspRailEnd - DongzhiClaspRailStart;
        return 2f * Vector2.Dot(point - DongzhiClaspRailStart, rail) / rail.sqrMagnitude;
    }

    private void TryDongzhiClasp(int which)
    {
        if (fittingStep != DongzhiFittingStep.FitClasp || !fittingGotArmPass || which < 0 || which >= DongzhiClaspSlotCount) return;
        fittingLastClasp = which;
        if (which != FittingMatchingClasp)
        {
            fittingCorrections++;
            WorkshopAudioFeedback.Notice();
            Render();
            return;
        }
        fittingClasp = which;
        fittingStep = DongzhiFittingStep.Deliver;
        fittingNotice = null;
        WorkshopAudioFeedback.Confirm();
        Render();
    }

    private void DrawDongzhiPass()
    {
        var card = DongzhiStageCard("试穿合格");
        WorkshopUi.Picture(card, "Dongzhi Receipt Seal", new Rect(72, 68, 156, 151), GameArtSprite("SuccessSeal"));
        WorkshopUi.Label(card, "Verified Reach", new Rect(20, 243, 261, 49), "✓ 抬手自如", 26, DongzhiGreen,
            TextAnchor.MiddleCenter);
        WorkshopUi.Label(card, "Verified Front", new Rect(20, 294, 261, 49), "✓ 衣襟平整", 26, DongzhiGreen,
            TextAnchor.MiddleCenter);
        if (readOnly || GameComplete) return;
        Copy("合身 · 可以交衣", "袖子和衣襟都通过试穿。把这件夹衣交到展架。");
        Act("Dongzhi Deliver Garment", "交衣验收", DeliverDongzhiGarment, true);
        var deliver = DongzhiButton(card, "Dongzhi Deliver from Stage", new Rect(32, 365, 237, 58), "交衣验收", DeliverDongzhiGarment, true);
        VisualStep((RectTransform)deliver.transform);
    }

    private void DeliverDongzhiGarment()
    {
        if (fittingStep != DongzhiFittingStep.Deliver || !fittingGotArmPass || fittingClasp < 0) return;
        SaveGarmentFittingResult(fittingLetOut, fittingClasp);
        int score = Mathf.Max(60, 100 - fittingCorrections * 3 - fittingLetOut);
        string result = fittingLetOut > 0 ? $"袖根放宽 {fittingLetOut} 格 · 衣襟贴合" : "抬手自如 · 衣襟贴合";
        FinishChallenge(score, result, WorkshopProgress.GarmentCutSample);
    }
}
