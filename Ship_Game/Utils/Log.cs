using System;
using System.Collections;
using System.Collections.Generic;
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

        static Log()
        {
            string init = "\r\n\r\n";
            init +=  " ================================================================== \r\n";
            init += $" ========== {GlobalStats.ExtendedVersion,-44} ==========\r\n";
            init += $" ========== UTC: {DateTime.UtcNow,-39} ==========\r\n";
            init +=  " ================================================================== \r\n";
            LogFile.Write(init);
            Raven.Release = GlobalStats.Version;
            Raven.Environment = GlobalStats.Branch;            
            if (HasDebugger)
            {
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

            // @todo Should we turn on logging for warnings?
            //Raven.Capture(new SentryEvent(text)
            //{
            //    Level = ErrorLevel.Warning
            //});

            if (!HasDebugger)
                return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
        }
        //@todo change this to read the added data from the exception. So the logic isnt repeated
        private static void CaptureEvent(string text, ErrorLevel level, Exception ex = null)
        {
            var evt = new SentryEvent(ex)
            {
                Message = text,
                Level   = level                
            };           
            if (GlobalStats.HasMod)
            {
                evt.Tags["Mod"]        = GlobalStats.ActiveMod.ModPath;
                evt.Tags["ModVersion"] = GlobalStats.ActiveModInfo.Version;
            }
            else evt.Tags["Mod"] = "Vanilla";                                    
            Raven.CaptureAsync(evt);                         
        }

        // write an error to logfile, sentry.io and debug console
        // plus trigger a Debugger.Break
        public static void Error(string format, params object[] args)
        {
            Error(string.Format(format, args));
        }
        public static void Error(string error)
        {
            string text = "!! Error: " + error;
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
        public static void Error(Exception ex, string error)
        {
            string text = "!! Exception: " + error;
            text += AddDataToException(ex);        
            LogFile.WriteLine(text);
            
            if (!HasDebugger) // only log errors to sentry if debugger not attached
            {
                CaptureEvent(text + " | " + ex.Message, ErrorLevel.Fatal, ex);
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(text);
            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }
        public static string AddDataToException(Exception ex)
        {
            if (ex.Data.Count == 0)
            {
                var evt = ex.Data;
                evt.Add("Version", GlobalStats.ExtendedVersion);
                if (GlobalStats.HasMod)
                {
                    evt.Add("Mod", GlobalStats.ActiveMod?.ModPath ?? "NULL");
                    evt.Add("ModVersion", GlobalStats.ActiveModInfo?.Version ?? "NULL");
                }
                else evt.Add("Mod", "Vanilla");
              //  if (Empire.Universe != null)
                {
                    evt.Add("StarDate", Empire.Universe?.StarDate.ToString("F1") ?? "NULL") ;
                    evt.Add("Ships", Empire.Universe?.MasterShipList?.Count.ToString() ?? "NULL");
                    evt.Add("Planets", Empire.Universe?.PlanetsDict?.Count.ToString() ?? "NULL");
                }
                evt.Add("Memory", (GC.GetTotalMemory(false) / 1024).ToString());
                evt.Add("ShipLimit", GlobalStats.ShipCountLimit.ToString());
                evt.Add("Commit",
                    String.Format("https://bitbucket.org/CrunchyGremlin/sd-blackbox/commits/{0}", GlobalStats.Version));
            }
            if (ex.Data.Count == 0) return string.Empty;
            string text = string.Empty;
            StringBuilder test = new StringBuilder("\nExtra Data Recorded :\n");
            //text += "\nExtra Data Recorded :\n";
            foreach (DictionaryEntry pair in ex.Data)
                test.AppendFormat("{0}\n {1} = {2}", text, pair.Key, pair.Value);
                //text = String.Format("{0}\n {1} = {2}", text, pair.Key, pair.Value); 
            
            return test.ToString();
            return text;
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
