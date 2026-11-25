using UnityEngine;

/// <summary>
/// 喂蚕小游戏中的一片桑叶：只记录初始位置，拖拽逻辑由 FeedOverlayManager 统一处理。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FeedLeafDraggable : MonoBehaviour
{
    /// <summary>未拖拽时停留的世界坐标</summary>
    public Vector3 homePosition;

    /// <summary>是否已被拖到蚕匾上（已消耗）</summary>
    public bool consumed;

    private SpriteRenderer _sr;

    public SpriteRenderer Sr
    {
        get
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            return _sr;
        }
    }
}
