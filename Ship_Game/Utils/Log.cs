using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SDUtils;
using Sentry;
using Ship_Game.Universe;
using Ship_Game.Utils;
#pragma warning disable CA1060

namespace Ship_Game
{
    public static class Log
    {
        [Flags]
        enum LogTarget
        {
            Console = 1,
            LogFile = 2,
            ConsoleAndLog = Console | LogFile
        }

        struct LogEntry
        {
            public DateTime Time;
            public string Message;
            public ConsoleColor Color;
            public LogTarget Target;
        }

        static StreamWriter LogFile;
        static Thread LogThread;
        static readonly SafeQueue<LogEntry> LogQueue = new(64);
        public static bool HasDebugger;

        // Either there is an active Console Window
        // OR Console output is redirected to some pipe, like VS Debug Output
        public static bool HasActiveConsole { get; private set; }

        const ConsoleColor DefaultColor = ConsoleColor.Gray;
        static ConsoleColor CurrentColor = DefaultColor;
        static readonly object Sync = new();

        /// <summary>
        /// If TRUE, then [INFO] messages will be written to LogFile
        /// If FALSE, then [INFO] messages will only be seen in console
        /// </summary>
        public static bool VerboseLogging = false;

        // sentry.io automatic crash reporting
        static IDisposable Sentry;

        /// <summary>
        /// Whether we should report error statistics and other telemetry to ensure quality
        /// </summary>
        public static bool IsStatsReportEnabled => Sentry != null && GlobalStats.AutoErrorReport;

        /// <summary>
        /// Whether to include SaveGame files in Sentry reports.
        /// This can take a huge amount of data usage budget.
        /// </summary>
        public static bool IncludeSaveGameInReports = false;

        // Max bytes of the blackbox.log TAIL attached to Sentry error reports.
        // The whole file is no longer attached, and nothing is attached to the
        // frequent Info/stats events — that scope-level whole-file attachment was
        // the main Sentry storage cost. See ConfigureStatsReporter / CaptureEvent.
        const int SentryLogTailBytes = 128 * 1024;

        // Last save path, cached by ConfigureStatsReporter so error events can
        // optionally attach the save (only when IncludeSaveGameInReports is true).
        static string LastAutoSavePath;

        // prevent flooding Sentry with 2000 error messages if we fall into an exception loop
        // instead, we count identical exceptions and resend them only over a certain threshold
        static readonly Map<ulong, int> ReportedErrors = new();
        const int ErrorThreshold = 100;

        /// <summary>
        /// Whether the Application is currently handling a fatal error, and will terminate soon
        /// </summary>
        public static bool IsTerminating { get; private set; }

        static readonly Array<Thread> MonitoredThreads = new();

        public static string LogFilePath { get; private set; }
        public static string OldLogFilePath { get; private set; }

        // official releases version as exactly Major.Minor.Patch, e.g. "1.60.00046 jupiter-1.60"
        public static bool IsOfficialVersionFormat(string version)
        {
            string[] parts = version.Split(' ')[0].Split('.');
            if (parts.Length != 3)
                return false;
            for (int i = 0; i < parts.Length; ++i)
                if (!uint.TryParse(parts[i], out _))
                    return false;
            return true;
        }

