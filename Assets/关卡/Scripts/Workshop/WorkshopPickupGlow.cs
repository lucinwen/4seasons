using System;
using UnityEngine;

/// <summary>A soft silhouette drawn behind an available world pickup or station.</summary>
internal sealed class WorkshopPickupGlow : MonoBehaviour
{
    private static readonly Vector2[] Directions =
    {
        Vector2.right, new Vector2(0.7071f, 0.7071f),
        Vector2.up, new Vector2(-0.7071f, 0.7071f),
        Vector2.left, new Vector2(-0.7071f, -0.7071f),
        Vector2.down, new Vector2(0.7071f, -0.7071f)
    };

    private static readonly int TextureId = Shader.PropertyToID("_MainTex");
    private static Material material;
    private SpriteRenderer source;
    private Func<bool> isAvailable;
    private float intensity = 1f;
    private MeshRenderer halo;
    private Mesh mesh;
    private MaterialPropertyBlock properties;
    private Sprite shape;
    private Vector2[] shapeVertices;
    private Vector3[] glowVertices;
    private Color32[] glowColors;
    private bool flipX;
    private bool flipY;
    private Vector2 radius;
    private float sourceAlpha = -1f;

    internal static void Bind(GameObject target, Func<bool> available, float strength = 1f)
    {
        if (target == null || available == null || !WorkshopProgress.TryGetSeason(target.scene.name, out _)) return;
        var sprite = target.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.drawMode != SpriteDrawMode.Simple) return;
        var glow = target.GetComponent<WorkshopPickupGlow>() ?? target.AddComponent<WorkshopPickupGlow>();
        glow.source = sprite;
        glow.isAvailable = available;
        glow.intensity = Mathf.Clamp01(strength);
    }

    private void LateUpdate()
    {
        bool show = source != null && source.enabled && source.sprite != null && source.color.a > 0f &&
            !WorkshopQuestGuide.IsOpen && !WorkshopQuestGuide.IsWorldOverlayActive && isAvailable != null && isAvailable();
        if (!show)
        {
            if (halo != null) halo.enabled = false;
            return;
        }

        if (halo == null && !CreateHalo()) return;
        float worldRadius = WorldRadius();
        Vector3 scale = transform.lossyScale;
        var localRadius = new Vector2(worldRadius / Mathf.Max(Mathf.Abs(scale.x), 0.001f),
            worldRadius / Mathf.Max(Mathf.Abs(scale.y), 0.001f));
        bool changed = shape != source.sprite;
        if (changed) ResetShape(source.sprite);
        if (changed || localRadius != radius || source.flipX != flipX || source.flipY != flipY ||
            !Mathf.Approximately(sourceAlpha, source.color.a * intensity))
            PositionHalo(localRadius);

        halo.sortingLayerID = source.sortingLayerID;
        halo.sortingOrder = Mathf.Max(short.MinValue, source.sortingOrder - 1);
        halo.enabled = true;
    }

    private static float WorldRadius()
    {
        var camera = Camera.main;
        if (camera == null || !camera.orthographic || camera.pixelHeight < 1) return 0.044f;
        // A few actual screen pixels, independent of the source sprite's scale.
        return Mathf.Clamp(camera.orthographicSize * 9.2f / camera.pixelHeight, 0.018f, 0.105f);
    }

    private bool CreateHalo()
    {
        if (material == null) material = Resources.Load<Material>("WorkshopPickupGlow");
        if (material == null) return false;

        var visual = new GameObject("Collectible Glow");
        visual.layer = gameObject.layer;
        visual.transform.SetParent(transform, false);
        mesh = new Mesh { name = "Collectible Glow Mesh" };
        mesh.MarkDynamic();
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        halo = visual.AddComponent<MeshRenderer>();
        halo.sharedMaterial = material;
        halo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        halo.receiveShadows = false;
        halo.enabled = false;
        properties = new MaterialPropertyBlock();
        return true;
    }

    private void ResetShape(Sprite sprite)
    {
        shape = sprite;
        shapeVertices = sprite.vertices;
        var sourceUv = sprite.uv;
        var sourceTriangles = sprite.triangles;
        int vertexCount = shapeVertices.Length;
        int copies = Directions.Length * 2;
        glowVertices = new Vector3[vertexCount * copies];
        glowColors = new Color32[glowVertices.Length];
        var uvs = new Vector2[glowVertices.Length];
        var triangles = new int[sourceTriangles.Length * copies];

        for (int i = 0; i < copies; i++)
        {
            Array.Copy(sourceUv, 0, uvs, i * vertexCount, vertexCount);
            for (int j = 0; j < sourceTriangles.Length; j++)
                triangles[i * sourceTriangles.Length + j] = sourceTriangles[j] + i * vertexCount;
        }

        mesh.Clear();
        mesh.indexFormat = glowVertices.Length > ushort.MaxValue
            ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = glowVertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        properties.SetTexture(TextureId, sprite.texture);
        halo.SetPropertyBlock(properties);
    }

    private void PositionHalo(Vector2 localRadius)
    {
        radius = localRadius;
        flipX = source.flipX;
        flipY = source.flipY;
        sourceAlpha = source.color.a * intensity;
        int count = shapeVertices.Length;
        for (int ring = 0; ring < 2; ring++)
        {
            float distance = ring == 0 ? 1f : 0.63f;
            var tint = new Color32(222, 196, 146,
                (byte)Mathf.RoundToInt(sourceAlpha * (ring == 0 ? 10f : 27f)));
            for (int direction = 0; direction < Directions.Length; direction++)
            {
                Vector2 shift = Vector2.Scale(Directions[direction], radius) * distance;
                int offset = (ring * Directions.Length + direction) * count;
                for (int i = 0; i < count; i++)
                {
                    Vector2 vertex = shapeVertices[i];
                    glowVertices[offset + i] = new Vector3(
                        (flipX ? -vertex.x : vertex.x) + shift.x,
                        (flipY ? -vertex.y : vertex.y) + shift.y, 0f);
                    glowColors[offset + i] = tint;
                }
            }
        }
        mesh.vertices = glowVertices;
        mesh.colors32 = glowColors;
        mesh.RecalculateBounds();
    }

    private void OnDisable()
    {
        if (halo != null) halo.enabled = false;
    }

    private void OnDestroy()
    {
        if (halo != null) Destroy(halo.gameObject);
        if (mesh != null) Destroy(mesh);
    }
}
