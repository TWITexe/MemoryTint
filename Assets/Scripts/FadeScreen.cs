using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FadeScreen : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField]
    private Image fadeImage; // UI Image для затемнения

    [SerializeField]
    private float defaultDuration = 1f; // стандартная длительность

    [SerializeField]
    private Color fadeColor = Color.black;

    [SerializeField] float timeToFadeStartLevelTransition; // время для Fade вначале уровня

    public static FadeScreen instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            Debug.Log("Удалён лишний Fade");
            return;
        }

        instance = this;
    }

    private void Start()
    {
        FadeOut(timeToFadeStartLevelTransition);
    }
    /// <summary>
    /// Появление из черного (прозрачный -> черный)
    /// </summary>
    public void FadeOut(float? duration = null, Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1f, 0f, duration ?? defaultDuration, onComplete));
    }

    /// <summary>
    /// Исчезновение в черное (черный -> прозрачный)
    /// </summary>
    public void FadeIn(float? duration = null, Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(0f, 1f, duration ?? defaultDuration, onComplete));
    }

    /// <summary>
    /// Переключение состояния
    /// </summary>
    public void ToggleFade(float? duration = null, Action onComplete = null)
    {
        if (fadeImage.color.a > 0.5f)
            FadeIn(duration, onComplete);
        else
            FadeOut(duration, onComplete);
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration, Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogError("FadeImage не назначен!");
            onComplete?.Invoke(); // всё равно вызываем callback, даже если ошибка
            yield break;
        }

        // Включаем raycastTarget во время анимации (опционально)
        fadeImage.raycastTarget = true;

        float elapsedTime = 0f;
        Color currentColor = fadeColor;
        currentColor.a = startAlpha;
        fadeImage.color = currentColor;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Плавное изменение альфа-канала
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            currentColor.a = alpha;
            fadeImage.color = currentColor;

            yield return null;
        }

        // Убеждаемся, что достигли целевого значения
        currentColor.a = targetAlpha;
        fadeImage.color = currentColor;

        // если полностью прозрачные - выключаем raycastTarget
        if (targetAlpha == 0f)
            fadeImage.raycastTarget = false;

        // вызываем callback после завершения анимации
        onComplete?.Invoke();
    }

    /// <summary>
    /// Мгновенная установка прозрачности
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = Mathf.Clamp01(alpha);
            fadeImage.color = color;
            fadeImage.raycastTarget = alpha > 0f;
        }
    }

    /// <summary>
    /// Зацикленное мигание
    /// </summary>
    public void StartBlinking(float speed = 1f)
    {
        StartCoroutine(BlinkRoutine(speed));
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
