using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundRevealController : MonoBehaviour
{
    private const int MaxRevealShapes = 16;
    private const int MaxRevealVertices = 192;
    private const int MaxVerticesPerShape = 24;

    private static readonly int GlobalRevealId = Shader.PropertyToID("_GlobalReveal");
    private static readonly int SoftEdgeId = Shader.PropertyToID("_SoftEdge");
    private static readonly int RevealShapeCountId = Shader.PropertyToID("_RevealShapeCount");
    private static readonly int RevealShapeMetaId = Shader.PropertyToID("_RevealShapeMeta");
    private static readonly int RevealVerticesId = Shader.PropertyToID("_RevealVertices");

    public bool IsRevealing { get; private set; }

    [Header("Target")]
    [SerializeField]
    private Camera targetCamera;

    [Header("Sources")]
    [SerializeField]
    private Transform revealSourcesRoot;

    [Header("Reveal Shape")]
    [SerializeField]
    private float glowRadiusPixels = 22f;

    [SerializeField]
    private float softEdgePixels = 10f;

    [SerializeField]
    private float revealStrength = 1f;

    [Header("Pulse")]
    [SerializeField]
    private bool pulseEnabled = true;

    [SerializeField]
    private float pulseSpeed = 1.8f;

    [SerializeField]
    private float pulseAmplitude = 0.15f;

    [Header("Final Reveal")]
    [SerializeField]
    private float globalReveal = 0f;
    [SerializeField]
    private float revealDuration = 4f;


    private readonly Vector4[] revealShapeMeta = new Vector4[MaxRevealShapes];
    private readonly Vector4[] revealVertices = new Vector4[MaxRevealVertices];
    private readonly List<PaintSurface> sourceBuffer = new(MaxRevealShapes);
    private readonly List<Vector2> localPointBuffer = new(32);
    private readonly Vector3[] boundsCornerBuffer = new Vector3[4];
    private MaterialPropertyBlock propertyBlock;
    private SpriteRenderer spriteRenderer;
    private int previousShapeCount;
    private int previousVertexCount;

    private void Awake()
    {
        this.ValidateSerializedFields();

        propertyBlock = new MaterialPropertyBlock();
        spriteRenderer = GetComponent<SpriteRenderer>();
        CollectRevealSources();
    }

    private void OnEnable()
    {
        ApplyReveal();
    }

    private void LateUpdate()
    {
        ApplyReveal();
    }

    [ContextMenu("Play Final Reveal")]
    public void PlayFinalReveal(float _finishRevealValue, float _finalRevealDuration)
    {
        IsRevealing = true;
        StopAllCoroutines();
        StartCoroutine(AnimateGlobalReveal(_finishRevealValue, _finalRevealDuration));
    }

    public void SetGlobalReveal(float value)
    {
        globalReveal = Mathf.Clamp01(value);
    }

    private IEnumerator AnimateGlobalReveal(float targetValue, float duration)
    {
        IsRevealing = true;
        duration = Mathf.Max(0.01f, duration);
        float startValue = globalReveal;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            globalReveal = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        globalReveal = targetValue;
    }

    private void CollectRevealSources()
    {
        sourceBuffer.Clear();

        PaintSurface[] surfaces = revealSourcesRoot.GetComponentsInChildren<PaintSurface>(true);

        for (int i = 0; i < surfaces.Length && sourceBuffer.Count < MaxRevealShapes; i++)
        {
            if (surfaces[i] != null)
                sourceBuffer.Add(surfaces[i]);
        }
    }

    private void ApplyReveal()
    {
        int shapeCount = 0;
        int vertexCount = 0;
        float baseHalo = PixelsToViewport(glowRadiusPixels);
        float softEdge = PixelsToViewport(softEdgePixels);

        for (int i = 0; i < sourceBuffer.Count && shapeCount < MaxRevealShapes; i++)
        {
            PaintSurface source = sourceBuffer[i];

            if (source == null)
                continue;

            Collider2D sourceCollider = source.Collider;

            if (sourceCollider == null)
                continue;

            float pulseScale = 1f;

            if (pulseEnabled)
            {
                float pulse = Mathf.Sin((Time.time * pulseSpeed) + (i * 0.7f)) * pulseAmplitude;
                pulseScale = Mathf.Max(0.2f, 1f + pulse);
            }

            AppendColliderShape(sourceCollider, baseHalo * pulseScale, ref shapeCount, ref vertexCount);
        }

        for (int i = shapeCount; i < previousShapeCount; i++)
            revealShapeMeta[i] = Vector4.zero;

        for (int i = vertexCount; i < previousVertexCount; i++)
            revealVertices[i] = Vector4.zero;

        propertyBlock.SetFloat(GlobalRevealId, globalReveal);
        propertyBlock.SetFloat(SoftEdgeId, Mathf.Max(0.0001f, softEdge));
        propertyBlock.SetFloat(RevealShapeCountId, shapeCount);
        propertyBlock.SetVectorArray(RevealShapeMetaId, revealShapeMeta);
        propertyBlock.SetVectorArray(RevealVerticesId, revealVertices);
        spriteRenderer.SetPropertyBlock(propertyBlock);

        previousShapeCount = shapeCount;
        previousVertexCount = vertexCount;
    }

    private void AppendColliderShape(Collider2D sourceCollider, float halo, ref int shapeCount, ref int vertexCount)
    {
        if (shapeCount >= MaxRevealShapes || vertexCount >= MaxRevealVertices)
            return;

        switch (sourceCollider)
        {
            case PolygonCollider2D polygonCollider:
                AppendPolygonCollider(polygonCollider, halo, ref shapeCount, ref vertexCount);
                return;

            case BoxCollider2D boxCollider:
                BuildBoxLocalPath(boxCollider, localPointBuffer);
                AppendLocalPath(localPointBuffer, boxCollider.transform, halo, ref shapeCount, ref vertexCount);
                return;

            case CircleCollider2D circleCollider:
                BuildCircleLocalPath(circleCollider, localPointBuffer, MaxVerticesPerShape);
                AppendLocalPath(localPointBuffer, circleCollider.transform, halo, ref shapeCount, ref vertexCount);
                return;

            case CapsuleCollider2D capsuleCollider:
                BuildCapsuleLocalPath(capsuleCollider, localPointBuffer, MaxVerticesPerShape);
                AppendLocalPath(localPointBuffer, capsuleCollider.transform, halo, ref shapeCount, ref vertexCount);
                return;

            default:
                AppendBoundsShape(sourceCollider.bounds, halo, ref shapeCount, ref vertexCount);
                return;
        }
    }

    private void AppendPolygonCollider(PolygonCollider2D polygonCollider, float halo, ref int shapeCount,
        ref int vertexCount)
    {
        int pathCount = polygonCollider.pathCount;

        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            if (shapeCount >= MaxRevealShapes || vertexCount >= MaxRevealVertices)
                return;

            localPointBuffer.Clear();
            polygonCollider.GetPath(pathIndex, localPointBuffer);

            if (localPointBuffer.Count < 3)
                continue;

            Vector2 offset = polygonCollider.offset;

            for (int i = 0; i < localPointBuffer.Count; i++)
                localPointBuffer[i] += offset;

            AppendLocalPath(localPointBuffer, polygonCollider.transform, halo, ref shapeCount, ref vertexCount);
        }
    }

    private void AppendLocalPath(
        List<Vector2> localPath,
        Transform sourceTransform,
        float halo,
        ref int shapeCount,
        ref int vertexCount)
    {
        if (localPath == null || localPath.Count < 3 || shapeCount >= MaxRevealShapes)
            return;

        int availableVertices = MaxRevealVertices - vertexCount;
        int vertexTargetCount = Mathf.Min(localPath.Count, MaxVerticesPerShape, availableVertices);

        if (vertexTargetCount < 3)
            return;

        int startVertex = vertexCount;
        int writtenVertices = 0;

        for (int i = 0; i < vertexTargetCount; i++)
        {
            int sourceIndex = localPath.Count == vertexTargetCount
                ? i
                : Mathf.FloorToInt((i * (float)localPath.Count) / vertexTargetCount);

            Vector2 localPoint = localPath[sourceIndex];
            Vector3 worldPoint = sourceTransform.TransformPoint(localPoint);
            Vector3 viewportPoint = targetCamera.WorldToViewportPoint(worldPoint);

            if (viewportPoint.z <= 0f)
                continue;

            revealVertices[vertexCount] = new Vector4(viewportPoint.x, viewportPoint.y, 0f, 0f);
            vertexCount++;
            writtenVertices++;
        }

        if (writtenVertices < 3)
        {
            vertexCount = startVertex;
            return;
        }

        revealShapeMeta[shapeCount] = new Vector4(startVertex, writtenVertices, halo, revealStrength);
        shapeCount++;
    }

    private void AppendBoundsShape(Bounds bounds, float halo, ref int shapeCount, ref int vertexCount)
    {
        if (shapeCount >= MaxRevealShapes || vertexCount + 4 > MaxRevealVertices)
            return;

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        boundsCornerBuffer[0] = center + new Vector3(-extents.x, -extents.y, 0f);
        boundsCornerBuffer[1] = center + new Vector3(-extents.x, extents.y, 0f);
        boundsCornerBuffer[2] = center + new Vector3(extents.x, extents.y, 0f);
        boundsCornerBuffer[3] = center + new Vector3(extents.x, -extents.y, 0f);

        int startVertex = vertexCount;

        for (int i = 0; i < boundsCornerBuffer.Length; i++)
        {
            Vector3 viewport = targetCamera.WorldToViewportPoint(boundsCornerBuffer[i]);

            if (viewport.z <= 0f)
            {
                vertexCount = startVertex;
                return;
            }

            revealVertices[vertexCount] = new Vector4(viewport.x, viewport.y, 0f, 0f);
            vertexCount++;
        }

        revealShapeMeta[shapeCount] = new Vector4(startVertex, 4, halo, revealStrength);
        shapeCount++;
    }

    private static void BuildBoxLocalPath(BoxCollider2D boxCollider, List<Vector2> output)
    {
        output.Clear();
        Vector2 half = boxCollider.size * 0.5f;
        Vector2 offset = boxCollider.offset;

        output.Add(offset + new Vector2(-half.x, -half.y));
        output.Add(offset + new Vector2(-half.x, half.y));
        output.Add(offset + new Vector2(half.x, half.y));
        output.Add(offset + new Vector2(half.x, -half.y));
    }

    private static void BuildCircleLocalPath(CircleCollider2D circleCollider, List<Vector2> output, int pointCount)
    {
        output.Clear();
        pointCount = Mathf.Max(6, pointCount);
        float radius = Mathf.Max(0.0001f, circleCollider.radius);
        Vector2 offset = circleCollider.offset;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = (i / (float)pointCount) * Mathf.PI * 2f;
            Vector2 local = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            output.Add(offset + local);
        }
    }

    private static void BuildCapsuleLocalPath(CapsuleCollider2D capsuleCollider, List<Vector2> output, int pointCount)
    {
        output.Clear();
        pointCount = Mathf.Max(8, pointCount);

        Vector2 size = capsuleCollider.size;
        Vector2 offset = capsuleCollider.offset;
        bool vertical = capsuleCollider.direction == CapsuleDirection2D.Vertical;

        int halfSegments = Mathf.Max(4, pointCount / 2);

        if (vertical)
        {
            float radius = Mathf.Max(0.0001f, size.x * 0.5f);
            float bodyHalfHeight = Mathf.Max(0f, (size.y * 0.5f) - radius);
            Vector2 topCenter = offset + new Vector2(0f, bodyHalfHeight);
            Vector2 bottomCenter = offset + new Vector2(0f, -bodyHalfHeight);

            for (int i = 0; i < halfSegments; i++)
            {
                float t = i / (float)(halfSegments - 1);
                float angle = Mathf.Lerp(0f, Mathf.PI, t);
                output.Add(topCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            for (int i = 0; i < halfSegments; i++)
            {
                float t = i / (float)(halfSegments - 1);
                float angle = Mathf.Lerp(Mathf.PI, Mathf.PI * 2f, t);
                output.Add(bottomCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            return;
        }

        float horizontalRadius = Mathf.Max(0.0001f, size.y * 0.5f);
        float bodyHalfWidth = Mathf.Max(0f, (size.x * 0.5f) - horizontalRadius);
        Vector2 rightCenter = offset + new Vector2(bodyHalfWidth, 0f);
        Vector2 leftCenter = offset + new Vector2(-bodyHalfWidth, 0f);

        for (int i = 0; i < halfSegments; i++)
        {
            float t = i / (float)(halfSegments - 1);
            float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, t);
            output.Add(rightCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * horizontalRadius);
        }

        for (int i = 0; i < halfSegments; i++)
        {
            float t = i / (float)(halfSegments - 1);
            float angle = Mathf.Lerp(Mathf.PI * 0.5f, Mathf.PI * 1.5f, t);
            output.Add(leftCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * horizontalRadius);
        }
    }

    private float PixelsToViewport(float pixels)
    {
        int width = Mathf.Max(1, targetCamera.pixelWidth);
        int height = Mathf.Max(1, targetCamera.pixelHeight);
        int minDim = Mathf.Max(1, Mathf.Min(width, height));
        return Mathf.Max(0f, pixels / minDim);
    }
}
