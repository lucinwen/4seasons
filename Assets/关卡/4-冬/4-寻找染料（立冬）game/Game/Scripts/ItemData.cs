using UnityEngine;

namespace JiangnanDye
{
    /// <summary>
    /// 单个可收集物品的数据。每种颜色 + 状态都对应一个独立资源，
    /// 采集时不会堆叠，也不会互相转换。
    /// </summary>
    [CreateAssetMenu(fileName = "Item_New", menuName = "江南染料/物品数据 ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("标识")]
        public string itemId = "DYE_RED_NORMAL";

        [Header("分类")]
        public DyeColor dyeColor = DyeColor.Red;
        public DyeCondition condition = DyeCondition.Normal;

        [Header("显示")]
        public string chineseName = "红色染料";
        public string stateText = "正常";
        /// <summary>染料原料植物名，例如「红花」。</summary>
        public string plantName = "红花";
        /// <summary>原料简介，展示在采集弹窗上。</summary>
        [TextArea]
        public string description = "花瓣可提取红色染料，为丝织物染出鲜亮的红色。";

        [Header("图标")]
        public Sprite icon;

        [Header("世界表现")]
        public Sprite worldSprite;

        public string FullName
        {
            get { return chineseName + "（" + stateText + "）"; }
        }
    }
}
