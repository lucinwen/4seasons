using UnityEngine;
using UnityEngine.UI;

namespace Qingming.Harvest
{
    /// <summary>
    /// 桑叶拾取触发器：挂在玩家子物体上，范围覆盖玩家身体和篮子上方。
    /// FallingMulberryLeaf 在 OnTriggerEnter2D 中查找该组件来判定被拾取。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LeafCollector : MonoBehaviour
    {
        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }
    }
}
