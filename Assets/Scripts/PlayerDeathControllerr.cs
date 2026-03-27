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

    [Header("UI сообщения")]
    [SerializeField] private CanvasGroup messageCanvasGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Настройки")]
    [SerializeField] private float fadeToBlackDuration = 0.8f;
    [SerializeField] private float fadeFromBlackDuration = 0.8f;
    [SerializeField] private float messageFadeDuration = 0.5f;
    [SerializeField] private float messageDuration = 3f;



    [TextArea]
    [SerializeField]
    private string[] deathMessages =
    {
        "Кажется, ещё рано...",
        "Я знаю, ты сможешь!!",
        "Не сегодня!",
        "Ещё не конец!"
        
    };

    private bool isDying;
    private Coroutine deathRoutine;

    public void Die()
    {
        if (isDying)
            return;

        deathRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
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
            FadeScreen.instance.FadeIn(fadeToBlackDuration, () => fadeFinished = true);
            yield return new WaitUntil(() => fadeFinished);
        }

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
    }

    private IEnumerator ShowMessageRoutine()
    {
        if (messageCanvasGroup == null || messageText == null)
            yield break;

        // текст
        if (deathMessages == null || deathMessages.Length == 0)
            messageText.text = "Кажется, ещё рано...";
        else
            messageText.text = deathMessages[Random.Range(0, deathMessages.Length)];

        messageCanvasGroup.interactable = false;
        messageCanvasGroup.blocksRaycasts = false;

        // Плавное появление
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

    private IEnumerator HideMessageRoutine()
    {
        if (messageCanvasGroup == null)
            yield break;

        yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 1f, 0f, messageFadeDuration));
    }
}
