using System;
using System.Collections.Generic;
using UnityEngine;

namespace JiangnanDye
{
    /// <summary>
    /// 玩家实际收下的物品清单。容量固定 5 件，不做堆叠、不做替换。
    /// </summary>
    public class CollectionInventory : MonoBehaviour
    {
        public const int MAX_CAPACITY = 5;

        [SerializeField]
        private List<ItemData> collectedItems = new List<ItemData>();

        public IReadOnlyList<ItemData> Items
        {
            get { return collectedItems; }
        }

        public int Count
        {
            get { return collectedItems.Count; }
        }

        public bool IsFull
        {
            get { return collectedItems.Count >= MAX_CAPACITY; }
        }

        public event Action Changed;

        public bool TryAdd(ItemData item)
        {
            if (item == null) return false;
            if (collectedItems.Count >= MAX_CAPACITY) return false;
            collectedItems.Add(item);
            if (Changed != null) Changed();
            return true;
        }

        public void Clear()
        {
            collectedItems.Clear();
            if (Changed != null) Changed();
        }
    }
}
