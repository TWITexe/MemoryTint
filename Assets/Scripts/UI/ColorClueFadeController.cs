using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ColorClueFadeController : MonoBehaviour
{
    [SerializeField] private BrushColorController brushColorController;
    [SerializeField] private Color targetColor = new Color32(255, 150, 150, 255);
    [SerializeField, Min(0f)] private float delayBeforeFade = 10f;
    [SerializeField, Min(0f)] private float fadeDuration = 1.5f;

    private SpriteRenderer[] sprites;
    private Graphic[] graphics;
    private float[] spriteAlphas;
    private float[] graphicAlphas;
    private bool colorMatched;
    private float elapsed;

    private void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        graphics = GetComponentsInChildren<Graphic>(true);
        spriteAlphas = new float[sprites.Length];
        graphicAlphas = new float[graphics.Length];

        for (int i = 0; i < sprites.Length; i++)
            spriteAlphas[i] = sprites[i].color.a;
        for (int i = 0; i < graphics.Length; i++)
            graphicAlphas[i] = graphics[i].color.a;
    }

    // Read after the brush's Update has applied any newly mixed color.
    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        if (!colorMatched)
        {
            if (brushColorController == null || !brushColorController.HasColor)
                return;

            // Compare RGB bytes, matching the Inspector's hex representation.
            Color32 current = brushColorController.CurrentColor;
            Color32 target = targetColor;
            colorMatched = current.r == target.r && current.g == target.g && current.b == target.b;
            return;
        }

        // Once matched, changing or clearing the brush does not reset the timer.
        elapsed += Time.deltaTime;
        if (elapsed < delayBeforeFade)
            return;

        float progress = fadeDuration > 0f
            ? Mathf.Clamp01((elapsed - delayBeforeFade) / fadeDuration)
            : 1f;
        float opacity = 1f - Mathf.SmoothStep(0f, 1f, progress);

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            Color color = sprites[i].color;
            color.a = spriteAlphas[i] * opacity;
            sprites[i].color = color;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null)
                continue;

            Color color = graphics[i].color;
            color.a = graphicAlphas[i] * opacity;
            graphics[i].color = color;
            if (progress >= 1f)
                graphics[i].raycastTarget = false;
        }

        if (progress >= 1f)
            enabled = false;
    }
}
