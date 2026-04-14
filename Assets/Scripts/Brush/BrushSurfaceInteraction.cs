using UnityEngine;
using Utils;

[RequireComponent(typeof(BrushColorController))]
public class BrushSurfaceInteraction : MonoBehaviour
{
    // Смешивание цветов
    
    private BrushColorController brushColorController;

    [SerializeField] LayerMask colorLayer;
    [SerializeField] private float colorCheckRadius = 0.2f;
    [SerializeField] private Transform colorCheck;
    PaintSurface lastSurface; // прошлая поверхость ( для смены цвета, при переходе на другую поверхность )


    private void Awake()
    {
        brushColorController = GetComponent<BrushColorController>();
        
        this.ValidateSerializedFields();
    }

    private void Update()
    {
        Collider2D hit = Physics2D.OverlapCircle(colorCheck.position, colorCheckRadius, colorLayer);

        if (hit == null) 
            return;
        
        hit.gameObject.TryGetComponent(out PaintSurface paintSurface);

        if (paintSurface == null || paintSurface == lastSurface)
            return;
            
        brushColorController.ApplyColor(paintSurface.SurfaceColor);
        lastSurface = paintSurface;
    }
    public void ResetSurfaceContact()
    {
        lastSurface = null;
    }
}
