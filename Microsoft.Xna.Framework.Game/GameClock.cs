namespace Microsoft.Xna.Framework;

// game clock that accounts for suspended time
internal class GameClock
{
    long lastRealTime;
    long suspendStartTime;
    long timeLostToSuspension;
    int suspendCount;
    bool lastRealTimeValid;

    readonly double InvFrequency;

    public GameClock()
    {
        NativeMethods.QueryPerformanceFrequency(out long frequency);
        InvFrequency = 1.0 / frequency;
        Reset();
    }

    internal void Reset()
    {
        lastRealTimeValid = false;
    }

    /// <summary>
    /// Take a new clock step, account for suspended time, and return real time since last step
    /// </summary>
    internal double Step()
    {
        NativeMethods.QueryPerformanceCounter(out long now);
        if (!lastRealTimeValid)
        {
            lastRealTime = now;
            lastRealTimeValid = true;
        }

        long elapsedTicks = now - (lastRealTime + timeLostToSuspension);
        double elapsedAdjusted = elapsedTicks * InvFrequency;
        if (elapsedAdjusted < 0.0)
            elapsedAdjusted = 0.0;

        timeLostToSuspension = 0L;
        lastRealTime = now;

        return elapsedAdjusted;
    }

    internal void Suspend()
    {
        ++suspendCount;
        if (suspendCount != 1)
            return;

        NativeMethods.QueryPerformanceCounter(out suspendStartTime);
    }

    internal void Resume()
    {
        --suspendCount;
        if (suspendCount > 0)
            return;

        NativeMethods.QueryPerformanceCounter(out long timeStamp);

        timeLostToSuspension += timeStamp - suspendStartTime;
        suspendStartTime = 0L;
    }
}
