using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiXu.CocoonMatch
{
    [Serializable]
    public sealed class CocoonPattern
    {
        public string label;
        public Sprite sprite;
        [TextArea(2, 3)] public string harvestMessage;
    }

    [Serializable]
    public sealed class CocoonPlacement
    {
        public int pattern;
        public int layer;
        public Vector2 position;
    }

    [CreateAssetMenu(menuName = "时序小织匠/蔟上收茧关卡")]
    public sealed class CocoonMatchLevel : ScriptableObject
    {
        public float tileSize = 72f;
        public int seed = 20261116;
        public CocoonPattern[] patterns;
        public List<CocoonPlacement> placements = new List<CocoonPlacement>();
        [Tooltip("一条经过遮挡约束生成的通关序列；修改布局后需重新生成。")]
        public List<int> solution = new List<int>();

        public void ValidateData()
        {
            if (patterns == null || patterns.Length == 0 || placements.Count == 0)
                throw new InvalidOperationException("关卡缺少图案或图块。");
            if (tileSize <= 0) throw new InvalidOperationException("图块尺寸必须为正数。");
            var counts = new int[patterns.Length];
            foreach (var tile in placements)
            {
                if (tile.pattern < 0 || tile.pattern >= patterns.Length)
                    throw new InvalidOperationException("图块的图案编号越界。");
                counts[tile.pattern]++;
            }
            for (int i = 0; i < counts.Length; i++)
                if (patterns[i] == null || patterns[i].sprite == null || counts[i] < 3 || counts[i] % 3 != 0)
                    throw new InvalidOperationException("每种图案须有素材，并出现至少三张且数量是三的倍数。");
        }
    }
}
