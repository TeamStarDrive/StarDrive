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
             
            try{
                Exception ex = (Exception)e.ExceptionObject;
                
                ex.Data["Date"] = DateTime.Now;                
                string mod ="Vanilla";
                
                string data ="No Extra Info";
                if(GlobalStats.ActiveMod !=null)                   
                mod=GlobalStats.ActiveMod.ModPath;
                if (Empire.universeScreen != null)
                {
                    if (Empire.universeScreen.MasterShipList != null)
                        ex.Data["ShipCount"] = Empire.universeScreen.MasterShipList.Count.ToString();
                    
                    ex.Data["Planet Count"] = Empire.universeScreen.PlanetsDict.Count.ToString();
                    
                }

                ex.Data["Mod"] = mod;
                
                ex.Data["Memory"] = ((int)(GC.GetTotalMemory(false)/1000)).ToString();
                ex.Data["Memory Limit"] = GlobalStats.MemoryLimiter;
                ex.Data["Ship Limit"] = GlobalStats.ShipCountLimit; 

                if(ex.Data.Count >0)
                {
                    data="Extra Data Recorded :";
                    foreach(DictionaryEntry pair in ex.Data )
                    data = string.Concat(data,"\n",pair.Key," = ",pair.Value);
                }
                

                string[] version = new string[] { "Whoops! Please post a screenshot of this to the StarDrive forums \n(", MainMenuScreen.Version,
                    ")\n",data,":\n\n", ex.Message.ToString(), ex.StackTrace.ToString(), ex.ToString() };
				MessageBox.Show(string.Concat(version), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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




           // var ex = new NullReferenceException("Message");

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
					Exception e = exception;
					MessageBox.Show(string.Concat("Whoops! Please post a screenshot of this to the StarDrive forums (", MainMenuScreen.Version, "):\n\n", e.ToString()));
				}
			}
		}
	}
}