using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;

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
				Exception ex = (Exception)e.ExceptionObject;
				string[] version = new string[] { "Whoops! Please post a screenshot of this to the StarDrive forums (", MainMenuScreen.Version, "):\n\n", ex.Message.ToString(), ex.StackTrace.ToString() };
				MessageBox.Show(string.Concat(version), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
					Exception e = exception;
					MessageBox.Show(string.Concat("Whoops! Please post a screenshot of this to the StarDrive forums (", MainMenuScreen.Version, "):\n\n", e.ToString()));
				}
			}
		}
	}
}