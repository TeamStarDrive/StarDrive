using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.GameScreens.NewGame;

namespace UnitTests
{
    [TestClass]
    public class TestProgressCounter
    {
        static void LinearSubStep(ProgressCounter subStep, int max)
        {
            subStep.Start(max);
            for (int i = 0; i < max; ++i) subStep.Advance();
        }

        [TestMethod]
        public void TestLinearProgress()
        {
            var counter = new ProgressCounter();
            counter.Start(0.3f, 0.3f, 0.4f);

            LinearSubStep(counter.NextStep(), 11);
            Assert.AreEqual(counter.Percent, 0.3f);

            LinearSubStep(counter.NextStep(), 17);
            Assert.AreEqual(counter.Percent, 0.6f);

            LinearSubStep(counter.NextStep(), 33);
            Assert.AreEqual(counter.Percent, 1.0f);
        }

        static void MultiTierSubStep(ProgressCounter subStep)
        {
            subStep.Start(0.5f, 0.4f, 0.1f);
            LinearSubStep(subStep.NextStep(), 17);
            Assert.AreEqual(subStep.Percent, 0.5f);
            LinearSubStep(subStep.NextStep(), 10);
            Assert.AreEqual(subStep.Percent, 0.9f);
            LinearSubStep(subStep.NextStep(), 5);
            Assert.AreEqual(subStep.Percent, 1.0f);
        }

        [TestMethod]
        public void TestMultiTierProgress()
        {
            var counter = new ProgressCounter();
            counter.Start(0.25f, 0.25f, 0.5f);

            MultiTierSubStep(counter.NextStep());
            Assert.AreEqual(counter.Percent, 0.25f);
            
            MultiTierSubStep(counter.NextStep());
            Assert.AreEqual(counter.Percent, 0.5f);

            MultiTierSubStep(counter.NextStep());
            Assert.AreEqual(counter.Percent, 1.0f);
        }
    }
}
