using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Utils;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField]
    private Slider musicVolumeSlider;

    [SerializeField]
    private Slider sfxVolumeSlider;

    [SerializeField]
    private Toggle fullscreenToggle;

    [SerializeField]
    private TMP_Dropdown resolutionDropdown;

    [SerializeField]
    private AudioMixer audioMixer;

    private Resolution[] resolutions;

    private const string MusicVolumePrefKey = "MusicVolume";
    private const string SfxVolumePrefKey = "SfxVolume";
    private const string FullscreenPrefKey = "Fullscreen";
    private const string ResolutionPrefKey = "Resolution";

    private void Awake()
    {
        this.ValidateSerializedFields();

        AddSubscriptions();
    }

    public void InitializeAndLoad()
    {
        InitializeResolutions();
        LoadSettings();
    }

    private void AddSubscriptions()
    {
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void LoadSettings()
    {
        int currentResIndex = GetCurrentResolutionIndex();
        float savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, 0.7f);
        float savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefKey, 0.7f);
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, 1) == 1;
        int savedResolution = PlayerPrefs.GetInt(ResolutionPrefKey, currentResIndex);

        savedResolution = resolutions.Length > 0
            ? Mathf.Clamp(savedResolution, 0, resolutions.Length - 1)
            : 0;

        musicVolumeSlider.SetValueWithoutNotify(savedMusicVolume);
        sfxVolumeSlider.SetValueWithoutNotify(savedSfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);

        if (resolutions.Length > 0)
        {
            resolutionDropdown.SetValueWithoutNotify(savedResolution);
            resolutionDropdown.RefreshShownValue();
        }

        SetMusicVolume(savedMusicVolume);
        SetSfxVolume(savedSfxVolume);
        SetFullscreen(savedFullscreen);
        SetResolution(savedResolution);
    }

    private void OnDestroy()
    {
        RemoveSubscriptions();
        PlayerPrefs.Save();
    }

    private void RemoveSubscriptions()
    {
        musicVolumeSlider?.onValueChanged.RemoveListener(SetMusicVolume);
        sfxVolumeSlider?.onValueChanged.RemoveListener(SetSfxVolume);
        fullscreenToggle?.onValueChanged.RemoveListener(SetFullscreen);
        resolutionDropdown?.onValueChanged.RemoveListener(SetResolution);
    }

    private void InitializeResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = resolutions.Select(t => t.width + "x" + t.height).ToList();

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.interactable = resolutions.Length > 0;
    }

    private int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }

        return 0;
    }

    private void SetMusicVolume(float volume)
    {
        ApplyMusicVolume(volume);
        PlayerPrefs.SetFloat(MusicVolumePrefKey, volume);
    }

    private void SetSfxVolume(float volume)
    {
        ApplySfxVolume(volume);
        PlayerPrefs.SetFloat(SfxVolumePrefKey, volume);
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPrefKey, isFullscreen ? 1 : 0);
    }

    private void SetResolution(int resIndex)
    {
        if (resolutions == null || resolutions.Length == 0)
            return;

        resIndex = Mathf.Clamp(resIndex, 0, resolutions.Length - 1);
        Resolution res = resolutions[resIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionPrefKey, resIndex);
    }

    public void ApplySavedAudioSettings()
    {
        float savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, 0.7f);
        float savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefKey, 0.7f);

        ApplyMusicVolume(savedMusicVolume);
        ApplySfxVolume(savedSfxVolume);
    }

    private void ApplyMusicVolume(float volume)
    {
        if (volume <= 0.001f)
            audioMixer.SetFloat("MusicVolume", -80f);
        else
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20f);
    }

    private void ApplySfxVolume(float volume)
    {
        if (volume <= 0.001f)
            audioMixer.SetFloat("SfxVolume", -80f);
        else
            audioMixer.SetFloat("SfxVolume", Mathf.Log10(volume) * 20f);
    }
}
