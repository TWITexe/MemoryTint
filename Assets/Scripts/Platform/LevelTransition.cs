using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
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

    [Header("Hint Pulse")]
    [SerializeField] private Transform pulseTarget;
    [SerializeField] private float pulseScaleAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float colorToleranceSqr = 0.01f;

    private bool isTransitioning;
    private Vector3 baseScale;
    private BrushColorController playerBrush;

    [Header("Sound Notification")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip colorCompleteAudioClip;
    private bool hasPlayedColorCompleteSound;

    [Header("Timer")]
    [SerializeField] private LevelTimerController levelTimerController;

    private void Awake()
    {
        this.ValidateSerializedFields(false);

        if (pulseTarget == null)
            pulseTarget = transform;

        baseScale = pulseTarget.localScale;
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

        bool isCorrectColor = IsColorMatch(playerBrush.CurrentColor, requiredColor);

        if (isCorrectColor)
        {
            if (!hasPlayedColorCompleteSound && colorCompleteAudioClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(colorCompleteAudioClip);
                hasPlayedColorCompleteSound = true;
            }

            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;
            pulseTarget.localScale = baseScale * pulse;
        }
        else
        {
            hasPlayedColorCompleteSound = false;

            pulseTarget.localScale = Vector3.Lerp(
                pulseTarget.localScale,
                baseScale,
                Time.deltaTime * 8f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning)
            return;

        if (!collision.TryGetComponent(out BrushColorController brushColor))
            return;

        if (!IsColorMatch(brushColor.CurrentColor, requiredColor))
            return;

        PlayerFadeController playerFade = collision.GetComponent<PlayerFadeController>();
        if (playerFade != null)
        {
            isTransitioning = true;

            if (levelTimerController != null)
                levelTimerController.StopTimer();

            playerFade.FadeOut(() =>
            {
                PlayerMoveController moveController = collision.GetComponent<PlayerMoveController>();
                if (moveController != null)
                    moveController.LockMove();

                Debug.Log("Отлично! Новый уровень)");
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
    }

    private IEnumerator RevealThenLastLevelSequence()
    {
        backgroundReveal.PlayFinalReveal(1, revealDurationBeforeLoad);

        yield return new WaitForSeconds(Mathf.Max(0f, revealDurationBeforeLoad * 2));
        yield return StartCoroutine(LastLevelSequence());
    }
    private bool IsColorMatch(Color current, Color target)
    {
        Vector3 colorDiff = new Vector3(
            current.r - target.r,
            current.g - target.g,
            current.b - target.b);

        return colorDiff.sqrMagnitude < colorToleranceSqr;
    }

    private void ResetPulse()
    {
        if (pulseTarget != null)
        {
            pulseTarget.localScale = Vector3.Lerp(
                pulseTarget.localScale,
                baseScale,
                Time.deltaTime * 8f);
        }
    }

    private IEnumerator RevealThenLoad()
    {
        backgroundReveal.PlayFinalReveal(1, revealDurationBeforeLoad);

        yield return new WaitForSeconds(Mathf.Max(0f, revealDurationBeforeLoad * 2));
        LoadNextScene();
    }

    private IEnumerator LastLevelSequence()
    {
        if (FadeScreen.instance == null)
        {
            Debug.LogWarning("FadeScreen.instance не найден, загружаю сцену сразу.");
            SceneManager.LoadScene(sceneNumber);
            yield break;
        }

        bool whiteFadeCompleted = false;

        FadeScreen.instance.FadeIn(
            Color.white,
            whiteFadeDuration,
            () => whiteFadeCompleted = true,
            false
        );

        yield return new WaitUntil(() => whiteFadeCompleted);

        yield return new WaitForSeconds(whiteScreenHoldDuration);

        float longestClipLength = 0f;

        if (finalAudioSource != null)
        {
            if (finalClip1 != null)
            {
                finalAudioSource.PlayOneShot(finalClip1);
                longestClipLength = Mathf.Max(longestClipLength, finalClip1.length);
            }

            if (finalClip2 != null)
            {
                finalAudioSource.PlayOneShot(finalClip2);
                longestClipLength = Mathf.Max(longestClipLength, finalClip2.length);
            }
        }

        if (longestClipLength > 0f)
            yield return new WaitForSeconds(longestClipLength + extraDelayAfterFinalSounds);

        bool colorFadeCompleted = false;

        FadeScreen.instance.FadeColor(
            Color.white,
            Color.black,
            blackFadeDuration,
            () => colorFadeCompleted = true,
            true
        );

        yield return new WaitUntil(() => colorFadeCompleted);

        SceneManager.LoadScene(sceneNumber);
    }

    private void LoadNextScene()
    {
        if (FadeScreen.instance != null)
        {
            FadeScreen.instance.FadeIn(timeToFadeNextLevelTransition, () =>
            {
                SceneManager.LoadScene(sceneNumber);
            });
            return;
        }

        SceneManager.LoadScene(sceneNumber);
    }
}
