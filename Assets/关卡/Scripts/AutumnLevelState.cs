/// <summary>
/// 秋天关卡（Level3_Autumn）的关卡进度状态。
/// 用于判断织机和酬蚕仪式等交互的前置条件。
/// </summary>
public static class AutumnLevelState
{
    /// <summary>是否已获得蚕花酒</summary>
    public static bool HasWine;

    /// <summary>是否已从香蕉果篮领取贡品香蕉</summary>
    public static bool HasBanana;

    /// <summary>是否已获得水稻</summary>
    public static bool HasRice;

    /// <summary>是否已从工具箱获得梭子</summary>
    public static bool HasShuttle;

    /// <summary>是否已找到工具箱（点击工具箱后置 true）</summary>
    public static bool HasToolbox;

    /// <summary>是否已通过与 NPC 的对话获得锤子（修复织机工具）</summary>
    public static bool HasHammer;

    /// <summary>是否已通过与 NPC 的对话获得糕点</summary>
    public static bool HasCake;

    internal static int OfferingCount => (HasWine ? 1 : 0) + (HasBanana ? 1 : 0) + (HasCake ? 1 : 0);

    /// <summary>是否已修复织机（修复小游戏完成时置 true）</summary>
    public static bool HasLoomRepaired;

    /// <summary>是否已完成酬蚕仪式</summary>
    public static bool HasRitualDone;

    /// <summary>是否已获得嫘祖雕像</summary>
    public static bool HasStatue;

    /// <summary>是否从秋天关卡进入修复织机小游戏（用于小游戏结束后返回关卡）</summary>
    public static bool ReturningFromLoom;

    public static void RestoreProgress()
    {
        HasWine = WorkshopProgress.HasStep("autumn.wine");
        HasBanana = WorkshopProgress.HasStep("autumn.banana");
        HasRice = WorkshopProgress.HasStep("autumn.rice");
        HasShuttle = WorkshopProgress.Has(WorkshopItem.Shuttle);
        HasToolbox = HasShuttle;
        HasHammer = WorkshopProgress.HasStep("autumn.hammer");
        HasCake = WorkshopProgress.HasStep("autumn.cake");
        HasLoomRepaired = WorkshopProgress.HasStep("autumn.loom");
        HasStatue = WorkshopProgress.Has(WorkshopItem.Statue);
        HasRitualDone = HasStatue;
        ReturningFromLoom = false;
    }
}
