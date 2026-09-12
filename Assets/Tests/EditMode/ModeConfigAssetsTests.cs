using NightCafe.Config;
using NightCafe.Services;
using NUnit.Framework;
using UnityEditor;

namespace NightCafe.Tests
{
    /// <summary>
    /// The one mechanical guard for "docs/GDD.md is the source of truth": the shipped config
    /// assets must carry the GDD numbers. If a value here changes, the GDD changes first.
    /// </summary>
    public sealed class ModeConfigAssetsTests
    {
        static ModeConfig Load(string name)
        {
            var config = AssetDatabase.LoadAssetAtPath<ModeConfig>($"Assets/Settings/{name}.asset");
            Assert.IsNotNull(config, $"{name} missing - run NightCafe/Build Scene Setup");
            return config;
        }

        [Test]
        public void ModeAMatchesTheGdd()
        {
            ModeConfig a = Load("ModeConfig_A");

            // 2.2 tempo
            Assert.AreEqual(0.90f, a.baseStepTime, 0.0001f);
            Assert.AreEqual(0.055f, a.stepTimeDecrement, 0.0001f);
            Assert.AreEqual(0.40f, a.minStepTime, 0.0001f);
            Assert.AreEqual(12, a.catchesPerTempoLevel);
            Assert.AreEqual(9, a.maxTempoLevel);
            Assert.AreEqual(0, a.startTempoLevel);

            // 2.3 spawner
            Assert.AreEqual(1.2f, a.firstSpawnDelay, 0.0001f);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, a.intervalMultipliers);
            CollectionAssert.AreEqual(new[] { 0.20f, 0.50f, 0.30f }, a.intervalWeights);
            Assert.AreEqual(2, a.maxCupsPerLane);
            Assert.AreEqual(2, a.minStepGap);
            CollectionAssert.AreEqual(new[] { 2, 2, 2, 3, 3, 3, 4, 4, 4, 4 }, a.screenLimitByTempo);

            // 2.4 - 2.7 score
            Assert.AreEqual(1, a.pointsPerCatch);
            Assert.AreEqual(25, a.comboBonusEvery);
            Assert.AreEqual(5, a.comboBonusPoints);
            Assert.AreEqual(1000, a.rolloverModulo);
            CollectionAssert.AreEqual(new[] { 200, 500 }, a.mercyThresholds);
            Assert.AreEqual(100, a.breatherEvery);
            Assert.AreEqual(2.5f, a.breatherDuration, 0.0001f);

            // 2.5 penalty and presentation
            Assert.AreEqual(3, a.maxStains);
            Assert.AreEqual(0.12f, a.catchPoseDuration, 0.0001f);
            Assert.AreEqual(0.30f, a.missPoseDuration, 0.0001f);
            Assert.AreEqual(0.40f, a.brokenCupDuration, 0.0001f);
            Assert.AreEqual(1.6f, a.catCrossingDuration, 0.0001f);

            // 3 / 6
            Assert.IsFalse(a.ordersEnabled);
            Assert.AreEqual(SkinCatalog.AshId, a.unlockSkinId);
            Assert.AreEqual(250, a.unlockSkinScore);
        }

        [Test]
        public void ModeBDiffersFromAOnlyWhereTheGddSaysSo()
        {
            ModeConfig a = Load("ModeConfig_A");
            ModeConfig b = Load("ModeConfig_B");

            Assert.IsTrue(b.ordersEnabled);
            Assert.AreEqual(2, b.pointsPerCatch);
            Assert.AreEqual(2, b.startTempoLevel);
            Assert.AreEqual(10, b.catchesPerTempoLevel);
            Assert.AreEqual(SkinCatalog.OnyxId, b.unlockSkinId);
            Assert.AreEqual(500, b.unlockSkinScore);

            Assert.AreEqual(0.55f, b.orderedColorWeight, 0.0001f);
            Assert.AreEqual(10, b.orderChangeCatches);
            Assert.AreEqual(20f, b.orderChangeSeconds, 0.0001f);
            Assert.AreEqual(4, b.orderColors.Length);

            // Everything else is shared with Mode A.
            Assert.AreEqual(a.baseStepTime, b.baseStepTime);
            Assert.AreEqual(a.stepTimeDecrement, b.stepTimeDecrement);
            Assert.AreEqual(a.minStepTime, b.minStepTime);
            Assert.AreEqual(a.maxTempoLevel, b.maxTempoLevel);
            Assert.AreEqual(a.firstSpawnDelay, b.firstSpawnDelay);
            CollectionAssert.AreEqual(a.intervalMultipliers, b.intervalMultipliers);
            CollectionAssert.AreEqual(a.intervalWeights, b.intervalWeights);
            Assert.AreEqual(a.maxCupsPerLane, b.maxCupsPerLane);
            Assert.AreEqual(a.minStepGap, b.minStepGap);
            CollectionAssert.AreEqual(a.screenLimitByTempo, b.screenLimitByTempo);
            Assert.AreEqual(a.comboBonusEvery, b.comboBonusEvery);
            Assert.AreEqual(a.comboBonusPoints, b.comboBonusPoints);
            Assert.AreEqual(a.rolloverModulo, b.rolloverModulo);
            CollectionAssert.AreEqual(a.mercyThresholds, b.mercyThresholds);
            Assert.AreEqual(a.breatherEvery, b.breatherEvery);
            Assert.AreEqual(a.breatherDuration, b.breatherDuration);
            Assert.AreEqual(a.maxStains, b.maxStains);
        }
    }
}
