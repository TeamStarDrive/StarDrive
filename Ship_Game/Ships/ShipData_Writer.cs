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
        void Save(FileInfo file, int version, bool isHull)
        {
            var sb = new StringBuilder();

            WriteValue(sb, "version", version);
            WriteValue(sb, "name", Name);
            WriteValue(sb, "hull", Hull);
            WriteValue(sb, "role", Role);
            WriteValue(sb, "mod", ModName);
            WriteValue(sb, "style", ShipStyle);

            WriteValue(sb, "size", $"{GridInfo.Size.X},{GridInfo.Size.Y}");
            WriteValue(sb, "area", GridInfo.SurfaceArea);

            WriteValue(sb, "icon", IconPath);
            WriteValue(sb, "model", ModelPath);

            WriteValue(sb, "selection_gfx", SelectionGraphic);
            WriteValue(sb, "default_ai_state", DefaultAIState);
            WriteValue(sb, "combat_state", CombatState);

            WriteValue(sb, "animated", Animated);

            foreach (ThrusterZone t in ThrusterList)
                WriteValue(sb, "thruster", $"pos:{t.Position.X},{t.Position.Y} scale:{t.Scale}");

            if (isHull)
            {
                foreach (ModuleSlotData slot in ModuleSlots)
                {
                    // For Hulls, all slots are 1x1, there are no coordinates, the layout is described
                    // by lines and columns, each column is 3 characters wide and separated by a |
                    // ___|O__|___
                    // IO_|E__|IO_


                }
            }
            else
            {
                WriteValue(sb, "orbital_defense", IsOrbitalDefense);
            }

            File.WriteAllText(file.FullName, sb.ToString(), Encoding.UTF8);
        }


        static void WriteValue<T>(StringBuilder sb, string key, T value)
        {
            sb.Append($"{key}={value}\n");
        }

        static void WriteValue(StringBuilder sb, string key, string value)
        {
            if (value.NotEmpty())
                sb.Append($"{key}={value}\n");
        }
    }
}
