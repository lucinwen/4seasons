using System;
using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 冬季发放蚕连并提示裁衣进度；染材和研磨对话供秋季工位复用。
/// 由 WinterLevelInteractions 在运行时挂到 NPC 上，不改场景。
/// </summary>
public class WinterNpcDialogue : MonoBehaviour
{
    private const string DyeMaterialsIntro = "dialogue.winter.materials";
    private const string GrindingIntro = "dialogue.winter.grinding";
    private const string TailoringIntro = "dialogue.winter.tailoring";
    private static readonly WorkshopDialogueLine[] EggSheetIntro =
    {
        new WorkshopDialogueLine("裁衣师傅", "这卷附着蚕卵的纸叫蚕连。冬天把它挂起来保存，留下明年春天要孵的蚕种。"),
        new WorkshopDialogueLine("裁衣师傅", "收下这卷蚕连，等缝衣针和衣片也齐了，就一起交到任务面板。")
    };

    public void Talk()
    {
        if (gameObject.scene.name == "Level3_Autumn")
        {
            WorkshopProgress.UnlockSolarTerms(WorkshopSeason.Autumn);
            WorkshopDialoguePresenter.Play(new[] {
                new WorkshopDialogueLine("织染师傅", "立秋采染材，秋分研磨和染色。先拾锄头去染材园，再找研磨棒和研钵。"),
                new WorkshopDialogueLine("织染师傅", "白露奇偶推梭后收下的生坯绸可以用来染色。选好喜欢的颜色，寒露再去织造坊攀花获得缬。道具都会保留。") });
            return;
        }
        if (WorkshopProgress.IsComplete(WorkshopSeason.Winter))
        {
            WorkshopDialoguePresenter.Play(WorkshopDialogueScripts.WinterDone);
            return;
        }
        if (gameObject.scene.name == WorkshopProgress.SceneName(WorkshopSeason.Winter) &&
            WorkshopProgress.IsUnlocked(WorkshopSeason.Winter) && !WorkshopProgress.Has(WorkshopItem.SilkwormEggSheet))
        {
            WorkshopDialoguePresenter.Play(EggSheetIntro, () =>
                SimplePopup.ShowItem(WorkshopItem.SilkwormEggSheet, "收好这卷蚕连，留存来年春蚕的蚕种。", CollectEggSheet));
            return;
        }

        var stage = WorkshopWordRules.WinterStage;
        if (WinterLevelState.HasDyedCloth && stage == WorkshopWinterStage.Preparing &&
            !WorkshopProgress.HasStep(TailoringIntro))
        {
            IntroduceTailoring();
            return;
        }

        string next;
        switch (stage)
        {
            case WorkshopWinterStage.Preparing: next = "带秋季染布到裁衣台，晾干后把五片衣样纸拖到布上。衣样固定方向、不可旋转，避开布疵并刚好排满布面。"; break;
            case WorkshopWinterStage.Cutting: next = "染布已经晾好、衣样也排齐了。到裁衣台沿虚线剪出前衣、后衣和衣袖。"; break;
            case WorkshopWinterStage.Needle: next = "衣片已经剪好。收下缝素堂里发光的缝衣针，就能选款缝衣。"; break;
            default: next = "缝衣针、蚕连和衣片都备齐了。选好喜欢的款式，拼合三块衣片，缝好后去试衣。"; break;
        }
        WorkshopDialoguePresenter.Play(new[] { new WorkshopDialogueLine("裁衣师傅", next) });
    }

    private static void CollectEggSheet()
    {
        if (WorkshopProgress.Award(WorkshopItem.SilkwormEggSheet))
            WorkshopProcessCardView.Show(WorkshopProcessCard.WinterEggs, null);
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
