using System.Collections;
using UnityEngine;

namespace JiangnanDye
{
    /// <summary>采集特效：在资源点位置迸发星屑。</summary>
    public class CollectEffect : MonoBehaviour
    {
        public Sprite sparkleSprite;
        public Sprite glowSprite;
        public int sparkCount = 10;

        public void Play(Vector3 worldPosition, Color tint)
        {
            StartCoroutine(Run(worldPosition, tint));
        }

        private IEnumerator Run(Vector3 origin, Color tint)
        {
            GameObject root = new GameObject("CollectFX");
            root.transform.SetParent(transform, false);
            root.transform.position = origin;

            Vector3[] dirs = new Vector3[sparkCount];
            SpriteRenderer[] sparks = new SpriteRenderer[sparkCount];
            float[] speeds = new float[sparkCount];
            float[] scales = new float[sparkCount];

            for (int i = 0; i < sparkCount; i++)
            {
                float angle = (360f / sparkCount) * i + Random.Range(-12f, 12f);
                float rad = angle * Mathf.Deg2Rad;
                dirs[i] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad) * 0.7f + 0.4f, 0f).normalized;
                speeds[i] = Random.Range(1.6f, 3.1f);
                scales[i] = Random.Range(0.12f, 0.24f);

                GameObject go = new GameObject("Spark");
                go.layer = gameObject.layer;
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0f, 0.25f), 0f);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sparkleSprite;
                sr.sortingOrder = 60;
                sr.color = Color.Lerp(Color.white, tint, 0.55f);
                sparks[i] = sr;
            }

            GameObject glowGo = new GameObject("Glow");
            glowGo.layer = gameObject.layer;
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = Vector3.zero;
            SpriteRenderer glow = glowGo.AddComponent<SpriteRenderer>();
            glow.sprite = glowSprite;
            glow.sortingOrder = 55;
            glow.color = new Color(tint.r, tint.g, tint.b, 0.55f);

            float duration = 0.62f;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float p = Mathf.Clamp01(time / duration);

                for (int i = 0; i < sparkCount; i++)
                {
                    if (sparks[i] == null) continue;
                    sparks[i].transform.localPosition += dirs[i] * speeds[i] * Time.deltaTime;
                    sparks[i].transform.localScale = Vector3.one * scales[i] * (1f - p * 0.55f);
                    Color c = sparks[i].color;
                    c.a = 1f - p;
                    sparks[i].color = c;
                }

                if (glow != null)
                {
                    float gs = Mathf.Lerp(0.6f, 1.5f, p);
                    glow.transform.localScale = new Vector3(gs, gs, 1f);
                    Color gc = glow.color;
                    gc.a = (1f - p) * 0.55f;
                    glow.color = gc;
                }

                yield return null;
            }

            Destroy(root);
        }
    }
}
