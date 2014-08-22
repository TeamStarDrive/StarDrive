using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;
using System.Collections;

namespace Ship_Game
{
	internal static class Program
	{
		private static bool CatchStuff = false;

		static Program()
		{
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
             

            
  
            
            try
            {
                //added by CrimsonED
                //---
                Exception ex = (Exception)e.ExceptionObject;
                #if !DEBUG //only log exception on release build
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
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Program.CurrentDomain_UnhandledException);



            //ExceptionTracker.TestStackTrace(0, 10);
            //var ex = new NullReferenceException("Message");
            //throw ex;

            if (!Program.CatchStuff)
			{
				using (Game1 game = new Game1())
				{
					game.Run();
				}
			}
			else
			{
				try
				{
					using (SingleGlobalInstance singleGlobalInstance = new SingleGlobalInstance(1000))
					{
						using (Game1 game = new Game1())
						{
							game.Run();
						}
					}
				}
				catch (Exception exception)
				{
                    //var form = (Form)Form.FromHandle(Game1.Instance.Window.Handle);
                    //form.WindowState = FormWindowState.Minimized;
                    
                    Exception e = exception;
                    MessageBox.Show(string.Concat("Whoops! Please post a screenshot of this to the StarDrive forums (", MainMenuScreen.Version, "):\n\n", e.ToString()));
				}
			}
		}
	}
}