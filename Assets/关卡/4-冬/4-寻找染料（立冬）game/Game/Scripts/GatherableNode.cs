using System.Collections.Generic;
using UnityEngine;

namespace JiangnanDye
{
    public enum ResourceType
    {
        Plant = 0,
        Stone = 1
    }

    /// <summary>
    /// 场景中一个可采集资源点。每个植物 / 石头都是独立 GameObject。
    /// 植物节点支持运行时随机好/坏品质，即使没有配置 DyeDefinition 也能根据
    /// 场景中已有的 ItemData 资源自动匹配对应的正常/坏损物品。
    /// </summary>
    public class GatherableNode : MonoBehaviour
    {
        public const float PlantInteractionRadius = 2f;

        /// <summary>(颜色, 状态) → ItemData，运行时从所有节点的 itemData 自动收集。</summary>
        private static Dictionary<long, ItemData> ItemLookupTable;

        [Header("标识")]
        public string resourceId = "PLANT_RED_NORMAL_01";
        public ResourceType resourceType = ResourceType.Plant;

        [Header("染料信息")]
        public DyeColor dyeColor = DyeColor.Red;
        public DyeCondition condition = DyeCondition.Normal;
        public ItemData itemData;
        public string displayName = "红色染料";
        [Tooltip("提供同一颜色的正常/坏损物品，重置时随机决定本次出现的品质。")]
        public DyeDefinition dyeDefinition;
        [Range(0f, 1f)]
        [Tooltip("重置为正常状态的概率；值越低越容易出现坏损植物。")]
        public float normalChance = 0.5f;

        [Header("采集状态")]
        public bool isCollected;
        /// <summary>本轮是否处于可采集状态（每种颜色的点位同一时间只激活一个）。</summary>
        public bool available = true;
        public float interactionRadius = 1.2f;

        [Header("渲染引用")]
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer highlight;
        public SpriteRenderer highlightGlow;
        public Sprite originalSprite;
        /// <summary>植物点位在地图上不显示任何图形（仅作为隐形检测点），石头不受影响。</summary>
        public bool hideVisual;

        [Header("石头专用")]
        public Sprite depletedSprite;

        [Header("石头自动吸附")]
        [Tooltip("石头节点启动时自动寻找最近的 Rock_*/Decor_Stone_* 装饰并吸附过去，隐藏 prefab 自带的小石头视觉。")]
        public bool autoSnapToSceneRock = true;
        [Tooltip("从物体名字中匹配太湖石的关键字，用 '|' 分隔。匹配到任一关键字的 GameObject 都会被视为候选。")]
        public string sceneRockNamePattern = "Rock|Decor_Stone|Taifu|Taihu";
        [Tooltip("只考虑该半径以内的候选太湖石，超出则放弃吸附。")]
        public float maxSnapDistance = 8f;
        [Tooltip("吸附后是否隐藏本节点自带的 SpriteRenderer 视觉（建议开启，让地图装饰太湖石成为唯一视觉）。")]
        public bool hideOwnSpriteAfterSnap = true;

        private Collider2D triggerCollider;
        /// <summary>Awake 时成功吸附到场景太湖石后置为 true，ResetNode 据此不再恢复自身 sprite。</summary>
        private bool snappedToSceneRock;

        /// <summary>
        /// 植物使用地图上黄圈的位置，石头使用触发器的底部中心。
        /// </summary>
        public Vector3 InteractionPoint
        {
            get
            {
                if (resourceType == ResourceType.Plant && highlight != null)
                    return highlight.transform.position;

                if (triggerCollider != null)
                {
                    Bounds b = triggerCollider.bounds;
                    return new Vector3(b.center.x, b.min.y, b.center.z);
                }
                return transform.position;
            }
        }

        public bool CanHarvest
        {
            get { return available && !isCollected; }
        }

        /// <summary>本轮点位激活 / 休眠：休眠时关闭触发器与高亮，逻辑上也不可采集。</summary>
        public void SetInteractable(bool on)
        {
            available = on;
            if (!on) SetHighlighted(false);
            if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null) triggerCollider.enabled = on;
        }

        /// <summary>按本轮随机结果写入品质与对应物品（点位视觉由地图承担）。</summary>
        public void ApplyRoundState(DyeCondition newCondition, ItemData item)
        {
            condition = newCondition;
            if (item != null)
            {
                itemData = item;
                displayName = item.chineseName;
            }
        }

        private static long MakeKey(DyeColor color, DyeCondition cond)
        {
            return ((long)color << 8) | (long)cond;
        }

