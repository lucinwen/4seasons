using System;
using UnityEngine;

/// <summary>
/// 冬天关卡 NPC 对话，按裁衣进度播放；采染对话由秋季复用。
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
