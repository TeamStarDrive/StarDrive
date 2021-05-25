using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    // NOTE: public variables are SERIALIZED
    public partial class ShipData
    {
        static ShipData ParseHull(FileInfo info)
        {
            return null;
        }

        static void ConvertXMLToHull(FileInfo xml, FileInfo outFile)
        {
            ShipData hull = ParseXML(xml, isHullDefinition: true);
            var newHull = new ShipHull(hull);
            newHull.Save(outFile);
        }
    }
}
