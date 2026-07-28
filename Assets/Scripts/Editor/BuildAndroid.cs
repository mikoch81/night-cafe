using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NightCafe.EditorTools
{
    public static class BuildAndroid
    {
        const string OutputPath = "build/NightCafe.apk";

        /// <summary>
        /// Development build so the on-screen FPS counter and Unity logcat output stay available.
        /// </summary>
        [MenuItem("NightCafe/Build Android APK")]
        public static void BuildApk()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[NightCafe] No scenes enabled in Build Settings.");
                EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[NightCafe] Build succeeded: {summary.totalSize / 1024 / 1024} MB -> {OutputPath}");
                return;
            }

            Debug.LogError($"[NightCafe] Build failed: {summary.result} ({summary.totalErrors} errors)");
            EditorApplication.Exit(1);
        }
    }
}
