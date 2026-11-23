using System.Collections.Generic;
using UnityEngine;
using JiangnanDye;

/// <summary>冬天关卡中待研磨的一种染料原料（从寻找染料小游戏带回）。</summary>
public class WinterDyeMaterial
{
    public DyeColor Color;
    public string Name;
    public Sprite Icon;
    public bool IsNormal;
}

/// <summary>
/// 冬天关卡（Level4_Winter）的关卡进度状态。
/// 冬季玩法的场景状态，关键道具与染材从工坊存档恢复。
/// </summary>
public static class WinterLevelState
{
    /// <summary>是否已拾取锄头（进入染料园的前置条件）</summary>
    public static bool HasHoe;

    /// <summary>是否已拥有染好颜色的布匹（开始裁衣的前置条件）</summary>
    public static bool HasDyedCloth;

    /// <summary>玩家选好的衣服颜色名称，染色功能完成后写入，供裁衣等后续流程使用</summary>
    public static string ChosenClothColor;

    /// <summary>是否已经点击拾取裁衣工具针</summary>
    public static bool HasSewingNeedle;

    /// <summary>从寻找染料小游戏返回后，需要在冬天场景提示玩家去询问 NPC 研磨之法</summary>
    public static bool PendingGrindingPrompt;

    /// <summary>从染料园返回时，是否要把玩家放回进入前的位置</summary>
    public static bool HasReturnPosition;

    /// <summary>进入染料园前玩家所在的位置</summary>
    public static Vector3 ReturnPosition;

    /// <summary>是否已经向 NPC 询问过研磨之法（第一阶段对话完成）</summary>
    public static bool HasAskedGrindingMethod;

    /// <summary>NPC 是否已经告诉玩家去寻找研磨棒和钵体（第二阶段对话完成）</summary>
    public static bool HasGrindingTask;

    /// <summary>是否已经点击拾取研磨棒（开始研磨的前置条件之一）</summary>
    public static bool HasGrindingRod;

    /// <summary>从寻找染料小游戏带回的原料，按颜色去重（同色优先正常状态）。</summary>
    public static readonly List<WinterDyeMaterial> CollectedDyes = new List<WinterDyeMaterial>();

    /// <summary>已经研磨完成的颜色，每种颜色只产出一次。</summary>
    public static readonly HashSet<DyeColor> ProducedDyeColors = new HashSet<DyeColor>();

    /// <summary>
    /// 把寻找染料小游戏的收下清单同步到冬天场景。
    /// 同一颜色只保留一件，优先保留正常状态；已研磨过的颜色不再出现。
    /// </summary>
    public static void SetCollectedDyes(IEnumerable<ItemData> items)
    {
        WorkshopProgress.RecordRawDyes(items);
        RestoreDyes();
    }

    public static void RestoreWorkshopProgress()
    {
        HasHoe = WorkshopProgress.HasStep("winter.hoe");
        HasGrindingRod = WorkshopProgress.HasStep("winter.rod");
        HasAskedGrindingMethod = WorkshopProgress.HasStep("winter.grindExplained");
        HasGrindingTask = WorkshopProgress.HasStep("winter.grindTask");
        HasSewingNeedle = WorkshopProgress.Has(WorkshopItem.Needle);
        HasDyedCloth = WorkshopProgress.Has(WorkshopItem.DyedCloth);
        ChosenClothColor = HasDyedCloth ? WorkshopProgress.DyedColor + "布" : null;
        RestoreDyes();
    }

    private static void RestoreDyes()
    {
        CollectedDyes.Clear();
        ProducedDyeColors.Clear();
        foreach (DyeColor color in DyeColorUtil.AllColors)
        {
            if (WorkshopProgress.IsDyeGround(color))
            {
                ProducedDyeColors.Add(color);
                continue;
            }

            ItemData item = WorkshopProgress.PendingDye(color);
            if (item == null) continue;
            CollectedDyes.Add(new WinterDyeMaterial
            {
                Color = item.dyeColor,
                Name = item.chineseName,
                Icon = item.icon,
                IsNormal = item.condition == DyeCondition.Normal
            });
        }

    }

    /// <summary>研磨完成后记录颜色；返回 false 表示该颜色已经产出过。</summary>
    public static bool MarkDyeProduced(DyeColor color)
    {
        if (!ProducedDyeColors.Add(color)) return false;

        WorkshopProgress.RecordGroundDye(color);
        CollectedDyes.RemoveAll(m => m.Color == color);
        Debug.Log("[WinterLevelState] ✅ 已研磨出 " + DyeColorUtil.ChineseName(color) + "染料");
        return true;
    }

    /// <summary>完成染色并选好颜色后调用，解锁裁衣台。</summary>
    public static void SetDyedCloth(string colorName = null)
    {
        HasDyedCloth = true;
        ChosenClothColor = colorName;
        Debug.Log("[WinterLevelState] ✅ 已拥有染好颜色的布匹，颜色：" + (colorName ?? "未指定"));
    }
}
