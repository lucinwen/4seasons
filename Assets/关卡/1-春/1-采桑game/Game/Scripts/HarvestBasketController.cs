using System.Collections;
using UnityEngine;

namespace Qingming.Harvest
{
    /// <summary>
    /// 采桑篮控制器：4 种形态 Sprite 切换，跟随玩家并在切换时播放轻微弹动。
    /// 自身在 LateUpdate 中自动查找并跟随玩家，无需额外组件。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HarvestBasketController : MonoBehaviour
    {
        [Header("篮子形态（按采集数量区间）")]
        [Tooltip("[0]=0-1片, [1]=2-3片, [2]=4-5片, [3]=6-8片")]
        public Sprite[] basketSprites = new Sprite[4];

        [Header("弹动动画")]
        public float bounceDuration = 0.15f;
        public float bounceScale = 1.15f;

        [Header("跟随目标（自动查找名为 Player 的物体）")]
        public Transform playerTarget;

        private SpriteRenderer _sr;
        private Coroutine _bounceRoutine;
        private Vector3 _baseScale;
        private HarvestPlayerController _playerCtrl;
        private Vector3 _offsetFromPlayer;
        private bool _offsetSnapshotted;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            // 保留外部已设置的初始形态（Awake 时 basketSprites 可能尚未赋值）
            if (_sr.sprite == null && basketSprites != null && basketSprites.Length > 0 &&
                basketSprites[0] != null)
            {
                _sr.sprite = basketSprites[0];
            }
        }

        private void LateUpdate()
        {
            EnsurePlayerRef();
            if (playerTarget == null) return;

            // 第一次 LateUpdate 时记录初始偏移
            if (!_offsetSnapshotted)
            {
                _offsetFromPlayer = transform.position - playerTarget.position;
                _offsetFromPlayer.z = 0f;
                _offsetSnapshotted = true;
            }

            Vector3 worldPos = playerTarget.position + _offsetFromPlayer;
            worldPos.z = transform.position.z;
            transform.position = worldPos;
        }

        private void EnsurePlayerRef()
        {
            if (playerTarget != null) return;
            var go = GameObject.Find("Player");
            if (go != null)
            {
                playerTarget = go.transform;
                _playerCtrl = go.GetComponent<HarvestPlayerController>();
            }
        }

        /// <summary>根据当前已采集数量选择篮子 Sprite 并播放切换动画。</summary>
        public void UpdateBasketSprite(int collectedCount)
        {
            if (_sr == null || basketSprites == null || basketSprites.Length == 0) return;

            int idx;
            if (collectedCount <= 1) idx = 0;
            else if (collectedCount <= 3) idx = 1;
            else if (collectedCount <= 5) idx = 2;
            else idx = 3;

            if (idx >= basketSprites.Length) idx = basketSprites.Length - 1;
            if (basketSprites[idx] == null) return;

            if (_sr.sprite != basketSprites[idx])
            {
                _sr.sprite = basketSprites[idx];
                PlayBounce();
            }
        }

        /// <summary>让篮子保持与玩家的初始相对位置，稳定跟随。</summary>
        public void FollowPlayer(Transform playerTransform)
        {
            if (playerTransform == null) return;

            Vector3 worldPos = playerTransform.position + _offsetFromPlayer;
            worldPos.z = transform.position.z;
            transform.position = worldPos;
        }

        private void PlayBounce()
        {
            if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
            _bounceRoutine = StartCoroutine(BounceCoroutine());
        }

        private IEnumerator BounceCoroutine()
        {
            float t = 0f;
            Vector3 startScale = _baseScale;
            Vector3 targetScale = _baseScale * bounceScale;
            while (t < bounceDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / bounceDuration);
                // 上半程放大，下半程缩回
                float k = Mathf.Sin(p * Mathf.PI);
                transform.localScale = Vector3.Lerp(startScale, targetScale, k);
                yield return null;
            }
            transform.localScale = _baseScale;
        }
    }
}
