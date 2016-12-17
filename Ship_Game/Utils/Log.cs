using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ship_Game
{
    public static class Log
    {
        private static readonly StreamWriter LogFile = new StreamWriter("blackbox.log", true, Encoding.ASCII, 8192);
        public static readonly bool HasDebugger = Debugger.IsAttached;
        static Log()
        {
            string init = "\r\n\r\n";
            init += $" ============ StarDrive {MainMenuScreen.Version} ============ \r\n";
            init += $" ============ UTC: {DateTime.UtcNow} \r\n";
            init +=  " ============================================================ \r\n";
            LogFile.Write(init);

            if (HasDebugger)
            {
                ShowConsoleWindow();
                Console.WriteLine(init);
            }
            else
            {
                HideConsoleWindow();
            }
        }


        // just info, don't write to logfile
        [Conditional("DEBUG")]
        public static void Info(string format, params object[] args)
        {
            if (!HasDebugger)
                return;
            string text = string.Format(format, args);
            Console.ResetColor();
            Console.WriteLine(text);
        }
        [Conditional("DEBUG")]
        public static void Info(string text)
        {
            if (!HasDebugger)
                return;
            Console.ResetColor();
            Console.WriteLine(text);
        }


        // write a warning to logfile and debug console
        public static void Warning(string format, params object[] args)
        {
            string text = "Warning: " + string.Format(format, args);
            LogFile.WriteLine(text);
            if (!HasDebugger)
                return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
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


        // write an error to logfile and debug console
        // plus trigger a Debugger.Break
        public static void Error(string format, params object[] args)
        {
            string text = "!! Error: " + string.Format(format, args);
            LogFile.WriteLine(text);
            if (!HasDebugger)
                return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);

            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }
        public static void Error(string error)
        {
            string text = "!! Error: " + error;
            LogFile.WriteLine(text);
            if (!HasDebugger)
                return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);

            // Error triggered while in Debug mode. Check the error message for what went wrong
            Debugger.Break();
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(int nStdHandle, IntPtr handle);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static bool HasActiveConsole => GetConsoleWindow() != IntPtr.Zero;

        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
            {
                //// @todo This doesn't work with VS Debugger
                //AllocConsole();
                //Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }
            else ShowWindow(handle, 5/*SW_SHOW*/);
        }

        public static void HideConsoleWindow()
        {
            ShowWindow(GetConsoleWindow(), 0/*SW_HIDE*/);
        }

    }
}
