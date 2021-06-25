using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Utils
{
    [TestClass]
    public class PerfTimerTests
    {
        const float _100ms = 100/1000f;
        const float _50ms = 50/1000f;
        const float _20ms = 20/1000f;

        public PerfTimerTests()
        {
            PerfTimer.SpinWait(0f); // JIT
        }

        [TestMethod]
        public void PerfTimerWorksAsExpected()
        {
            var timer = new PerfTimer(start:true);
            PerfTimer.SpinWait(_20ms);
            float elapsed = timer.Elapsed;
            Assert.AreEqual(_20ms, elapsed, 0.001f);
        }

        [TestMethod]
        public void TimeUntilNextRefresh()
        {
            var timer = new AggregatePerfTimer(statRefreshInterval:1f);
            timer.Start();
            Assert.AreEqual(1f, timer.TimeUntilNextRefresh, 0.001f);
        }

        [TestMethod]
        public void MultipleEqualSamples()
        {
            var timer = new AggregatePerfTimer(statRefreshInterval:_100ms);

            bool didRefresh = false;
            const int n = 5;
            for (int i = 0; i < n; ++i)
            {
                timer.Start();
                PerfTimer.SpinWait(_20ms);
                didRefresh |= timer.Stop();
            }
            
            Assert.IsTrue(didRefresh, "Timer should have refreshed after 100ms");
            Assert.AreEqual(n, timer.MeasuredSamples);
            Assert.AreEqual(_100ms, timer.MeasuredTotal, 0.01f);
            Assert.AreEqual(_20ms, timer.MeasuredMax, 0.01f);
            Assert.AreEqual(_20ms, timer.AvgTime, 0.01f);
        }

        [TestMethod]
        public void RuntimeLongerThanRefreshInterval()
        {
            var timer = new AggregatePerfTimer(statRefreshInterval:_50ms);
            timer.Start();
            PerfTimer.SpinWait(_50ms);
            bool didRefresh = timer.Stop();
            Assert.AreEqual(_50ms, timer.TimeUntilNextRefresh, 0.001f);
            Assert.IsTrue(didRefresh, "Timer should have refreshed");
            Assert.AreEqual(1, timer.MeasuredSamples);
            Assert.AreEqual(_50ms, timer.MeasuredTotal, 0.001f);
            Assert.AreEqual(_50ms, timer.MeasuredMax, 0.001f);
            Assert.AreEqual(_50ms, timer.AvgTime, 0.001f);
        }
    }
}
