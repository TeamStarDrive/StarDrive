// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameClock
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework
{
    internal class GameClock
    {
        private long baseRealTime;
        private long lastRealTime;
        private bool lastRealTimeValid;
        private int suspendCount;
        private long suspendStartTime;
        private long timeLostToSuspension;
        private TimeSpan currentTimeOffset;
        private TimeSpan currentTimeBase;
        private TimeSpan elapsedTime;
        private TimeSpan elapsedAdjustedTime;

        internal TimeSpan CurrentTime => currentTimeBase + currentTimeOffset;
        internal TimeSpan ElapsedTime => elapsedTime;
        internal TimeSpan ElapsedAdjustedTime => elapsedAdjustedTime;

        internal static long Counter => Stopwatch.GetTimestamp();
        internal static readonly long Frequency = Stopwatch.Frequency; // frequency is fixed at boot, so it only has to be queried once

        public GameClock()
        {
            Reset();
        }

        internal void Reset()
        {
            currentTimeBase = TimeSpan.Zero;
            currentTimeOffset = TimeSpan.Zero;
            baseRealTime = Stopwatch.GetTimestamp();
            lastRealTimeValid = false;
        }

        internal void Step()
        {
            long counter = Stopwatch.GetTimestamp();
            if (!lastRealTimeValid)
            {
                lastRealTime = counter;
                lastRealTimeValid = true;
            }

            if (!TryConvertToTimeSpan(counter - baseRealTime, ref currentTimeOffset))
            {
                currentTimeBase += currentTimeOffset;
                baseRealTime = lastRealTime;

                if (!TryConvertToTimeSpan(counter - baseRealTime, ref currentTimeOffset))
                {
                    baseRealTime = counter;
                    currentTimeOffset = TimeSpan.Zero;
                }
            }

            if (!TryConvertToTimeSpan(counter - lastRealTime, ref elapsedTime))
                elapsedTime = TimeSpan.Zero;


            if (!TryConvertToTimeSpan(counter - lastRealTime - timeLostToSuspension, ref elapsedAdjustedTime))
            {
                elapsedAdjustedTime = TimeSpan.Zero;
            }
            else timeLostToSuspension = 0L;

            lastRealTime = counter;
        }

        internal void Suspend()
        {
            ++suspendCount;
            if (suspendCount != 1)
                return;
            suspendStartTime = Stopwatch.GetTimestamp();
        }

        internal void Resume()
        {
            --suspendCount;
            if (suspendCount > 0)
                return;
            timeLostToSuspension += Stopwatch.GetTimestamp() - suspendStartTime;
            suspendStartTime = 0L;
        }

        private static bool TryConvertToTimeSpan(long delta, ref TimeSpan outValue)
        {
            const long num = 10000000;
            if (delta > 922337203685) // will it overflow?
            {
                return false; // failed
            }
            outValue = new TimeSpan(unchecked((delta * num) / Stopwatch.Frequency));
            return true;
        }
    }
}
