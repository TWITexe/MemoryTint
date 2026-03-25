using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class PaintSurface : MonoBehaviour
{
    private static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
    private const string LocalLightName = "PlatformLocalLight2D";

    [SerializeField] private Color surfaceColor = Color.white;
    [SerializeField] private bool syncGlowColorWithSurface = true;

    [Header("Background Light")]
    [SerializeField] private bool useLocalLight = true;
    [SerializeField] private bool createLocalLightIfMissing = true;
    [SerializeField] private float localLightIntensity = 0.95f;
    [SerializeField] private float localLightInnerRadius = 0.35f;
    [SerializeField] private float localLightOuterRadius = 2.8f;
    [SerializeField] private float localLightFalloff = 0.8f;
    [SerializeField] private Vector3 localLightOffset = new Vector3(0f, 0.15f, 0f);

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Light2D localLight;

    public Color SurfaceColor => surfaceColor;
    public Collider2D Collider { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Collider = GetComponent<Collider2D>();
        propertyBlock = new MaterialPropertyBlock();

        if (useLocalLight) 
            ConfigureLocalLight();

        ApplyVisuals(surfaceColor);
    }

    public void SetSurfaceColor(Color newColor)
    {
        surfaceColor = newColor;
        ApplyVisuals(newColor);
    }

    private void ApplyVisuals(Color newColor)
    {
        if (syncGlowColorWithSurface)
        {
            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(GlowColor, newColor);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }

        if (useLocalLight && localLight != null)
        {
            localLight.color = newColor;
            localLight.intensity = localLightIntensity;
        }
    }

    private void ConfigureLocalLight()
    {
        localLight = GetComponentInChildren<Light2D>(true);

        if (localLight == null && createLocalLightIfMissing)
        {
            GameObject lightObject = new GameObject(LocalLightName);
            lightObject.transform.SetParent(transform, false);
            localLight = lightObject.AddComponent<Light2D>();
        }

        if (localLight == null)
            return;

        localLight.lightType = Light2D.LightType.Point;
        localLight.pointLightInnerRadius = Mathf.Max(0f, localLightInnerRadius);
        localLight.pointLightOuterRadius = Mathf.Max(localLight.pointLightInnerRadius + 0.05f, localLightOuterRadius);
        localLight.falloffIntensity = Mathf.Clamp01(localLightFalloff);
        localLight.intensity = localLightIntensity;
        localLight.transform.localPosition = localLightOffset;
        localLight.shadowsEnabled = false;
        localLight.enabled = true;
    }
}
