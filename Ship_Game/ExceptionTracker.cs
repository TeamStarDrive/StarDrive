using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

///Added by Crimsoned

namespace Ship_Game
{
    internal static class ExceptionTracker
    {
        public const string BugtrackerURL = "https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/new";
        public const string KudosURL = "http://www.indiedb.com/mods/deveks-mod/reviews";
        const string DefaultText = "Whoops! Please post this StarDrive forums or in the Bugtracker";
        public static bool active = false;
        public static bool Kudos = false;
        private static string GenerateErrorLines(Exception ex)
        {

            string rn = Environment.NewLine;
            //######################
            //----------
            // Moved from Ship_Game\Program.cs

            ex.Data["Date"] = DateTime.Now;                
            string mod ="Vanilla";
            string modVersion = null;
                
            string data ="No Extra Info";
			if (GlobalStats.ActiveMod != null)
            {
                mod = GlobalStats.ActiveMod.ModPath;
                if(GlobalStats.ActiveModInfo !=null && !string.IsNullOrEmpty(GlobalStats.ActiveModInfo.Version)) // && GlobalStats.ActiveModInfo.Version !="" )
                {
                    modVersion = GlobalStats.ActiveMod.mi.Version;
                }
            }
            
            if (Empire.Universe != null)
            {
                ex.Data["StarDate"] = Empire.Universe.StarDate.ToString("F1");
                if (Empire.Universe.MasterShipList != null)
                    ex.Data["ShipCount"] = Empire.Universe.MasterShipList.Count.ToString();
                    
                ex.Data["Planet Count"] = Empire.Universe.PlanetsDict.Count.ToString();
                    
            }

            ex.Data["Mod"] = mod;
            if (mod != null)
                ex.Data["Mod Version"] = modVersion;    
            ex.Data["Memory"] = ((int)(GC.GetTotalMemory(false)/1000)).ToString();
            ex.Data["Memory Limit"] = GlobalStats.MemoryLimiter;
            ex.Data["Ship Limit"] = GlobalStats.ShipCountLimit; 

            if(ex.Data.Count >0)
            {
                data="Extra Data Recorded :";
                foreach(DictionaryEntry pair in ex.Data )
                data = string.Concat(data,rn,pair.Key," = ",pair.Value);
            }
            //----------

            //string[] version = new string[] { "Whoops! Please post a screenshot of this to the StarDrive forums \n(", MainMenuScreen.Version,
            //    ")\n",data,":\n\n", ex.Message.ToString(), ex.StackTrace.ToString()};
            //######################


            
            string msg = "Version: ("+ MainMenuScreen.Version+ "): "+rn;
            msg += data+rn+rn;
            msg += "Exception : "+ ex.Message.ToString()+rn;
            msg += "ExceptionClass : "+ex.GetType().ToString()+rn;
            if(ex.StackTrace != null)
            msg += "Stacktrace: " + rn + ex.StackTrace.ToString()+ rn ;
            if (ex.InnerException != null)
            {
                msg += "Inner Exception: " +ex.InnerException.Message.ToString() + rn ;
                msg += "Inner Exception Stacktrace: " + rn + ex.InnerException.StackTrace.ToString();
            }

            /*string[] messageblock = new string[] { "Version: (", MainMenuScreen.Version, "): ",rn,
                    "Exception : ", ex.Message.ToString(),rn,
                    "ExceptionClass : ",ex.GetType().ToString(),rn,
                    ex.InnerException.Message.ToString()
                    "Stacktrace: ", rn , ex.StackTrace.ToString()};
            */

            return msg;
        }
        private static string GenerateErrorLines_withWhoops(Exception ex)
        {
            if (!(ex.Message == "Manual Report" || ex.Message == "Kudos"))
                return DefaultText + Environment.NewLine + GenerateErrorLines(ex);
            return ex.Message + Environment.NewLine + GenerateErrorLines(ex);            
        }

        public static void TrackException(Exception ex)
        {
            try
            {
                active = true;
                string dts = DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss");
                string path = AppDomain.CurrentDomain.BaseDirectory + "Exception " + dts + ".log";
                System.IO.StreamWriter file = new System.IO.StreamWriter(path);
                file.Write(GenerateErrorLines(ex));
                file.Close();
                
            }
            catch (Exception)
            {
                MessageBox.Show(GenerateErrorLines_withWhoops(ex));
            }

        }


        public static void DisplayException(Exception ex)
        {
            #if DEBUG
                if (!(ex.Message == "Manual Report" || ex.Message =="Kudos"))
                    return;
            #endif
            try
            {
                Form form = (Form)Control.FromHandle(Game1.Instance.Window.Handle);
                form.WindowState = FormWindowState.Minimized;
                form.Update();
            }
            catch { }
            try
            {
                ExceptionViewer exviewer = new ExceptionViewer();
                exviewer.ShowDialog(GenerateErrorLines_withWhoops(ex));
               
            }
            catch (Exception)
            {
                MessageBox.Show(GenerateErrorLines_withWhoops(ex));
            }
            active = false;
        }
    }
}
