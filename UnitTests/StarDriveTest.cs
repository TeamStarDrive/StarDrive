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
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");

            try
            {
                Assembly xna = Assembly.Load("Microsoft.Xna.Framework");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException?.Message ?? "");
                Console.WriteLine(e.InnerException?.InnerException?.Message ?? "");
            }
        }

        public StarDriveTest()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
        }
    }
}
