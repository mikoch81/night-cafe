using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NightCafe.EditorTools
{
    public static class BuildAndroid
    {
        const string ApkPath = "build/NightCafe.apk";
        const string AabPath = "build/NightCafe.aab";

        /// <summary>
        /// Upload-key credentials, outside the repo (build/ is ignored):
        /// { "keystore": "C:/NIGHT/keys/nightcafe-upload.jks", "keystorePass": "...", "alias": "nightcafe", "aliasPass": "..." }
        /// </summary>
        const string KeystoreConfigPath = "build/keystore.local.json";

        [Serializable]
        sealed class KeystoreConfig
        {
            public string keystore;
            public string keystorePass;
            public string alias;
            public string aliasPass;
        }

        /// <summary>
        /// Development build so the on-screen FPS counter and Unity logcat output stay available.
        /// </summary>
        [MenuItem("NightCafe/Build Android APK")]
        public static void BuildApk()
        {
            Build(ApkPath, BuildOptions.Development, appBundle: false);
        }

        /// <summary>
        /// The Play upload: an AAB, IL2CPP release, ARM64, engine code stripped, no development
        /// flags, signed with the upload key from build/keystore.local.json. The keystore path
        /// and passwords are set for this build only and cleared again, so nothing about the key
        /// lands in ProjectSettings.
        /// </summary>
        [MenuItem("NightCafe/Build Android AAB (Release)")]
        public static void BuildAab()
        {
            KeystoreConfig key = LoadKeystore();
            if (key == null)
                return;

            bool customKeystore = PlayerSettings.Android.useCustomKeystore;
            try
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = key.keystore;
                PlayerSettings.Android.keystorePass = key.keystorePass;
                PlayerSettings.Android.keyaliasName = key.alias;
                PlayerSettings.Android.keyaliasPass = key.aliasPass;
                Build(AabPath, BuildOptions.None, appBundle: true);
            }
            finally
            {
                // Names first, the switch last: with the switch on, an empty name is rewritten
                // as a project-relative one and the switch stays on in the saved settings.
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.useCustomKeystore = customKeystore;
                AssetDatabase.SaveAssets();
            }
        }

        static KeystoreConfig LoadKeystore()
        {
            if (!File.Exists(KeystoreConfigPath))
            {
                Fail($"{KeystoreConfigPath} missing - see the comment in BuildAndroid.cs for its shape.");
                return null;
            }

            var key = JsonUtility.FromJson<KeystoreConfig>(File.ReadAllText(KeystoreConfigPath));
            if (key == null || string.IsNullOrEmpty(key.keystore) || !File.Exists(key.keystore))
            {
                Fail($"keystore not found: {key?.keystore}");
                return null;
            }

            if (string.IsNullOrEmpty(key.alias) || string.IsNullOrEmpty(key.keystorePass) || string.IsNullOrEmpty(key.aliasPass))
            {
                Fail("keystore.local.json needs keystorePass, alias and aliasPass.");
                return null;
            }

            return key;
        }

        static void Build(string outputPath, BuildOptions options, bool appBundle)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Fail("No scenes enabled in Build Settings.");
                return;
            }

            bool release = (options & BuildOptions.Development) == 0;
            if (release && !ReleaseSettingsHold())
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            EditorUserBuildSettings.buildAppBundle = appBundle;
            if (release)
                PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

            var playerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = options
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[NightCafe] Build succeeded: {summary.totalSize / 1024 / 1024} MB -> {outputPath}" +
                          (release ? $" (release, versionCode {PlayerSettings.Android.bundleVersionCode}, {PlayerSettings.bundleVersion})" : ""));
                return;
            }

            Fail($"Build failed: {summary.result} ({summary.totalErrors} errors)");
        }

        /// <summary>
        /// The release checklist the setup cannot guarantee at build time: IL2CPP + ARM64,
        /// engine stripping, no development player, and an application identifier that is
        /// ours. Anything off stops the build rather than shipping it.
        /// </summary>
        static bool ReleaseSettingsHold()
        {
            var android = NamedBuildTarget.Android;
            bool ok = true;
            ok &= Check(PlayerSettings.GetScriptingBackend(android) == ScriptingImplementation.IL2CPP, "scripting backend must be IL2CPP");
            ok &= Check(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "target architecture must be ARM64 only");
            ok &= Check(PlayerSettings.stripEngineCode, "engine code stripping must be on");
            ok &= Check(!EditorUserBuildSettings.development, "development build must be off");
            ok &= Check(!EditorUserBuildSettings.allowDebugging, "script debugging must be off");
            ok &= Check(PlayerSettings.GetApplicationIdentifier(android) == "com.mikoch81.nightcafe", "application identifier");
            ok &= Check((int)PlayerSettings.Android.targetSdkVersion >= 35, "target SDK must be 35 or newer for Play");
            return ok;
        }

        static bool Check(bool condition, string what)
        {
            if (!condition)
                Debug.LogError($"[NightCafe] Release check failed: {what}.");
            return condition;
        }

        static void Fail(string message)
        {
            Debug.LogError($"[NightCafe] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
