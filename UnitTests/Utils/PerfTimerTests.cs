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
    public class PerfTimerTests : StarDriveTest
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
            PerfTimer.SpinWait(0f); // JIT
            var timer = new PerfTimer(start:true);
            PerfTimer.SpinWait(_20ms);
            float elapsed = timer.Elapsed;
            AssertEqual(0.005f, _20ms, elapsed);
        }

        [TestMethod]
        public void TimeUntilNextRefresh()
        {
            var timer = new AggregatePerfTimer(statRefreshInterval:1f);
            timer.Start();
            AssertEqual(0.001f, 1f, timer.TimeUntilNextRefresh);
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
            AssertEqual(n, timer.MeasuredSamples);
            AssertEqual(0.01f, _100ms, timer.MeasuredTotal);
            AssertEqual(0.01f, _20ms, timer.MeasuredMax);
            AssertEqual(0.01f, _20ms, timer.AvgTime);
        }

        [TestMethod]
        public void RuntimeLongerThanRefreshInterval()
        {
            var timer = new AggregatePerfTimer(statRefreshInterval:_50ms);
            timer.Start();
            PerfTimer.SpinWait(_50ms);
            bool didRefresh = timer.Stop();
            AssertEqual(0.001f, _50ms, timer.TimeUntilNextRefresh);
            Assert.IsTrue(didRefresh, "Timer should have refreshed");
            AssertEqual(1, timer.MeasuredSamples);
            AssertEqual(0.001f,_50ms, timer.MeasuredTotal);
            AssertEqual(0.001f,_50ms, timer.MeasuredMax);
            AssertEqual(0.001f,_50ms, timer.AvgTime);
        }
    }
}
