using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace NightCafe.EditorTools
{
    /// <summary>
    /// Stage 3 of 1.1.0: turns the Play Console's resource XML (Play Games Services → Leaderboards →
    /// Get resources → Android, saved as docs/store/games-ids.xml - docs/store/PLAY_GAMES_SETUP.md)
    /// into a configured build in one step: the plugin's own setup (app id, its manifest library,
    /// GameInfo, a GPGSIds constants class) and our PlayGamesIds with the two leaderboards.
    /// </summary>
    public static class PlayGamesSetup
    {
        public const string ResourcesPath = "docs/store/games-ids.xml";
        const string IdsPath = "Assets/Scripts/PlayGames/PlayGamesIds.cs";
        const string ConstantsDirectory = "Assets/Scripts/PlayGames";

        [MenuItem("NightCafe/Apply Play Games Resources")]
        public static void Apply()
        {
            if (!File.Exists(ResourcesPath))
            {
                Debug.LogError($"[NightCafe] {ResourcesPath} is missing: save the Play Console resource XML there first.");
                return;
            }

            string xml = File.ReadAllText(ResourcesPath);
            if (!TryReadLeaderboards(xml, out string modeA, out string modeB, out string error))
            {
                Debug.LogError($"[NightCafe] {error}");
                return;
            }

            // The plugin's setup, through reflection so the project compiles without its editor assembly.
            Type setup = Type.GetType("GooglePlayGames.Editor.GPGSAndroidSetupUI, Google.Play.Games.Editor");
            var perform = setup?.GetMethod("PerformSetup", new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
            if (perform == null)
            {
                Debug.LogError("[NightCafe] Google Play Games plugin not found (Assets/GooglePlayGames).");
                return;
            }

            bool ok = (bool)perform.Invoke(null, new object[] { "", ConstantsDirectory, "NightCafe.PlayGames.GPGSIds", xml, null });
            if (!ok)
            {
                Debug.LogError("[NightCafe] The plugin's setup refused the resources (see its dialog).");
                return;
            }

            File.WriteAllText(IdsPath, IdsSource(modeA, modeB), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(IdsPath);
            Debug.Log($"[NightCafe] Play Games set up: mode A {modeA}, mode B {modeB}.");
        }

        /// <summary>
        /// The two leaderboards by the end of their resource name (…_a / …_b, from "Tryb A" / "Mode B");
        /// with no such names and exactly two boards, the first is A.
        /// </summary>
        public static bool TryReadLeaderboards(string xml, out string modeA, out string modeB, out string error)
        {
            modeA = modeB = null;
            error = null;
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception e)
            {
                error = "The resource XML does not parse: " + e.Message;
                return false;
            }

            var boards = doc.Descendants("string")
                .Where(e => ((string)e.Attribute("name") ?? "").StartsWith("leaderboard_"))
                .Select(e => (name: (string)e.Attribute("name"), id: e.Value.Trim()))
                .ToList();

            modeA = boards.FirstOrDefault(b => b.name.EndsWith("_a")).id;
            modeB = boards.FirstOrDefault(b => b.name.EndsWith("_b")).id;
            if ((modeA == null || modeB == null) && boards.Count == 2)
            {
                modeA = boards[0].id;
                modeB = boards[1].id;
            }

            if (string.IsNullOrEmpty(modeA) || string.IsNullOrEmpty(modeB))
            {
                error = $"Expected two leaderboards (mode A and B) in the resources, found {boards.Count}.";
                return false;
            }

            bool hasAppId = doc.Descendants("string").Any(e => (string)e.Attribute("name") == "app_id");
            if (!hasAppId)
            {
                error = "The resources have no app_id.";
                return false;
            }

            return true;
        }

        static string IdsSource(string modeA, string modeB) =>
$@"namespace NightCafe.PlayGames
{{
    /// <summary>
    /// The leaderboard ids from Play Console, written by NightCafe/Apply Play Games Resources from
    /// {ResourcesPath} (docs/store/PLAY_GAMES_SETUP.md). Not secrets.
    /// </summary>
    public static class PlayGamesIds
    {{
        public const string LeaderboardModeA = ""{modeA}"";
        public const string LeaderboardModeB = ""{modeB}"";
    }}
}}
";
    }
}
