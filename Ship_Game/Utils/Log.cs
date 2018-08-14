using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SharpRaven;
using SharpRaven.Data;

namespace Ship_Game
{
    public static class Log
    {
        private static readonly StreamWriter LogFile;
        public static readonly bool HasDebugger = Debugger.IsAttached;

        // sentry.io automatic crash reporting
        private static readonly RavenClient Raven = new RavenClient("https://1c5a169d2a304e5284f326591a2faae3:3e8eaeb6d9334287955fdb8101ae8eab@sentry.io/123180");
        private static readonly ConsoleColor DefaultColor = Console.ForegroundColor;

        // prevent flooding Raven with 2000 error messages if we fall into an exception loop
        // instead, we count identical exceptions and resend them only over a certain threshold 
        private static readonly Map<ulong, int> ReportedErrors = new Map<ulong, int>();
        private const int ErrorThreshold = 100;
        private static bool IsTerminating;
        //public static bool FatalError = true;
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
            Raven.Release = GlobalStats.ExtendedVersion;
            if (HasDebugger)
            {
                Raven.Environment = "Staging";

                // if Console output is redirected, all console text is sent to VS Output instead
                // in that case, showing the console is pointless, however if output isn't redirected
                // we should enable the console window
                if (Console.IsOutputRedirected == false)
                {
                    ShowConsoleWindow();
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(init);
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

        private static void WriteToLog(string text)
        {
            if (LogFile.BaseStream.CanWrite)
            {
                LogFile.WriteLine(text);
                LogFile.Flush();
            }
        }

        // just echo info to console, don't write to logfile
        // not used in release builds or if there's no debugger attached
        [Conditional("DEBUG")] public static void Info(string text)
        {
            if (GlobalStats.VerboseLogging)
                WriteToLog(text);
            if (!HasDebugger) return;
            Console.ForegroundColor = DefaultColor;
            Console.WriteLine(text);
        }
        [Conditional("DEBUG")] public static void Info(string format, params object[] args)
        {
            Info(string.Format(format, args));
        }

        [Conditional("DEBUG")] public static void Info(ConsoleColor color, string text)
        {
            if (!HasDebugger) return;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
        }
        


        // write a warning to logfile and debug console
        public static void WarningVerbose(string warning)
        {
            if (GlobalStats.VerboseLogging)
                Warning(warning);
        }
        public static void Warning(string warning)
        {
            string text = "Warning: " + warning;
            WriteToLog(text);
            if (!HasDebugger) return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
        }

        public static void WarningWithCallStack(string warning)
        {
            var t = new StackTrace();

            string text = $"Warning:  {warning}\n{t}";
            WriteToLog(text);
            if (!HasDebugger)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
        }

        public static bool TestMessage(string testMessage, 
            Importance importance = Importance.None, 
            bool waitForEnter = false,
            bool waitForYes = false)
        {
            WriteToLog(testMessage);
            if (!HasActiveConsole)
            {
                return false;
            }
            Console.ForegroundColor = ImportanceColor(importance);
            Console.WriteLine(testMessage);
            if (waitForEnter)
            {                
                Console.ForegroundColor = Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Press Any Key To Continue");
                Console.ReadKey();
            }
            if (waitForYes)
            {
                Console.ForegroundColor = Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("(Y/N)");
                return Console.ReadKey(true).Key == ConsoleKey.Y ;
            }

            return false;
        }


        private static ulong Fnv64(string text)
        {
            ulong hash = 0xcbf29ce484222325UL;
            for (int i = 0; i < text.Length; ++i)
            {
                hash ^= text[i];
                hash *= 0x100000001b3UL;
            }
            return hash;
        }

        private static bool ShouldIgnoreErrorText(string error)
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
            if (!HasDebugger && ShouldIgnoreErrorText(error))
                return;

            string text = "(!) Error: " + error;
            WriteToLog(text);
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                CaptureEvent(text, ErrorLevel.Error);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);

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

            string withStack = text + "\n" + CleanStackTrace(ex.StackTrace ?? ex.InnerException?.StackTrace ?? "");
            WriteToLog(withStack);
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {                
                CaptureEvent(text, errorLevel, ex);                
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(withStack);

            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }
        public static void Fatal(Exception ex, string error = null) => Error(ex, error, ErrorLevel.Fatal);

        public static void ErrorDialog(Exception ex, string error = null)
        {
            if (IsTerminating)
                return;
            IsTerminating = true;

            string text = CurryExceptionMessage(ex, error);
            string withStack = text + "\n" + CleanStackTrace(ex.StackTrace ?? ex.InnerException?.StackTrace);
            WriteToLog(withStack);
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                CaptureEvent(text, ErrorLevel.Fatal, ex);
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(withStack);

            ExceptionViewer.ShowExceptionDialog(withStack);
            Environment.Exit(-1);
        }

        [Conditional("DEBUG")] public static void Assert(bool trueCondition, string message)
        {
            if (trueCondition != true) Error(message);
        }

        private static void CaptureEvent(string text, ErrorLevel level, Exception ex = null)
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

        private static string CurryExceptionMessage(Exception ex, string moreInfo = null)
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

                evt["StarDate"]  = Empire.Universe?.StarDate.ToString("F1") ?? "NULL";
                evt["Ships"]     = Empire.Universe?.MasterShipList?.Count.ToString() ?? "NULL";
                evt["Planets"]   = Empire.Universe?.PlanetsDict?.Count.ToString() ?? "NULL";

                evt["Memory"]    = (GC.GetTotalMemory(false) / 1024).ToString();
                evt["XnaMemory"] = Game1.Instance != null ? (Game1.Instance.Content.GetLoadedAssetBytes() / 1024).ToString() : "0";
                evt["ShipLimit"] = GlobalStats.ShipCountLimit.ToString();
                if (GlobalStats.WarpBehaviorsEnabled)
                    evt["WarpBehaviorsEnabled"] = true;
            }
            var sb = new StringBuilder("(!) Exception: ");
            sb.Append(ex.Message);

            if (ex.InnerException != null)
                sb.Append("\nInnerEx: ").Append(ex.InnerException.Message);

            if (moreInfo.NotEmpty())
                sb.Append("\nInfo: ").Append(moreInfo);

            if (evt.Count != 0)
            {
                foreach (DictionaryEntry pair in evt)
                    sb.Append('\n').Append(pair.Key).Append(" = ").Append(pair.Value);
            }
            return sb.ToString();
        }

        private static string CleanStackTrace(string stackTrace)
        {
            var sb = new StringBuilder("StackTrace:\r\n");
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

                    sb.Append(method).Append(" in ").Append(file).AppendLine();
                }
                else if (line.Contains("System.Windows.Forms"))    continue; // ignore winforms
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

        [DllImport("kernel32.dll")] private static extern bool AllocConsole();
        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]   private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32.dll")]   private static extern IntPtr SetWindowPos(IntPtr hwnd, int hwndAfter, int x, int y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]   private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
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

        private static ConsoleColor ImportanceColor(Importance importance)
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
