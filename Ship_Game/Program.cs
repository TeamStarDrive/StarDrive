using System;
using System.Windows.Forms;
namespace Ship_Game
{
	internal static class Program
	{
        // Refactored by RedFox: should we keep this enabled?
        private static readonly bool CatchStuff = true;

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{

            try
            {
                if (Game1.Instance.graphics.IsFullScreen)
                    Game1.Instance.graphics.ToggleFullScreen();
            }
            catch { }
            try
            {
                
                //added by CrimsonED
                //---
                Exception ex = (Exception)e.ExceptionObject;
                #if RELEASE //only log exception on release build
                  ExceptionTracker.TrackException(ex);
                #endif
                ExceptionTracker.DisplayException(ex);
                //---
            }
            catch
            {
                Exception ex = (Exception)e.ExceptionObject; 
                MessageBox.Show("BlackBox failsafe Error Trap\n\n"+e);
            }
			finally
			{
				Game1.Instance.Exit();
			}
		}

		[STAThread]
		private static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            if (!CatchStuff)
			{
				using (Game1 game = new Game1())
					game.Run();
			    return;
			}

            try
            {
                using (new SingleGlobalInstance())
                using (Game1 game = new Game1())
                    game.Run();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Whoops! Please post a screenshot of this to the StarDrive forums ({MainMenuScreen.Version}):\n\n{e.ToString()}");
            }
        }
	}
}