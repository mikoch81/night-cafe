using NightCafe.Core;
using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    /// <summary>The online top 10 (stage 3): offline until Play Games is set up, and harmless then.</summary>
    public sealed class OnlineScoreTests
    {
        [Test]
        public void WithoutPlayGamesTheGameStaysOffline()
        {
            // The editor never installs the Play Games backend (it needs an Android device and the ids).
            IOnlineScores online = OnlineScores.Backend;
            Assert.IsFalse(online.Available);
            Assert.IsFalse(online.SignedIn);

            bool? signedIn = null;
            online.SignIn(ok => signedIn = ok);
            Assert.AreEqual(false, signedIn, "asked to sign in, it answers no at once instead of hanging");

            Assert.DoesNotThrow(() =>
            {
                online.SignInSilently();
                online.Submit(GameMode.A, 120);
                online.ShowBoard(GameMode.B);
            });
        }

        [Test]
        public void TheMenuRowIsNamedInBothLanguages()
        {
            try
            {
                Assert.AreEqual("ONLINE TOP 10", Loc.T(Txt.OnlineTopTen));
                Loc.Set(Language.Polish);
                Assert.AreEqual("RANKING ONLINE", Loc.T(Txt.OnlineTopTen));
            }
            finally
            {
                Loc.Set(Language.English);
            }
        }
    }
}
