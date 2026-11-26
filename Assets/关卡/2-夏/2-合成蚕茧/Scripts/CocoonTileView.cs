using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShiXu.CocoonMatch
{
    public sealed class CocoonTileView : MonoBehaviour
    {
        public Image frame;
        public Image icon;
        public Button button;
        public CanvasGroup group;
        [SerializeField] private int tileId;
        public int TileId => tileId;
        public RectTransform Rect => (RectTransform)transform;

        public void Configure(int id, CocoonPattern pattern, Action<int> clicked)
        {
            tileId = id;
            icon.sprite = pattern.sprite;
            gameObject.name = $"Tile_{id:00}_{pattern.label}";
            button.onClick.RemoveAllListeners();
            if (clicked != null) button.onClick.AddListener(() => clicked(tileId));
            group.alpha = 1;
            transform.localScale = Vector3.one;
        }

        public void SetState(bool exposed, bool inTray, bool busy)
        {
            button.interactable = exposed && !inTray && !busy;
            // Blocked cards still intercept rays: their exposed corners cannot click through.
            frame.raycastTarget = true;
            frame.color = exposed || inTray ? Color.white : new Color(.68f,.72f,.65f,1f);
            var shadow = frame.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = true;
            icon.color = exposed || inTray ? Color.white : new Color(.55f, .61f, .55f, 1);
        }
    }
}
