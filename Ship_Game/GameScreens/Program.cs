using System;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Ship_Game;

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

    public static void RunCleanup()
    {
        if (HasRunCleanupTasks)
            return;
        try
        {
            HasRunCleanupTasks = true;
            Parallel.ClearPool(); // Dispose all thread pool Threads
            Log.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while trying to exit the process");
        }
        finally
        {
            Log.FlushAllLogs(); // desperate
        }
    }

    public static void RunCleanupAndExit(int exitCode)
    {
        if (HasRunCleanupTasks)
            return;
            
        Log.Write($"RunCleanupAndExit({exitCode})");
        RunCleanup();
        Environment.Exit(exitCode);
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
                GlobalStats.ExportMeshes = value.IsEmpty() ? "obj" : value;
            }
            else if (key == "--generate-hulls")
            {
                GlobalStats.GenerateNewHullFiles = true;
            }
            else if (key == "--generate-ships")
            {
                GlobalStats.GenerateNewShipDesignFiles = true;
            }
            else if (key == "--fix-roles")
            {
                GlobalStats.FixDesignRoleAndCategory = true;
            }
            else if (key.StartsWith("--run-localizer"))
            {
                GlobalStats.RunLocalizer = value.IsEmpty() ? 1 : int.Parse(value);
            }
            else if (key == "--resource-debug")
            {
                GlobalStats.DebugResourceLoading = true;
            }
            else if (key == "--asset-debug")
            {
                GlobalStats.DebugAssetLoading = true;
            }
            else if (key == "--console")
            {
                Log.ShowConsoleWindow();
            }
            else if (key == "--continue")
            {
                GlobalStats.ContinueToGame = true;
            }
            else
            {
                Log.Warning($"Unrecognized argument: '{arg}'");
            }
        }
        return true; // all ok
    }

    static void PrintHelp()
    {
        Log.Write("StarDrive BlackBox Command Line Interface (CLI)");
        Log.Write("  --help              Shows this help message");
        Log.Write("  --mod=\"<mod>\"     Load the game with the specified <mod> path, eg: --mod=\"Combined Arms\" ");
        Log.Write("  --export-textures   Exports all texture files as PNG and DDS to game/ExportedTextures");
        Log.Write("  --export-meshes=obj Exports all mesh files and textures, options: fbx obj fbx+obj");
        Log.Write("  --generate-hulls    Generates new .hull files from old XML hulls");
        Log.Write("  --generate-ships    Generates new ship .design files from old XML ships");
        Log.Write("  --fix-roles         Fixes Role and Category for all .design ships");
        Log.Write("  --run-localizer=[0-2] Run localization tool to merge missing translations and generate id-s");
        Log.Write("                        0: disabled  1: generate with YAML NameIds  2: generate with C# NameIds");
        Log.Write("  --resource-debug    Debug logs all resource loading, mainly for Mods to ensure their assets are loaded");
        Log.Write("  --asset-debug       Debug logs all asset load events, useful for analyzing the order of assets being loaded");
        Log.Write("  --console           Enable the Debug Console which mirrors blackbox.log");
        Log.Write("  --continue          After running CLI tasks, continue to game as normal");
    }

    static void PressAnyKey()
    {
        if (Console.IsInputRedirected)
            return;
        Log.Write(ConsoleColor.Gray, "Press any key to continue...");
        Console.ReadKey(false);
    }

    // CLI tasks
    static bool RunInitializationTasks()
    {
        bool runGame = true; // Ok, continue to game

        if (GlobalStats.RunLocalizer > 0)
        {
            Tools.Localization.LocalizationTool.Run(GlobalStats.ModPath, GlobalStats.RunLocalizer);
            runGame = GlobalStats.ContinueToGame;
        }

        if (GlobalStats.ExportTextures)
        {
            ResourceManager.RootContent.RawContent.ExportAllTextures();
            runGame = GlobalStats.ContinueToGame;
        }

        if (GlobalStats.ExportMeshes != null)
        {
            Log.Write($"ExportMeshes {GlobalStats.ExportMeshes}");
            string[] formats = GlobalStats.ExportMeshes.Split('+'); // "fbx+obj"
            foreach (string ext in formats)
            {
                ResourceManager.RootContent.RawContent.ExportAllXnbMeshes(ext);
            }
            runGame = GlobalStats.ContinueToGame;
        }

        if (!runGame && Log.HasDebugger)
        {
            PressAnyKey();
        }
        return runGame;
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
            Log.Initialize(enableSentry: true, showHeader: true);
            Thread.CurrentThread.Name = "Main Thread";
            Log.AddThreadMonitor();

            if (!ParseMainArgs(args))
            {
                PrintHelp();
            }
            else
            {
                using StarDriveGame game = new();
                game.OnInitialize = RunInitializationTasks;
                game.Run();
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
