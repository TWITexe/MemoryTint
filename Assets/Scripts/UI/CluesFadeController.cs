using UnityEngine;

[DisallowMultipleComponent]
public class CluesFadeController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float fadeDuration = 1.5f;
    [SerializeField] private LevelIntroSequence introSequence;

    private SpriteRenderer[] sprites;
    private float[] initialAlphas;
    private bool pressedD;
    private bool pressedSpace;
    private float fadeElapsed;

    private void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        initialAlphas = new float[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            initialAlphas[i] = sprites[i].color.a;
    }

    private void Update()
    {
        // Ignore input during pause, the intro overlay, and screen transitions.
        if (Time.timeScale <= 0f ||
            (introSequence != null && introSequence.IsPlaying) ||
            (FadeScreen.instance != null && FadeScreen.instance.IsFading))
            return;

        // Remember each key independently, in either order.
        pressedD |= Input.GetKeyDown(KeyCode.D);
        pressedSpace |= Input.GetKeyDown(KeyCode.Space);
        if (!pressedD || !pressedSpace)
            return;

        fadeElapsed += Time.deltaTime;
        float progress = fadeDuration > 0f ? Mathf.Clamp01(fadeElapsed / fadeDuration) : 1f;
        float opacity = 1f - Mathf.SmoothStep(0f, 1f, progress);

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            Color color = sprites[i].color;
            color.a = initialAlphas[i] * opacity;
            sprites[i].color = color;
        }

        if (progress >= 1f)
            enabled = false;
    }
}
