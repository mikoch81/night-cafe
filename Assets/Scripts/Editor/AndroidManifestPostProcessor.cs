using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace NightCafe.EditorTools
{
    /// <summary>
    /// Adds android.permission.VIBRATE to the generated Gradle project.
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

        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
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
    }
}
