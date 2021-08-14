using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Gameplay;
using Ship_Game.Ships.Legacy;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships
{
    public class ShipHull
    {
        // Current version of ShipData files
        // If we introduce incompatibilities we need to convert old to new
        public const int Version = 1;
        
        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! ShipHull must not be automatically serialized! Add [XmlIgnore][JsonIgnore] to `public ShipHull XXX;` PROPERTIES/FIELDS. {this}");

        public string HullName; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string ModName; // null if vanilla, else mod name eg "Combined Arms"
        public string Style; // "Terran"
        public string Description; // "With the advent of more powerful StarDrives, this giant cruiser hull was ..."
        public Point Size;
        public int SurfaceArea;
        public Vector2 MeshOffset; // offset of the mesh from Mesh object center, for grid to match model
        public string IconPath; // "ShipIcons/shuttle"
        public string ModelPath; // "Model/Ships/Terran/Shuttle/ship08"

        public ShipData.RoleName Role = ShipData.RoleName.fighter;
        public string SelectIcon = "";
        public bool Animated;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        public ShipData.ThrusterZone[] Thrusters = Empty<ShipData.ThrusterZone>.Array;
        public HullSlot[] HullSlots;

        // center of this ShipHull's Grid
        [XmlIgnore][JsonIgnore] public Point GridCenter;

        // top-left of this ShipHull's Grid FROM GridCenter
        [XmlIgnore][JsonIgnore] public Point GridOrigin;

        [XmlIgnore][JsonIgnore] public bool Unlockable = true;
        [XmlIgnore][JsonIgnore] public HashSet<string> TechsNeeded = new HashSet<string>();

        [XmlIgnore][JsonIgnore] public FileInfo Source;
        [XmlIgnore][JsonIgnore] public SubTexture Icon => ResourceManager.Texture(IconPath);
        [XmlIgnore][JsonIgnore] public Vector3 Volume { get; private set; }
        [XmlIgnore][JsonIgnore] public float ModelZ { get; private set; }
        [XmlIgnore] [JsonIgnore] public HullBonus Bonuses { get; private set; }

        public HullSlot FindSlot(Point p)
        {
            for (int i = 0; i < HullSlots.Length; ++i)
            {
                var slot = HullSlots[i];
                if (slot.Pos == p)
                    return slot;
            }
            return null;
        }

        public void LoadModel(out SceneObject shipSO, GameContentManager content)
        {
            lock (this)
            {
                shipSO = StaticMesh.GetSceneMesh(content, ModelPath, Animated);

                if (Volume.X.AlmostEqual(0f))
                {
                    Volume = shipSO.GetMeshBoundingBox().Max;
                    ModelZ = Volume.Z;
                }
            }
        }

        public ShipHull()
        {
        }

        // LEGACY: convert old ShipData hulls into new .hull
        public ShipHull(LegacyShipData sd)
        {
            HullName = sd.Hull;
            ModName = sd.ModName ?? "";
            Style = sd.ShipStyle;
            Size = sd.GridInfo.Size;
            SurfaceArea = sd.GridInfo.SurfaceArea;
            MeshOffset  = sd.GridInfo.MeshOffset;
            IconPath = sd.IconPath;
            ModelPath = sd.ModelPath;

            Role = (ShipData.RoleName)(int)sd.Role;
            SelectIcon = sd.SelectionGraphic;
            Animated = sd.Animated;

            Thrusters = new ShipData.ThrusterZone[sd.ThrusterList.Length];
            for (int i = 0; i < sd.ThrusterList.Length; ++i)
            {
                Thrusters[i].Position = sd.ThrusterList[i].Position;
                Thrusters[i].Scale = sd.ThrusterList[i].Scale;
            }

            Vector2 origin = sd.GridInfo.VirtualOrigin;
            var legacyOffset = new Vector2(ShipModule.ModuleSlotOffset);

            HullSlots = new HullSlot[sd.ModuleSlots.Length];
            for (int i = 0; i < sd.ModuleSlots.Length; ++i)
            {
                LegacyModuleSlotData msd = sd.ModuleSlots[i];
                Vector2 pos = (msd.Position - legacyOffset) - origin;
                HullSlots[i] = new HullSlot((int)(pos.X / 16f),
                                            (int)(pos.Y / 16f),
                                            msd.Restrictions);
            }

            Array.Sort(HullSlots, HullSlot.Sorter);

            GridCenter = new Point(sd.GridInfo.Size.X/2, sd.GridInfo.Size.Y/2);
            GridOrigin = new Point(-GridCenter.X, -GridCenter.Y);
            InitializeCommon();
        }

        public ShipHull(string filePath) : this(new FileInfo(filePath))
        {
        }

        public ShipHull(FileInfo file)
        {
            ModName = "";
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
                        case "Description":Description = val; break;
                        case "Style":      Style = val; break;
                        case "Size":       Size = PointSerializer.FromString(val); break;
                        case "MeshOffset": MeshOffset = Vector2Serializer.FromString(val); break;
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
                        if (col != "___")
                        {
                            if (col == "IC_" || col == "IOC")
                            {
                                GridCenter = new Point(x, height);
                                GridOrigin = new Point(-GridCenter.X, -GridCenter.Y);
                                slots.Add(new HullSlot(x, height, col == "IC_" ? Restrictions.I : Restrictions.IO));
                            }
                            else if (Enum.TryParse(col.Trim(), out Restrictions r))
                            {
                                slots.Add(new HullSlot(x, height, r));
                            }
                        }
                    }
                    ++height;
                }
            }

            if (height != Size.Y)
                Log.Error($"Hull {file.NameNoExt()} design rows={height} does not match defined Size Height={Size.Y}");

            if (GridCenter == Point.Zero || GridOrigin == Point.Zero)
                Log.Error($"Hull {file.NameNoExt()} invalid GridCenter={GridCenter} GridOrigin={GridOrigin} is `IC` slot missing?");

            HullSlots = slots.ToArray();
            SurfaceArea = HullSlots.Length;

            InitializeCommon();
        }

        public ShipHull GetClone()
        {
            ShipHull hull = (ShipHull)MemberwiseClone();
            hull.HullSlots = HullSlots.CloneArray();
            hull.TechsNeeded = new HashSet<string>(TechsNeeded);
            return hull;
        }

        void InitializeCommon()
        {
            Bonuses = ResourceManager.HullBonuses.TryGetValue(HullName, out HullBonus bonus) ? bonus : HullBonus.Default;
        }

        public void Save(string filePath)
        {
            Save(new FileInfo(filePath));
        }

        public void Save(FileInfo file)
        {
            var sw = new ShipDataWriter();
            sw.Write("Version", Version);
            sw.Write("HullName", HullName);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", Style);
            sw.Write("Description", Description);
            sw.Write("Size", Size.X+","+Size.Y);
            sw.Write("MeshOffset", MeshOffset.X+","+MeshOffset.Y);
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
                        string r;
                        if (slot.Pos == GridCenter) // GridCenter must always be IO or I
                            r = (slot.R == Restrictions.IO ? "IOC" : "IC_");
                        else
                            r = slot.R.ToString();

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
            Log.Info($"Saved '{HullName}' to {file.FullName}");
        }
    }
}
