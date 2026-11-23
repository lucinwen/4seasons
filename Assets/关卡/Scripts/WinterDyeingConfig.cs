using UnityEngine;
using JiangnanDye;

/// <summary>
/// 冬天染色界面的图片与配色。放在 Resources 下，运行时加载。
/// 清水缸使用 4-染料空缸，五种颜色使用对应的彩色染缸和布料图。
/// </summary>
[CreateAssetMenu(fileName = "WinterDyeingConfig", menuName = "江南染料/冬天染色配置")]
public class WinterDyeingConfig : ScriptableObject
{
    [Header("清水缸与素绸")]
    public Sprite clearWaterVat;
    public Sprite silk;

    [Header("五色染缸")]
    public Sprite redVat;
    public Sprite yellowVat;
    public Sprite greenVat;
    public Sprite blueVat;
    public Sprite purpleVat;

    [Header("五色布料（缺图时用素绸+下方颜色染色）")]
    public Sprite redCloth;
    public Sprite yellowCloth;
    public Sprite greenCloth;
    public Sprite blueCloth;
    public Sprite purpleCloth;

    [Header("缺图时的布料染色")]
    public Color redTint = new Color(0.86f, 0.30f, 0.26f);
    public Color yellowTint = new Color(0.95f, 0.80f, 0.30f);
    public Color greenTint = new Color(0.42f, 0.68f, 0.42f);
    public Color blueTint = new Color(0.32f, 0.52f, 0.80f);
    public Color purpleTint = new Color(0.62f, 0.42f, 0.72f);

    public Sprite GetVat(DyeColor color)
    {
        switch (color)
        {
            case DyeColor.Red: return redVat;
            case DyeColor.Yellow: return yellowVat;
            case DyeColor.Green: return greenVat;
            case DyeColor.Blue: return blueVat;
            case DyeColor.Purple: return purpleVat;
        }
        return clearWaterVat;
    }

    public Sprite GetCloth(DyeColor color)
    {
        switch (color)
        {
            case DyeColor.Red: return redCloth;
            case DyeColor.Yellow: return yellowCloth;
            case DyeColor.Green: return greenCloth;
            case DyeColor.Blue: return blueCloth;
            case DyeColor.Purple: return purpleCloth;
        }
        return null;
    }

    public Color GetClothTint(DyeColor color)
    {
        switch (color)
        {
            case DyeColor.Red: return redTint;
            case DyeColor.Yellow: return yellowTint;
            case DyeColor.Green: return greenTint;
            case DyeColor.Blue: return blueTint;
            case DyeColor.Purple: return purpleTint;
        }
        return Color.white;
    }
}
