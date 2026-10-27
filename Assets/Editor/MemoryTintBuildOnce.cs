using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MemoryTintBuildOnce
{
    static MemoryTintBuildOnce() { EditorApplication.update += Run; }
    private static void Run()
    {
        string root = Path.GetDirectoryName(Application.dataPath);
        string request = Path.Combine(root, "Library/MemoryTintBuild.request");
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        File.Delete(request);
        string result = Path.Combine(root, "Library/MemoryTintBuild-result.txt");
        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new Exception("Select the Web platform before building.");
            File.WriteAllText(result, "RUNNING " + DateTime.Now.ToString("O"));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(root, "Builds/MemoryTint-WebGL"),
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            File.WriteAllText(result, report.summary.result + " errors=" + report.summary.totalErrors +
                " warnings=" + report.summary.totalWarnings + " duration=" + report.summary.totalTime);
        }
        catch (Exception e) { File.WriteAllText(result, "FAILED " + e); Debug.LogException(e); }
    }
}
