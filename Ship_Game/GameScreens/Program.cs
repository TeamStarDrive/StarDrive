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

        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
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
                        // TODO: Uncomment this
                        //MessageBox.Show("Another instance of SD-BlackBox is already running!");
                        //return;
                    }

                    using (var game = new StarDriveGame())
                        game.Run();
                }
            }
            catch (Exception ex)
            {
                Log.WarningVerbose($"FailSafe log {ex.InnerException}");
                Log.ErrorDialog(ex, "Fatal main loop failure");
            }
            finally
            {
                Parallel.ClearPool();
                Log.FlushAllLogs();
                Environment.Exit(0);
            }
        }
    }
}