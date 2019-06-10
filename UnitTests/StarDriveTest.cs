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
        static StarDriveTest()
        {
            try
            {
                var xna2 = Assembly.LoadFile(
                    "C:/Projects/BlackBox/StarDrive/Microsoft.Xna.Framework.dll");
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

        public StarDriveTest()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
        }
    }
}
