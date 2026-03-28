using System;
using UnityEngine;
using Utils;

public class BrushColorController : MonoBehaviour
{
    [Header("Нынешний цвет")]
    [SerializeField] private Color currentColor = Color.white; // текущий цвет кисти
    public Color CurrentColor => currentColor;
 
    [SerializeField] private bool hasColor = false; // есть ли у кисти цвет
    public bool HasColor => hasColor;

    [Header("Спрайт рендерер кисти")]
    [SerializeField] private SpriteRenderer brushRenderer;

    private void Awake()
    {
        this.ValidateSerializedFields();
    }

    private void Start()
    {
        UpdateVisual();
    }

    public void ApplyColor(Color newColor) // для применения нового цвета
    {
        Debug.Log($"ApplyColor вызван. Новый цвет: {newColor}");

        if (!hasColor)
        {
            currentColor = newColor; // чисто берём новый цвет
            hasColor = true;
        }
        else 
        {
            // если цвет уже есть
            currentColor = MixColors(currentColor, newColor); // смешиваем старый и новый цвет
        }

        UpdateVisual();
    }

    private Color MixColors(Color firstColor, Color secondColor) // для смешивания цветов
    {
        float r = (firstColor.r + secondColor.r) * 0.5f; // на 2 делим, потому что в rgb максимальное значение 1,
        float g = (firstColor.g + secondColor.g) * 0.5f; // а при смешивании цветов может получиться число больше 1, 
        float b = (firstColor.b + secondColor.b) * 0.5f; // поэтому мы всё, что получится делим на 2

        return new Color(r, g, b, 1f);
    }

    private void UpdateVisual()
    {
        if (brushRenderer != null)
        {
            brushRenderer.color = currentColor;
        }
    }

    public void ClearColor()
    {
        hasColor = false; 
        currentColor = Color.white; 
        UpdateVisual();

        Debug.Log($"ClearColor вызван. currentColor = {currentColor}, hasColor = {hasColor}");
    }
}
