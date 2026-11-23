using UnityEngine;

namespace JiangnanDye
{
    /// <summary>负责生成待处理物品并交给弹窗展示。</summary>
    public class ItemRewardManager : MonoBehaviour
    {
        public ItemRewardPopup popup;

        public ItemData PendingItem { get; private set; }

        public bool HasPending
        {
            get { return PendingItem != null; }
        }

        public void Show(ItemData item)
        {
            if (item == null) return;
            PendingItem = item;
            if (popup != null) popup.Show(item);
        }

        public ItemData TakePending()
        {
            ItemData item = PendingItem;
            PendingItem = null;
            return item;
        }

        public void Clear()
        {
            PendingItem = null;
        }
    }
}
