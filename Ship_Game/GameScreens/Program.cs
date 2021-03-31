using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    internal static class Program
    {
        public const int GAME_RUN_FAILURE = -1;
        public const int SCREEN_UPDATE_FAILURE = -2;
        public const int UNHANDLED_EXCEPTION = -3;

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GraphicsDeviceManager graphicsMgr = StarDriveGame.Instance?.Graphics;
            if (graphicsMgr != null && graphicsMgr.IsFullScreen)
                graphicsMgr.ToggleFullScreen();

            var ex = e.ExceptionObject as Exception;
            Log.ErrorDialog(ex, "Program.CurrentDomain_UnhandledException", UNHANDLED_EXCEPTION);
        }

        // in case of abnormal termination, run cleanup tasks during process exit
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            RunCleanupAndExit(Environment.ExitCode);
        }

        static bool HasRunCleanupTasks;
        
        public static void RunCleanupAndExit(int exitCode)
        {
            if (HasRunCleanupTasks)
                return;
            try
            {
                HasRunCleanupTasks = true;
                Log.Write($"RunCleanupAndExit({exitCode})");
                Log.StopLogThread();
                Parallel.ClearPool(); // Dispose all thread pool Threads
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while trying to exit the process");
            }
            finally
            {
                Log.FlushAllLogs();
                Environment.Exit(exitCode);
            }
        }

        static void ParseMainArgs(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg == "--export-textures")
                    GlobalStats.ExportTextures = true;
                else if (arg == "--export-meshes")
                    GlobalStats.ExportMeshes = true;
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit        += CurrentDomain_ProcessExit;
            Thread.CurrentThread.CurrentCulture   = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture   = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            try
            {
                // WARNING: This must be called before ANY Log calls
                // @note This will override and initialize global system settings
                GlobalStats.LoadConfig();
                Log.Initialize();

                ParseMainArgs(args);

                Thread.CurrentThread.Name = "Main Thread";
                Log.AddThreadMonitor();

                using (var game = new StarDriveGame())
                    game.Run();

                Log.Write("The game exited normally.");
                RunCleanupAndExit(0);
            }
            catch (Exception ex)
            {
                Log.ErrorDialog(ex, "Game.Run() failed", GAME_RUN_FAILURE);
            }
        }
    }
}