using System;
using System.Windows.Forms;
namespace Ship_Game
{
	internal static class Program
	{
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
            var graphicsMgr = Game1.Instance?.Graphics;
            if (graphicsMgr != null && graphicsMgr.IsFullScreen)
                graphicsMgr.ToggleFullScreen();

            try
            {
                Exception ex = e.ExceptionObject as Exception;
                Log.Error(ex, "Unhandled Exception");
                ExceptionViewer.ShowExceptionDialog(ex);
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
            try
            {
                using (var instance = new SingleGlobalInstance())
                {
                    if (!instance.UniqueInstance)
                    {
                        MessageBox.Show("Another instance of SD-BlackBox is already running!");
                        return;
                    }
                    new Game1().Run();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fatal main loop failure");
                ExceptionViewer.ShowExceptionDialog(ex);
            }
        }
	}
}