        public static void Initialize(bool enableSentry, bool showHeader)
        {
            if (LogThread != null)
                return; // already initialized!

            HasDebugger = Debugger.IsAttached;

            if (LogFile == null)
            {
                bool isReadOnly = Directory.GetCurrentDirectory().Contains("Program Files");
                string logDir = isReadOnly ? Dir.StarDriveAppData + "/" : "";

                LogFilePath = $"{logDir}blackbox.log";
                OldLogFilePath = $"{logDir}blackbox.old.log";
                if (File.Exists(LogFilePath))
                    File.Copy(LogFilePath, OldLogFilePath, true);
                LogFile = OpenLog(LogFilePath);
            }

            LogThread = new Thread(LogAsyncWriter) { Name = "AsyncLogWriter" };
            LogThread.Start();

            string environment = "Release";

            if (HasDebugger)
            {
                VerboseLogging = true;
                environment = "Staging";

                // if Console output is redirected, all console text is sent to VS Output instead
                // in that case, showing the console is pointless, however if output isn't redirected
                // we should enable the console window
                if (Console.IsOutputRedirected == false)
                    ShowConsoleWindow();
                else
                    HasActiveConsole = true;
            }
            else
            {
            #if DEBUG
                environment = "Staging";
            #else
                environment = GlobalStats.Version.ToLower().Contains("test") ? "Test" : "Release";
            #endif

            #if !DEBUG
                // Only inspect Console.Title when a real console is attached.
                // Mars 1.51 ran on net48 with <OutputType>Exe</OutputType> — every
                // process had a console, so reading Console.Title was always safe.
                // Jupiter (.NET 8 + <OutputType>WinExe</OutputType>) has no console
                // when launched from Explorer or NSIS finishpage, and the property
                // throws "The handle is invalid" in that case. IsOutputRedirected
                // returns true when there's no console attached, so use that as
                // the guard (mirroring the HasDebugger branch above).
                if (Console.IsOutputRedirected == false)
                {
                    bool shouldHideConsole = Console.Title.Contains("\\StarDrive.exe");
                    if (shouldHideConsole)
                        HideConsoleWindow();
                }
            #endif
            }

            if (showHeader) // only write log header in main game
            {
                string init = "\r\n";
                init +=  " ======================================================\r\n";
                init += $" ==== {GlobalStats.ExtendedVersion,-44} ====\r\n";
                init += $" ==== UTC: {DateTime.UtcNow,-39} ====\r\n";
                init +=  " ======================================================\r\n";
                LogWriteAsync(init, ConsoleColor.Green);
            }

            if (enableSentry && !IsOfficialVersionFormat(GlobalStats.Version))
            {
                // forked builds tag extra version parts (e.g. "1.60.00046.123") and
                // must not report into the official Sentry project
                enableSentry = false;
                LogWriteAsync($"Sentry disabled: unofficial version format '{GlobalStats.Version}'", ConsoleColor.Yellow);
            }

            if (enableSentry)
            {
                try // init can fail due to some .NET issues, in this case just give up, because it's rare
                {
                    Sentry = SentrySdk.Init(o =>
                    {
                        o.Dsn = "https://4f9d175d7aab41d0a82cccde4161dc35@o57461.ingest.sentry.io/4504827693367296";
                        // When configuring for the first time, to see what the SDK is doing:
                        //o.Debug = true;
                        o.Environment = environment;
                        var versionParts = GlobalStats.Version.Split(' '); // "1.30.13000 release/mars-1.41/f83ab4a"
                        o.Release = versionParts[0]; // 1.30.13000
                        if (versionParts.Length > 1)
                            o.Distribution = versionParts[1].Replace('/', '-'); // release/mars-1.41/f83ab4a -> release-mars-1.41-f83ab4a
                        o.IsGlobalModeEnabled = true;
                        o.CacheDirectoryPath = Dir.StarDriveAppData;

                        // send usage statistics to Sentry
                        if (GlobalStats.AutoErrorReport)
                            o.AutoSessionTracking = true;
                    });
                }
                catch (Exception e)
                {
                    Log.Error($"Sentry init failed: {e.Message}");
                }
                ConfigureStatsReporter(null);
            }
        }

        public static void Close()
        {
            try
            {
                SentrySdk.Flush(TimeSpan.FromSeconds(5));
                Mem.Dispose(ref Sentry);
            }
            catch
            {
            }
            StopLogThread();
            FlushAllLogs();
            // Dispose the file stream so another process can open blackbox.log
            // (the AutoPatcher self-elevation flow spawns an elevated child that
            // needs write access to this exact path before the parent exits).
            // Lock(Sync) so any in-flight LogAsyncWriter iteration completes
            // before we tear the stream down. Idempotent — Log.Write/Flush all
            // null-check LogFile, so subsequent writes become safe no-ops.
            lock (Sync)
            {
                try { LogFile?.Dispose(); } catch { }
                LogFile = null;
            }
        }

        public static void AddThreadMonitor()
        {
            var current = Thread.CurrentThread;
            lock (MonitoredThreads)
                MonitoredThreads.Add(current);
        }

        public static void RemoveThreadMonitor()
        {
            var current = Thread.CurrentThread;
            lock (MonitoredThreads)
                MonitoredThreads.Remove(current);
        }

