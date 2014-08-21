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
        public const string BugtrackerURL = "https://bitbucket.org/CrunchyGremlin/sd-idk/issues?status=new&status=open";
        const string DefaultText = "Whoops! Please post this StarDrive forums or in the Bugtracker";

        private static string GenerateErrorLines(Exception ex)
        {

            string rn = Environment.NewLine;
            //######################
            //----------
            // Moved from Ship_Game\Program.cs

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
            return DefaultText + Environment.NewLine + GenerateErrorLines(ex);
        }

        public static void TrackException(Exception ex)
        {
            try
            {
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
            try
            {
                ExceptionViewer exviewer = new ExceptionViewer();
                exviewer.ShowDialog(GenerateErrorLines_withWhoops(ex));
            }
            catch (Exception)
            {
                MessageBox.Show(GenerateErrorLines_withWhoops(ex));
            }
        }

        #if DEBUG
        //test exception
        //ExceptionTracker.TestStackTrace(0, 10);

        /// <summary>
        /// testing stacktrace
        /// </summary>
        /// <param name="StartCount">sould be smaller than para2</param>
        /// <param name="ErrorOn">ErrorOn = on which number make a crash </param>
        public static void TestStackTrace(int StartCount,int ErrorOn)
        {
            if (StartCount == ErrorOn)
                throw new StarDriveTestException("test exception");
            else
            {
                StartCount++;
                TestStackTrace(StartCount, ErrorOn);
            }

        }
        #endif //debug
    }

    #if DEBUG
    internal class StarDriveTestException : Exception
    {
        public StarDriveTestException()
        {
        }

        public StarDriveTestException(string message)
            : base(message)
        {
        }

        public StarDriveTestException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    #endif //debug
}
