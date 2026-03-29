using System.Collections;
using UnityEngine;

public class PlayerFadeController : MonoBehaviour
{
    [SerializeField] private PlayerMoveController playerMoveController;
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private float fadePlayerDuration = 2f;
    [SerializeField] private bool fadeToStart = false;

    public bool IsFading { get; private set; }

    private Coroutine currentFade;

    private void Start()
    {
        SetAlpha(0f);

        if (fadeToStart)
        {
            FadeIn(fadePlayerDuration);
        }
    }

    public void FadeIn(float duration = -1f)
    {
        float finalDuration = duration > 0f ? duration : fadePlayerDuration;
        StartFade(0f, 1f, finalDuration, true);
    }

    public void FadeOut(System.Action onComplete = null, float duration = -1f)
    {
        float finalDuration = duration > 0f ? duration : fadePlayerDuration;
        StartFade(1f, 0f, finalDuration, false, onComplete);
    }

    public void SetVisibleImmediate(float alpha)
    {
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
            currentFade = null;
        }

        IsFading = false;
        SetAlpha(alpha);
    }

    private void StartFade(float from, float to, float duration, bool unlockAfter, System.Action onComplete = null)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeRoutine(from, to, duration, unlockAfter, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, bool unlockAfter, System.Action onComplete)
    {
        IsFading = true;
        playerMoveController?.LockMove();

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
            float alpha = Mathf.Lerp(from, to, t);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(to);

        if (unlockAfter)
            playerMoveController?.UnlockMove();

        IsFading = false;
        currentFade = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            if (r == null)
                continue;

            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }
}
