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
        private static readonly StreamWriter LogFile = new StreamWriter("blackbox.log", true, Encoding.ASCII, 8192);
        public static readonly bool HasDebugger = Debugger.IsAttached;

        // sentry.io automatic crash reporting
        private static readonly RavenClient Raven = new RavenClient("https://3e16bcf9f97d4af3b3fb4f8d4ba1830b:1f0e6d3598e14584877e0c0e87554966@sentry.io/123180");
        private static readonly ConsoleColor DefaultColor = Console.ForegroundColor;

        // prevent flooding Raven with 2000 error messages if we fall into an exception loop
        // instead, we count identical exceptions and resend them only over a certain threshold 
        private static readonly Map<ulong, int> ReportedErrors = new Map<ulong, int>();
        private const int ErrorThreshold = 100;
        private static bool IsTerminating;

        static Log()
        {
            string init = "\r\n\r\n";
            init +=  " ================================================================== \r\n";
            init += $" ========== {GlobalStats.ExtendedVersion,-44} ==========\r\n";
            init += $" ========== UTC: {DateTime.UtcNow,-39} ==========\r\n";
            init +=  " ================================================================== \r\n";
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
                Raven.Environment = "Release";
            #endif
                HideConsoleWindow();
            }
        }


        // just echo info to console, don't write to logfile
        // not used in release builds or if there's no debugger attached
        [Conditional("DEBUG")] public static void Info(string format, params object[] args)
        {
            if (!HasDebugger) return;
            Console.ForegroundColor = DefaultColor;
            Console.WriteLine(string.Format(format, args));
        }
        [Conditional("DEBUG")] public static void Info(string text)
        {
            if (!HasDebugger) return;
            Console.ForegroundColor = DefaultColor;
            Console.WriteLine(text);
        }
        [Conditional("DEBUG")] public static void Info(ConsoleColor color, string format, params object[] args)
        {
            if (!HasDebugger) return;
            Console.ForegroundColor = color;
            Console.WriteLine(string.Format(format, args));
        }
        [Conditional("DEBUG")] public static void Info(ConsoleColor color, string text)
        {
            if (!HasDebugger) return;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
        }


        // write a warning to logfile and debug console
        public static void Warning(string format, params object[] args)
        {
            Warning(string.Format(format, args));
        }
        public static void Warning(string warning)
        {
            string text = "Warning: " + warning;
            LogFile.WriteLine(text);

            if (!HasDebugger)
                return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
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
            LogFile.WriteLine(text);

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
        public static void Error(Exception ex, string error = null)
        {
            string text = CurryExceptionMessage(ex, error);
            if (!HasDebugger && ShouldIgnoreErrorText(text))
                return;

            string withStack = text + "\n" + CleanStackTrace(ex.StackTrace ?? ex.InnerException?.StackTrace);
            LogFile.WriteLine(withStack);
            
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                CaptureEvent(text, ErrorLevel.Fatal, ex);
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(withStack);

            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }

        public static void ErrorDialog(Exception ex, string error = null)
        {
            if (IsTerminating)
                return;
            IsTerminating = true;

            string text = CurryExceptionMessage(ex, error);
            string withStack = text + "\n" + CleanStackTrace(ex.StackTrace ?? ex.InnerException?.StackTrace);
            LogFile.WriteLine(withStack);
            
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

        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
            {
                AllocConsole();
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput())  { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
            else ShowWindow(handle, 5/*SW_SHOW*/);

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
    }
}
