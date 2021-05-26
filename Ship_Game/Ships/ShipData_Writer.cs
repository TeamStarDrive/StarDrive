using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    // NOTE: public variables are SERIALIZED
    public partial class ShipData
    {
        void Save(FileInfo file, int version)
        {
            var sw = new ShipDataWriter();
            sw.Write("Version", version);
            sw.Write("Name", Name);
            sw.Write("Hull", Hull);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", ShipStyle);
            sw.Write("Size", $"{GridInfo.Size.X},{GridInfo.Size.Y}");
            sw.Write("Area", GridInfo.SurfaceArea);
            sw.Write("IconPath", IconPath);
            sw.Write("ModelPath", ModelPath);

            if (this != BaseHull && SelectionGraphic != BaseHull.SelectionGraphic)
                sw.Write("SelectIcon", SelectionGraphic);

            sw.Write("DefaultAIState", DefaultAIState);
            sw.Write("CombatState", CombatState);
            sw.Write("OrbitalDefense", IsOrbitalDefense);

            sw.FlushToFile(file);
        }
    }
}
