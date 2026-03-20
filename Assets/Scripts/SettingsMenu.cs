using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField]
    private Slider volumeSlider;

    [SerializeField]
    private Toggle fullscreenToggle;

    [SerializeField]
    private TMP_Dropdown resolutionDropdown;

    private Resolution[] resolutions;

    private const string VolumePrefKey = "Volume";
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
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void LoadSettings()
    {
        int currentResIndex = GetCurrentResolutionIndex();
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefKey, 1f));
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, 1) == 1;
        int savedResolution = PlayerPrefs.GetInt(ResolutionPrefKey, currentResIndex);

        savedResolution = resolutions.Length > 0
            ? Mathf.Clamp(savedResolution, 0, resolutions.Length - 1)
            : 0;

        volumeSlider.SetValueWithoutNotify(savedVolume);
        fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
        
        if (resolutions.Length > 0)
        {
            resolutionDropdown.SetValueWithoutNotify(savedResolution);
            resolutionDropdown.RefreshShownValue();
        }

        SetVolume(savedVolume);
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
        volumeSlider?.onValueChanged.RemoveListener(SetVolume);
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

    private void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);

        Debug.Log($"Volume = {volume}");
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPrefKey, isFullscreen ? 1 : 0);
        
        Debug.Log($"Fullscreen = {isFullscreen}");
    }

    private void SetResolution(int resIndex)
    {
        if (resolutions == null || resolutions.Length == 0)
            return;

        resIndex = Mathf.Clamp(resIndex, 0, resolutions.Length - 1);
        Resolution res = resolutions[resIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionPrefKey, resIndex);
        
        Debug.Log($"Resolution = {res.width}x{res.height}");
    }
}
