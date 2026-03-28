using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class FadeScreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    [SerializeField] private float timeToFadeStartLevelTransition;
    [SerializeField] private bool fadeToStart = true;

    [Header("Audio Fade")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private float mutedVolumeDb = -80f;
    [SerializeField] private bool fadeAudioOnFadeIn = true;

    public static FadeScreen instance { get; private set; }

    private Coroutine currentFadeRoutine;
    private Coroutine blinkRoutine;
    private Coroutine currentAudioFadeRoutine;

    private static float storedMasterVolumeDb;
    private static bool hasStoredMasterVolume;

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
        RestoreStoredMasterVolumeIfNeeded();

        if (!fadeToStart)
            return;

        SetAlpha(1f);
        FadeOut(timeToFadeStartLevelTransition);
    }

    /// <summary>
    /// Открыть экран: из чёрного в прозрачный.
    /// Звук не трогаем.
    /// </summary>
    public void FadeOut(float? duration = null, Action onComplete = null)
    {
        float fadeDuration = duration ?? defaultDuration;
        StartNewFade(1f, 0f, fadeDuration, onComplete);
    }

    /// <summary>
    /// Закрыть экран: из прозрачного в чёрный.
    /// При необходимости приглушить звук.
    /// </summary>
    public void FadeIn(float? duration = null, Action onComplete = null)
    {
        float fadeDuration = duration ?? defaultDuration;

        if (fadeAudioOnFadeIn)
            StartAudioFadeDown(fadeDuration);

        StartNewFade(0f, 1f, fadeDuration, onComplete);
    }

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

    public void StartBlinking(float speed = 1f)
    {
        StopBlinking();
        blinkRoutine = StartCoroutine(BlinkRoutine(speed));
    }

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

    private void StartAudioFadeDown(float duration)
    {
        if (audioMixer == null)
            return;

        if (!audioMixer.GetFloat(masterVolumeParameter, out float currentDb))
            return;

        storedMasterVolumeDb = currentDb;
        hasStoredMasterVolume = true;

        StartAudioFade(currentDb, mutedVolumeDb, duration);
    }

    private void StartAudioFade(float startDb, float targetDb, float duration)
    {
        if (currentAudioFadeRoutine != null)
            StopCoroutine(currentAudioFadeRoutine);

        currentAudioFadeRoutine = StartCoroutine(AudioFadeRoutine(startDb, targetDb, duration));
    }

    private IEnumerator AudioFadeRoutine(float startDb, float targetDb, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsedTime / duration) : 1f;

            float currentDb = Mathf.Lerp(startDb, targetDb, t);
            audioMixer.SetFloat(masterVolumeParameter, currentDb);

            yield return null;
        }

        audioMixer.SetFloat(masterVolumeParameter, targetDb);
        currentAudioFadeRoutine = null;
    }

    private void RestoreStoredMasterVolumeIfNeeded()
    {
        if (audioMixer == null)
            return;

        if (!hasStoredMasterVolume)
            return;

        audioMixer.SetFloat(masterVolumeParameter, storedMasterVolumeDb);
        hasStoredMasterVolume = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
