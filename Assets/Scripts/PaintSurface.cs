using UnityEngine;
using Utils;

[RequireComponent(typeof(SpriteRenderer))]
public class PaintSurface : MonoBehaviour
{
    private static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
    
    [SerializeField] private Color surfaceColor = Color.white;
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private bool syncGlowColorWithSurface = true;
    
    private SpriteRenderer spriteRenderer;
    private Material material;

    public Color SurfaceColor => surfaceColor;
    private void Awake()
    {
        if (syncGlowColorWithSurface == false) 
            return;
        
        this.ValidateSerializedFields();
            
        spriteRenderer = GetComponent<SpriteRenderer>();
        material = spriteRenderer.material;
        SetGlowColor(glowColor);
    }

    public void SetSurfaceColor(Color newColor)
    {
        surfaceColor = newColor;
        SetGlowColor(newColor);
    }
    
    private void SetGlowColor(Color newColor)
    {

        if (syncGlowColorWithSurface == false)
            return;
        
        material.GetColor(GlowColor);
        material.SetColor(GlowColor, newColor);
    }
}
