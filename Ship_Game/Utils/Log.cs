using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
            init += $" ============ StarDrive {MainMenuScreen.Version} \r\n";
            init += $" ============ UTC: {DateTime.UtcNow} \r\n";
            init +=  " ================================================================== \r\n";
            LogFile.Write(init);

            if (HasDebugger)
            {
                // if Console output is redirected, all console text is sent to VS Output instead
                // in that case, showing the console is pointless, however if output isn't redirected
                // we should enable the console window
                if (Console.IsOutputRedirected == false)
                    ShowConsoleWindow();

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

            if (!HasDebugger)
            {
                // only log errors to sentry if debugger not attached
                Raven.Capture(new SentryEvent(text)
                {
                    Level = ErrorLevel.Error
                });
                return;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }

        // write an Exception to logfile, sentry.io and debug console with an error message
        // plus trigger a Debugger.Break
        public static void Exception(Exception ex, string format, params object[] args)
        {
            Exception(ex, string.Format(format, args));
        }
        public static void Exception(Exception ex, string error)
        {
            string text = "!! Exception: " + error;
            LogFile.WriteLine(text);

            if (!HasDebugger)
            {
                // only log errors to sentry if debugger not attached
                Raven.Capture(new SentryEvent(ex)
                {
                    Message = new SentryMessage(text + " | " + ex.Message),
                    Level = ErrorLevel.Fatal, // this is soooooo serious
                });
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(text);
            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }

        [DllImport("kernel32.dll")] private static extern bool AllocConsole();
        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]   private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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
        }

        public static void HideConsoleWindow()
        {
            ShowWindow(GetConsoleWindow(), 0/*SW_HIDE*/);
        }

    }
}
