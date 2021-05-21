using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SharpRaven;
using SharpRaven.Data;
using Ship_Game.Utils;

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
        static readonly SafeQueue<LogEntry> LogQueue = new SafeQueue<LogEntry>(64);
        public static bool HasDebugger;

        // Either there is an active Console Window
        // OR Console output is redirected to some pipe, like VS Debug Output
        public static bool HasActiveConsole { get; private set; }

        const ConsoleColor DefaultColor = ConsoleColor.Gray;
        static ConsoleColor CurrentColor = DefaultColor;
        static readonly object Sync = new object();

        /// <summary>
        /// If TRUE, then [INFO] messages will be written to LogFile
        /// If FALSE, then [INFO] messages will only be seen in console
        /// </summary>
        public static bool VerboseLogging = false;

        // sentry.io automatic crash reporting
        static readonly RavenClient Raven = new RavenClient("https://1c5a169d2a304e5284f326591a2faae3:3e8eaeb6d9334287955fdb8101ae8eab@sentry.io/123180");

        // prevent flooding Raven with 2000 error messages if we fall into an exception loop
        // instead, we count identical exceptions and resend them only over a certain threshold
        static readonly Map<ulong, int> ReportedErrors = new Map<ulong, int>();
        const int ErrorThreshold = 100;
        public static bool IsTerminating { get; private set; }

        static readonly Array<Thread> MonitoredThreads = new Array<Thread>();

        public static void Initialize()
        {
            if (LogThread != null)
                return; // already initialized!

            HasDebugger = Debugger.IsAttached;

            if (LogFile == null)
            {
                if (File.Exists("blackbox.log"))
                    File.Copy("blackbox.log", "blackbox.old.log", true);
                LogFile = OpenLog("blackbox.log");
            }

            LogThread = new Thread(LogAsyncWriter) { Name = "AsyncLogWriter" };
            LogThread.Start();

            Raven.Release = GlobalStats.ExtendedVersion;
            if (HasDebugger)
            {
                VerboseLogging = true;
                Raven.Environment = "Staging";

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
                Raven.Environment = "Staging";
            #else
                Raven.Environment = GlobalStats.Version.Contains("_TEST_") ? "Test" : "Release";
            #endif
                HideConsoleWindow();
            }

            string init = "\r\n";
            init += " ================================================================== \r\n";
            init += $" ========== {GlobalStats.ExtendedVersion,-44} ==========\r\n";
            init += $" ========== UTC: {DateTime.UtcNow,-39} ==========\r\n";
            init += " ================================================================== \r\n";
            LogWriteAsync(init, ConsoleColor.Green);
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
            return new StreamWriter(
                stream: File.Open(logPath, FileMode.Create, FileAccess.Write, FileShare.Read),
                encoding: Encoding.ASCII,
                bufferSize: 32 * 1024)
            {
                AutoFlush = true
            };
        }

        static void SetConsoleColor(ConsoleColor color, bool force)
        {
            if (force || CurrentColor != color)
            {
                CurrentColor = color;
                Console.ForegroundColor = color;
            }
        }

        // specialized for Log Entry formatting, because it's very slow
        class LogStringBuffer
        {
            public readonly char[] Characters = new char[1024 * 32];
            public int Length;

            public void Append(char ch)
            {
                Characters[Length++] = ch;
            }
            public void Append(string s)
            {
                int n = s.Length;
                s.CopyTo(0, Characters, Length, n);
                Length += n;
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

            if (log.Target.HasFlag(LogTarget.LogFile))
            {
                LogFile?.Write(sb.Characters, 0, sb.Length);
            }

            if (log.Target.HasFlag(LogTarget.Console))
            {
                SetConsoleColor(log.Color, force: false);
                Console.Write(sb.Characters, 0, sb.Length);
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

        public static void StopLogThread()
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
            
        #if DEBUG && !NOBREAK
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                if (!ShouldIgnoreErrorText(error))
                {
                    var ex = new Exception(new StackTrace(1).ToString());
                    CaptureEvent(text, ErrorLevel.Error, ex);
                }
                return;
            }

            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        #endif
        }

        // write an Exception to logfile, sentry.io and debug console with an error message
        // plus trigger a Debugger.Break
        public static void Error(Exception ex, string error = null, ErrorLevel errorLevel = ErrorLevel.Error)
        {
            string text = ExceptionString(ex, "(!) Exception: ", error);
            LogWriteAsync(text, ConsoleColor.DarkRed);
            FlushAllLogs();
            
        #if DEBUG && !NOBREAK
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                if (!ShouldIgnoreErrorText(text))
                {
                    CaptureEvent(text, errorLevel, ex);
                }
                return;
            }
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
            LogWriteAsync(text, ConsoleColor.DarkRed);
            FlushAllLogs();

            if (!HasDebugger && IsTerminating) // only log errors to sentry if debugger not attached
            {
                CaptureEvent(text, ErrorLevel.Fatal, ex);
            }

            ExceptionViewer.ShowExceptionDialog(text, GlobalStats.AutoErrorReport);
            if (IsTerminating) Program.RunCleanupAndExit(exitCode);
        }

        [Conditional("DEBUG")] public static void Assert(bool trueCondition, string message)
        {
            if (trueCondition != true) Error(message);
        }

        static void CaptureEvent(string text, ErrorLevel level, Exception ex = null)
        {
            var evt = new SentryEvent(ex)
            {
                Message = text,
                Level   = level
            };

            if (GlobalStats.HasMod)
            {
                evt.Tags["Mod"]        = GlobalStats.ActiveMod.ModName;
                evt.Tags["ModVersion"] = GlobalStats.ActiveModInfo.Version;
            }

            if (level == ErrorLevel.Fatal) // for fatal errors, we can't do ASYNC reports
                Raven.Capture(evt);
            else
                Raven.CaptureAsync(evt);
        }

        struct TraceContext
        {
            public Thread Thread;
            public StackTrace Trace;
        }

        static void CollectSuspendedStackTraces(Array<TraceContext> suspended)
        {
            for (int i = 0; i < suspended.Count; ++i)
            {
                TraceContext context = suspended[i];
                try
                {
                    #pragma warning disable 618 // Method is Deprecated
                    context.Trace = new StackTrace(context.Thread, true);
                    #pragma warning restore 618 // Method is Deprecated
                    suspended[i] = context;
                }
                catch
                {
                    suspended.RemoveAt(i--);
                }
            }
        }

        static Array<TraceContext> GatherMonitoredThreadStackTraces()
        {
            var suspended = new Array<TraceContext>();
            try
            {
                int currentThreadId = Thread.CurrentThread.ManagedThreadId;
                lock (MonitoredThreads)
                {
                    // suspend as fast as possible, do nothing else!
                    for (int i = 0; i < MonitoredThreads.Count; ++i)
                    {
                        Thread monitored = MonitoredThreads[i];
                        if (monitored.ManagedThreadId != currentThreadId) // don't suspend ourselves
                        {
                            #pragma warning disable 618 // Method is Deprecated
                            monitored.Suspend();
                            #pragma warning restore 618 // Method is Deprecated
                        }
                    }
                    // now that we suspended the threads, list them
                    for (int i = 0; i < MonitoredThreads.Count; ++i)
                    {
                        Thread monitored = MonitoredThreads[i];
                        if (monitored.ManagedThreadId != currentThreadId)
                            suspended.Add(new TraceContext { Thread = monitored });
                    }
                }
                CollectSuspendedStackTraces(suspended);
            }
            finally
            {
                // We got the stack traces, resume the threads
                foreach (TraceContext context in suspended)
                {
                    #pragma warning disable 618 // Method is Deprecated
                    context.Thread.Resume();
                    #pragma warning restore 618 // Method is Deprecated
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
                    evt["Mod"]        = GlobalStats.ActiveMod.ModName;
                    evt["ModVersion"] = GlobalStats.ActiveModInfo.Version;
                }
                else
                {
                    evt["Mod"] = "Vanilla";
                }

                evt["StarDate"]  = Empire.Universe?.StarDateString ?? "NULL";
                evt["Ships"]     = Empire.Universe?.GetMasterShipList().Count.ToString() ?? "NULL";
                evt["Planets"]   = Empire.Universe?.PlanetsDict?.Count.ToString() ?? "NULL";

                evt["Memory"]    = (GC.GetTotalMemory(false) / 1024).ToString();
                evt["XnaMemory"] = StarDriveGame.Instance != null ? (StarDriveGame.Instance.Content.GetLoadedAssetBytes() / 1024).ToString() : "0";
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
            if (SteamManager.isInitialized)
            {
                SteamManager.ActivateOverlayWebPage(url);
            }
            else
            {
                Process.Start(url);
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
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

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

            Console.BufferHeight = bufferHeight;

            // Move the console window to a secondary screen if we have multiple monitors
            if (Screen.AllScreens.Length > 1 && (handle = GetConsoleWindow()) != IntPtr.Zero)
            {
                Screen primary = Screen.PrimaryScreen;
                Screen[] screens = Screen.AllScreens;
                Screen screen = screens.Find(s => s != primary && s.Bounds.Y == primary.Bounds.Y) ?? primary;

                GetWindowRect(handle, out RECT rect);
                System.Drawing.Rectangle bounds = screen.Bounds;
                const int noResize = 0x0001;
                SetWindowPos(handle, 0, bounds.Left + rect.Left, bounds.Top + rect.Top, 0, 0, noResize);
            }

            HasActiveConsole = handle != IntPtr.Zero;
        }

        public static void HideConsoleWindow()
        {
            ShowWindow(GetConsoleWindow(), 0/*SW_HIDE*/);
            HasActiveConsole = false;
        }
    }
}
