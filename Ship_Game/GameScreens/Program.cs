using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    internal static class Program
    {
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GraphicsDeviceManager graphicsMgr = Game1.Instance?.Graphics;
            if (graphicsMgr != null && graphicsMgr.IsFullScreen)
                graphicsMgr.ToggleFullScreen();

            try
            {
                var ex = e.ExceptionObject as Exception;
                Log.ErrorDialog(ex, "Program.CurrentDomain_UnhandledException");
            }
            finally
            {
                Game1.Instance?.Exit();
            }
        }

        [STAThread]
        private static void Main(string[] args)
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
                        MessageBox.Show("Another instance of SD-BlackBox is already running!");
                        return;
                    }

                    using (var game = new Game1())
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
                Environment.Exit(0);
            }
        }
    }
}