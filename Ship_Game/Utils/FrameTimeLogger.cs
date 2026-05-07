using System;
using System.IO;
using SDUtils;

namespace Ship_Game.Utils;

// Always-on per-frame perf trace. Writes one CSV row per frame to
// x64Migration/phase4-logs/perf-baseline/frames.csv. Used to capture
// the §4.1 baseline and as the reference data source for §4.4.
//
// Columns: WallSec,FrameId,TopScreen,UpdateMs,DrawMs,TotalMs
// TotalMs is wall delta from previous frame's EndFrame (frame interval).
public static class FrameTimeLogger
{
    static StreamWriter Writer;
    static long T0;
    static long UpdateStart;
    static long UpdateEnd;
    static long DrawStart;
    static long PrevFrameEnd;
    static int FrameId;
    static int UnflushedRows;
    static bool Initialized;
    static bool Failed;

    public static void Init(string path)
    {
        if (Initialized) return;
        Initialized = true;
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            Writer = new StreamWriter(path, append: false) { NewLine = "\n" };
            Writer.WriteLine("WallSec,FrameId,TopScreen,UpdateMs,DrawMs,TotalMs");
            // PerfTimer.InvFrequency is initialized lazily by the PerfTimer ctor;
            // the static GetTicks alone won't trigger it. Force it here so that
            // every frame from frame 1 has correct wall-time math.
            if (PerfTimer.InvFrequency == 0)
                PerfTimer.GetFrequency(out PerfTimer.Frequency, out PerfTimer.InvFrequency);
            PerfTimer.GetTicks(out T0);
            PrevFrameEnd = T0;
            Log.Info($"FrameTimeLogger writing to {path}");
        }
        catch (Exception ex)
        {
            Log.Warning($"FrameTimeLogger init failed: {ex.Message}");
            Failed = true;
            Writer = null;
        }
    }

    public static void BeginUpdate()
    {
        if (Failed || !Initialized) return;
        PerfTimer.GetTicks(out UpdateStart);
    }

    public static void EndUpdate()
    {
        if (Failed || !Initialized) return;
        PerfTimer.GetTicks(out UpdateEnd);
    }

    public static void BeginDraw()
    {
        if (Failed || !Initialized) return;
        PerfTimer.GetTicks(out DrawStart);
    }

    public static void EndFrame(string topScreen)
    {
        if (Failed || !Initialized || Writer == null) return;
        try
        {
            PerfTimer.GetTicks(out long now);
            double inv = PerfTimer.InvFrequency;
            double wallSec  = (now - T0) * inv;
            double updateMs = (UpdateEnd - UpdateStart) * inv * 1000.0;
            double drawMs   = (now - DrawStart) * inv * 1000.0;
            double totalMs  = (now - PrevFrameEnd) * inv * 1000.0;
            PrevFrameEnd = now;
            FrameId++;
            Writer.Write(wallSec.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
            Writer.Write(',');
            Writer.Write(FrameId);
            Writer.Write(',');
            Writer.Write(topScreen ?? "");
            Writer.Write(',');
            Writer.Write(updateMs.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            Writer.Write(',');
            Writer.Write(drawMs.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            Writer.Write(',');
            Writer.WriteLine(totalMs.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            UnflushedRows++;
            if (UnflushedRows >= 60) // ~1s at 60fps
            {
                Writer.Flush();
                UnflushedRows = 0;
            }
        }
        catch
        {
            Failed = true;
        }
    }

    public static void Stop()
    {
        if (Writer == null) return;
        try { Writer.Flush(); Writer.Dispose(); } catch { }
        Writer = null;
    }
}