        static StreamWriter OpenLog(string logPath)
        {
            // FileMode.Create truncates an existing log to zero bytes. If a
            // previous process (typically the parent in a patch-relaunch
            // handoff) hasn't fully released the handle yet, the open throws
            // IOException "file is being used by another process". Retry
            // briefly so a stale lock from a process that's about to exit
            // doesn't kill the new instance on first boot. Parent-side fixes
            // (AutoPatcher.RelaunchAsAdminWithMarker / RestartAsync both call
            // Log.Close before Process.Start) handle the common case; this
            // child-side retry covers the edge cases — kernel-level
            // handle-release latency under UAC consent.exe handoff, Sentry
            // SDK background threads briefly touching the file during
            // shutdown, antivirus scanners holding the file open after the
            // process exits, etc.
            // Only retry on the sharing/lock HRESULTs that mean "another process
            // still has the file" — retrying on disk-full, perm-denied, or
            // invalid-path just delays surfacing the real error by ~1s.
            const int errorSharingViolation = unchecked((int)0x80070020); // ERROR_SHARING_VIOLATION
            const int errorLockViolation    = unchecked((int)0x80070021); // ERROR_LOCK_VIOLATION
            const int maxAttempts = 10;
            const int retryDelayMs = 100;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return new StreamWriter(
                        stream: File.Open(logPath, FileMode.Create, FileAccess.Write, FileShare.Read),
                        encoding: Encoding.ASCII,
                        bufferSize: 32 * 1024)
                    {
                        AutoFlush = true
                    };
                }
                catch (IOException ex) when (attempt < maxAttempts
                    && (ex.HResult == errorSharingViolation || ex.HResult == errorLockViolation))
                {
                    Thread.Sleep(retryDelayMs);
                }
            }
        }

        static void SetConsoleColor(ConsoleColor color, bool force)
        {
            if (force || CurrentColor != color)
            {
                try
                {
                    Console.ForegroundColor = color;
                    CurrentColor = color;
                }
                catch
                {
                    HasActiveConsole = false;
                    CurrentColor = DefaultColor;
                }
            }
        }

        // specialized for Log Entry formatting, because it's very slow
        class LogStringBuffer
        {
            public char[] Characters = new char[1024 * 32];
            public int Length;

            public void Append(char ch)
            {
                Characters[Length++] = ch;
            }
            public void Append(string s)
            {
                int n = s.Length;
                int pos = Expand(n);
                s.CopyTo(0, Characters, pos, n);
            }
            int Expand(int count)
            {
                int len = Length;
                int newLength = len + count;
                if (newLength > Characters.Length)
                {
                    int newCapacity = Characters.Length * 2;
                    while (newCapacity < newLength) newCapacity *= 2;

                    char[] newChars = new char[newCapacity];
                    Array.Copy(Characters, newChars, len);
                    Characters = newChars;
                }
                Length = newLength;
                return len;
            }
            // it only outputs 2 char length positive integers
            // and always prefixes with 0
            public void AppendInt2Chars(int value)
            {
                Characters[Length++] = (char)('0' + ((value / 10) % 10));
                Characters[Length++] = (char)('0' + (value % 10));
            }
            public void AppendInt3Chars(int value)
            {
                Characters[Length++] = (char)('0' + ((value / 100) % 10));
                Characters[Length++] = (char)('0' + ((value / 10) % 10));
                Characters[Length++] = (char)('0' + (value % 10));
            }
            public void Clear()
            {
                Length = 0;
            }
        }

        static void WriteLogEntry(LogStringBuffer sb, in LogEntry log)
        {
            TimeSpan t = log.Time.TimeOfDay;
            sb.Clear();
            sb.AppendInt2Chars(t.Hours);
            sb.Append(':');
            sb.AppendInt2Chars(t.Minutes);
            sb.Append(':');
            sb.AppendInt2Chars(t.Seconds);
            sb.Append('.');
            sb.AppendInt3Chars(t.Milliseconds);
            sb.Append('m');
            sb.Append('s');
            sb.Append(':');
            sb.Append(' ');
            sb.Append(log.Message);
            sb.Append('\n');

            if ((log.Target & LogTarget.LogFile) != 0)
            {
                LogFile?.Write(sb.Characters, 0, sb.Length);
            }

            if ((log.Target & LogTarget.Console) != 0 && HasActiveConsole)
            {
                try
                {
                    SetConsoleColor(log.Color, force: false);
                    Console.Write(sb.Characters, 0, sb.Length);
                }
                catch // Console handle invalid
                {
                    HasActiveConsole = false;
                }
            }
        }

        static readonly LogStringBuffer LogBuffer = new LogStringBuffer();

        public static void FlushAllLogs()
        {
            lock (Sync) // synchronize with LogAsyncWriter()
            {
                foreach (LogEntry log in LogQueue.TakeAll())
                    WriteLogEntry(LogBuffer, log);
                LogFile?.Flush();
                SetConsoleColor(DefaultColor, force: true);
            }
        }

        static void LogAsyncWriter()
        {
            while (LogThread != null)
            {
                lock (Sync) // synchronize with FlushAllLogs()
                {
                    if (LogQueue.WaitDequeue(out LogEntry log, 15))
                    {
                        WriteLogEntry(LogBuffer, log);
                        foreach (LogEntry log2 in LogQueue.TakeAll())
                            WriteLogEntry(LogBuffer, log2);
                    }
                }
            }
        }

        static void StopLogThread()
        {
            LogThread = null;
            lock (Sync)
            {
                LogQueue.Notify();
            }
        }

        static void LogWriteAsync(string text, ConsoleColor color, LogTarget target = LogTarget.ConsoleAndLog)
        {
            // We don't lock here because LogQueue itself is ThreadSafe
            // ReSharper disable once InconsistentlySynchronizedField
            LogQueue.Enqueue(new LogEntry
            {
                Time = DateTime.UtcNow,
                Message = text,
                Color = color,
                Target = target,
            });
        }

        public enum GameEvent
        {
            NewGame,
            LoadGame,
            YouWin,
            YouLose,
            AutoUpdateClicked, // user has clicked on the Auto-Update banner
            AutoUpdateStarted, // user actually OK'd the Auto-Update process
            AutoUpdateFinished, // auto-update actually finished
            AutoUpdateFailed, // auto-update failed somehow
        }

        /// <summary>
        /// Logs event statistics to Sentry if AutoErrorReport is enabled
        /// </summary>
        public static void LogEventStats(GameEvent evt, UniverseParams p = null, string message = "")
        {
            if (!IsStatsReportEnabled)
                return;

            string evtMessage = message.NotEmpty() ? (evt + " " + message) : evt.ToString();
            Write($"GameEvent: {evtMessage}");

            SentryEvent e = new()
            {
                Level = SentryLevel.Info,
                Message = evtMessage,
            };

            e.SetTag("Mod", GlobalStats.ModOrVanillaName);
            e.SetTag("ModVersion", GlobalStats.ModVersion);
            e.SetTag("TimesPlayed", GlobalStats.TimesPlayed.ToString());

            if (p != null)
            {
                e.SetTag("Difficulty", p.Difficulty.ToString());
                e.SetTag("StarsCount", p.StarsCount.ToString());
                e.SetTag("GalaxySize", p.GalaxySize.ToString());
                e.SetTag("ExtraRemnant", p.ExtraRemnant.ToString());
                e.SetTag("NumSystems", p.NumSystems.ToString());
                e.SetTag("NumOpponents", p.NumOpponents.ToString());
                e.SetTag("GameMode", p.Mode.ToString());
                e.SetTag("Pace", p.Pace.String(2));
                e.SetTag("GameMode", p.Mode.ToString());
                e.SetTag("ExtraPlanets", p.ExtraPlanets.ToString());
            }
            SentrySdk.CaptureEvent(e);
        }

        /// <summary>
        /// Configures sentry stats reporter
        /// </summary>
        /// <param name="autoSavePath">If `IncludeSaveGameInReports` == true, then this autosave will be attached to the report</param>
        public static void ConfigureStatsReporter(string autoSavePath = null)
        {
            if (!IsStatsReportEnabled)
                return;

            // Straight assignment (not ??): a null path clears the cached save so it
            // stops riding error events. UniverseScreen calls ConfigureStatsReporter(null)
            // on exit precisely to reset the latest savegame attachment.
            LastAutoSavePath = autoSavePath;
            SentrySdk.ConfigureScope(scope =>
            {
                if (!scope.HasUser())
                {
                    scope.User.Username = Environment.UserName;
                }

                // NOTE: blackbox.log is intentionally NOT attached on the scope.
                // A scope attachment rides EVERY captured event — including the
                // frequent Info/stats events (LogEventStats) — and uploaded the
                // whole, ever-growing log each time, which was the dominant Sentry
                // storage cost (exceeded the 1GB quota in ~3 days). Error/Fatal
                // events now attach only a bounded log TAIL, added per-event in
                // CaptureEvent; stats events carry no attachment at all.
                scope.ClearAttachments();

                if (GlobalStats.HasMod)
                {
                    scope.SetTag("Mod", GlobalStats.ModName);
                    scope.SetTag("ModVersion", GlobalStats.ModVersion);
                }
            });
        }

        // Reads up to maxBytes from the END of blackbox.log so error reports can
        // attach a bounded tail instead of the whole (unbounded) session log.
        static byte[] ReadLogTail(int maxBytes)
        {
            try
            {
                using var fs = new FileStream(LogFilePath, FileMode.Open,
                                              FileAccess.Read, FileShare.ReadWrite);
                long start = Math.Max(0, fs.Length - maxBytes);
                fs.Seek(start, SeekOrigin.Begin);
                int count = (int)(fs.Length - start);
                var buf = new byte[count];
                int read = 0;
                while (read < count)
                {
                    int n = fs.Read(buf, read, count - read);
                    if (n <= 0) break;
                    read += n;
                }
                if (read == count)
                    return buf;
                var trimmed = new byte[read]; // slice-free: repo aliases Range to SDGraphics.Range
                Array.Copy(buf, trimmed, read);
                return trimmed;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Truncates the live blackbox.log to zero bytes mid-session (called after
        /// each autosave) to bound log growth and the tail attached to Sentry error
        /// reports. Full rotation to blackbox.old still happens once at startup; this
        /// is an in-session reset only. Best-effort — never throws.
        /// </summary>
        public static void ResetCurrentLog()
        {
            lock (Sync) // synchronize with LogAsyncWriter()
            {
                if (LogFile == null)
                    return;
                try
                {
                    // AutoFlush keeps the writer buffer empty, and holding Sync blocks
                    // the async writer, so the stream is quiescent here. Truncate it.
                    // Any entries still queued (the newest lines) are left in the queue
                    // and get written fresh at the top of the reset log afterwards.
                    Stream bs = LogFile.BaseStream;
                    LogFile.Flush();
                    bs.SetLength(0);
                    bs.Seek(0, SeekOrigin.Begin);
                    LogFile.Write($" ==== blackbox.log reset after autosave  UTC: {DateTime.UtcNow}  {GlobalStats.ExtendedVersion} ====\r\n");
                    LogFile.Flush();
                }
                catch
                {
                    // logging cleanup must never crash the game
                }
            }
        }

        // just echo info to console, don't write to logfile
        // not used in release builds or if there's no debugger attached
        [Conditional("DEBUG")] public static void Info(string text)
        {
            LogWriteAsync(text, DefaultColor, VerboseLogging ? LogTarget.ConsoleAndLog : LogTarget.Console);
        }
        [Conditional("DEBUG")] public static void Info(string format, params object[] args)
        {
            Info(string.Format(format, args));
        }

        [Conditional("DEBUG")] public static void Info(ConsoleColor color, string text)
        {
            LogWriteAsync(text, color, VerboseLogging ? LogTarget.ConsoleAndLog : LogTarget.Console);
        }

        public static void DebugInfo(ConsoleColor color, string text)
        {
            if (VerboseLogging)
                LogWriteAsync(text, color, LogTarget.Console);
        }

        // write a warning to logfile and debug console
        public static void WarningVerbose(string warning)
        {
            if (GlobalStats.VerboseLogging)
                Warning(warning);
        }

        // Always write a neutral message to both log file and console
        public static void Write(ConsoleColor color, string message)
        {
            LogWriteAsync(message, color);
        }

        // Always write a neutral message to both log file and console
        public static void Write(string message)
        {
            LogWriteAsync(message, DefaultColor);
        }

        public static void Warning(string warning)
        {
            Warning(ConsoleColor.Yellow, warning);
        }

        public static void WarningWithCallStack(string warning)
        {
            Warning(ConsoleColor.Yellow, $"{warning}\n{new StackTrace()}");
        }

        public static void Warning(ConsoleColor color, string text)
        {
            LogWriteAsync("Warning: " + text, color);
        }

        static ulong Fnv64(string text)
        {
            ulong hash = 0xcbf29ce484222325UL;
            for (int i = 0; i < text.Length; ++i)
            {
                hash ^= text[i];
                hash *= 0x100000001b3UL;
            }
            return hash;
        }

        static bool ShouldIgnoreErrorText(string error)
        {
            ulong hash = Fnv64(error);
            if (ReportedErrors.TryGetValue(hash, out int count)) // already seen this error?
            {
                ReportedErrors[hash] = ++count;
                return (count % ErrorThreshold) != 0; // only log error when we reach threshold
            }
            ReportedErrors[hash] = 1;
            return false; // log error
        }

        // write an error to logfile, sentry.io and debug console
        // plus trigger a Debugger.Break
        public static void Error(string format, params object[] args)
        {
            Error(string.Format(format, args));
        }

        public static void Error(string error)
        {
            string text = "(!) Error: " + error;
            LogWriteAsync(text, ConsoleColor.Red);
            FlushAllLogs();

            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                if (!ShouldIgnoreErrorText(error))
                {
                    var ex = new Exception(new StackTrace(1).ToString());
                    CaptureEvent(text, SentryLevel.Error, ex);
                }
                return;
            }

        #if !NOBREAK
            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        #endif
        }

        // write an Exception to logfile, sentry.io and debug console with an error message
        // plus trigger a Debugger.Break
        public static void Error(Exception ex, string error = null, SentryLevel errorLevel = SentryLevel.Error)
        {
            string text = ExceptionString(ex, "(!) Exception: ", error);
            LogWriteAsync(text, ConsoleColor.Red);
            FlushAllLogs();
            
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                if (!ShouldIgnoreErrorText(text))
                {
                    CaptureEvent(text, errorLevel, ex);
                }
                return;
            }

        #if !NOBREAK
            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        #endif
        }

        // if exitCode != 0, then program is terminated
        public static void ErrorDialog(Exception ex, string error, int exitCode)
        {
            if (IsTerminating)
                return;

            IsTerminating = exitCode != 0;

            string text = ExceptionString(ex, "(!) Exception: ", error);
            LogWriteAsync(text, ConsoleColor.Red);
            FlushAllLogs();

            SentryId? eventId = null;
            if (!HasDebugger && IsTerminating) // only log errors to sentry if debugger not attached
            {
                eventId = CaptureEvent(text, SentryLevel.Fatal, ex);
            }

            ExceptionViewer.ShowExceptionDialog(text, autoReport: eventId != null);
            if (IsTerminating) Program.RunCleanupAndExit(exitCode);
        }

        [Conditional("DEBUG")] public static void Assert(bool trueCondition, string message)
        {
            if (trueCondition != true) Error(message);
        }

        static SentryId? CaptureEvent(string text, SentryLevel level, Exception ex = null)
        {
            if (!IsStatsReportEnabled)
                return null;

            var evt = new SentryEvent(ex)
            {
                Message = text,
                Level   = level
            };

            // Attach a bounded log tail per-event (only on error/fatal captures),
            // read fresh here so it reflects the real pre-error context. This replaces
            // the old scope-level whole-file attachment that rode every event.
            SentryId eventId = SentrySdk.CaptureEvent(evt, scope =>
            {
                byte[] tail = ReadLogTail(SentryLogTailBytes);
                if (tail != null)
                    scope.AddAttachment(tail, "blackbox.tail.log");

                if (IncludeSaveGameInReports && !string.IsNullOrEmpty(LastAutoSavePath) && File.Exists(LastAutoSavePath))
                {
                    // Best-effort: a missing/locked save must never break the error report.
                    try { scope.AddAttachment(LastAutoSavePath); }
                    catch { /* ignore attachment failure */ }
                }
            });

            if (level == SentryLevel.Fatal) // for fatal errors, we can't do ASYNC reports
            {
                SentrySdk.Flush(TimeSpan.FromSeconds(5));
            }

            return eventId;
        }

        struct TraceContext
        {
            public Thread Thread;
            // populated by CollectSuspendedStackTraces; net8 path leaves it null
            // until ClrMd / P/Invoke restores cross-thread StackTrace capture
#pragma warning disable CS0649
            public StackTrace Trace;
#pragma warning restore CS0649
        }

        static void CollectSuspendedStackTraces(Array<TraceContext> suspended)
        {
            // Phase 2.6.A: net8 removed StackTrace(Thread, bool) — cross-thread stack
            // capture isn't supported without unmanaged interop. Skip; the deadlock-
            // detection logger will report which threads are suspended but not their
            // call stacks. Restore via ClrMd or P/Invoke if needed.
            suspended.Clear();
        }

        static Array<TraceContext> GatherMonitoredThreadStackTraces()
        {
            var suspended = new Array<TraceContext>();
            try
            {
                int currentThreadId = Thread.CurrentThread.ManagedThreadId;
                lock (MonitoredThreads)
                {
                    // Thread.Suspend/Resume are deprecated and throw ThreadStateException
                    // when the target isn't in a suspendable state. Tolerate per-thread
                    // failure so the exception logger doesn't itself crash and mask the
                    // original error. Threads that can't be suspended are simply omitted
                    // from the gathered stack-trace report.
                    for (int i = 0; i < MonitoredThreads.Count; ++i)
                    {
                        Thread monitored = MonitoredThreads[i];
                        if (monitored.ManagedThreadId == currentThreadId) continue;
                        try
                        {
                            #pragma warning disable 618 // Method is Deprecated
                            monitored.Suspend();
                            #pragma warning restore 618
                            suspended.Add(new TraceContext { Thread = monitored });
                        }
                        catch
                        {
                            // Skip threads we couldn't suspend (terminated, not yet started, etc.)
                        }
                    }
                }
                CollectSuspendedStackTraces(suspended);
            }
            finally
            {
                foreach (TraceContext context in suspended)
                {
                    try
                    {
                        #pragma warning disable 618 // Method is Deprecated
                        context.Thread.Resume();
                        #pragma warning restore 618
                    }
                    catch { /* best-effort; thread may have terminated since suspend */ }
                }
            }
            return suspended;
        }

        static string ExceptionString(Exception ex, string title, string details = null)
        {
            Array<TraceContext> stackTraces = GatherMonitoredThreadStackTraces();

            var sb = new StringBuilder(title, 4096);
            if (details != null) { sb.AppendLine(details); }

            CollectMessages(sb, ex);
            CollectExData(sb, ex);
            
            string exceptionThread = (string)ex.Data["Thread"];
            int exThreadId = (int)ex.Data["ThreadId"];
            sb.Append("\nThread #").Append(exThreadId).Append(' ');
            sb.Append(exceptionThread).Append(" StackTrace:\n");
            CollectAndCleanStackTrace(sb, ex);

            foreach (TraceContext trace in stackTraces)
            {
                int monitoredId = trace.Thread.ManagedThreadId;
                if (trace.Trace != null && monitoredId != exThreadId)
                {
                    string stackTrace = trace.Trace.ToString();
                    sb.Append("\nThread #").Append(monitoredId).Append(' ');
                    sb.Append(trace.Thread.Name).Append(" StackTrace:\n");
                    CleanStackTrace(sb, stackTrace);
                }
            }
            return sb.ToString();
        }

        static void CollectExData(StringBuilder sb, Exception ex)
        {
            IDictionary evt = ex.Data;
            if (!evt.Contains("Version"))
            {
                evt["Version"] = GlobalStats.Version;
                if (GlobalStats.HasMod)
                {
                    evt["Mod"] = GlobalStats.ModName;
                    evt["ModVersion"] = GlobalStats.ActiveMod.Mod.Version;
                }
                else
                {
                    evt["Mod"] = "Vanilla";
                }
                evt["Language"] = GlobalStats.Language.ToString();

                // find root UniverseScreen from ScreenManager, unless the crash is before ScreenManager is created
                var universe = ScreenManager.Instance?.FindScreen<UniverseScreen>();
                evt["StarDate"] = universe?.StarDateString ?? "NULL";
                evt["Ships"] = universe?.UState.Ships.Length.ToString() ?? "NULL";
                evt["Planets"] = universe?.UState.Planets?.Count.ToString() ?? "NULL";

                evt["Memory"] = (GC.GetTotalMemory(false) / 1024).ToString();
                evt["XnaMemory"] = StarDriveGame.Instance != null ? (StarDriveGame.Instance.Content.GetLoadedAssetBytes() / 1024).ToString() : "0";

                // System-memory pressure context (KB). When MemoryLoad reaches
                // HighMemoryLoadThreshold the GC stops growing the heap and
                // throws OOM on large allocations even if the managed heap is
                // healthy — this fingerprint is invisible without GCMemoryInfo.
                try
                {
                    GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();
                    evt["GCHeap"] = (gcInfo.HeapSizeBytes / 1024).ToString();
                    evt["GCFragmented"] = (gcInfo.FragmentedBytes / 1024).ToString();
                    evt["MemoryLoad"] = (gcInfo.MemoryLoadBytes / 1024).ToString();
                    evt["MemoryLoadThreshold"] = (gcInfo.HighMemoryLoadThresholdBytes / 1024).ToString();
                    evt["TotalAvailableMemory"] = (gcInfo.TotalAvailableMemoryBytes / 1024).ToString();
                }
                catch { /* GCMemoryInfo unsupported on this runtime — skip */ }
            }

            if (!evt.Contains("Thread"))
            {
                var currentThread = Thread.CurrentThread;
                evt["Thread"] = currentThread.Name;
                evt["ThreadId"] = currentThread.ManagedThreadId;
            }

            if (evt.Count != 0)
            {
                foreach (DictionaryEntry pair in evt)
                    sb.Append('\n').Append(pair.Key).Append(" = ").Append(pair.Value);
            }
        }

        static void CollectMessages(StringBuilder sb, Exception ex)
        {
            Exception inner = ex.InnerException;
            if (inner != null)
            {
                CollectMessages(sb, inner);
                sb.Append("\nFollowed by: ");
            }
            sb.Append(ex.Message);
        }

        static void CollectStackTraces(StringBuilder trace, Exception ex)
        {
            Exception inner = ex.InnerException;
            if (inner != null)
            {
                CollectStackTraces(trace, inner);
                trace.AppendLine("\nFollowed by:");
            }
            trace.AppendLine(ex.StackTrace ?? "");
        }

        static void CollectAndCleanStackTrace(StringBuilder sb, Exception ex)
        {
            var trace = new StringBuilder(4096);
            CollectStackTraces(trace, ex);
            string stackTraces = trace.ToString();
            CleanStackTrace(sb, stackTraces);
        }

        static void CleanStackTrace(StringBuilder @out, string stackTrace)
        {
            string[] lines = stackTrace.Split(new[]{ '\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string errorLine in lines)
            {
                string line = errorLine.Replace("Microsoft.Xna.Framework", "XNA");

                if (line.Contains(" in "))
                {
                    string[] parts = line.Split(new[] { " in " }, StringSplitOptions.RemoveEmptyEntries);
                    string method = parts[0].Replace("Ship_Game.", "");
                    int idx       = parts[1].IndexOf("Ship_Game\\", StringComparison.Ordinal);
                    string file   = parts[1].Substring(idx + "Ship_Game\\".Length);

                    @out.Append(method).Append(" in ").Append(file).Append('\n');
                }
                else if (line.Contains("System.Windows.Forms")) continue; // ignore winforms
                else @out.Append(line).Append('\n');
            }
        }


        public static void OpenURL(string url)
        {
            if (SteamManager.IsInitialized)
            {
                SteamManager.ActivateWebOverlay(url);
            }
            else
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowPos(IntPtr hwnd, int hwndAfter, int x, int y, int cx, int cy, int wFlags);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public static void ShowConsoleWindow(int bufferHeight = 2000)
        {
            var handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
            {
                AllocConsole();
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput())  { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
            else ShowWindow(handle, 5/*SW_SHOW*/);

            if (Console.BufferHeight < bufferHeight)
                Console.BufferHeight = bufferHeight;

            // Move the console window to a secondary screen if we have multiple monitors
            if (Screen.AllScreens.Length > 1 && (handle = GetConsoleWindow()) != IntPtr.Zero)
            {
                Screen primary = Screen.PrimaryScreen;
                Screen[] screens = Screen.AllScreens;
                Screen screen = screens.Find(s => s != primary && s.Bounds.Y == primary.Bounds.Y) ?? primary;

                System.Drawing.Rectangle bounds = screen.Bounds;
                const int noResize = 0x0001;
                SetWindowPos(handle, 0, bounds.Left + 40, bounds.Top + 40, 0, 0, noResize);
            }

            HasActiveConsole = handle != IntPtr.Zero;
        }

        public static void HideConsoleWindow()
        {
            HasActiveConsole = false;
            ShowWindow(GetConsoleWindow(), 0/*SW_HIDE*/);
        }
    }
}
