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
                #if RELEASE //only log exception on release build
                  ExceptionTracker.TrackException(ex);
                #endif
                ExceptionTracker.DisplayException(ex);
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
            catch (Exception e)
            {
                MessageBox.Show($"Whoops! Please post a screenshot of this to the StarDrive forums ({MainMenuScreen.Version}):\n\n{e.ToString()}");
            }
        }
	}
}