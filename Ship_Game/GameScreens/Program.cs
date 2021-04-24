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

        // @return false if Help should be printed to console
        static bool ParseMainArgs(string[] args)
        {
            foreach (string arg in args)
            {
                string[] parts = arg.Split('=');
                string key = parts[0];
                string value = parts.Length > 1 ? parts[1] : "";

                if (key == "--help")
                {
                    return false;
                }
                else if (key == "--mod")
                {
                    GlobalStats.LoadModInfo(value);
                    if (!GlobalStats.HasMod)
                        throw new Exception($"Mod {value} not found. Argument was: {arg}");
                }
                else if (key == "--export-textures")
                {
                    GlobalStats.ExportTextures = true;
                }
                else if (key == "--export-meshes")
                {
                    GlobalStats.ExportMeshes = true;
                }
                else if (key.StartsWith("--run-localizer"))
                {
                    GlobalStats.RunLocalizer = value.IsEmpty() ? true : value == "1";
                }
                else if (key == "--continue")
                {
                    GlobalStats.ContinueToGame = true;
                }
            }
            return true; // all ok
        }

        static void PrintHelp()
        {
            Log.Write("StarDrive BlackBox Command Line Interface (CLI)");
            Log.Write("  --help             Shows this help message");
            Log.Write("  --mod=\"<mod>\"    Load the game with the specified <mod>, eg: --mod=\"Combined Arms\" ");
            Log.Write("  --export-textures  Exports all texture files as PNG and DDS");
            Log.Write("  --export-meshes    Exports all mesh files as FBX");
            Log.Write("  --run-localizer    Run localization tool to merge missing translations and generate id-s");
            PressAnyKey();
        }

        static void PressAnyKey()
        {
            Log.Write(ConsoleColor.Gray, "Press any key to continue...");
            Console.ReadKey(false);
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
                Thread.CurrentThread.Name = "Main Thread";
                Log.AddThreadMonitor();
                
                if (!ParseMainArgs(args))
                {
                    PrintHelp();
                }
                else
                {
                    bool runGame = true;
                    if (GlobalStats.RunLocalizer)
                    {
                        Tools.Localization.LocalizationTool.Run(GlobalStats.ModName);
                        runGame = GlobalStats.ContinueToGame;
                    }

                    if (runGame)
                    {
                        using (var game = new StarDriveGame())
                            game.Run();
                    }
                    else
                    {
                        PressAnyKey();
                    }
                }

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