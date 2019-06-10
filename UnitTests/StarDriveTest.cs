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
            catch (Exception e)
            {
                Console.WriteLine("XNA Load Failed");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException?.Message ?? "");
                Console.WriteLine(e.InnerException?.InnerException?.Message ?? "");
                throw;
            }
        }

        public StarDriveTest()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
        }
    }
}
