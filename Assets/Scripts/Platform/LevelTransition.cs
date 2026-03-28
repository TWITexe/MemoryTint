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

    [Header("Hint Pulse")]
    [SerializeField] private Transform pulseTarget; // что именно пульсирует (дверь / спрайт двери)
    [SerializeField] private float pulseScaleAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float colorToleranceSqr = 0.01f;

    private bool isTransitioning;
    private Vector3 baseScale;
    private BrushColorController playerBrush;

    [Header("Sound Notification")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip colorCompleteAudioClip;


    private void Awake()
    {
        this.ValidateSerializedFields();

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
            if (colorCompleteAudioClip != null)
            {
                audioSource.PlayOneShot(colorCompleteAudioClip);
                colorCompleteAudioClip = null;
            }
            
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;
            pulseTarget.localScale = baseScale * pulse;
        }
        else
        {
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
            playerFade.FadeOut(() =>
            {
                collision.GetComponent<PlayerMoveController>().LockMove();
                Debug.Log("Отлично! Новый уровень)");
                
                ResetPulse();

                if (playRevealBeforeLoad && backgroundReveal != null)
                {
                    StartCoroutine(RevealThenLoad());
                }
                else
                {
                    LoadNextScene();
                }
            });
        }
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
