using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    internal static class Program
    {
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GraphicsDeviceManager graphicsMgr = StarDriveGame.Instance?.Graphics;
            if (graphicsMgr != null && graphicsMgr.IsFullScreen)
                graphicsMgr.ToggleFullScreen();

            try
            {
                var ex = e.ExceptionObject as Exception;
                Log.ErrorDialog(ex, "Program.CurrentDomain_UnhandledException");
            }
            finally
            {
                StarDriveGame.Instance?.Exit();
            }
        }

        // in case of abnormal termination, run cleanup tasks during process exit
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            RunCleanupTasks();
        }

        static bool HasRunCleanupTasks;
        
        static void RunCleanupTasks()
        {
            if (HasRunCleanupTasks)
                return;
            try
            {
                HasRunCleanupTasks = true;
                Log.Write("RunCleanupTasks()");
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
                using (var instance = new SingleGlobalInstance())
                {
                    if (!instance.UniqueInstance)
                    {
                        MessageBox.Show("Another instance of SD-BlackBox is already running!");
                        return;
                    }

                    using (var game = new StarDriveGame())
                        game.Run();
                }

                Log.Write("The game exited normally.");
            }
            catch (Exception ex)
            {
                Log.ErrorDialog(ex, "Fatal main loop failure");
            }
            finally
            {
                RunCleanupTasks();
            }
        }
    }
}