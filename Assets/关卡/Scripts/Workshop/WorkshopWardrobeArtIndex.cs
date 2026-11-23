using System;
using System.Collections.Generic;
using UnityEngine;

internal static class WorkshopWardrobeArtIndex
{
    [Serializable]
    internal sealed class Item
    {
        public string id;
        public string silkResource;
        public string originalDetailResource;
        public string silkDetailResource;
        public PixelRect detailCrop;
        public CameraAnchors arInitialAnchors;
        public string note;
    }

    [Serializable]
    internal struct PixelRect
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public Rect Bounds => new Rect(x, y, width, height);
    }

    [Serializable]
    internal sealed class CameraAnchors
    {
        public int[] neck;
        public int[] shoulderImageLeft;
        public int[] shoulderImageRight;

        internal bool TryGet(out Vector2 midpoint, out float shoulderWidth)
        {
            midpoint = default;
            shoulderWidth = 0f;
            if (shoulderImageLeft?.Length != 2 || shoulderImageRight?.Length != 2)
                return false;

            midpoint = new Vector2((shoulderImageLeft[0] + shoulderImageRight[0]) * 0.5f,
                (shoulderImageLeft[1] + shoulderImageRight[1]) * 0.5f);
            shoulderWidth = shoulderImageRight[0] - shoulderImageLeft[0];
            return shoulderWidth > 0f;
        }
    }

    [Serializable]
    private sealed class Index
    {
        public Item[] items;
    }

    private static Dictionary<string, Item> itemsById;

    internal static Item Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (itemsById == null)
        {
            itemsById = new Dictionary<string, Item>(StringComparer.Ordinal);
            var data = Resources.Load<TextAsset>("WorkshopFinish/SilkVariants/GarmentArtIndex");
            var index = data == null ? null : JsonUtility.FromJson<Index>(data.text);
            if (index?.items != null)
            {
                foreach (Item item in index.items)
                    if (!string.IsNullOrEmpty(item?.id))
                        itemsById[item.id] = item;
            }
        }
        return itemsById.TryGetValue(id, out Item row) ? row : null;
    }
}
