using System;
using UnityEngine;

namespace JiangnanDye
{
    /// <summary>
    /// 历史采集记录（与最终收下的物品严格分离）。
    /// 记录采集次数、收下次数、丢弃次数，以及五种颜色 × 两种状态的历史统计。
    /// </summary>
    public class DyeCollectionManager : MonoBehaviour
    {
        private readonly int[] normalCounts = new int[5];
        private readonly int[] damagedCounts = new int[5];

        public int TotalHarvests { get; private set; }
        public int AcceptedCount { get; private set; }
        public int DiscardedCount { get; private set; }

        public event Action StatsChanged;

        public void RecordHarvest(ItemData item)
        {
            TotalHarvests++;
            Raise();
        }

        public void RecordAccept(ItemData item)
        {
            AcceptedCount++;
            if (item != null)
            {
                int index = (int)item.dyeColor;
                if (item.condition == DyeCondition.Normal) normalCounts[index]++;
                else damagedCounts[index]++;
            }
            Raise();
        }

        public void RecordDiscard(ItemData item)
        {
            DiscardedCount++;
            Raise();
        }

        public int GetNormalCount(DyeColor color)
        {
            return normalCounts[(int)color];
        }

        public int GetDamagedCount(DyeColor color)
        {
            return damagedCounts[(int)color];
        }

        public string GetSummaryLine(DyeColor color)
        {
            return DyeColorUtil.ChineseName(color)
                   + "：正常 " + GetNormalCount(color) + " 件，坏损 " + GetDamagedCount(color) + " 件";
        }

        public void ResetStats()
        {
            TotalHarvests = 0;
            AcceptedCount = 0;
            DiscardedCount = 0;
            for (int i = 0; i < 5; i++)
            {
                normalCounts[i] = 0;
                damagedCounts[i] = 0;
            }
            Raise();
        }

        private void Raise()
        {
            if (StatsChanged != null) StatsChanged();
        }
    }
}
