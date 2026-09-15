using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace NightCafe.EditorTools
{
    /// <summary>
    /// Adds android.permission.VIBRATE to the generated Gradle project, and strips the network
    /// permissions Unity merges in on its own (INTERNET, and ACCESS_LOCAL_NETWORK since API 36):
    /// the game never touches the network and the privacy policy says so, so the launcher
    /// manifest removes them with tools:node="remove" before the merge.
    ///
    /// Unity only auto-adds that permission when it sees Handheld.Vibrate in the assemblies.
    /// Our haptics go through the platform Vibrator via JNI (Handheld.Vibrate cannot honour a
    /// duration), so without this the vibrator silently does nothing on device.
    ///
    /// A checked-in Assets/Plugins/Android/AndroidManifest.xml would be the other option, but in
    /// Unity 2020+ that file *replaces* the main manifest, and an incomplete one drops the
    /// activity declaration and boots to a black screen.
    /// </summary>
    public sealed class AndroidManifestPostProcessor : IPostGenerateGradleAndroidProject
    {
        const string Permission = "android.permission.VIBRATE";
        const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        const string ToolsNamespace = "http://schemas.android.com/tools";
        static readonly string[] RemovedPermissions =
        {
            "android.permission.INTERNET",
            "android.permission.ACCESS_LOCAL_NETWORK",
            "android.permission.ACCESS_NETWORK_STATE",
        };

        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // The Gradle project is incremental: both edits must be safe to repeat on a manifest that already has them.
            AddVibrate(Path.Combine(path, "src", "main", "AndroidManifest.xml"));
            RemoveNetworkPermissions(Path.Combine(path, "..", "launcher", "src", "main", "AndroidManifest.xml"));
        }

        static void AddVibrate(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"[NightCafe] No manifest at {manifestPath}; VIBRATE not added.");
                return;
            }

            var document = new XmlDocument();
            document.Load(manifestPath);

            XmlElement manifest = document.DocumentElement;
            if (manifest == null)
                return;

            foreach (XmlNode node in manifest.SelectNodes("uses-permission")!)
            {
                if (node.Attributes?["android:name"]?.Value == Permission)
                    return;
            }

            XmlElement element = document.CreateElement("uses-permission");
            element.SetAttribute("name", AndroidNamespace, Permission);
            manifest.AppendChild(element);
            document.Save(manifestPath);

            Debug.Log("[NightCafe] Added VIBRATE permission to the Android manifest.");
        }

        static void RemoveNetworkPermissions(string launcherManifest)
        {
            if (!File.Exists(launcherManifest))
            {
                Debug.LogWarning($"[NightCafe] No launcher manifest at {launcherManifest}; network permissions stay.");
                return;
            }

            var document = new XmlDocument();
            document.Load(launcherManifest);
            XmlElement manifest = document.DocumentElement;
            if (manifest == null)
                return;

            if (!manifest.HasAttribute("xmlns:tools"))
                manifest.SetAttribute("xmlns:tools", ToolsNamespace);

            foreach (string permission in RemovedPermissions)
            {
                bool present = false;
                foreach (XmlNode node in manifest.SelectNodes("uses-permission")!)
                    present |= node.Attributes?["android:name"]?.Value == permission;
                if (present)
                    continue;

                XmlElement element = document.CreateElement("uses-permission");
                element.SetAttribute("name", AndroidNamespace, permission);
                element.SetAttribute("node", ToolsNamespace, "remove");
                manifest.AppendChild(element);
            }

            document.Save(launcherManifest);
            Debug.Log("[NightCafe] Network permissions removed from the launcher manifest (offline game).");
        }
    }
}
