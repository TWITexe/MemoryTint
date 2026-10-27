using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SplashImage : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private int nextSceneIndex = 1;
    [SerializeField] private Font interfaceFont;
    [SerializeField, Min(1f)] private float prepareTimeout = 20f;
    [SerializeField, Min(1f)] private float stallTimeout = 15f;
    [SerializeField, Min(1f)] private float playbackTimeout = 120f;

    private bool videoFinished;
    private bool loadingMenu;
    private string loadError;
    private GUIStyle skipStyle;
    private GUIStyle messageStyle;

    private void Awake()
    {
        if (videoPlayer == null) return;
        videoPlayer.playOnAwake = false;
        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.clip = null;
        // StreamingAssets is bundled with both builds. Web returns an HTTP URL;
        // the Windows player and Editor use a local file URI.
        string path = Application.streamingAssetsPath.Replace('\\', '/').TrimEnd('/') + "/0318.mp4";
#if UNITY_WEBGL && !UNITY_EDITOR
        videoPlayer.url = path;
#else
        videoPlayer.url = new Uri(path).AbsoluteUri;
#endif
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
    }

    private IEnumerator Start()
    {
        if (videoPlayer == null)
        {
            FinishVideo("VideoPlayer is missing.");
        }
        else
        {
            Debug.Log("[Splash] Preparing bundled intro: " + videoPlayer.url);
            try { videoPlayer.Prepare(); }
            catch (Exception exception) { FinishVideo(exception.Message); }

            double deadline = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, prepareTimeout);
            while (!videoFinished && !videoPlayer.isPrepared)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                    FinishVideo("Video preparation timed out.");
                yield return null;
            }

            if (!videoFinished)
            {
                try { videoPlayer.Play(); }
                catch (Exception exception) { FinishVideo(exception.Message); }
                double started = Time.realtimeSinceStartupAsDouble;
                double lastProgress = started;
                double lastVideoTime = videoPlayer.time;
                while (!videoFinished)
                {
                    double now = Time.realtimeSinceStartupAsDouble;
                    double videoTime = videoPlayer.time;
                    if (videoTime > lastVideoTime + 0.01)
                    {
                        lastVideoTime = videoTime;
                        lastProgress = now;
                    }
                    if (now - lastProgress >= Mathf.Max(1f, stallTimeout))
                        FinishVideo("Video stalled or browser autoplay was blocked.");
                    else if (now - started >= Mathf.Max(1f, playbackTimeout))
                        FinishVideo("Video playback timed out.");
                    yield return null;
                }
            }
        }

        // One coroutine owns the transition. Never leave an async scene load
        // waiting for video callbacks with allowSceneActivation disabled.
        loadingMenu = true;
        if (videoPlayer != null) videoPlayer.Stop();
        if (nextSceneIndex < 0 || nextSceneIndex >= SceneManager.sceneCountInBuildSettings ||
            nextSceneIndex == SceneManager.GetActiveScene().buildIndex ||
            !Application.CanStreamedLevelBeLoaded(nextSceneIndex))
        {
            loadError = "Не удалось открыть меню: сцена отсутствует в сборке.";
            Debug.LogError("[Splash] Invalid menu scene index: " + nextSceneIndex);
            yield break;
        }

        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(nextSceneIndex); }
        catch (Exception exception)
        {
            loadError = "Не удалось открыть меню.";
            Debug.LogError("[Splash] Menu load failed: " + exception);
        }
        if (operation != null)
        {
            Debug.Log("[Splash] Loading menu scene " + nextSceneIndex);
            yield return operation;
        }
        else if (loadError == null)
        {
            loadError = "Не удалось открыть меню.";
            Debug.LogError("[Splash] Menu load returned no operation.");
        }
    }

    private void OnVideoFinished(VideoPlayer player) { FinishVideo(null); }
    private void OnVideoError(VideoPlayer player, string message) { FinishVideo(message); }

    private void FinishVideo(string warning)
    {
        if (videoFinished) return;
        videoFinished = true;
        if (!string.IsNullOrEmpty(warning)) Debug.LogWarning("[Splash] " + warning + " Opening menu.");
        else Debug.Log("[Splash] Intro completed or skipped.");
    }

    private void OnGUI()
    {
        if (skipStyle == null)
        {
            // Explicit scene reference includes the font data in WebGL builds.
            // Do not depend on the platform's default IMGUI font.
            skipStyle = new GUIStyle
            {
                font = interfaceFont,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 6, 6)
            };
            foreach (GUIStyleState state in new[]
            {
                skipStyle.normal, skipStyle.hover, skipStyle.active, skipStyle.focused,
                skipStyle.onNormal, skipStyle.onHover, skipStyle.onActive, skipStyle.onFocused
            })
            {
                state.background = Texture2D.blackTexture;
                state.textColor = Color.white;
            }
            skipStyle.hover.background = Texture2D.whiteTexture;
            skipStyle.hover.textColor = Color.black;
            messageStyle = new GUIStyle(skipStyle) { wordWrap = true };
        }

        float scale = Mathf.Clamp(Screen.height / 600f, 0.75f, 2f);
        skipStyle.fontSize = Mathf.RoundToInt(18 * scale);
        messageStyle.fontSize = skipStyle.fontSize;
        if (loadingMenu)
        {
            GUI.Box(new Rect(20, 20, Mathf.Max(1, Screen.width - 40), 70 * scale), loadError ?? "Загрузка меню…", messageStyle);
        }
        else if (!videoFinished && GUI.Button(
            new Rect(Mathf.Max(10, Screen.width - 190 * scale), Mathf.Max(10, Screen.height - 64 * scale), 170 * scale, 44 * scale),
            "Пропустить", skipStyle))
        {
            FinishVideo(null);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer == null) return;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
    }
}
