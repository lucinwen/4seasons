using System;
using UnityEngine;

/// <summary>
/// 冬季只提示裁衣进度；染材和研磨对话供秋季工位复用。
/// 由 WinterLevelInteractions 在运行时挂到 NPC 上，不改场景。
/// </summary>
public class WinterNpcDialogue : MonoBehaviour
{
    private const string DyeMaterialsIntro = "dialogue.winter.materials";
    private const string GrindingIntro = "dialogue.winter.grinding";
    private const string TailoringIntro = "dialogue.winter.tailoring";

    public void Talk()
    {
        if (gameObject.scene.name == "Level3_Autumn")
        {
            WorkshopProgress.UnlockSolarTerms(WorkshopSeason.Autumn);
            WorkshopDialoguePresenter.Play(new[] {
                new WorkshopDialogueLine("染整师傅", "立秋采染材，秋分研磨和染色。先拾锄头去染材园，再找研磨棒和研钵。"),
                new WorkshopDialogueLine("染整师傅", "白露织出的生坯绸可以用来染色。选好喜欢的颜色，寒露再去织造坊攀花。道具都会保留。") });
            return;
        }
        if (WorkshopProgress.IsComplete(WorkshopSeason.Winter))
        {
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.WinterDone);
            return;
        }

        if (WinterLevelState.HasDyedCloth && !WorkshopProgress.Has(WorkshopItem.CutPieces) &&
            !WorkshopProgress.HasStep(TailoringIntro))
        {
            IntroduceTailoring();
            return;
        }

        string next;
        switch (WorkshopWordRules.WinterStage)
        {
            case WorkshopWinterStage.Cutting: next = "带秋季染布到裁衣台，沿虚线剪出前衣、后衣和衣袖。"; break;
            case WorkshopWinterStage.Fitting: next = "衣片已经剪好。再点裁衣台，将三张纸样拖到同名轮廓，确认版型。"; break;
            case WorkshopWinterStage.Needle: next = "版型已经确认。收下缝素堂里发光的缝衣针，回裁衣台选款缝衣。"; break;
            default: next = "材料与版型都备齐了。直接在裁衣台选择喜欢的款式，拼合三块衣片，缝好后去试衣。"; break;
        }
        WorkshopDialoguePresenter.Play(new[] { new WorkshopDialogueLine("缝衣师傅", next) });
    }

    internal static void IntroduceDyeMaterials(Action onFinished = null)
    {
        WorkshopDialoguePresenter.PlayOnce(DyeMaterialsIntro, WorkshopDialogueScripts.WinterDyeMaterialsIntro,
            onFinished);
    }

    internal static void IntroduceGrinding(Action onFinished = null)
    {
        WorkshopDialoguePresenter.PlayOnce(GrindingIntro, WorkshopDialogueScripts.WinterGrindingIntro, () =>
        {
            WinterLevelState.HasAskedGrindingMethod = true;
            WinterLevelState.HasGrindingTask = true;
            WorkshopProgress.MarkStep("winter.grindExplained");
            WorkshopProgress.MarkStep("winter.grindTask");
            onFinished?.Invoke();
        });
    }

    internal static void IntroduceTailoring(Action onFinished = null)
    {
        WorkshopDialoguePresenter.PlayOnce(TailoringIntro, WorkshopDialogueScripts.WinterTailoringIntro,
            onFinished);
    }
}
