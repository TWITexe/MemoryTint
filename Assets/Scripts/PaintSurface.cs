using UnityEngine;

public class PaintSurface : MonoBehaviour
{
    [SerializeField] private Color surfaceColor = Color.white;

    public Color SurfaceColor => surfaceColor;
}
