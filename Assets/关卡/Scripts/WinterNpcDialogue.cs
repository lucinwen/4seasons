using System;
using UnityEngine;

/// <summary>
/// 冬天关卡 NPC 对话，按采染、研磨和裁衣进度播放。
/// 由 WinterLevelInteractions 在运行时挂到 NPC 上，不改场景。
/// </summary>
public class WinterNpcDialogue : MonoBehaviour
{
    private const string DyeMaterialsIntro = "dialogue.winter.materials";
    private const string GrindingIntro = "dialogue.winter.grinding";
    private const string TailoringIntro = "dialogue.winter.tailoring";

    public void Talk()
    {
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

        if (WinterLevelState.CollectedDyes.Count > 0 && !WinterLevelState.HasDyedCloth &&
            !WorkshopProgress.HasStep(GrindingIntro))
        {
            IntroduceGrinding();
            return;
        }

        if (!WorkshopProgress.HasStep("winter.dyes") && !WorkshopProgress.HasStep("winter.ground") &&
            !WinterLevelState.HasDyedCloth && !WorkshopProgress.HasStep(DyeMaterialsIntro))
        {
            IntroduceDyeMaterials();
            return;
        }

        WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.WinterInProgress);
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
