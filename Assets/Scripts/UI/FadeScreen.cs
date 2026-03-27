using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeScreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    [SerializeField] float timeToFadeStartLevelTransition; // время для Fade вначале уровня
    public static FadeScreen instance { get; private set; }

    private Coroutine currentFadeRoutine;
    private Coroutine blinkRoutine;

    [SerializeField] private bool fadeToStart = true;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            Debug.Log("Удалён лишний FadeScreen");
            return;
        }

        instance = this;
    }

    private void Start() 
    { 
        if (fadeToStart)
            FadeOut(timeToFadeStartLevelTransition); 
    }
    /// <summary>
    /// Открыть экран: из чёрного в прозрачный.
    /// </summary>

    public void FadeOut(float? duration = null, Action onComplete = null)
    {
        StartNewFade(1f, 0f, duration ?? defaultDuration, onComplete);
    }

    /// <summary>
    /// Закрыть экран: из прозрачного в чёрный.
    /// </summary>
    public void FadeIn(float? duration = null, Action onComplete = null)
    {
        StartNewFade(0f, 1f, duration ?? defaultDuration, onComplete);
    }

    /// <summary>
    /// Переключить состояние.
    /// </summary>
    public void ToggleFade(float? duration = null, Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogError("FadeImage не назначен!");
            onComplete?.Invoke();
            return;
        }

        if (fadeImage.color.a > 0.5f)
            FadeOut(duration, onComplete);
        else
            FadeIn(duration, onComplete);
    }

    /// <summary>
    /// Мгновенно установить альфу затемнения.
    /// 0 = прозрачный, 1 = полностью чёрный.
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            Debug.LogError("FadeImage не назначен!");
            return;
        }

        Color color = fadeColor;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
        fadeImage.raycastTarget = alpha > 0f;
    }

    /// <summary>
    /// Запустить мигание.
    /// </summary>
    public void StartBlinking(float speed = 1f)
    {
        StopBlinking();
        blinkRoutine = StartCoroutine(BlinkRoutine(speed));
    }

    /// <summary>
    /// Остановить мигание.
    /// </summary>
    public void StopBlinking()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    private void StartNewFade(float startAlpha, float targetAlpha, float duration, Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogError("FadeImage не назначен!");
            onComplete?.Invoke();
            return;
        }

        if (currentFadeRoutine != null)
            StopCoroutine(currentFadeRoutine);

        currentFadeRoutine = StartCoroutine(FadeRoutine(startAlpha, targetAlpha, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration, Action onComplete = null)
    {
        fadeImage.raycastTarget = true;

        float elapsedTime = 0f;
        Color currentColor = fadeColor;
        currentColor.a = startAlpha;
        fadeImage.color = currentColor;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsedTime / duration) : 1f;

            currentColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = currentColor;

            yield return null;
        }

        currentColor.a = targetAlpha;
        fadeImage.color = currentColor;

        if (Mathf.Approximately(targetAlpha, 0f))
            fadeImage.raycastTarget = false;

        currentFadeRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator BlinkRoutine(float speed)
    {
        while (true)
        {
            yield return FadeRoutine(0f, 1f, speed);
            yield return FadeRoutine(1f, 0f, speed);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
