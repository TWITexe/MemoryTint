using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

public class UIButtonBounce : MonoBehaviour,    
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private static AudioMixerGroup cachedSfxMixerGroup;

    private Vector3 originalScale;

    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float speed = 10f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;    // звук при наведении
    [SerializeField] private AudioClip clickSound;    // Звук при клике
    [SerializeField] private float minPitch = 0.9f;   // Минимальный питч
    [SerializeField] private float maxPitch = 1.1f;   // Максимальный питч
    [SerializeField] private bool randomizePitch = true; // Включить рандомизацию питча
    [SerializeField] private AudioMixerGroup fallbackSfxMixerGroup;

    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        audioSource = ResolveButtonAudioSource();

        AudioMixerGroup resolvedMixerGroup = ResolveMixerGroup();

        if (resolvedMixerGroup != null)
            audioSource.outputAudioMixerGroup = resolvedMixerGroup;
    }
    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * pressedScale;
        if (clickSound != null)
        {
            PlaySound(clickSound);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        if (randomizePitch)
        {
            // сохраняем оригинальный питч
            float originalPitch = audioSource.pitch;

            // устанавливаем случайный питч
            audioSource.pitch = Random.Range(minPitch, maxPitch);

            // воспроизводим звук
            audioSource.PlayOneShot(clip);

            // возвращаем оригинальный питч (чтобы не влиять на другие звуки)
            audioSource.pitch = originalPitch;
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }
    private void OnEnable()
    {
        // при повторном включении кнопки сбрасываем масштаб
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    private AudioMixerGroup ResolveMixerGroup()
    {
        if (fallbackSfxMixerGroup != null)
            return fallbackSfxMixerGroup;

        if (cachedSfxMixerGroup != null)
            return cachedSfxMixerGroup;

        if (IsSfxGroup(audioSource?.outputAudioMixerGroup))
        {
            cachedSfxMixerGroup = audioSource.outputAudioMixerGroup;
            return cachedSfxMixerGroup;
        }

        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        AudioMixerGroup firstAvailableGroup = null;

        for (int i = 0; i < allSources.Length; i++)
        {
            AudioMixerGroup group = allSources[i].outputAudioMixerGroup;

            if (group == null)
                continue;

            if (firstAvailableGroup == null)
                firstAvailableGroup = group;

            if (IsSfxGroup(group))
            {
                cachedSfxMixerGroup = group;
                return group;
            }
        }

        AudioMixerGroup[] allMixerGroups = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();

        for (int i = 0; i < allMixerGroups.Length; i++)
        {
            AudioMixerGroup group = allMixerGroups[i];

            if (IsSfxGroup(group))
            {
                cachedSfxMixerGroup = group;
                return group;
            }
        }

        return firstAvailableGroup;
    }

    private AudioSource ResolveButtonAudioSource()
    {
        AudioSource source = audioSource;

        if (source == null || source.gameObject != gameObject)
            source = GetComponent<AudioSource>();

        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static bool IsSfxGroup(AudioMixerGroup group)
    {
        return group != null && group.name.Equals("Sfx", StringComparison.OrdinalIgnoreCase);
    }
}
