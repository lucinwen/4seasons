using UnityEngine;

/// <summary>夏天关卡道具配置：索绪等物品图标。</summary>
public class SummerSuoxuConfig : ScriptableObject
{
    public Sprite suoxu;

    public static Sprite RewardSprite => Resources.Load<SummerSuoxuConfig>("SummerSuoxuConfig")?.suoxu;
}
