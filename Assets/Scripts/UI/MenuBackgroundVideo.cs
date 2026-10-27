using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public sealed class MenuBackgroundVideo : MonoBehaviour
{
    private VideoPlayer player;
    private bool failed;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.Stop();
        player.source = VideoSource.Url;
        player.clip = null;
        string path = Application.streamingAssetsPath.Replace('\\', '/').TrimEnd('/') + "/menu.mp4";
#if UNITY_WEBGL && !UNITY_EDITOR
        player.url = path;
#else
        player.url = new Uri(path).AbsoluteUri;
#endif
        player.isLooping = true;
        // The background movie has no audio; menu music uses its own source.
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.errorReceived += OnVideoError;
    }

    private IEnumerator Start()
    {
        try { player.Prepare(); }
        catch (Exception exception) { OnVideoError(player, exception.Message); }
        double deadline = Time.realtimeSinceStartupAsDouble + 20;
        while (!failed && !player.isPrepared)
        {
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                OnVideoError(player, "Video preparation timed out.");
                yield break;
            }
            yield return null;
        }
        if (failed) yield break;
        try { player.Play(); }
        catch (Exception exception) { OnVideoError(player, exception.Message); }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        if (failed) return;
        failed = true;
        Debug.LogWarning("[MenuBackground] " + message + " Menu remains available.");
        source.Stop();
    }

    private void OnDestroy()
    {
        if (player != null) player.errorReceived -= OnVideoError;
    }
}
