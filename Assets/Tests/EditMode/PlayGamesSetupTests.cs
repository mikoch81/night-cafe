using NightCafe.EditorTools;
using NUnit.Framework;

namespace NightCafe.Tests
{
    /// <summary>Reading the Play Console resource XML (stage 3).</summary>
    public sealed class PlayGamesSetupTests
    {
        const string Resources = @"<?xml version=""1.0"" encoding=""utf-8""?>
<resources>
  <string name=""app_id"" translatable=""false"">123456789012</string>
  <string name=""package_name"" translatable=""false"">com.mikoch81.nightcafe</string>
  <string name=""leaderboard_night_caf__tryb_b"" translatable=""false"">CgkIbbbbbbbbEAIQAg</string>
  <string name=""leaderboard_night_caf__tryb_a"" translatable=""false"">CgkIaaaaaaaaEAIQAQ</string>
</resources>";

        [Test]
        public void FindsBothBoardsByTheirModeLetter()
        {
            Assert.IsTrue(PlayGamesSetup.TryReadLeaderboards(Resources, out string a, out string b, out string error), error);
            Assert.AreEqual("CgkIaaaaaaaaEAIQAQ", a);
            Assert.AreEqual("CgkIbbbbbbbbEAIQAg", b);
        }

        [Test]
        public void RefusesResourcesWithoutTwoBoardsOrAnAppId()
        {
            Assert.IsFalse(PlayGamesSetup.TryReadLeaderboards("<resources><string name=\"app_id\">1</string></resources>",
                out _, out _, out string error));
            StringAssert.Contains("two leaderboards", error);
            Assert.IsFalse(PlayGamesSetup.TryReadLeaderboards(Resources.Replace("app_id", "not_it"), out _, out _, out _));
            Assert.IsFalse(PlayGamesSetup.TryReadLeaderboards("not xml", out _, out _, out _));
        }
    }
}
