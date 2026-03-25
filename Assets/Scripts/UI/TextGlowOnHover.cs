using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextGlowOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI text;
    private Material material;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        material = text.fontMaterial;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        material.SetFloat("_GlowPower", 0.5f); // включаем свечение
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        material.SetFloat("_GlowPower", 0f);
    }
}
