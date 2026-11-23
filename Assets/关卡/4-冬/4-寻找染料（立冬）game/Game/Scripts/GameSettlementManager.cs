using UnityEngine;

namespace JiangnanDye
{
    /// <summary>监听收集数量，达到 5 件时结束探索并打开结算界面。</summary>
    public class GameSettlementManager : MonoBehaviour
    {
        public CollectionInventory inventory;
        public GameManager gameManager;

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += HandleInventoryChanged;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= HandleInventoryChanged;
        }

        private void HandleInventoryChanged()
        {
            if (inventory == null || gameManager == null) return;
            if (inventory.Count >= CollectionInventory.MAX_CAPACITY && gameManager.State == GameState.RewardPrompt)
            {
                gameManager.EnterSettlement();
            }
        }
    }
}
