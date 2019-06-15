using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    /// <summary>
    /// Automatic setup for StarDrive unit tests
    /// </summary>
    public class StarDriveTest
    {
        public static string StarDriveAbsolutePath { get; private set; }
        static StarDriveTest()
        {
            SetGameDirectory();
            try
            {
                var xna2 = Assembly.LoadFile(
                    //"C:/Projects/BlackBox/StarDrive/Microsoft.Xna.Framework.dll");
                    $"{StarDriveAbsolutePath}\\Microsoft.Xna.Framework.dll");
                //Assembly xna = Assembly.LoadFrom(
                //    "/Projects/BlackBox/StarDrive/Microsoft.Xna.Framework.dll");
                Console.WriteLine($"XNA Path: {xna2.Location}");
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine($"XNA Load Failed: {e.Message}\n{e.FileName}\n{e.FusionLog}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"XNA Load Failed: {e.Message}\n");
                throw;
            }
        }

        public static void SetGameDirectory()
        {
            Directory.SetCurrentDirectory("../../../stardrive");
            StarDriveAbsolutePath = Directory.GetCurrentDirectory();
        }
    }
}
