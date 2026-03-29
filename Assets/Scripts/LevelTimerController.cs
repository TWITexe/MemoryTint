using UnityEngine;
using UnityEngine.UI;

public class LevelTimerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image lightBarFillImage; // верхняя светлая полоска
    [SerializeField] private Image darkBarImage;      // нижняя темная полоска (просто для ссылки, опционально)

    [Header("Settings")]
    [SerializeField] private float maxTime = 10f;
    [SerializeField] private bool startOnLevelStart = true;

    [Header("Links")]
    [SerializeField] private PlayerDeathController playerDeathController;

    private float currentTime;
    private bool isRunning;
    private bool timerExpired;

    public float CurrentTime => currentTime;
    public float MaxTime => maxTime;
    public bool IsRunning => isRunning;

    private void Awake()
    {
        currentTime = maxTime;
        UpdateVisual();
    }

    private void Start()
    {
        if (startOnLevelStart)
            StartTimer();
        else
            StopTimer();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        if (timerExpired)
            return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            timerExpired = true;
            isRunning = false;

            UpdateVisual();

            if (playerDeathController != null)
                playerDeathController.Die();

            return;
        }

        UpdateVisual();
    }

    public void StartTimer()
    {
        timerExpired = false;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer(bool autoStart = true)
    {
        currentTime = maxTime;
        timerExpired = false;
        UpdateVisual();

        isRunning = autoStart;
    }

    private void UpdateVisual()
    {
        if (lightBarFillImage != null)
            lightBarFillImage.fillAmount = Mathf.Clamp01(currentTime / maxTime);
    }
}
