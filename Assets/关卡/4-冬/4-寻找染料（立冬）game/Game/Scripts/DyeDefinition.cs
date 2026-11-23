using UnityEngine;

namespace JiangnanDye
{
    /// <summary>单种颜色染料的完整数据（正常 / 坏损两套素材与物品）。</summary>
    [CreateAssetMenu(fileName = "Dye_New", menuName = "江南染料/染料定义 DyeDefinition")]
    public class DyeDefinition : ScriptableObject
    {
        public DyeColor color = DyeColor.Red;
        public string chineseName = "红色染料";

        [Header("植物素材")]
        public Sprite normalSprite;
        public Sprite damagedSprite;

        [Header("图标")]
        public Sprite normalIcon;
        public Sprite damagedIcon;

        [Header("物品数据")]
        public ItemData normalItemData;
        public ItemData damagedItemData;

        public Sprite GetSprite(DyeCondition condition)
        {
            return condition == DyeCondition.Normal ? normalSprite : damagedSprite;
        }

        public Sprite GetIcon(DyeCondition condition)
        {
            return condition == DyeCondition.Normal ? normalIcon : damagedIcon;
        }

        public ItemData GetItem(DyeCondition condition)
        {
            return condition == DyeCondition.Normal ? normalItemData : damagedItemData;
        }
    }
}
