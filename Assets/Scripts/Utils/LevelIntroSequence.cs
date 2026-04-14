using System.Collections;
using TMPro;
using UnityEngine;

public class LevelIntroSequence : MonoBehaviour
{
    [Header("Quote UI")]
    [SerializeField] private TMP_Text firstQuoteText;
    [SerializeField] private TMP_Text secondQuoteText;

    [TextArea(2, 4)]
    [SerializeField] private string firstQuoteLine = "«У художника вселенная в его разуме и теле»";

    [TextArea(1, 2)]
    [SerializeField] private string secondQuoteLine = "— Леонардо да Винчи";

    [Header("Timing")]
    [SerializeField] private float delayBeforeFirstLine = 0.5f;
    [SerializeField] private float firstLineFadeDuration = 1f;
    [SerializeField] private float delayBetweenLines = 0.6f;
    [SerializeField] private float secondLineFadeDuration = 1f;
    [SerializeField] private float delayBeforeSceneOpen = 0.15f;
    [SerializeField] private float screenFadeOutDuration = 1.5f;
    [SerializeField] private float delayBeforePlayerFade = 0.2f;
    [SerializeField] private float playerFadeDuration = 2f;

    [Header("Scene Links")]
    [SerializeField] private BackgroundRevealController backgroundRevealController;
    [SerializeField] private PlayerFadeController playerFadeController;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        IsPlaying = true;

        if (FadeScreen.instance != null)
            FadeScreen.instance.SetAlpha(1f);

        PrepareTexts();
        HideTextsImmediate();

        if (playerFadeController != null)
            playerFadeController.SetVisibleImmediate(0f);
    }

    private void Start()
    {
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return null;

        yield return new WaitForSeconds(delayBeforeFirstLine);

        if (firstQuoteText != null)
            yield return FadeTMPText(firstQuoteText, 0f, 1f, firstLineFadeDuration);

        yield return new WaitForSeconds(delayBetweenLines);

        if (secondQuoteText != null)
            yield return FadeTMPText(secondQuoteText, 0f, 1f, secondLineFadeDuration);

        yield return StartCoroutine(WaitForSkipInput());

        Coroutine fade1 = null;
        Coroutine fade2 = null;

        if (firstQuoteText != null)
            fade1 = StartCoroutine(FadeTMPText(firstQuoteText, 1f, 0f, 0.8f));

        if (secondQuoteText != null)
            fade2 = StartCoroutine(FadeTMPText(secondQuoteText, 1f, 0f, 0.8f));

        if (fade1 != null) yield return fade1;
        if (fade2 != null) yield return fade2;

        yield return new WaitForSeconds(delayBeforeSceneOpen);
        yield return new WaitForSeconds(delayBeforePlayerFade);

        if (playerFadeController != null)
            playerFadeController.FadeIn(playerFadeDuration);

        if (FadeScreen.instance != null)
            FadeScreen.instance.FadeOut(screenFadeOutDuration);

        IsPlaying = false;
    }

    private IEnumerator WaitForSkipInput()
    {
        while (true)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                yield break;

            yield return null;
        }
    }

    private void PrepareTexts()
    {
        if (firstQuoteText != null)
        {
            firstQuoteText.text = firstQuoteLine;
            SetTextAlpha(firstQuoteText, 0f);
        }

        if (secondQuoteText != null)
        {
            secondQuoteText.text = secondQuoteLine;
            SetTextAlpha(secondQuoteText, 0f);
        }
    }

    private void HideTextsImmediate()
    {
        if (firstQuoteText != null)
            SetTextAlpha(firstQuoteText, 0f);

        if (secondQuoteText != null)
            SetTextAlpha(secondQuoteText, 0f);
    }

    private IEnumerator FadeTMPText(TMP_Text text, float from, float to, float duration)
    {
        float time = 0f;
        Color color = text.color;
        color.a = from;
        text.color = color;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            color.a = Mathf.Lerp(from, to, t);
            text.color = color;
            yield return null;
        }

        color.a = to;
        text.color = color;
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}
