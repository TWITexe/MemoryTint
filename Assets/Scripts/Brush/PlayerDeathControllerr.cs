using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerDeathController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private PlayerMoveController playerMoveController;
    [SerializeField] private PlayerFadeController playerFadeController;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BrushColorController brushColorController;
    [SerializeField] private UI.InGamePauseMenu pauseMenu;
    private BrushSurfaceInteraction brushSurfaceInteraction;
    private BackgroundRevealController backgroundRevealController;
    private LevelIntroSequence levelIntroSequence;
    

    [Header("UI сообщения")]
    [SerializeField] private CanvasGroup messageCanvasGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Настройки")]
    [SerializeField] private float fadeToBlackDuration = 0.8f;
    [SerializeField] private float fadeFromBlackDuration = 0.8f;
    [SerializeField] private float messageFadeDuration = 0.5f;
    [SerializeField] private float messageDuration = 3f;

    [Header("Рестарт по кнопке")]
    [SerializeField] private bool allowRestartByKey = true;
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    [Header("Таймер игрока ( если есть )")]
    [SerializeField] private LevelTimerController levelTimerController;



    [TextArea]
    [SerializeField]
    private string[] deathMessages =
    {
        "Кажется, ещё рано...",
        "Я знаю, ты сможешь!!",
        "Не сегодня!",
        "Не сдавайся, прошу!",
        "Ещё не конец!"
    };

    private bool isDying;
    private Coroutine deathRoutine;

    private void Start()
    {
        if (brushColorController != null)
            brushSurfaceInteraction = brushColorController.GetComponent<BrushSurfaceInteraction>();

        // FindFirstObjectByType - практика не из лучших, но времени в обрез
        levelIntroSequence = FindFirstObjectByType<LevelIntroSequence>();
        backgroundRevealController = FindFirstObjectByType<BackgroundRevealController>();
    }
    private void Update()
    {
        if (!CanRestartByKey())
            return;

        if (Input.GetKeyDown(restartKey))
        {
            RestartWithoutMessage();
        }
    }

    public void Die()
    {
        if (isDying)
            return;

        levelTimerController?.StopTimer();
        deathRoutine = StartCoroutine(DeathRoutine(showMessage: true));
    }

    public void RestartWithoutMessage()
    {
        if (isDying)
            return;

        levelTimerController?.StopTimer();
        deathRoutine = StartCoroutine(DeathRoutine(showMessage: false));
    }

    private IEnumerator DeathRoutine(bool showMessage)
    {
        isDying = true;

        playerMoveController?.LockMove();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        bool fadeFinished = false;

        if (FadeScreen.instance != null)
        {
            FadeScreen.instance.FadeIn(fadeToBlackDuration, () => fadeFinished = true, fadeAudio: false);
            yield return new WaitUntil(() => fadeFinished);
        }

        if (showMessage)
        {
            yield return StartCoroutine(ShowMessageRoutine());

            float timer = 0f;
            while (timer < messageDuration)
            {
                timer += Time.deltaTime;

                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                    break;

                yield return null;
            }

            yield return StartCoroutine(HideMessageRoutine());
        }
        else
        {
            HideMessageImmediate();
        }

        Respawn();

        bool screenFadeFinished = false;

        if (FadeScreen.instance != null)
        {
            FadeScreen.instance.FadeOut(fadeFromBlackDuration, () => screenFadeFinished = true);
            yield return new WaitUntil(() => screenFadeFinished);
        }

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        playerMoveController?.UnlockMove();

        isDying = false;
    }

    private void Respawn()
    {
        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        brushColorController?.ClearColor();
        brushSurfaceInteraction?.ResetSurfaceContact();
        levelTimerController?.ResetTimer(true);
    }

    private IEnumerator ShowMessageRoutine()
    {
        if (messageCanvasGroup == null || messageText == null)
            yield break;

        if (deathMessages == null || deathMessages.Length == 0)
            messageText.text = "Кажется, ещё рано...";
        else
            messageText.text = deathMessages[Random.Range(0, deathMessages.Length)];

        messageCanvasGroup.interactable = false;
        messageCanvasGroup.blocksRaycasts = false;

        yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 0f, 1f, messageFadeDuration));
    }

    private void HideMessageImmediate()
    {
        if (messageCanvasGroup == null)
            return;

        messageCanvasGroup.alpha = 0f;
        messageCanvasGroup.interactable = false;
        messageCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator HideMessageRoutine()
    {
        if (messageCanvasGroup == null)
            yield break;

        yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 1f, 0f, messageFadeDuration));
    }

    private bool CanRestartByKey()
    {
        if (!allowRestartByKey || isDying)
            return false;

        if (pauseMenu != null && pauseMenu.IsPaused)
            return false;

        if (levelIntroSequence != null && levelIntroSequence.IsPlaying)
            return false;

        if (backgroundRevealController != null && backgroundRevealController.IsRevealing)
            return false;

        if (playerFadeController != null && playerFadeController.IsFading)
            return false;

        if (FadeScreen.instance != null && FadeScreen.instance.IsFading)
            return false;

        return true;
    }
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float time = 0f;
        cg.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.SmoothStep(from, to, time / duration);
            yield return null;
        }

        cg.alpha = to;
    }
}
