using System.Collections;
using UnityEngine;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private void ResetSpringChallenge()
    {
        if (observation == WorkshopObservation.GuyuLeaf)
            ResetGuyuOrder();
    }

    private static bool HasSpringBit(int mask, int bit) => (mask & (1 << bit)) != 0;

    private static int CountSpringBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            mask &= mask - 1;
            count++;
        }
        return count;
    }

    private Sprite SpringSprite(string resource)
    {
        if (!sprites.TryGetValue(resource, out var sprite))
        {
            sprite = Resources.Load<Sprite>(resource);
            sprites.Add(resource, sprite);
        }
        return sprite;
    }

    private Sprite SpringSummerArt(string asset) => SpringSprite("WorkshopGameArt/" + asset);

    private void AnimateSpringSummerSprite(Image picture, Vector2 offset, float scale, float duration, float lift = 0f)
    {
        if (picture != null && picture.sprite != null)
            StartCoroutine(SpringSummerSpriteMotion(picture.rectTransform, offset, scale, duration, lift));
    }

    private static IEnumerator SpringSummerSpriteMotion(RectTransform picture, Vector2 offset, float scale,
        float duration, float lift)
    {
        Vector2 resting = picture.anchoredPosition;
        for (float time = 0f; picture != null && picture.gameObject.activeInHierarchy && time < duration;
             time += Time.unscaledDeltaTime)
        {
            float progress = time / duration;
            float remaining = 1f - Mathf.SmoothStep(0f, 1f, progress);
            picture.anchoredPosition = resting + offset * remaining + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * lift);
            picture.localScale = Vector3.one * Mathf.Lerp(1f, scale, remaining);
            yield return null;
        }
        if (picture == null) yield break;
        picture.anchoredPosition = resting;
        picture.localScale = Vector3.one;
    }
}
