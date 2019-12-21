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
        struct LogEntry
        {
            public TimeSpan Time;
            public string Message;
        }

        static readonly StreamWriter LogFile;
        static Thread LogThread;
        static readonly SafeQueue<LogEntry> LogQueue = new SafeQueue<LogEntry>(64);
        public static readonly bool HasDebugger = Debugger.IsAttached;
        public static bool VerboseLogging;

        // sentry.io automatic crash reporting
        static readonly RavenClient Raven = new RavenClient("https://1c5a169d2a304e5284f326591a2faae3:3e8eaeb6d9334287955fdb8101ae8eab@sentry.io/123180");
        static readonly ConsoleColor DefaultColor = Console.ForegroundColor;
        static ConsoleColor CurrentColor = DefaultColor;
        static readonly object ConsoleSync = new object();

        // prevent flooding Raven with 2000 error messages if we fall into an exception loop
        // instead, we count identical exceptions and resend them only over a certain threshold
        static readonly Map<ulong, int> ReportedErrors = new Map<ulong, int>();
        const int ErrorThreshold = 100;
        static bool IsTerminating;

        static Log()
        {
            string init = "\r\n\r\n";
            init +=  " ================================================================== \r\n";
            init += $" ========== {GlobalStats.ExtendedVersion,-44} ==========\r\n";
            init += $" ========== UTC: {DateTime.UtcNow,-39} ==========\r\n";
            init +=  " ================================================================== \r\n";

            if (File.Exists("blackbox.log"))
                File.Copy("blackbox.log", "blackbox.old.log", true);

            LogFile = new StreamWriter("blackbox.log", false, Encoding.ASCII, 32*1024);
            LogFile.Write(init);
            LogThread = new Thread(LogAsyncWriter) {Name = "AsyncLogWriter"};
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
                {
                    ShowConsoleWindow();
                }

                WriteToConsole(ConsoleColor.Green, init);
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
        }

        static void WriteToFile(in LogEntry log)
        {
            LogFile.Write(log.Time.Hours.ToString("00"));
            LogFile.Write(':');
            LogFile.Write(log.Time.Minutes.ToString("00"));
            LogFile.Write(':');
            LogFile.Write(log.Time.Seconds.ToString("00"));
            LogFile.Write('.');
            LogFile.Write(log.Time.Milliseconds.ToString("000"));
            LogFile.Write('m');
            LogFile.Write('s');
            LogFile.Write(':');
            LogFile.Write(' ');
            LogFile.Write(log.Message);
            LogFile.Write('\n');
        }

        public static void FlushAllLogs()
        {
            LogThread = null;
            foreach (LogEntry log in LogQueue.TakeAll())
                WriteToFile(log);
            LogFile.Flush();
        }

        static void LogAsyncWriter()
        {
            while (LogThread != null)
            {
                if (LogQueue.WaitDequeue(out LogEntry log, 15))
                {
                    WriteToFile(log);
                    while (LogQueue.WaitDequeue(out log, 15))
                        WriteToFile(log);
                    LogFile.Flush();
                }
            }
        }

        static void WriteToLog(string text)
        {
            LogQueue.Enqueue(new LogEntry
            {
                Time = DateTime.UtcNow.TimeOfDay,
                Message = text
            });
        }

        static void WriteToConsole(ConsoleColor color, string text)
        {
            lock (ConsoleSync)
            {
                if (CurrentColor != color)
                {
                    Console.ForegroundColor = color;
                    CurrentColor = color;
                }
                Console.WriteLine(text);
            }
        }

        // just echo info to console, don't write to logfile
        // not used in release builds or if there's no debugger attached
        [Conditional("DEBUG")] public static void Info(string text)
        {
            if (GlobalStats.VerboseLogging)
                WriteToLog(text);
            if (VerboseLogging)
                WriteToConsole(DefaultColor, text);
        }
        [Conditional("DEBUG")] public static void Info(string format, params object[] args)
        {
            Info(string.Format(format, args));
        }

        [Conditional("DEBUG")] public static void Info(ConsoleColor color, string text)
        {
            if (VerboseLogging)
                WriteToConsole(color, text);
        }

        public static void DebugInfo(ConsoleColor color, string text)
        {
            if (VerboseLogging)
                WriteToConsole(color, text);
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
            WriteToLog(message);
            if (VerboseLogging)
                WriteToConsole(color, message);
        }

        // Always write a neutral message to both log file and console
        public static void Write(string message)
        {
            WriteToLog(message);
            if (VerboseLogging)
                WriteToConsole(DefaultColor, message);
        }

        public static void Warning(string warning)
        {
            Warning(ConsoleColor.Yellow, warning);
        }

        public static void WarningWithCallStack(string warning)
        {
            Warning(ConsoleColor.Yellow, $"{warning}\n{new StackTrace()}");
        }

        public static void Warning(ConsoleColor color, string warning)
        {
            string text = "Warning: " + warning;
            WriteToLog(text);
            if (VerboseLogging)
                WriteToConsole(color, text);
        }

        public static bool TestMessage(string testMessage,
            Importance importance = Importance.None,
            bool waitForEnter = false,
            bool waitForYes = false)
        {
            WriteToLog(testMessage);
            if (!HasActiveConsole)
                return false;

            WriteToConsole(ImportanceColor(importance), testMessage);

            if (waitForEnter)
            {
                WriteToConsole(ConsoleColor.White, "Press Any Key To Continue");
                Console.ReadKey();
            }
            if (waitForYes)
            {
                WriteToConsole(ConsoleColor.White, "(Y/N)");
                return Console.ReadKey(true).Key == ConsoleKey.Y;
            }
            return false;
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

        class StackTraceEx : Exception
        {
            public override string StackTrace { get; }
            public StackTraceEx(string stackTrace) { StackTrace = stackTrace; }
        }

        public static void Error(string error)
        {
            if (!HasDebugger && ShouldIgnoreErrorText(error))
                return;

            string text = "(!) Error: " + error;
            WriteToLog(text);
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                var ex = new StackTraceEx(new StackTrace(1).ToString());
                CaptureEvent(text, ErrorLevel.Error, ex);
                return;
            }

            WriteToConsole(ConsoleColor.Red, text);

            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }

        // write an Exception to logfile, sentry.io and debug console with an error message
        // plus trigger a Debugger.Break
        public static void Error(Exception ex, string format, params object[] args)
        {
            Error(ex, string.Format(format, args));
        }

        public static void Error(Exception ex, string error = null, ErrorLevel errorLevel = ErrorLevel.Error)
        {
            string text = CurryExceptionMessage(ex, error);
            if (!HasDebugger && ShouldIgnoreErrorText(text))
                return;

            string withStack = text + "\n" + CleanStackTrace(ex);
            WriteToLog(withStack);
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                CaptureEvent(text, errorLevel, ex);
                return;
            }

            WriteToConsole(ConsoleColor.DarkRed, withStack);

            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }

        public static void Fatal(Exception ex, string error = null) => Error(ex, error, ErrorLevel.Fatal);

        public static void ErrorDialog(Exception ex, string error = null, bool isFatal = true)
        {
            if (IsTerminating)
                return;

            IsTerminating = isFatal;

            string text = CurryExceptionMessage(ex, error);
            string withStack = text + "\n" + CleanStackTrace(ex);
            WriteToLog(withStack);
            if (!HasDebugger && isFatal) // only log errors to sentry if debugger not attached
            {
                CaptureEvent(text, ErrorLevel.Fatal, ex);
                return;
            }

            WriteToConsole(ConsoleColor.DarkRed, withStack);

            ExceptionViewer.ShowExceptionDialog(withStack, GlobalStats.AutoErrorReport);
            if (isFatal) Environment.Exit(-1);
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
            Raven.CaptureAsync(evt);
        }

        static string CurryExceptionMessage(Exception ex, string moreInfo = null)
        {
            IDictionary evt = ex.Data;
            if (evt.Count == 0)
            {
                evt["Version"] = GlobalStats.Version;
                if (GlobalStats.HasMod)
                {
                    evt["Mod"]        = GlobalStats.ActiveMod.ModName;
                    evt["ModVersion"] = GlobalStats.ActiveModInfo.Version;
                }
                else evt["Mod"] = "Vanilla";

                evt["StarDate"]  = Empire.Universe?.StarDateString ?? "NULL";
                evt["Ships"]     = Empire.Universe?.MasterShipList?.Count.ToString() ?? "NULL";
                evt["Planets"]   = Empire.Universe?.PlanetsDict?.Count.ToString() ?? "NULL";

                evt["Memory"]    = (GC.GetTotalMemory(false) / 1024).ToString();
                evt["XnaMemory"] = StarDriveGame.Instance != null ? (StarDriveGame.Instance.Content.GetLoadedAssetBytes() / 1024).ToString() : "0";
                evt["ShipLimit"] = GlobalStats.ShipCountLimit.ToString();
                if (GlobalStats.WarpBehaviorsEnabled)
                    evt["WarpBehaviorsEnabled"] = true;
            }
            var sb = new StringBuilder("(!) Exception: ");
            AppendMessages(sb, ex);

            if (moreInfo.NotEmpty())
                sb.Append("\nInfo: ").Append(moreInfo);

            if (evt.Count != 0)
            {
                foreach (DictionaryEntry pair in evt)
                    sb.Append('\n').Append(pair.Key).Append(" = ").Append(pair.Value);
            }
            return sb.ToString();
        }

        static void AppendMessages(StringBuilder sb, Exception ex)
        {
            Exception inner = ex.InnerException;
            if (inner != null)
            {
                AppendMessages(sb, inner);
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

        static string CleanStackTrace(Exception ex)
        {
            var trace = new StringBuilder(4096);
            CollectStackTraces(trace, ex);
            string stackTraces = trace.ToString();

            var sb = new StringBuilder("StackTrace:\r\n");
            string[] lines = stackTraces.Split(new[]{ '\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string errorLine in lines)
            {
                string line = errorLine.Replace("Microsoft.Xna.Framework", "XNA");

                if (line.Contains(" in "))
                {
                    string[] parts = line.Split(new[] { " in " }, StringSplitOptions.RemoveEmptyEntries);
                    string method = parts[0].Replace("Ship_Game.", "");
                    int idx       = parts[1].IndexOf("Ship_Game\\", StringComparison.Ordinal);
                    string file   = parts[1].Substring(idx + "Ship_Game\\".Length);

                    sb.Append(method).Append(" in ").Append(file).AppendLine();
                }
                else if (line.Contains("System.Windows.Forms")) continue; // ignore winforms
                else sb.AppendLine(line);
            }
            return sb.ToString();
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

        public static bool HasActiveConsole => GetConsoleWindow() != IntPtr.Zero;

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
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.Primary)
                        continue;

                    GetWindowRect(handle, out RECT rect);
                    var bounds = screen.Bounds;
                    const int noResize = 0x0001;
                    SetWindowPos(handle, 0, bounds.Left + rect.Left, bounds.Top + rect.Top, 0, 0, noResize);
                    break;
                }
            }
        }

        public static void HideConsoleWindow()
        {
            ShowWindow(GetConsoleWindow(), 0/*SW_HIDE*/);
        }

        static ConsoleColor ImportanceColor(Importance importance)
        {
            switch (importance)
            {
                case Importance.None:      return ConsoleColor.Cyan;
                case Importance.Trivial:   return ConsoleColor.Green;
                case Importance.Regular:   return ConsoleColor.White;
                case Importance.Important: return ConsoleColor.Yellow;
                case Importance.Critical:  return ConsoleColor.Red;
                default: throw new ArgumentOutOfRangeException(nameof(importance), importance, null);
            }
        }

        public enum Importance
        {
            None,
            Trivial,
            Regular,
            Important,
            Critical
        }
    }
}
