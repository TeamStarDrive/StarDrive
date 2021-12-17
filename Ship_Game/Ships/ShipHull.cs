using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Gameplay;
using Ship_Game.Ships.Legacy;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This describes an unique StarShip Hull. Ship Designs are built upon
    /// the slots described by this Hull definition
    ///
    /// It also describes visual aspects of the StarShip, such as the Mesh and its visual offset
    ///
    /// This class is Serialized/Deserialized using a custom text-based format
    /// </summary>
    public class ShipHull
    {
        // Current version of ShipData files
        // If we introduce incompatibilities we need to convert old to new
        public const int Version = 1;
        
        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! ShipHull must not be automatically serialized! Add [XmlIgnore][JsonIgnore] to `public ShipHull XXX;` PROPERTIES/FIELDS. {this}");

        public string HullName; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string VisibleName; // Visible name of the Hull in the UI, ex: Misc/HaulerSmall -> "Small Freighter"
        public string ModName; // null if vanilla, else mod name eg "Combined Arms"
        public string Style; // "Terran"
        public string Description; // "With the advent of more powerful StarDrives, this giant cruiser hull was ..."
        public Point Size;
        public int SurfaceArea;
        public Vector2 MeshOffset; // offset of the mesh from Mesh object center, for grid to match model
        public string IconPath; // "ShipIcons/shuttle"
        public string ModelPath; // "Model/Ships/Terran/Shuttle/ship08"

        public RoleName Role = RoleName.fighter;
        public string SelectIcon = "";
        public bool Animated;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        public ThrusterZone[] Thrusters = Empty<ThrusterZone>.Array;
        public HullSlot[] HullSlots;

        // offset from grid TopLeft to the Center slot
        public Point GridCenter;

        public bool Unlockable = true;
        public HashSet<string> TechsNeeded = new HashSet<string>();

        public FileInfo Source;
        public SubTexture Icon => ResourceManager.Texture(IconPath);
        public Vector3 Volume { get; private set; }
        public float ModelZ { get; private set; }
        public HullBonus Bonuses { get; private set; }
        public bool IsValidForCurrentMod => ModName.IsEmpty() || ModName == GlobalStats.ModName;

        public struct ThrusterZone
        {
            public Vector3 Position;
            public float Scale;

            public Vector2 WorldPos2D =>
                Thruster.GetPosition(Vector2.Zero, 0, Vector3.Down, Position).ToVec2();

            public void SetWorldPos2D(Vector2 worldPos)
            {
                var pos = Thruster.FromWorldPos(worldPos);
                Position.X = (float)Math.Round(pos.X);
                Position.Y = (float)Math.Round(pos.Y);
            }

            public float WorldRadius => Scale / 8f;
        }

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
            VisibleName = sd.Name;
            ModName = sd.ModName ?? "";
            Style = sd.ShipStyle;
            Size = sd.GridInfo.Size;
            SurfaceArea = sd.GridInfo.SurfaceArea;
            MeshOffset  = sd.GridInfo.MeshOffset;
            IconPath = sd.IconPath;
            ModelPath = sd.ModelPath;

            Role = (RoleName)(int)sd.Role;
            SelectIcon = sd.SelectionGraphic;
            Animated = sd.Animated;

            Thrusters = new ThrusterZone[sd.ThrusterList.Length];
            for (int i = 0; i < sd.ThrusterList.Length; ++i)
            {
                Thrusters[i].Position = sd.ThrusterList[i].Position;
                Thrusters[i].Scale = sd.ThrusterList[i].Scale;
            }

            Vector2 origin = sd.GridInfo.VirtualOrigin;
            var legacyOffset = new Vector2(LegacyShipGridInfo.ModuleSlotOffset);

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

            GridCenter = sd.GridInfo.GridCenter;
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
                        case "VisibleName": VisibleName = val; break;
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
                            ref ThrusterZone tz = ref Thrusters[Thrusters.Length - 1];
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
                            if (col.IndexOf('C') != -1) // grid center marker
                            {
                                GridCenter = new Point(x, height);
                                if (col != "C__")
                                {
                                    var r = Restrictions.I;
                                    if      (col == "IOC") r = Restrictions.IO;
                                    else if (col == "OC ") r = Restrictions.O;
                                    else if (col == "EC ") r = Restrictions.E;
                                    else if (col == "IEC") r = Restrictions.IE;
                                    else if (col == "OEC") r = Restrictions.OE;
                                    slots.Add(new HullSlot(x, height, r));
                                }
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

            if (GridCenter == Point.Zero)
                Log.Error($"Hull {file.NameNoExt()} invalid GridCenter={GridCenter}, is `IC` slot missing?");

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

        // Sets hull slots of this design and recalculates grid size
        public void SetHullSlots(Array<HullSlot> slots)
        {
            HullSlots = slots.ToArray();
            var info = new ShipGridInfo(HullSlots);
            Size = info.Size;
            SurfaceArea = info.SurfaceArea;
            Array.Sort(HullSlots, HullSlot.Sorter);
        }

        public void Save(string filePath)
        {
            Save(new FileInfo(filePath));
        }

        public void Save(FileInfo file)
        {
            var sw = new ShipDesignWriter();
            sw.Write("Version", Version);
            sw.Write("HullName", HullName);
            sw.Write("VisibleName", VisibleName);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", Style);
            sw.Write("Description", Description);
            sw.Write("Size", Size.X+","+Size.Y);
            // GridCenter is saved as IOC / IC slot
            sw.Write("MeshOffset", MeshOffset.X+","+MeshOffset.Y);
            sw.Write("IconPath", IconPath);
            sw.Write("ModelPath", ModelPath);
            sw.Write("SelectIcon", SelectIcon);

            sw.Write("Animated", Animated);
            sw.Write("IsShipyard", IsShipyard);
            sw.Write("IsOrbitalDefense", IsOrbitalDefense);

            sw.WriteLine("#Thruster PosX,PosY,PosZ,Scale");
            foreach (ThrusterZone t in Thrusters)
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
                        if (GridCenter.X == x && GridCenter.Y == y)
                            sb.Append("C__"); // GridCenter in an empty slot (hole in center of ship)
                        else
                            sb.Append("___");
                    }
                    else
                    {
                        string r;
                        if (slot.Pos == GridCenter) // GridCenter ontop of an existing slot
                        {
                            if      (slot.R == Restrictions.IO) r = "IOC";
                            else if (slot.R == Restrictions.O)  r = "OC ";
                            else if (slot.R == Restrictions.E)  r = "EC ";
                            else if (slot.R == Restrictions.IE) r = "IEC";
                            else if (slot.R == Restrictions.OE) r = "OEC";
                            else                                r = "IC ";
                        }
                        else
                        {
                            r = slot.R.ToString();
                        }

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
