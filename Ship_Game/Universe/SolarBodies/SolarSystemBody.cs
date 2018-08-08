using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public enum SunZone
    {
        Near,
        Habital,
        Far,
        VeryFar,
        Any
    }

    public enum PlanetType
    {
        Other,
        Barren,
        Terran,
    }
    public enum Richness
    {
        UltraPoor,
        Poor,
        Average,
        Rich,
        UltraRich,
    }
    public class OrbitalDrop
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public PlanetGridSquare Target;
    }


    public class SolarSystemBody : Explorable
    {
        public SBProduction SbProduction;
        public BatchRemovalCollection<Combat> ActiveCombats = new BatchRemovalCollection<Combat>();
        public BatchRemovalCollection<OrbitalDrop> OrbitalDropList = new BatchRemovalCollection<OrbitalDrop>();
        public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
        //public BatchRemovalCollection<QueueItem> ConstructionQueue = new BatchRemovalCollection<QueueItem>();
        public BatchRemovalCollection<Ship> BasedShips = new BatchRemovalCollection<Ship>();
        public BatchRemovalCollection<Projectile> Projectiles = new BatchRemovalCollection<Projectile>();
        //protected readonly Map<string, float> ResourcesDict = new Map<string, float>(StringComparer.OrdinalIgnoreCase);
        //protected IReadOnlyDictionary<string, float> ResourceDictionary => new Map<string, float>(StringComparer.OrdinalIgnoreCase);
        protected readonly Array<Building> BuildingsCanBuild = new Array<Building>();
        public BatchRemovalCollection<QueueItem> ConstructionQueue => SbProduction.ConstructionQueue;
        public Array<string> Guardians = new Array<string>();
        public Array<string> PlanetFleets = new Array<string>();
        public Map<Guid, Ship> Shipyards = new Map<Guid, Ship>();
        public Matrix RingWorld;
        public SceneObject SO;
        // ReSharper disable once InconsistentNaming some conflict issues makhing this GUID and possible save and load issues changing this. 
        public Guid guid = Guid.NewGuid();
        protected AudioEmitter Emit = new AudioEmitter();
        public Vector2 Center;
        public SolarSystem ParentSystem;
        public Matrix CloudMatrix;
        public bool HasEarthLikeClouds;
        public string SpecialDescription;
        public bool HasShipyard;        //This is terrably named. This should be 'HasStarPort'
        public string Name;
        public string Description;
        public Empire Owner;
        public float OrbitalAngle;
        public float OrbitalRadius;
        public int PlanetType;
        public bool HasRings;
        public float PlanetTilt;
        public float RingTilt;
        public float Scale;
        public Matrix World;
        public bool Habitable;
        public string PlanetComposition;
        public string Type;
        protected float Zrotate;
        public int DevelopmentLevel;
        public bool UniqueHab = false;
        public int UniqueHabPercent;
        public SunZone Zone { get; private set; }
        protected AudioEmitter Emitter;
        protected float InvisibleRadius;
        public float GravityWellRadius { get; protected set; }
        public Array<PlanetGridSquare> TilesList = new Array<PlanetGridSquare>(35);
        protected float HabitalTileChance = 10;        
        public float Density;
        public float Fertility;
        public float MineralRichness;
        public float MaxPopulation;
        public Array<Building> BuildingList = new Array<Building>();
        public float ShieldStrengthCurrent;
        public float ShieldStrengthMax;        
        private float PosUpdateTimer = 1f;
        private float ZrotateAmount = 0.03f;
        public string DevelopmentStatus = "Undeveloped";
        public float TerraformPoints;
        public float TerraformToAdd;
        public Planet.ColonyType colonyType;        
        public void PlayPlanetSfx(string sfx, Vector3 position)
        {
            if (Emitter == null)
                Emitter = new AudioEmitter();
            Emitter.Position = position;
            GameAudio.PlaySfxAsync(sfx, Emitter);
        }
        public bool GovernorOn = true;  //This can be removed...It is set all over the place, but never checked. -Gretman
        public float ObjectRadius
        {
            get => SO != null ? SO.WorldBoundingSphere.Radius : InvisibleRadius;
            set => InvisibleRadius = SO != null ? SO.WorldBoundingSphere.Radius : value;
        }
        public int TurnsSinceTurnover { get; protected set; }
        public Shield Shield { get; protected set;}

        public Array<Building> GetBuildingsCanBuild () { return BuildingsCanBuild; }

        public string GetTypeTranslation()
        {
            switch (Type)
            {
                case "Terran": return Localizer.Token(1447);
                case "Barren": return Localizer.Token(1448);
                case "Gas Giant": return Localizer.Token(1449);
                case "Volcanic": return Localizer.Token(1450);
                case "Tundra": return Localizer.Token(1451);
                case "Desert": return Localizer.Token(1452);
                case "Steppe": return Localizer.Token(1453);
                case "Swamp": return Localizer.Token(1454);
                case "Ice": return Localizer.Token(1455);
                case "Oceanic": return Localizer.Token(1456);
                default: return "";
            }
        }
        protected void GenerateType(SunZone sunZone)
        {
            for (int x = 0; x < 5; x++)
            {
                Type = "";
                PlanetComposition = "";
                HasEarthLikeClouds = false;
                Habitable = false;
                MaxPopulation = 0;
                Fertility = 0;
                PlanetType = RandomMath.IntBetween(1, 24);
                TilesList.Clear();
                ApplyPlanetType();
                if (Zone == sunZone || (Zone == SunZone.Any && sunZone == SunZone.Near)) break;
                if (x > 2 && Zone == SunZone.Any) break;
            }
        }

        protected void ApplyPlanetType()
        {
            HabitalTileChance = 20;
            switch (PlanetType)
            {
                case 1:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.8f, 1.5f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 3:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 4:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 5:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 6:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 8:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 9:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1705);
                    Zone = SunZone.Any;
                    break;
                case 10:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1706);
                    Zone = SunZone.Far;
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1707);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.AvgRandomBetween(0.5f, 1f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    Zone = SunZone.Far;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Type = "Gas Giant";
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    Zone = SunZone.Far;
                    break;
                case 13:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.AvgRandomBetween(1f, 3f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1710);
                    HabitalTileChance = RandomMath.AvgRandomBetween(10f, 45f);
                    MaxPopulation = (int)HabitalTileChance * 100;
                    Fertility = RandomMath.AvgRandomBetween(-2f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    Habitable = true;
                    Zone = SunZone.Near;
                    break;
                case 15:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    Zone = SunZone.Far;
                    break;
                case 16:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 17:
                    Type = "Ice";
                    PlanetComposition = Localizer.Token(1713);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.VeryFar;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1714);
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = (int)HabitalTileChance * 200;
                    Fertility = RandomMath.AvgRandomBetween(0f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    Habitable = true;
                    Zone = SunZone.Habital;
                    break;
                case 19:
                    Type = "Swamp";
                    Habitable = true;
                    PlanetComposition = Localizer.Token(1715);
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = HabitalTileChance * 200;
                    Fertility = RandomMath.AvgRandomBetween(-2f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    HasEarthLikeClouds = true;
                    Zone = SunZone.Near;
                    break;
                case 20:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = HabitalTileChance * 100 + 1500;
                    Fertility = RandomMath.AvgRandomBetween(-3f, 5f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(0f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 23:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1718);
                    Zone = SunZone.Near;
                    break;
                case 24:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 25:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-.50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 26:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 27:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 29:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(50f, 80f);
                    MaxPopulation = HabitalTileChance * 150f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
            }
        }
        public void SetPlanetAttributes(bool setType = true)
        {
            HasEarthLikeClouds = false;
            float richness = RandomMath.RandomBetween(0.0f, 100f);
            if (richness >= 92.5f) MineralRichness = RandomMath.RandomBetween(2.00f, 2.50f);
            else if (richness >= 85.0f) MineralRichness = RandomMath.RandomBetween(1.50f, 2.00f);
            else if (richness >= 25.0f) MineralRichness = RandomMath.RandomBetween(0.75f, 1.50f);
            else if (richness >= 12.5f) MineralRichness = RandomMath.RandomBetween(0.25f, 0.75f);
            else if (richness < 12.5f) MineralRichness = RandomMath.RandomBetween(0.10f, 0.25f);

            if (setType) ApplyPlanetType();
            if (!Habitable)
                MineralRichness = 0.0f;


            AddEventsAndCommodities();
        }

        protected void AddEventsAndCommodities()
        {
            switch (Type)
            {
                case "Terran":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.TerranHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.TerranChance, item.TerranInstanceMax);
                    break;
                case "Steppe":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.SteppeHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.SteppeChance, item.SteppeInstanceMax);
                    break;
                case "Ice":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.IceHab ?? 15);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.IceChance, item.IceInstanceMax);
                    break;
                case "Barren":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.BarrenHab ?? 0);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.BarrenChance, item.BarrenInstanceMax);
                    break;
                case "Tundra":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.TundraChance, item.TundraInstanceMax);
                    break;
                case "Desert":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.DesertChance, item.DesertInstanceMax);
                    break;
                case "Oceanic":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.OceanicChance, item.OceanicInstanceMax);
                    break;
                case "Swamp":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.SteppeHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.SwampChance, item.SwampInstanceMax);
                    break;
            }
            AddTileEvents();
        }

        protected void SetTileHabitability(float habChance)
        {
            {
                if (UniqueHab)
                {
                    habChance = UniqueHabPercent;
                }
                bool habitable = false;
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        if (habChance > 0)
                            habitable = RandomMath.RandomBetween(0, 100) < habChance;

                        TilesList.Add(new PlanetGridSquare(x, y, null, habitable));
                    }
                }
            }
        }

        protected void AddTileEvents()
        {
            if (RandomMath.RandomBetween(0.0f, 100f) <= 15 && Habitable)
            {
                Array<string> list = new Array<string>();
                foreach (var kv in ResourceManager.BuildingsDict)
                {
                    if (!string.IsNullOrEmpty(kv.Value.EventTriggerUID) && !kv.Value.NoRandomSpawn)
                        list.Add(kv.Key);
                }
                int index = (int)RandomMath.RandomBetween(0f, list.Count + 0.85f);
                if (index >= list.Count)
                    index = list.Count - 1;
                PlanetGridSquare b = ResourceManager.CreateBuilding(list[index]).AssignBuildingToRandomTile(this);
                BuildingList.Add(b.building);
                Log.Info($"Event building : {b.building.Name} : created on {Name}");
            }
        }
        public void SpawnRandomItem(RandomItem randItem, float chance, float instanceMax)
        {
            if ((GlobalStats.HardcoreRuleset || !randItem.HardCoreOnly) && RandomMath.RandomBetween(0.0f, 100f) < chance)
            {
                int itemCount = (int)RandomMath.RandomBetween(1f, instanceMax + 0.95f);
                for (int i = 0; i < itemCount; ++i)
                {
                    if (!ResourceManager.BuildingsDict.ContainsKey(randItem.BuildingID)) continue;
                    var pgs = ResourceManager.CreateBuilding(randItem.BuildingID).AssignBuildingToRandomTile(this);
                    pgs.Habitable = true;
                    Log.Info($"Resouce Created : '{pgs.building.Name}' : on '{Name}' ");
                    BuildingList.Add(pgs.building);
                }
            }
        }

        public void SetPlanetAttributes(float mrich)
        {
            float num1 = mrich;
            if (num1 >= 87.5f)
            {
                //this.richness = Planet.Richness.UltraRich;
                MineralRichness = 2.5f;
            }
            else if (num1 >= 75f)
            {
                //this.richness = Planet.Richness.Rich;
                MineralRichness = 1.5f;
            }
            else if (num1 >= 25.0)
            {
                //this.richness = Planet.Richness.Average;
                MineralRichness = 1f;
            }
            else if (num1 >= 12.5)
            {
                MineralRichness = 0.5f;
                //this.richness = Planet.Richness.Poor;
            }
            else if (num1 < 12.5)
            {
                MineralRichness = 0.1f;
                //this.richness = Planet.Richness.UltraPoor;
            }

            TilesList.Clear();
            switch (PlanetType)
            {
                case 1:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 2f);
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 4:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 5:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 6:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 8:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 9:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1707);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 0.9f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Type = "Gas Giant";
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(0.8f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1710);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(0.2f, 1.8f);
                    Habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 15:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    break;
                case 16:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 17:
                    Type = "Ice";
                    PlanetComposition = Localizer.Token(1713);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    HabitalTileChance = 10;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1714);
                    Fertility = RandomMath.RandomBetween(0.4f, 1.4f);
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(2000f, 4000f);
                    Habitable = true;
                    HabitalTileChance = 50;
                    break;
                case 19:
                    Type = "Swamp";
                    Habitable = true;
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(1f, 5f);
                    HasEarthLikeClouds = true;
                    HabitalTileChance = 20;
                    break;
                case 20:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(3000f, 6000f);
                    Fertility = RandomMath.RandomBetween(2f, 5f);
                    HabitalTileChance = 20;
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 23:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 25:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 2f);
                    HabitalTileChance = 90;
                    break;
                case 26:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 60;
                    break;
                case 29:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 50;
                    break;
            }

            if (!Habitable)
                MineralRichness = 0.0f;
            else
            {
                if (Fertility <= 0 && Owner.Capital == null)
                    return;

                float chance = Owner.Capital == null ? HabitalTileChance : 75; // homeworlds always get 75% habitable chance per tile
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        bool habitableTile =  (int)RandomMath.RandomBetween(0.0f, 100f) < chance;
                        TilesList.Add(new PlanetGridSquare(x, y, null, habitableTile));
                    }
                }
            }


        }
        public void LoadAttributes()
        {
            switch (PlanetType)
            {
                case 1:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 2:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    break;
                case 4:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    break;
                case 5:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    break;
                case 6:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    Habitable = true;
                    break;
                case 8:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    break;
                case 9:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1707);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 12:
                    Type = "Gas Giant";
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1710);
                    Habitable = true;
                    break;
                case 15:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    break;
                case 16:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1712);
                    Habitable = true;
                    break;
                case 17:
                    Type = "Ice";
                    PlanetComposition = Localizer.Token(1713);
                    Habitable = true;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1714);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 19:
                    Type = "Swamp";
                    PlanetComposition = "";
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 20:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 23:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    break;
                case 25:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 26:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 29:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
            }
        }
        public string GetRichness()
        {
            if (MineralRichness > 2.5)
                return Localizer.Token(1442);
            if (MineralRichness > 1.5)
                return Localizer.Token(1443);
            if (MineralRichness > 0.75)
                return Localizer.Token(1444);
            if (MineralRichness > 0.25)
                return Localizer.Token(1445);
            else
                return Localizer.Token(1446);
        }

        public string GetOwnerName()
        {
            if (Owner != null)
                return Owner.data.Traits.Singular;
            return Habitable ? " None" : " Uninhabitable";
        }

        public string GetTile()
        {
            if (Type != "Terran")
                return Type;
            switch (PlanetType)
            {
                case 1: return "Terran";
                case 13: return "Terran_2";
                case 22: return "Terran_3";
                default: return "Terran";
            }
        }

        public void InitializePlanetMesh(GameScreen screen)
        {
            Shield = ShieldManager.AddPlanetaryShield(Center);
            UpdateDescription();
            CreatePlanetSceneObject(screen);

            GravityWellRadius = (float)(GlobalStats.GravityWellRange * (1 + ((Math.Log(Scale)) / 1.5)));
        }

        protected void UpdatePosition(float elapsedTime)
        {
            
        
            PosUpdateTimer -= elapsedTime;
            if (!Empire.Universe.Paused && (PosUpdateTimer <= 0.0f || ParentSystem.isVisible))
            {
                PosUpdateTimer = 5f;
                OrbitalAngle += (float) Math.Asin(15.0 / OrbitalRadius);
                if (OrbitalAngle >= 360.0f)
                    OrbitalAngle -= 360f;
                Center = ParentSystem.Position.PointOnCircle(OrbitalAngle, OrbitalRadius);
            }

            if (ParentSystem.isVisible)
            {
                Zrotate += ZrotateAmount * elapsedTime;
                SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(Scale) *
                           Matrix.CreateRotationZ(-Zrotate) * Matrix.CreateRotationX(-45f.ToRadians()) *
                           Matrix.CreateTranslation(new Vector3(Center, 2500f));
                CloudMatrix = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(Scale) *
                              Matrix.CreateRotationZ((float) (-Zrotate / 1.5)) *
                              Matrix.CreateRotationX(-45f.ToRadians()) *
                              Matrix.CreateTranslation(new Vector3(Center, 2500f));
                RingWorld = Matrix.Identity * Matrix.CreateRotationX(RingTilt.ToRadians()) *
                            Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                SO.Visibility = ObjectVisibility.Rendered;
            }
            else
                SO.Visibility = ObjectVisibility.None;
        }

        protected void CreatePlanetSceneObject(GameScreen screen)
        {
            if (SO != null)
                screen?.RemoveObject(SO);
            var contentManager =  ResourceManager.RootContent;
            SO = ResourceManager.GetPlanetarySceneMesh(contentManager, "Model/SpaceObjects/planet_" + PlanetType);
            SO.World = Matrix.CreateScale(Scale * 3)
                       * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            RingWorld = Matrix.CreateRotationX(RingTilt.ToRadians())
                        * Matrix.CreateScale(5f)
                        * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            screen?.AddObject(SO);
        }
        protected void UpdateDescription()
        {
            if (SpecialDescription != null)
            {
                Description = SpecialDescription;
            }
            else
            {
                Description = "";
                var planet1 = this;
                string str1 = planet1.Description + Name + " " + PlanetComposition + ". ";
                planet1.Description = str1;
                if (Fertility > 2)
                {
                    if (PlanetType == 21)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1729);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1730);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1731);
                        planet2.Description = str2;
                    }
                }
                else if (Fertility > 1)
                {
                    if (PlanetType == 19)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1732);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 21)
                        Description += Localizer.Token(1733);
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1734);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1735);
                        planet2.Description = str2;
                    }
                }
                else if (Fertility > 0.6f)
                {
                    if (PlanetType == 14)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1736);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 21)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1737);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 17)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1738);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 19)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1739);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 18)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1740);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 11)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1741);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1742);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1743);
                        planet2.Description = str2;
                    }
                }
                else
                {
                    switch (PlanetType) {
                        case 9:
                        case 23:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1744);
                            planet2.Description = str2;
                            break;
                        }
                        case 20:
                        case 15:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1745);
                            planet2.Description = str2;
                            break;
                        }
                        case 17:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1746);
                            planet2.Description = str2;
                            break;
                        }
                        case 18:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1747);
                            planet2.Description = str2;
                            break;
                        }
                        case 11:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1748);
                            planet2.Description = str2;
                            break;
                        }
                        case 14:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1749);
                            planet2.Description = str2;
                            break;
                        }
                        case 2:
                        case 6:
                        case 10:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1750);
                            planet2.Description = str2;
                            break;
                        }
                        case 3:
                        case 4:
                        case 16:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1751);
                            planet2.Description = str2;
                            break;
                        }
                        case 1:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1752);
                            planet2.Description = str2;
                            break;
                        }
                        default:
                            if (Habitable)
                            {
                                var planet2 = this;
                                string str2 = planet2.Description ?? "";
                                planet2.Description = str2;
                            }
                            else
                            {
                                var planet2 = this;
                                string str2 = planet2.Description + Localizer.Token(1753);
                                planet2.Description = str2;
                            }
                            break;
                    }
                }
                if (Fertility < 0.6f && MineralRichness >= 2 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1754);
                    planet2.Description = str2;
                    if (MineralRichness > 3)
                    {
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1755);
                        planet3.Description = str3;
                    }
                    else if (MineralRichness >= 2)
                    {
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1756);
                        planet3.Description = str3;
                    }
                    else
                    {
                        if (MineralRichness < 1)
                            return;
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1757);
                        planet3.Description = str3;
                    }
                }
                else if (MineralRichness > 3 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1758);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 2 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1759);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 1 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1760);
                    planet2.Description = str2;
                }
                else
                {
                    if (MineralRichness >= 1 || !Habitable)
                        return;
                    if (PlanetType == 14)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1761);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1762);
                        planet2.Description = str2;
                    }
                }
            }
        }
        public void Terraform()
        {
            switch (PlanetType)
            {
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(0.0f, 500f);
                    HasEarthLikeClouds = false;
                    Habitable = true;
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1724);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1725);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Habitable = true;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1726);
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(2000f, 4000f);
                    Habitable = true;
                    break;
                case 19:
                    Type = "Swamp";
                    PlanetComposition = Localizer.Token(1727);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    HasEarthLikeClouds = true;
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1728);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(3000f, 6000f);
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(6000f, 10000f);
                    break;
            }
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                switch (Type)
                {
                    case "Barren":
                        if (!planetGridSquare.Biosphere)
                        {
                            planetGridSquare.Habitable = false;
                            continue;
                        }
                        else
                            continue;
                    case "Terran":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Swamp":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Ocean":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Desert":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Steppe":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Tundra":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    default:
                        continue;
                }
            }
            UpdateDescription();
            CreatePlanetSceneObject(Empire.Universe);
        }
        private static void TraitLess(ref float invaderValue, ref float ownerValue) => invaderValue = Math.Max(invaderValue, ownerValue);
        private static void TraitMore(ref float invaderValue, ref float ownerValue) => invaderValue = Math.Min(invaderValue, ownerValue);
        public void ChangeOwnerByInvasion(Empire newOwner)
        {
            if (newOwner.TryGetRelations(Owner, out Relationship rel))
            {
                if (rel.AtWar && rel.ActiveWar != null)
                    ++rel.ActiveWar.ColoniestWon;
            }
            else if (Owner.TryGetRelations(newOwner, out Relationship relship) && relship.AtWar && relship.ActiveWar != null)
                ++relship.ActiveWar.ColoniesLost;
            ConstructionQueue.Clear();
            foreach (PlanetGridSquare planetGridSquare in TilesList)
                planetGridSquare.QItem = null;
            Owner.RemovePlanet((Planet)this);
            if (newOwner == Empire.Universe.PlayerEmpire && Owner == EmpireManager.Cordrazine)
                GlobalStats.IncrementCordrazineCapture();

            if (this.IsExploredBy(Empire.Universe.PlayerEmpire))
            {
                if (!newOwner.isFaction)
                    Empire.Universe.NotificationManager.AddConqueredNotification((Planet)this, newOwner, Owner);
                else
                {
                    lock (GlobalStats.OwnedPlanetsLock)
                    {
                        Empire.Universe.NotificationManager.AddPlanetDiedNotification((Planet)this, Empire.Universe.PlayerEmpire);
                        bool local_7 = true;

                        if (Owner != null)
                        {
                            foreach (Planet item_3 in ParentSystem.PlanetList)
                            {
                                if (item_3.Owner == Owner && item_3 != (Planet)this)
                                    local_7 = false;
                            }
                            if (local_7)
                                ParentSystem.OwnerList.Remove(Owner);
                        }
                        Owner = null;
                    }
                    ConstructionQueue.Clear();
                    return;
                }
            }

            if (newOwner.data.Traits.Assimilators)
            {
                TraitLess(ref newOwner.data.Traits.DiplomacyMod, ref Owner.data.Traits.DiplomacyMod);
                TraitLess(ref newOwner.data.Traits.DodgeMod, ref Owner.data.Traits.DodgeMod);
                TraitLess(ref newOwner.data.Traits.EnergyDamageMod, ref Owner.data.Traits.EnergyDamageMod);
                TraitMore(ref newOwner.data.Traits.ConsumptionModifier, ref Owner.data.Traits.ConsumptionModifier);
                TraitLess(ref newOwner.data.Traits.GroundCombatModifier, ref Owner.data.Traits.GroundCombatModifier);
                TraitLess(ref newOwner.data.Traits.Mercantile, ref Owner.data.Traits.Mercantile);
                TraitLess(ref newOwner.data.Traits.PassengerModifier, ref Owner.data.Traits.PassengerModifier);
                TraitLess(ref newOwner.data.Traits.ProductionMod, ref Owner.data.Traits.ProductionMod);
                TraitLess(ref newOwner.data.Traits.RepairMod, ref Owner.data.Traits.RepairMod);
                TraitLess(ref newOwner.data.Traits.ResearchMod, ref Owner.data.Traits.ResearchMod);
                TraitLess(ref newOwner.data.Traits.ShipCostMod, ref Owner.data.Traits.ShipCostMod);
                TraitLess(ref newOwner.data.Traits.PopGrowthMin, ref Owner.data.Traits.PopGrowthMin);
                TraitMore(ref newOwner.data.Traits.PopGrowthMax, ref Owner.data.Traits.PopGrowthMax);
                TraitLess(ref newOwner.data.Traits.ModHpModifier, ref Owner.data.Traits.ModHpModifier);
                TraitLess(ref newOwner.data.Traits.TaxMod, ref Owner.data.Traits.TaxMod);
                TraitMore(ref newOwner.data.Traits.MaintMod, ref Owner.data.Traits.MaintMod);
                TraitLess(ref newOwner.data.SpyModifier, ref Owner.data.SpyModifier);
                TraitLess(ref newOwner.data.Traits.Spiritual, ref Owner.data.Traits.Spiritual);

            }
            if (newOwner.isFaction)
                return;

            foreach (var kv in Shipyards)
            {
                if (kv.Value.loyalty != newOwner && kv.Value.TroopList.Any(loyalty => loyalty.GetOwner() != newOwner))
                    continue;
                kv.Value.ChangeLoyalty(newOwner);             
                Log.Info($"Owner of platform tethered to {Name} changed from {Owner.PortraitName} to {newOwner.PortraitName}");
            }
            Owner = newOwner;
            TurnsSinceTurnover = 0;
            Owner.AddPlanet((Planet)this);
            ConstructionQueue.Clear();
            ParentSystem.OwnerList.Clear();

            foreach (Planet planet in ParentSystem.PlanetList)
            {
                if (planet.Owner != null && !ParentSystem.OwnerList.Contains(planet.Owner))
                    ParentSystem.OwnerList.Add(planet.Owner);
            }
            colonyType = Owner.AssessColonyNeeds((Planet)this);
            GovernorOn = true;
        }
        protected void GenerateMoons(Planet newOrbital)
        {
            int moonCount = (int)Math.Ceiling(ObjectRadius * .004f);
            moonCount = (int)Math.Round(RandomMath.AvgRandomBetween(-moonCount * .75f, moonCount));
            for (int j = 0; j < moonCount; j++)
            {
                float radius = newOrbital.ObjectRadius + 1500 + RandomMath.RandomBetween(1000f, 1500f) * (j + 1);
                Moon moon = new Moon
                {
                    orbitTarget = newOrbital.guid,
                    moonType = RandomMath.IntBetween(1, 29),
                    scale = 1,
                    OrbitRadius = radius,
                    OrbitalAngle = RandomMath.RandomBetween(0f, 360f),
                    Position = newOrbital.Center.GenerateRandomPointOnCircle(radius)
                };
                ParentSystem.MoonList.Add(moon);
            }
        }
        public void AddBasedShip(Ship ship)
        {
            BasedShips.Add(ship);
        }

    }
}