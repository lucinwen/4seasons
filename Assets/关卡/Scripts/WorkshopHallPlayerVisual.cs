using System;
using UnityEngine;

/// <summary>Runtime walk-cycle renderer for the playable main hall.</summary>
public sealed class WorkshopHallPlayerVisual : MonoBehaviour
{
    private const float WalkSoleHeightPixels = 30f;
    public float walkFramesPerSecond = 10f;
    public float runFramesPerSecond = 15f;

    private Sprite[] frames;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private float frameTimer;
    private int frameIndex;
    private bool initialized;

    public void Initialize(float targetWorldHeight)
    {
        if (initialized) return;
        initialized = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        // The main hall initializes the visual before activating the player hierarchy.
        body = GetComponentInParent<Rigidbody2D>(includeInactive: true);
        frames = LoadFrames(out float footOffset);
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;

        Sprite first = frames[0];
        float scale = targetWorldHeight / Mathf.Max(0.001f, first.bounds.size.y);
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.localPosition = Vector3.up * (footOffset * scale);
        spriteRenderer.sprite = first;
        spriteRenderer.sortingOrder = 40;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null || body == null) return;

        float speed = Mathf.Abs(body.velocity.x);
        if (speed < 0.08f)
        {
            frameIndex = 0;
            frameTimer = 0f;
            spriteRenderer.sprite = frames[0];
            return;
        }

        float fps = speed > 4.2f ? runFramesPerSecond : walkFramesPerSecond;
        frameTimer += Time.deltaTime * fps;
        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            frameIndex = (frameIndex + 1) % frames.Length;
        }
        spriteRenderer.sprite = frames[frameIndex];
    }

    private static Sprite[] LoadFrames(out float footOffset)
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>("WorkshopMainHall");
        Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
        footOffset = 0f;
        if (textures.Length == 0)
        {
            Sprite fallback = Resources.Load<Sprite>("WorkshopFinish/FittingHero");
            if (fallback != null) footOffset = -fallback.bounds.min.y;
            return fallback != null ? new[] { fallback } : Array.Empty<Sprite>();
        }

        var sprites = new Sprite[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i];
            sprites[i] = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, WalkSoleHeightPixels / texture.height), 64f, 0, SpriteMeshType.FullRect);
            sprites[i].name = texture.name;
        }
        return sprites;
    }
}