        /// <summary>
        /// 从场景中所有节点的 itemData 收集一张 (颜色, 状态) → ItemData 的查找表，
        /// 供没有 DyeDefinition 的节点在 ResetNode 时匹配对应状态的物品。
        /// </summary>
        private static void EnsureLookupTable()
        {
            if (ItemLookupTable != null) return;
            ItemLookupTable = new Dictionary<long, ItemData>();
            var nodes = FindObjectsOfType<GatherableNode>();
            for (int i = 0; i < nodes.Length; i++)
            {
                ItemData data = nodes[i].itemData;
                if (data == null) continue;
                long key = MakeKey(data.dyeColor, data.condition);
                if (!ItemLookupTable.ContainsKey(key))
                {
                    ItemLookupTable[key] = data;
                }
            }
        }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (originalSprite == null && spriteRenderer != null) originalSprite = spriteRenderer.sprite;
            if (hideVisual && spriteRenderer != null) spriteRenderer.enabled = false;
            triggerCollider = GetComponent<Collider2D>();
            SetHighlighted(false);

            if (resourceType == ResourceType.Stone && autoSnapToSceneRock)
            {
                AutoSnapToSceneRock();
            }
        }

        /// <summary>
        /// 石头节点自动吸附：从场景里找名字匹配 Rock_/Decor_Stone_ 等关键字的装饰 GameObject，
        /// 选距离最近的一个，把自身位置对齐过去，并隐藏自带视觉，只留下 collider + 高亮圈。
        /// 找不到候选时保持原位不动。
        /// </summary>
        private void AutoSnapToSceneRock()
        {
            if (string.IsNullOrEmpty(sceneRockNamePattern)) return;
            string[] keywords = sceneRockNamePattern.Split('|');

            var allGOs = FindObjectsOfType<GameObject>();
            GameObject best = null;
            float bestDist = maxSnapDistance;

            for (int i = 0; i < allGOs.Length; i++)
            {
                GameObject go = allGOs[i];
                if (go == null || go == gameObject || go.scene != gameObject.scene) continue;
                string name = go.name;
                bool match = false;
                for (int k = 0; k < keywords.Length; k++)
                {
                    if (!string.IsNullOrEmpty(keywords[k]) && name.Contains(keywords[k]))
                    {
                        match = true;
                        break;
                    }
                }
                if (!match) continue;

                float d = Vector3.Distance(transform.position, go.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = go;
                }
            }

            if (best != null)
            {
                transform.position = best.transform.position;
                transform.rotation = best.transform.rotation;
                transform.localScale = best.transform.lossyScale;
                snappedToSceneRock = true;

                if (hideOwnSpriteAfterSnap)
                {
                    // 只隐藏 prefab 自带的小石头视觉，保留 Highlight 子物体 —— 玩家靠近时高亮圈仍然会出现
                    if (spriteRenderer != null) spriteRenderer.enabled = false;
                }
            }
        }

        public void SetHighlighted(bool on)
        {
            if (highlight != null) highlight.enabled = on;
            if (highlightGlow != null) highlightGlow.enabled = on;
        }

        /// <summary>采集：隐藏植物 / 切换石头为采空状态，并关闭触发器。</summary>
        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;
            SetHighlighted(false);

            if (spriteRenderer != null)
            {
                if (resourceType == ResourceType.Stone && depletedSprite != null)
                {
                    spriteRenderer.sprite = depletedSprite;
                }
                else
                {
                    spriteRenderer.enabled = false;
                }
            }

            if (triggerCollider != null) triggerCollider.enabled = false;
        }

        /// <summary>
        /// 重新开始游戏时恢复资源点。
        /// 植物节点无条件随机好/坏品质：有 DyeDefinition 时使用完整素材，
        /// 没有时也会根据场景 ItemData 查找表匹配对应状态的物品。
        /// </summary>
        public void ResetNode()
        {
            isCollected = false;
            available = true;

            if (resourceType == ResourceType.Plant)
            {
                // 无条件随机好/坏
                bool isNormal = Random.value < Mathf.Clamp01(normalChance);
                condition = isNormal ? DyeCondition.Normal : DyeCondition.Damaged;

                if (dyeDefinition != null)
                {
                    // 有完整配置 → 直接使用
                    itemData = dyeDefinition.GetItem(condition);
                    Sprite newSprite = dyeDefinition.GetSprite(condition);
                    if (newSprite != null)
                    {
                        originalSprite = newSprite;
                    }
                }
                else
                {
                    // 没有 DyeDefinition → 从场景查找表匹配对应颜色+状态的 ItemData
                    EnsureLookupTable();
                    long key = MakeKey(dyeColor, condition);
                    if (ItemLookupTable.ContainsKey(key))
                    {
                        itemData = ItemLookupTable[key];
                    }
                }

                // 如果最终还是没有 itemData，至少同步一下 displayName
                if (itemData != null)
                {
                    displayName = itemData.chineseName;
                }
            }

            if (spriteRenderer != null)
            {
                // 如果是已经吸附到太湖石的石头节点，不要再把自身 sprite 打开
                if (snappedToSceneRock && resourceType == ResourceType.Stone && hideOwnSpriteAfterSnap)
                {
                    spriteRenderer.enabled = false;
                }
                else
                {
                    spriteRenderer.enabled = !hideVisual;
                }
                if (originalSprite != null) spriteRenderer.sprite = originalSprite;
            }
            if (triggerCollider != null) triggerCollider.enabled = true;
            SetHighlighted(false);
        }
    }
}
