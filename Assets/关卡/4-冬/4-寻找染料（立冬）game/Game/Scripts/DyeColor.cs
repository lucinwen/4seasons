using UnityEngine;

namespace JiangnanDye
{
    /// <summary>五种天然染料颜色。</summary>
    public enum DyeColor
    {
        Red = 0,
        Yellow = 1,
        Green = 2,
        Blue = 3,
        Purple = 4
    }

    /// <summary>染料状态：正常 / 坏损。</summary>
    public enum DyeCondition
    {
        Normal = 0,
        Damaged = 1
    }

    /// <summary>颜色与状态的显示工具。</summary>
    public static class DyeColorUtil
    {
        public static readonly DyeColor[] AllColors =
        {
            DyeColor.Red, DyeColor.Yellow, DyeColor.Green, DyeColor.Blue, DyeColor.Purple
        };

        public static string ChineseName(DyeColor color)
        {
            switch (color)
            {
                case DyeColor.Red: return "红色";
                case DyeColor.Yellow: return "黄色";
                case DyeColor.Green: return "绿色";
                case DyeColor.Blue: return "蓝色";
                case DyeColor.Purple: return "紫色";
            }
            return "未知";
        }

        public static string ConditionText(DyeCondition condition)
        {
            return condition == DyeCondition.Normal ? "正常" : "坏损";
        }

        public static string DisplayTintHex(DyeColor color)
        {
            switch (color)
            {
                case DyeColor.Red: return "#C2452E";
                case DyeColor.Yellow: return "#D9A227";
                case DyeColor.Green: return "#6E8B3D";
                case DyeColor.Blue: return "#3F6EA8";
                case DyeColor.Purple: return "#7B4B8E";
            }
            return "#8C7B66";
        }

        public static Color Tint(DyeColor color)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(DisplayTintHex(color), out c) ? c : Color.white;
        }
    }
}
