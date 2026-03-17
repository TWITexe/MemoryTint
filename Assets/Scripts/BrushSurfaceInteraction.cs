using UnityEngine;

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
    }

    private void Update()
    {
        Collider2D hit = Physics2D.OverlapCircle(colorCheck.position, colorCheckRadius, colorLayer);
        PaintSurface paintSurface = hit.gameObject.GetComponent<PaintSurface>();        
        if (hit != null && paintSurface != null && paintSurface != lastSurface)
        {        
            brushColorController.ApplyColor(paintSurface.SurfaceColor);
            lastSurface = paintSurface;
        }
    }
}