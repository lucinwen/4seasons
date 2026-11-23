using UnityEngine;
using JiangnanDye;

/// <summary>
/// 冬天研磨界面的图片引用。放在 Resources 下，由 WinterGrindingOverlay 运行时加载。
/// 图片均来自「关卡/4-冬/道具」文件夹。
/// </summary>
[CreateAssetMenu(fileName = "WinterGrindingConfig", menuName = "江南染料/冬天研磨配置")]
public class WinterGrindingConfig : ScriptableObject
{
    [Header("研磨工具")]
    public Sprite mortar;
    public Sprite rod;

    [Header("染料成品（钵体中的染料）")]
    public Sprite redDye;
    public Sprite yellowDye;
    public Sprite greenDye;
    public Sprite blueDye;
    public Sprite purpleDye;

    public Sprite GetDye(DyeColor color)
    {
        switch (color)
        {
            case DyeColor.Red: return redDye;
            case DyeColor.Yellow: return yellowDye;
            case DyeColor.Green: return greenDye;
            case DyeColor.Blue: return blueDye;
            case DyeColor.Purple: return purpleDye;
        }
        return null;
    }
}
