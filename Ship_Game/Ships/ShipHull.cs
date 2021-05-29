using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public class ShipHull
    {
        public const int Version = 1;

        public string HullName; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string ModName; // null if vanilla, else mod name eg "Combined Arms"
        public string Style; // "Terran"
        public Point Size;
        public int Area;
        public string IconPath; // "ShipIcons/shuttle"
        public string ModelPath; // "Model/Ships/Terran/Shuttle/ship08"
        
        public ShipData.RoleName Role = ShipData.RoleName.fighter;
        public string SelectIcon = "";
        public bool Animated;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        public ShipData.ThrusterZone[] Thrusters = Empty<ShipData.ThrusterZone>.Array;
        public HullSlot[] HullSlots;

        public FileInfo Source;

        public ShipHull()
        {
        }

        // LEGACY: convert old ShipData hulls into new .hull
        public ShipHull(ShipData sd)
        {
            HullName = sd.Hull;
            ModName = sd.ModName;
            Style = sd.ShipStyle;
            Size = sd.GridInfo.Size;
            Area = sd.GridInfo.SurfaceArea;
            IconPath = sd.IconPath;
            ModelPath = sd.ModelPath;

            Role = sd.Role;
            SelectIcon = sd.SelectionGraphic;
            Animated = sd.Animated;
            Thrusters = sd.ThrusterList.CloneArray();

            Vector2 origin = sd.GridInfo.Origin;
            var legacyOffset = new Vector2(ShipModule.ModuleSlotOffset);

            HullSlots = new HullSlot[sd.ModuleSlots.Length];
            for (int i = 0; i < sd.ModuleSlots.Length; ++i)
            {
                ModuleSlotData msd = sd.ModuleSlots[i];
                Vector2 pos = (msd.Position - legacyOffset) - origin;
                HullSlots[i] = new HullSlot((int)(pos.X / 16f),
                                            (int)(pos.Y / 16f),
                                            msd.Restrictions);
            }
        }

        public ShipHull(FileInfo file)
        {
            Source = file;

            string[] lines = File.ReadAllLines(file.FullName);
            bool parsingSlots = false;
            var slots = new Array<HullSlot>();
            int height = 0;

            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i];
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                if (!parsingSlots)
                {
                    string[] parts = line.Split('=');
                    string val = parts.Length > 1 ? parts[1] : "";
                    switch (parts[0])
                    {
                        case "Version":
                            int version = int.Parse(val);
                            // only emit a Warning, because we expect this format to stay stable
                            if (version != Version)
                                Log.Warning($"Hull {file.NameNoExt()} file version={version} does not match current={Version}");
                            break;
                        case "HullName":   HullName = val; break;
                        case "Role":       Enum.TryParse(val, out Role); break;
                        case "ModName":    ModName = val; break;
                        case "Style":      Style = val; break;
                        case "Size":       Size = PointSerializer.FromString(val); break;
                        case "IconPath":   IconPath = val; break;
                        case "ModelPath":  ModelPath = val; break;
                        case "SelectIcon": SelectIcon = val; break;
                        case "Animated":   Animated = (val == "true"); break;
                        case "IsShipyard": IsShipyard = (val == "true"); break;
                        case "IsOrbitalDefense": IsOrbitalDefense = (val == "true"); break;
                        case "Thruster":
                            Array.Resize(ref Thrusters, Thrusters.Length + 1);
                            ref ShipData.ThrusterZone tz = ref Thrusters[Thrusters.Length - 1];
                            Vector4 t = Vector4Serializer.FromString(val);
                            tz.Position = new Vector3(t.X, t.Y, t.Z);
                            tz.Scale = t.W;
                            break;
                        case "Slots":
                            parsingSlots = true;
                            break;
                    }
                }
                else // parsingSlots:
                {
                    string[] cols = line.Split('|');
                    if (cols.Length != Size.X)
                        Log.Error($"Hull {file.NameNoExt()} line {i+1} design columns={cols.Length} does not match defined Size Width={Size.X}");

                    for (int x = 0; x < cols.Length; ++x)
                    {
                        string col = cols[x];
                        if (col != "___" && Enum.TryParse(col.Trim(), out Restrictions r))
                            slots.Add(new HullSlot(x, height, r));
                    }
                    ++height;
                }
            }

            if (height != Size.Y)
                Log.Error($"Hull {file.NameNoExt()} design rows={height} does not match defined Size Height={Size.Y}");

            HullSlots = slots.ToArray();
            Area = HullSlots.Length;
        }

        public void Save(FileInfo file)
        {
            var sw = new ShipDataWriter();
            sw.Write("Version", Version);
            sw.Write("HullName", HullName);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", Style);
            sw.Write("Size", Size.X+","+Size.Y);
            sw.Write("IconPath", IconPath);
            sw.Write("ModelPath", ModelPath);
            sw.Write("SelectIcon", SelectIcon);

            if (Animated)         sw.Write("Animated", Animated);
            if (IsShipyard)       sw.Write("IsShipyard", IsShipyard);
            if (IsOrbitalDefense) sw.Write("IsOrbitalDefense", IsOrbitalDefense);

            sw.WriteLine("#Thruster PosX,PosY,PosZ,Scale");
            foreach (ShipData.ThrusterZone t in Thrusters)
                sw.Write("Thruster", $"{t.Position.X},{t.Position.Y},{t.Position.Z},{t.Scale}");

            sw.WriteLine("Slots");
            var gridInfo = new ShipGridInfo { Size = Size };
            var grid = new ModuleGrid<HullSlot>(gridInfo, HullSlots);

            var sb = new StringBuilder();
            for (int y = 0; y < grid.Height; ++y)
            {
                // For Hulls, all slots are 1x1, there are no coordinates, the layout is described
                // by lines and columns, each column is 3 characters wide and separated by a |
                // ___|O  |___
                // IO |E  |IO 
                for (int x = 0; x < grid.Width; ++x)
                {
                    HullSlot slot = grid[x, y];
                    if (slot == null)
                    {
                        sb.Append("___");
                    }
                    else
                    {
                        string r = slot.R.ToString();
                        sb.Append(r);
                        if (3 - r.Length > 0)
                            sb.Append(' ', 3 - r.Length);
                    }
                    if (x != (grid.Width-1))
                        sb.Append('|');
                }
                sw.WriteLine(sb.ToString());
                sb.Clear();
            }

            sw.FlushToFile(file);
            Log.Info($"Saved {file.FullName}");
        }
    }
}
