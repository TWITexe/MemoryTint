
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Utils;

public class LevelTransition : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private Color requiredColor = Color.white;
    [SerializeField] private int sceneNumber;
    [SerializeField] private float timeToFadeNextLevelTransition = 1f;
    [SerializeField] private BackgroundRevealController backgroundReveal;
    [SerializeField] private bool playRevealBeforeLoad = true;
    [SerializeField] private float revealDurationBeforeLoad = 1.8f;

    [Header("Last Level")]
    [SerializeField] private bool lastLevel = false;
    [SerializeField] private float whiteFadeDuration = 1.2f;
    [SerializeField] private float whiteScreenHoldDuration = 4f;
    [SerializeField] private float blackFadeDuration = 1.2f;
    [SerializeField] private bool fadeAudioOnFinalBlackFade = false;

    [Header("Last Level Sounds")]
    [SerializeField] private AudioSource finalAudioSource;
    [SerializeField] private AudioClip finalClip1;
    [SerializeField] private AudioClip finalClip2;
    [SerializeField] private float extraDelayAfterFinalSounds = 0.2f;

    [Header("Final Image")]
    [SerializeField] private CanvasGroup finalImageGroup;
    [SerializeField] private float finalImageFadeDuration = 2f;
    [SerializeField] private float finalImageHoldDuration = 4f;

    [Header("Hint Pulse")]
    [SerializeField] private Transform pulseTarget;
    [SerializeField] private float pulseScaleAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float colorToleranceSqr = 0.01f;

    [Header("Sound Notification")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip colorCompleteAudioClip;

    [Header("Timer")]
    [SerializeField] private LevelTimerController levelTimerController;

    private bool isTransitioning;
    private bool hasPlayedColorCompleteSound;
    private bool finalImageReady;

    private Vector3 baseScale;
    private BrushColorController playerBrush;

    private Image finalBlackOverlay;

    private void Awake()
    {
        this.ValidateSerializedFields(false);

        if (pulseTarget == null)
            pulseTarget = transform;

        baseScale = pulseTarget.localScale;

        if (finalImageGroup != null)
        {
            finalImageGroup.alpha = 0f;
            finalImageGroup.interactable = false;
            finalImageGroup.blocksRaycasts = false;
        }

        if (lastLevel)
            PrepareFinalImage();
    }

    private void Start()
    {
        playerBrush = FindFirstObjectByType<BrushColorController>();
    }

    private void Update()
    {
        if (isTransitioning || playerBrush == null)
        {
            ResetPulse();
            return;
        }

        bool isCorrectColor = IsColorMatch(
            playerBrush.CurrentColor,
            requiredColor
        );

        if (isCorrectColor)
        {
            if (!hasPlayedColorCompleteSound &&
                colorCompleteAudioClip != null &&
                audioSource != null)
            {
                audioSource.PlayOneShot(colorCompleteAudioClip);
                hasPlayedColorCompleteSound = true;
            }

            float pulse = 1f +
                Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;

            pulseTarget.localScale = baseScale * pulse;
        }
        else
        {
            hasPlayedColorCompleteSound = false;

            pulseTarget.localScale = Vector3.Lerp(
                pulseTarget.localScale,
                baseScale,
                Time.deltaTime * 8f
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning)
            return;

        if (!collision.TryGetComponent(
            out BrushColorController brushColor))
            return;

        if (!IsColorMatch(brushColor.CurrentColor, requiredColor))
            return;

        PlayerFadeController playerFade =
            collision.GetComponent<PlayerFadeController>();

        if (playerFade == null)
            return;

        isTransitioning = true;

        if (levelTimerController != null)
            levelTimerController.StopTimer();

        playerFade.FadeOut(() =>
        {
            PlayerMoveController moveController =
                collision.GetComponent<PlayerMoveController>();

            if (moveController != null)
                moveController.LockMove();

            ResetPulse();

            if (lastLevel)
            {
                if (playRevealBeforeLoad && backgroundReveal != null)
                    StartCoroutine(RevealThenLastLevelSequence());
                else
                    StartCoroutine(LastLevelSequence());
            }
            else
            {
                if (playRevealBeforeLoad && backgroundReveal != null)
                    StartCoroutine(RevealThenLoad());
                else
                    LoadNextScene();
            }
        });
    }

    private bool IsColorMatch(Color current, Color target)
    {
        Vector3 colorDiff = new Vector3(
            current.r - target.r,
            current.g - target.g,
            current.b - target.b
        );

        return colorDiff.sqrMagnitude < colorToleranceSqr;
    }

    private void ResetPulse()
    {
        if (pulseTarget != null)
        {
            pulseTarget.localScale = Vector3.Lerp(
                pulseTarget.localScale,
                baseScale,
                Time.deltaTime * 8f
            );
        }
    }

    private IEnumerator RevealThenLastLevelSequence()
    {
        backgroundReveal.PlayFinalReveal(
            1,
            revealDurationBeforeLoad
        );

        yield return new WaitForSeconds(
            Mathf.Max(0f, revealDurationBeforeLoad * 2f)
        );

        yield return StartCoroutine(LastLevelSequence());
    }

    private IEnumerator RevealThenLoad()
    {
        backgroundReveal.PlayFinalReveal(
            1,
            revealDurationBeforeLoad
        );

        yield return new WaitForSeconds(
            Mathf.Max(0f, revealDurationBeforeLoad * 2f)
        );

        LoadNextScene();
    }

    // =====================================================
    // ПОДГОТОВКА ФИНАЛЬНОГО ИЗОБРАЖЕНИЯ
    // =====================================================

    private void PrepareFinalImage()
    {
        if (finalImageGroup == null)
        {
            Debug.LogError(
                "LevelTransition: Final Image Group не назначен!"
            );
            return;
        }

        RectTransform imageRect =
            finalImageGroup.GetComponent<RectTransform>();

        if (imageRect == null)
        {
            Debug.LogError(
                "LevelTransition: Final Image Group должен быть UI-объектом!"
            );
            return;
        }

        Graphic imageGraphic =
            finalImageGroup.GetComponentInChildren<Graphic>(true);

        if (imageGraphic == null)
        {
            Debug.LogWarning(
                "LevelTransition: В Final Image Group не найден UI Image!"
            );
        }

        // Создаём независимый Canvas поверх всех остальных UI.
        GameObject canvasObject = new GameObject(
            "Final Ending Overlay",
            typeof(RectTransform),
            typeof(Canvas)
        );

        Canvas overlayCanvas =
            canvasObject.GetComponent<Canvas>();

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 32767;

        // Переносим финальное изображение на верхний Canvas.
        imageRect.SetParent(canvasObject.transform, false);

        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        imageRect.localScale = Vector3.one;
        imageRect.localRotation = Quaternion.identity;

        finalImageGroup.gameObject.SetActive(true);
        finalImageGroup.alpha = 0f;
        finalImageGroup.interactable = false;
        finalImageGroup.blocksRaycasts = false;

        // Создаём чёрный слой поверх картинки.
        GameObject blackObject = new GameObject(
            "Final Black Overlay",
            typeof(RectTransform),
            typeof(Image)
        );

        blackObject.transform.SetParent(
            finalImageGroup.transform,
            false
        );

        RectTransform blackRect =
            blackObject.GetComponent<RectTransform>();

        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;
        blackRect.localScale = Vector3.one;

        finalBlackOverlay = blackObject.GetComponent<Image>();
        finalBlackOverlay.color = new Color(0f, 0f, 0f, 0f);
        finalBlackOverlay.raycastTarget = false;

        blackObject.transform.SetAsLastSibling();

        finalImageReady = true;
    }

    // =====================================================
    // ФИНАЛЬНАЯ ПОСЛЕДОВАТЕЛЬНОСТЬ
    // =====================================================

    private IEnumerator LastLevelSequence()
    {
        if (FadeScreen.instance == null)
        {
            Debug.LogWarning(
                "FadeScreen.instance не найден!"
            );

            SceneManager.LoadScene(sceneNumber);
            yield break;
        }

        if (!finalImageReady)
        {
            Debug.LogError(
                "LevelTransition: Финальное изображение не настроено!"
            );
        }

        // 1. ПЛАВНЫЙ БЕЛЫЙ FADE

        bool whiteFadeCompleted = false;

        FadeScreen.instance.FadeIn(
            Color.white,
            whiteFadeDuration,
            () => whiteFadeCompleted = true,
            false
        );

        yield return new WaitUntil(
            () => whiteFadeCompleted
        );

        // 2. ПАУЗА НА БЕЛОМ ЭКРАНЕ

        yield return new WaitForSeconds(
            Mathf.Max(0f, whiteScreenHoldDuration)
        );

        // 3. ЗАПУСКАЕМ ЗВУКИ

        float soundStartTime = Time.time;
        float longestClipLength = 0f;

        if (finalAudioSource != null)
        {
            if (finalClip1 != null)
            {
                finalAudioSource.PlayOneShot(finalClip1);

                longestClipLength = Mathf.Max(
                    longestClipLength,
                    finalClip1.length
                );
            }

            if (finalClip2 != null)
            {
                finalAudioSource.PlayOneShot(finalClip2);

                longestClipLength = Mathf.Max(
                    longestClipLength,
                    finalClip2.length
                );
            }
        }

        // 4. ОДНОВРЕМЕННО СО ЗВУКАМИ ПОЯВЛЯЕТСЯ КАРТИНКА

        if (finalImageReady)
        {
            yield return StartCoroutine(
                FadeFinalImage(
                    0f,
                    1f,
                    finalImageFadeDuration
                )
            );
        }

        // 5. ЖДЁМ ЗАВЕРШЕНИЯ ЗВУКОВ

        float soundWaitDuration =
            longestClipLength +
            (longestClipLength > 0f
                ? Mathf.Max(0f, extraDelayAfterFinalSounds)
                : 0f);

        float elapsedSinceSoundStart =
            Time.time - soundStartTime;

        float remainingSoundWait =
            soundWaitDuration - elapsedSinceSoundStart;

        if (remainingSoundWait > 0f)
        {
            yield return new WaitForSeconds(
                remainingSoundWait
            );
        }

        // 6. ДЕРЖИМ ФИНАЛЬНОЕ ИЗОБРАЖЕНИЕ

        if (finalImageReady)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0f, finalImageHoldDuration)
            );
        }

        // 7. ПЛАВНО ЗАТЕМНЯЕМ ИЗОБРАЖЕНИЕ

        bool blackFadeCompleted = false;

        FadeScreen.instance.FadeColor(
            Color.white,
            Color.black,
            blackFadeDuration,
            () => blackFadeCompleted = true,
            fadeAudioOnFinalBlackFade
        );

        // Чёрный слой появляется поверх картинки,
        // чтобы белый FadeScreen не просвечивал.
        if (finalImageReady)
        {
            yield return StartCoroutine(
                FadeBlackOverlay(blackFadeDuration)
            );
        }

        yield return new WaitUntil(
            () => blackFadeCompleted
        );

        // 8. ЗАГРУЗКА СЛЕДУЮЩЕЙ СЦЕНЫ

        SceneManager.LoadScene(sceneNumber);
    }

    // =====================================================
    // ПЛАВНОЕ ПОЯВЛЕНИЕ ФИНАЛЬНОЙ КАРТИНКИ
    // =====================================================

    private IEnumerator FadeFinalImage(
        float from,
        float to,
        float duration)
    {
        if (finalImageGroup == null)
            yield break;

        finalImageGroup.alpha = from;

        if (duration <= 0f)
        {
            finalImageGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            // SmoothStep для более мягкого появления.
            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            finalImageGroup.alpha = Mathf.Lerp(
                from,
                to,
                progress
            );

            yield return null;
        }

        finalImageGroup.alpha = to;
    }

    // =====================================================
    // ПЛАВНОЕ ЗАТЕМНЕНИЕ ФИНАЛЬНОЙ КАРТИНКИ
    // =====================================================

    private IEnumerator FadeBlackOverlay(float duration)
    {
        if (finalBlackOverlay == null)
            yield break;

        if (duration <= 0f)
        {
            finalBlackOverlay.color = Color.black;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            finalBlackOverlay.color = new Color(
                0f,
                0f,
                0f,
                progress
            );

            yield return null;
        }

        finalBlackOverlay.color = Color.black;
    }

    // =====================================================
    // ОБЫЧНЫЙ ПЕРЕХОД МЕЖДУ УРОВНЯМИ
    // =====================================================

    private void LoadNextScene()
    {
        if (FadeScreen.instance != null)
        {
            FadeScreen.instance.FadeIn(
                timeToFadeNextLevelTransition,
                () =>
                {
                    SceneManager.LoadScene(sceneNumber);
                }
            );

            return;
        }

        SceneManager.LoadScene(sceneNumber);
    }
}
