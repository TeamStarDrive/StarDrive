// Type: Ship_Game.Planet
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace Ship_Game
{
    public sealed class Planet: IDisposable
    {
        public bool GovBuildings = true;
        public bool GovSliders = true;
        public BatchRemovalCollection<Combat> ActiveCombats = new BatchRemovalCollection<Combat>();
        public Guid guid = Guid.NewGuid();
        public List<PlanetGridSquare> TilesList = new List<PlanetGridSquare>();
        public string Special = "None";
        public BatchRemovalCollection<Planet.OrbitalDrop> OrbitalDropList = new BatchRemovalCollection<Planet.OrbitalDrop>();
        public Planet.GoodState fs = Planet.GoodState.STORE;
        public Planet.GoodState ps = Planet.GoodState.STORE;
        public Dictionary<Empire, bool> ExploredDict = new Dictionary<Empire, bool>();
        public List<Building> BuildingList = new List<Building>();
        public SpaceStation Station = new SpaceStation();
        public ConcurrentDictionary<Guid, Ship> Shipyards = new ConcurrentDictionary<Guid, Ship>();
        public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
        public BatchRemovalCollection<QueueItem> ConstructionQueue = new BatchRemovalCollection<QueueItem>();
        private float ZrotateAmount = 0.03f;
        public BatchRemovalCollection<Ship> BasedShips = new BatchRemovalCollection<Ship>();
        public bool GovernorOn = true;
        private AudioEmitter emit = new AudioEmitter();
        private float DecisionTimer = 0.5f;
        public BatchRemovalCollection<Projectile> Projectiles = new BatchRemovalCollection<Projectile>();
        private List<Building> BuildingsCanBuild = new List<Building>();
        public float FarmerPercentage = 0.34f;
        public float WorkerPercentage = 0.33f;
        public float ResearcherPercentage = 0.33f;
        public List<string> CommoditiesPresent = new List<string>();
        private Dictionary<string, float> ResourcesDict = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private float PosUpdateTimer = 1f;
        public float MAX_STORAGE = 10f;
        public string DevelopmentStatus = "Undeveloped";
        public List<string> Guardians = new List<string>();
        public bool FoodLocked;
        public bool ProdLocked;
        public bool ResLocked;
        public bool RecentCombat;
        public int Crippled_Turns;
        public Planet.ColonyType colonyType;
        public float ShieldStrengthCurrent;
        public float ShieldStrengthMax;
        private int TurnsSinceTurnover;
        public bool isSelected;
        public Vector2 Position;
        public string SpecialDescription;
        public static UniverseScreen universeScreen;
        public bool HasShipyard;
        public SolarSystem system;
        public Matrix cloudMatrix;
        public SolarSystem ParentSystem;
        public bool hasEarthLikeClouds;
        public string Name;
        public string Description;
        public Empire Owner;
        public float Objectradius;
        public float OrbitalAngle;
        public float Population;
        public float Density;
        public float Fertility;
        public float MineralRichness;
        public float OrbitalRadius;
        public int planetType;
        public bool hasRings;
        public float planetTilt;
        public float ringTilt;
        public float scale;
        public Matrix World;
        public Matrix RingWorld;
        public SceneObject SO;
        public bool habitable;
        public string planetComposition;
        public float MaxPopulation;
        public string Type;
        private float Zrotate;
        public float BuildingRoomUsed;
        public int StorageAdded;
        public float CombatTimer;
        private int numInvadersLast;
        public float ProductionHere;
        public float NetFoodPerTurn;
        public float FoodPercentAdded;
        public float FlatFoodAdded;
        public float NetProductionPerTurn;
        public float GrossProductionPerTurn;
        public float PlusFlatProductionPerTurn;
        public float NetResearchPerTurn;
        public float PlusTaxPercentage;
        public float PlusFlatResearchPerTurn;
        public float ResearchPercentAdded;
        public float PlusResearchPerColonist;
        public float TotalMaintenanceCostsPerTurn;
        public float PlusFlatMoneyPerTurn;
        private float PlusFoodPerColonist;
        public float PlusProductionPerColonist;
        public float MaxPopBonus;
        public bool AllowInfantry;
        public float TerraformPoints;
        public float TerraformToAdd;
        public float PlusFlatPopulationPerTurn;
        public int TotalDefensiveStrength;
        public float GrossFood;
        public float GrossMoneyPT;
        public float PlusCreditsPerColonist;
        public bool HasWinBuilding;
        public float ShipBuildingModifier;
        public float consumption;
        private float unfed;
        private Shield shield;
        public float FoodHere;
        public byte developmentLevel;
        public bool CorsairPresence;
        public bool queueEmptySent =true ;
        public float RepairPerTurn = 50;
        public List<string> PlanetFleets = new List<string>();
        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;
        private ReaderWriterLockSlim planetLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private bool PSexport = false;
        //private bool FSexport = false;
        public bool UniqueHab = false;
        public int uniqueHabPercent;
        public float ExportPSWeight =0;
        public float ExportFSWeight = 0;
       

        public float ObjectRadius
        {
            get
            {
                if (this.SO == null)
                    return this.Objectradius;                
                return this.SO.WorldBoundingSphere.Radius;
                ; }
            set { if (this.SO == null)
                    this.Objectradius =value;                
            else
                this.Objectradius = this.SO.WorldBoundingSphere.Radius;
            }
        }
        
        
        public Planet()
        {
            foreach (KeyValuePair<string, Good> keyValuePair in ResourceManager.GoodsDict)
                this.AddGood(keyValuePair.Key, 0);
            this.HasShipyard = false;
        }

        public void DropBombORIG(Bomb bomb)
        {
            
            if (bomb.Owner == this.Owner)
                return;
            if (this.Owner != null && !this.Owner.GetRelations(bomb.Owner).AtWar && (this.TurnsSinceTurnover > 10 && Empire.Universe.PlayerEmpire == bomb.Owner))
                this.Owner.GetGSAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
            if ((double)this.ShieldStrengthCurrent > 0.0)
            {
                AudioEmitter emitter = new AudioEmitter();
                emitter.Position = this.shield.Center;
                if (Planet.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    Cue cue = AudioManager.GetCue("sd_impact_shield_01");
                    cue.Apply3D(Planet.universeScreen.listener, emitter);
                    cue.Play();
                }
                this.shield.Rotation = Position.RadiansToTarget(new Vector2(bomb.Position.X, bomb.Position.Y));
                this.shield.displacement = 0.0f;
                this.shield.texscale = 2.8f;
                this.shield.Radius = this.SO.WorldBoundingSphere.Radius + 100f;
                this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                this.shield.texscale = 2.8f;
                this.shield.texscale = (float)(2.79999995231628 - 0.185000002384186 * (double)RandomMath.RandomBetween(1f, 10f));
                this.shield.Center = new Vector3(this.Position.X, this.Position.Y, 2500f);
                this.shield.pointLight.World = bomb.World;
                this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                this.shield.pointLight.Radius = 50f;
                this.shield.pointLight.Intensity = 8f;
                this.shield.pointLight.Enabled = true;
                Vector3 vector3 = Vector3.Normalize(bomb.Position - this.shield.Center);
                if (Planet.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    Planet.universeScreen.flash.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    for (int index = 0; index < 200; ++index)
                        Planet.universeScreen.sparks.AddParticleThreadB(bomb.Position, vector3 * new Vector3(RandomMath.RandomBetween(-25f, 25f), RandomMath.RandomBetween(-25f, 25f), RandomMath.RandomBetween(-25f, 25f)));
                }
                this.ShieldStrengthCurrent -= (float)ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Max;
                if ((double)this.ShieldStrengthCurrent >= 0.0)
                    return;
                this.ShieldStrengthCurrent = 0.0f;
            }
            else
            {
                float num1 = RandomMath.RandomBetween(0.0f, 100f);
                bool flag1 = true;
                if ((double)num1 < 75.0)
                    flag1 = false;
                this.Population -= 1000f * ResourceManager.WeaponsDict[bomb.WeaponName].BombPopulationKillPerHit;
                AudioEmitter emitter = new AudioEmitter();
                emitter.Position = bomb.Position;
                if (Planet.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView && this.system.isVisible)
                {
                    Cue cue = AudioManager.GetCue("sd_bomb_impact_01");
                    cue.Apply3D(Planet.universeScreen.listener, emitter);
                    cue.Play();
                    ExplosionManager.AddExplosionNoFlames(bomb.Position, 200f, 7.5f, 0.6f);
                    Planet.universeScreen.flash.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    for (int index = 0; index < 50; ++index)
                        Planet.universeScreen.explosionParticles.AddParticleThreadB(bomb.Position, Vector3.Zero);
                }
                Planet.OrbitalDrop orbitalDrop = new Planet.OrbitalDrop();
                List<PlanetGridSquare> list = new List<PlanetGridSquare>();
                if (flag1)
                {
                    foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                    {
                        if (planetGridSquare.building != null || planetGridSquare.TroopsHere.Count > 0)
                            list.Add(planetGridSquare);
                    }
                    if (list.Count > 0)
                    {
                        int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count + 1f);
                        if (index > list.Count - 1)
                            index = list.Count - 1;
                        orbitalDrop.Target = list[index];
                    }
                    else
                        flag1 = false;
                }
                if (!flag1)
                {
                    int num2 = (int)RandomMath.RandomBetween(0.0f, 5f);
                    int num3 = (int)RandomMath.RandomBetween(0.0f, 7f);
                    if (num2 > 4)
                        num2 = 4;
                    if (num3 > 6)
                        num3 = 6;
                    foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                    {
                        if (planetGridSquare.x == num3 && planetGridSquare.y == num2)
                        {
                            orbitalDrop.Target = planetGridSquare;
                            break;
                        }
                    }
                }
                if (orbitalDrop.Target.TroopsHere.Count > 0)
                {
                    orbitalDrop.Target.TroopsHere[0].Strength -= (int)RandomMath.RandomBetween((float)ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Min, (float)ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Max);
                    if (orbitalDrop.Target.TroopsHere[0].Strength <= 0)
                    {
                        this.TroopsHere.Remove(orbitalDrop.Target.TroopsHere[0]);
                        orbitalDrop.Target.TroopsHere.Clear();
                    }
                }
                else if (orbitalDrop.Target.building != null)
                {
                    orbitalDrop.Target.building.Strength -= (int)RandomMath.RandomBetween((float)ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMin, (float)ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMax);
                    if (orbitalDrop.Target.building.CombatStrength > 0)
                        orbitalDrop.Target.building.CombatStrength = orbitalDrop.Target.building.Strength;
                    if (orbitalDrop.Target.building.Strength <= 0)
                    {
                        this.BuildingList.Remove(orbitalDrop.Target.building);
                        orbitalDrop.Target.building = (Building)null;
                    }
                }
                if (Planet.universeScreen.workersPanel is CombatScreen && Planet.universeScreen.LookingAtPlanet && (Planet.universeScreen.workersPanel as CombatScreen).p == this)
                {
                    AudioManager.PlayCue("Explo1");
                    CombatScreen.SmallExplosion smallExplosion = new CombatScreen.SmallExplosion(4);
                    smallExplosion.grid = orbitalDrop.Target.ClickRect;
                    lock (GlobalStats.ExplosionLocker)
                        (Planet.universeScreen.workersPanel as CombatScreen).Explosions.Add(smallExplosion);
                }
                if ((double)this.Population <= 0.0)
                {
                    this.Population = 0.0f;
                    if (this.Owner != null)
                    {
                        this.Owner.RemovePlanet(this);
                        if (this.ExploredDict[Empire.Universe.PlayerEmpire])
                        {
                            Planet.universeScreen.NotificationManager.AddPlanetDiedNotification(this, Empire.Universe.PlayerEmpire);
                            bool flag2 = true;
                            if (this.Owner != null)
                            {
                                foreach (Planet planet in this.system.PlanetList)
                                {
                                    if (planet.Owner == this.Owner && planet != this)
                                        flag2 = false;
                                }
                                if (flag2)
                                    this.system.OwnerList.Remove(this.Owner);
                            }
                            this.ConstructionQueue.Clear();
                            this.Owner = (Empire)null;
                            return;
                        }
                    }
                }
                if (ResourceManager.WeaponsDict[bomb.WeaponName].HardCodedAction == null)
                    return;
                switch (ResourceManager.WeaponsDict[bomb.WeaponName].HardCodedAction)
                {
                    case "Free Owlwoks":
                        if (this.Owner == null || this.Owner != EmpireManager.GetEmpireByName("Cordrazine Collective"))
                            break;
                        for (int index = 0; index < this.TroopsHere.Count; ++index)
                        {
                            if (this.TroopsHere[index].GetOwner() == EmpireManager.GetEmpireByName("Cordrazine Collective") && this.TroopsHere[index].TargetType == "Soft")
                            {
#if STEAM
                                if (SteamManager.SetAchievement("Owlwoks_Freed"))
                                    SteamManager.SaveAllStatAndAchievementChanges();
#endif
                                this.TroopsHere[index].SetOwner(bomb.Owner);
                                this.TroopsHere[index].Name = Localizer.Token(EmpireManager.GetEmpireByName("Cordrazine Collective").data.TroopNameIndex);
                                this.TroopsHere[index].Description = Localizer.Token(EmpireManager.GetEmpireByName("Cordrazine Collective").data.TroopDescriptionIndex);
                            }
                        }
                        break;
                }
            }
        }
        //added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb)
        {
            if (bomb.Owner == this.Owner)
            {
                return;
            }
            if (this.Owner != null && !this.Owner.GetRelations(bomb.Owner).AtWar && this.TurnsSinceTurnover > 10 && Empire.Universe.PlayerEmpire == bomb.Owner)
            {
                this.Owner.GetGSAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
            }
            this.CombatTimer = 10f;
            if (this.ShieldStrengthCurrent <= 0f)
            {
                float ran = RandomMath.RandomBetween(0f, 100f);
                bool hit = true;
                if (ran < 75f)
                {
                    hit = false;
                }
                Planet population = this;
                population.Population = population.Population - 1000f * ResourceManager.WeaponsDict[bomb.WeaponName].BombPopulationKillPerHit;
                AudioEmitter e = new AudioEmitter();
                e.Position = bomb.Position;
                if (Planet.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView && this.system.isVisible)
                {
                    Cue Explode = AudioManager.GetCue("sd_bomb_impact_01");
                    Explode.Apply3D(Planet.universeScreen.listener, e);
                    Explode.Play();
                    ExplosionManager.AddExplosionNoFlames(bomb.Position, 200f, 7.5f, 0.6f);
                    Planet.universeScreen.flash.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    for (int i = 0; i < 50; i++)
                    {
                        Planet.universeScreen.explosionParticles.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    }
                }
                Planet.OrbitalDrop od = new Planet.OrbitalDrop();
                List<PlanetGridSquare> PotentialHits = new List<PlanetGridSquare>();
                if (hit)
                {
                    int buildingcount = 0;
                    foreach (PlanetGridSquare pgs in this.TilesList)
                    {
                        if (pgs.building == null && pgs.TroopsHere.Count <= 0)
                        {
                            continue;
                        }
                        PotentialHits.Add(pgs);
                        if(pgs.building!=null)
                        {
                            buildingcount++;
                        }
                    }
                    if ( PotentialHits.Count <= 0)
                    {
                        hit = false;
                        if (this.BuildingList.Count > 0)
                            this.BuildingList.Clear();
                    }
                    else
                    {
                        int ranhit = (int)RandomMath.RandomBetween(0f, (float)PotentialHits.Count + 1f);
                        if (ranhit > PotentialHits.Count - 1)
                        {
                            ranhit = PotentialHits.Count - 1;
                        }
                        od.Target = PotentialHits[ranhit];
                    }
                }
                if (!hit)
                {
                    int row = (int)RandomMath.RandomBetween(0f, 5f);
                    int column = (int)RandomMath.RandomBetween(0f, 7f);
                    if (row > 4)
                    {
                        row = 4;
                    }
                    if (column > 6)
                    {
                        column = 6;
                    }
                    foreach (PlanetGridSquare pgs in this.TilesList)
                    {
                        if (pgs.x != column || pgs.y != row)
                        {
                            continue;
                        }
                        od.Target = pgs;
                        break;
                    }
                }
                if (od.Target.TroopsHere.Count > 0)
                {
                    Troop item = od.Target.TroopsHere[0];
                    item.Strength = item.Strength - (int)RandomMath.RandomBetween((float)ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Min, (float)ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Max);
                    if (od.Target.TroopsHere[0].Strength <= 0)
                    {
                        this.TroopsHere.Remove(od.Target.TroopsHere[0]);
                        od.Target.TroopsHere.Clear();
                    }
                }
                else if (od.Target.building != null)
                {
                    Building target = od.Target.building;
                    target.Strength = target.Strength - (int)RandomMath.RandomBetween((float)ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMin, (float)ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMax);
                    if (od.Target.building.CombatStrength > 0)
                    {
                        od.Target.building.CombatStrength = od.Target.building.Strength;
                    }
                    if (od.Target.building.Strength <= 0)
                    {
                        this.BuildingList.Remove(od.Target.building);
                        od.Target.building = null;

                        bool flag = od.Target.Biosphere;
                        //Added Code here
                        od.Target.Habitable = false;
                        od.Target.highlighted = false;
                        od.Target.Biosphere = false;
                        this.TerraformPoints--; 
                        //Building Wasteland = new Building;
                        //Wasteland.Name="Fissionables";
                        //od.Target.building=Wasteland;
                        if (flag)
                        {
                            foreach (Building bios in this.BuildingList)
                            {
                                if (bios.Name == "Biospheres")
                                {
                                    od.Target.building = bios;
                                    break;
                                }
                            }
                            if (od.Target.building != null)
                            {
                                this.Population -= od.Target.building.MaxPopIncrease;
                                this.BuildingList.Remove(od.Target.building);
                                od.Target.building = null;
                            }
                        }

                   




                    }
                }
                if (Planet.universeScreen.workersPanel is CombatScreen && Planet.universeScreen.LookingAtPlanet && (Planet.universeScreen.workersPanel as CombatScreen).p == this)
                {
                    AudioManager.PlayCue("Explo1");
                    CombatScreen.SmallExplosion exp1 = new CombatScreen.SmallExplosion(4);
                    exp1.grid = od.Target.ClickRect;
                    lock (GlobalStats.ExplosionLocker)
                    {
                        (Planet.universeScreen.workersPanel as CombatScreen).Explosions.Add(exp1);
                    }
                }
                if (this.Population <= 0f)
                {
                    this.Population = 0f;
                    if (this.Owner != null)
                    {
                        this.Owner.RemovePlanet(this);
                        if (this.ExploredDict[Empire.Universe.PlayerEmpire])
                        {
                            Planet.universeScreen.NotificationManager.AddPlanetDiedNotification(this, Empire.Universe.PlayerEmpire);
                        }
                        bool removeowner = true;
                        if (this.Owner != null)
                        {
                            foreach (Planet other in this.system.PlanetList)
                            {
                                if (other.Owner != this.Owner || other == this)
                                {
                                    continue;
                                }
                                removeowner = false;
                            }
                            if (removeowner)
                            {
                                this.system.OwnerList.Remove(this.Owner);
                            }
                        }
                        this.ConstructionQueue.Clear();
                        this.Owner = null;
                        return;
                    }
                }
                if (ResourceManager.WeaponsDict[bomb.WeaponName].HardCodedAction != null)
                {
                    string hardCodedAction = ResourceManager.WeaponsDict[bomb.WeaponName].HardCodedAction;
                    string str = hardCodedAction;
                    if (hardCodedAction != null)
                    {
                        if (str != "Free Owlwoks")
                        {
                            return;
                        }
                        if (this.Owner != null && this.Owner == EmpireManager.GetEmpireByName("Cordrazine Collective"))
                        {
                            for (int i = 0; i < this.TroopsHere.Count; i++)
                            {
                                if (this.TroopsHere[i].GetOwner() == EmpireManager.GetEmpireByName("Cordrazine Collective") && this.TroopsHere[i].TargetType == "Soft")
                                {
                                    if (SteamManager.SetAchievement("Owlwoks_Freed"))
                                    {
                                        SteamManager.SaveAllStatAndAchievementChanges();
                                    }
                                    this.TroopsHere[i].SetOwner(bomb.Owner);
                                    this.TroopsHere[i].Name = Localizer.Token(EmpireManager.GetEmpireByName("Cordrazine Collective").data.TroopNameIndex);
                                    this.TroopsHere[i].Description = Localizer.Token(EmpireManager.GetEmpireByName("Cordrazine Collective").data.TroopDescriptionIndex);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                AudioEmitter emitter = new AudioEmitter();
                emitter.Position = this.shield.Center;
                if (Planet.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
                    shieldcue.Apply3D(Planet.universeScreen.listener, emitter);
                    shieldcue.Play();
                }
                this.shield.Rotation = Position.RadiansToTarget(new Vector2(bomb.Position.X, bomb.Position.Y));
                this.shield.displacement = 0f;
                this.shield.texscale = 2.8f;
                this.shield.Radius = this.SO.WorldBoundingSphere.Radius + 100f;
                this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                this.shield.texscale = 2.8f;
                this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                this.shield.Center = new Vector3(this.Position.X, this.Position.Y, 2500f);
                this.shield.pointLight.World = bomb.World;
                this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                this.shield.pointLight.Radius = 50f;
                this.shield.pointLight.Intensity = 8f;
                this.shield.pointLight.Enabled = true;
                Vector3 vel = Vector3.Normalize(bomb.Position - this.shield.Center);
                if (Planet.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    Planet.universeScreen.flash.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    for (int i = 0; i < 200; i++)
                    {
                        Planet.universeScreen.sparks.AddParticleThreadB(bomb.Position, vel * new Vector3(RandomMath.RandomBetween(-25f, 25f), RandomMath.RandomBetween(-25f, 25f), RandomMath.RandomBetween(-25f, 25f)));
                    }
                }
                Planet shieldStrengthCurrent = this;
                shieldStrengthCurrent.ShieldStrengthCurrent = shieldStrengthCurrent.ShieldStrengthCurrent - (float)ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Max;
                if (this.ShieldStrengthCurrent < 0f)
                {
                    this.ShieldStrengthCurrent = 0f;
                    return;
                }
            }
        }

        public float GetNetFoodPerTurn()
        {
            if (this.Owner != null && this.Owner.data.Traits.Cybernetic == 1)
                return this.NetFoodPerTurn;
            else
                return this.NetFoodPerTurn - this.consumption;
        }

        public float GetNetProductionPerTurn()
        {
            if (this.Owner != null && this.Owner.data.Traits.Cybernetic == 1)
                return this.NetProductionPerTurn - this.consumption;
            else
                return this.NetProductionPerTurn;
        }

        public string GetRichness()
        {
            if ((double)this.MineralRichness > 2.5)
                return Localizer.Token(1442);
            if ((double)this.MineralRichness > 1.5)
                return Localizer.Token(1443);
            if ((double)this.MineralRichness > 0.75)
                return Localizer.Token(1444);
            if ((double)this.MineralRichness > 0.25)
                return Localizer.Token(1445);
            else
                return Localizer.Token(1446);
        }

        public string GetOwnerName()
        {
            if (this.Owner != null)
                return this.Owner.data.Traits.Singular;
            return this.habitable ? " None" : " Uninhabitable";
        }

        public string GetTile()
        {
            if (Type != "Terran")
                return Type;
            switch (planetType)
            {
                case 1:  return "Terran";
                case 13: return "Terran_2";
                case 22: return "Terran_3";
                default: return "Terran";
            }
        }

        public string GetTypeTranslation()
        {
            switch (Type)
            {
                case "Terran":    return Localizer.Token(1447);
                case "Barren":    return Localizer.Token(1448);
                case "Gas Giant": return Localizer.Token(1449);
                case "Volcanic":  return Localizer.Token(1450);
                case "Tundra":    return Localizer.Token(1451);
                case "Desert":    return Localizer.Token(1452);
                case "Steppe":    return Localizer.Token(1453);
                case "Swamp":     return Localizer.Token(1454);
                case "Ice":       return Localizer.Token(1455);
                case "Oceanic":   return Localizer.Token(1456);
                default:          return "";
            }
        }

        public void SetPlanetAttributes()
        {
            hasEarthLikeClouds = false;
            float richness = RandomMath.RandomBetween(0.0f, 100f);
            if (richness >= 92.5f)      MineralRichness = RandomMath.RandomBetween(2.00f, 2.50f);
            else if (richness >= 85.0f) MineralRichness = RandomMath.RandomBetween(1.50f, 2.00f);
            else if (richness >= 25.0f) MineralRichness = RandomMath.RandomBetween(0.75f, 1.50f);
            else if (richness >= 12.5f) MineralRichness = RandomMath.RandomBetween(0.25f, 0.75f);
            else if (richness  < 12.5f) MineralRichness = RandomMath.RandomBetween(0.10f, 0.25f);

            switch (planetType)
            {
                case 1:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1700);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(4000f, 8000f);
                    this.Fertility = RandomMath.RandomBetween(0.1f, 0.2f);
                    break;
                case 2:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1702);
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    break;
                case 4:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1703);
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    break;
                case 5:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1704);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 6:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1704);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 8:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1703);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 9:
                    this.Type = "Volcanic";
                    this.planetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    this.Type = "Tundra";
                    this.planetComposition = Localizer.Token(1707);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(4000f, 8000f);
                    this.Fertility = RandomMath.RandomBetween(0.5f, 0.9f);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    break;
                case 12:
                    this.Type = "Gas Giant";
                    this.habitable = false;
                    this.planetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1709);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(0.8f, 3f);
                    break;
                case 14:
                    this.Type = "Desert";
                    this.planetComposition = Localizer.Token(1710);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(1000f, 3000f);
                    this.Fertility = RandomMath.RandomBetween(0.2f, 0.8f);
                    this.habitable = true;
                    double num2 = (double)RandomMath.RandomBetween(0.0f, 100f);
                    break;
                case 15:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    this.planetType = 26;
                    break;
                case 16:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1712);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 17:
                    this.Type = "Ice";
                    this.planetComposition = Localizer.Token(1713);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 18:
                    this.Type = "Steppe";
                    this.planetComposition = Localizer.Token(1714);
                    this.Fertility = RandomMath.RandomBetween(0.4f, 1.4f);
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(2000f, 4000f);
                    this.habitable = true;
                    break;
                case 19:
                    this.Type = "Swamp";
                    this.habitable = true;
                    this.planetComposition = Localizer.Token(1715);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(1000f, 3000f);
                    this.Fertility = RandomMath.RandomBetween(0.8f, 2f);
                    this.hasEarthLikeClouds = true;
                    break;
                case 20:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    this.Type = "Oceanic";
                    this.planetComposition = Localizer.Token(1716);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(3000f, 6000f);
                    this.Fertility = RandomMath.RandomBetween(2f, 5f);
                    break;
                case 22:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1717);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
                case 23:
                    this.Type = "Volcanic";
                    this.planetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1719);
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    break;
                case 25:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1720);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
                case 26:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1721);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
                case 29:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1722);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
            }
            if (!habitable)
                MineralRichness = 0.0f;

            if (UniqueHab)
            {
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        int num3 = (int)RandomMath.RandomBetween(0.0f, 100f);
                        TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, null, num3 < uniqueHabPercent));
                    }
                }
            }
            else
            {
                if (this.Type == "Barren")
                {
                    for (int x = 0; x < 7; ++x)
                    {
                        for (int y = 0; y < 5; ++y)
                        {
                            int num3 = (int)RandomMath.RandomBetween(0.0f, 100f);
                            if (GlobalStats.ActiveMod != null)
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 < GlobalStats.ActiveModInfo.BarrenHab));
                            }
                            else
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, false));
                            }
                        }
                    }
                }
                if (this.Type == "Ice")
                {
                    for (int x = 0; x < 7; ++x)
                    {
                        for (int y = 0; y < 5; ++y)
                        {
                            int num3 = (int)RandomMath.RandomBetween(0.0f, 100f);
                            if (GlobalStats.ActiveMod != null)
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 < GlobalStats.ActiveModInfo.IceHab));
                            }
                            else
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 < 15));
                            }
                        }
                    }
                }
                if (this.Type == "Terran")
                {
                    for (int x = 0; x < 7; ++x)
                    {
                        for (int y = 0; y < 5; ++y)
                        {
                            int num3 = (int)RandomMath.RandomBetween(0.0f, 100f);
                            if (GlobalStats.ActiveMod != null)
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 < GlobalStats.ActiveModInfo.TerranHab));
                            }
                            else
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 > 25));
                            }
                        }
                    }
                }
                if (this.Type == "Oceanic" || this.Type == "Desert" || this.Type == "Tundra")
                {
                    for (int x = 0; x < 7; ++x)
                    {
                        for (int y = 0; y < 5; ++y)
                        {
                            int num3 = (int)RandomMath.RandomBetween(0.0f, 100f);
                            if (GlobalStats.ActiveMod != null)
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 < GlobalStats.ActiveModInfo.OceanHab));
                            }
                            else
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 > 50));
                            }
                        }
                    }
                }
                if (this.Type == "Steppe" || this.Type == "Swamp")
                {
                    for (int x = 0; x < 7; ++x)
                    {
                        for (int y = 0; y < 5; ++y)
                        {
                            int num3 = (int)RandomMath.RandomBetween(0.0f, 100f);
                            if (GlobalStats.ActiveMod != null)
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 < GlobalStats.ActiveModInfo.SteppeHab));
                            }
                            else
                            {
                                this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 > 33));
                            }
                        }
                    }
                }
            }

            if ((double)RandomMath.RandomBetween(0.0f, 100f) <= 15.0 && this.habitable)
            {
                List<string> list = new List<string>();
                foreach (KeyValuePair<string, Building> keyValuePair in ResourceManager.BuildingsDict)
                {
                    if (!string.IsNullOrEmpty(keyValuePair.Value.EventTriggerUID) && !keyValuePair.Value.NoRandomSpawn)
                        list.Add(keyValuePair.Key);
                }
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count + 0.85f);
                if (index >= list.Count)
                    index = list.Count - 1;
                this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(list[index]));
            }
            switch (this.Type)
            {
                case "Terran":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.TerranChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.TerranInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if (ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
                case "Steppe":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.SteppeChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.SteppeInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if(ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
                case "Ice":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.IceChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.IceInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if (ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
                case "Barren":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.BarrenChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.BarrenInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if (ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
                case "Tundra":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.TundraChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.TundraInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if (ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
                case "Desert":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.DesertChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.DesertInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if (ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
                case "Oceanic":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.OceanicChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.OceanicInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if (ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
                case "Swamp":
                    using (List<RandomItem>.Enumerator enumerator = ResourceManager.RandomItemsList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            RandomItem current = enumerator.Current;
                            if ((GlobalStats.HardcoreRuleset || !current.HardCoreOnly) && (double)RandomMath.RandomBetween(0.0f, 100f) < (double)current.SwampChance)
                            {
                                int num3 = (int)RandomMath.RandomBetween(1f, current.SwampInstanceMax + 0.95f);
                                for (int index = 0; index < num3; ++index)
                                {
                                    if (ResourceManager.BuildingsDict.ContainsKey(current.BuildingID))
                                        this.AssignBuildingToRandomTile(ResourceManager.GetBuilding(current.BuildingID)).Habitable = true;
                                }
                            }
                        }
                        break;
                    }
            }
        }

        public void SetPlanetAttributes(float mrich)
        {
            float num1 = mrich;
            if ((double)num1 >= 87.5)
            {
                //this.richness = Planet.Richness.UltraRich;
                this.MineralRichness = 2.5f;
            }
            else if ((double)num1 >= 75.0)
            {
                //this.richness = Planet.Richness.Rich;
                this.MineralRichness = 1.5f;
            }
            else if ((double)num1 >= 25.0)
            {
                //this.richness = Planet.Richness.Average;
                this.MineralRichness = 1f;
            }
            else if ((double)num1 >= 12.5)
            {
                this.MineralRichness = 0.5f;
                //this.richness = Planet.Richness.Poor;
            }
            else if ((double)num1 < 12.5)
            {
                this.MineralRichness = 0.1f;
                //this.richness = Planet.Richness.UltraPoor;
            }
            switch (this.planetType)
            {
                case 1:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1700);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(4000f, 8000f);
                    this.Fertility = RandomMath.RandomBetween(0.1f, 0.2f);
                    break;
                case 2:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1702);
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    break;
                case 4:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1702);
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    break;
                case 5:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1703);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 6:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1704);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 8:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1703);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 9:
                    this.Type = "Volcanic";
                    this.planetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    this.Type = "Tundra";
                    this.planetComposition = Localizer.Token(1707);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(4000f, 8000f);
                    this.Fertility = RandomMath.RandomBetween(0.5f, 0.9f);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    break;
                case 12:
                    this.Type = "Gas Giant";
                    this.habitable = false;
                    this.planetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1709);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(0.8f, 3f);
                    break;
                case 14:
                    this.Type = "Desert";
                    this.planetComposition = Localizer.Token(1710);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(1000f, 3000f);
                    this.Fertility = RandomMath.RandomBetween(0.2f, 0.8f);
                    this.habitable = true;
                    break;
                case 15:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    this.planetType = 26;
                    break;
                case 16:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1712);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 17:
                    this.Type = "Ice";
                    this.planetComposition = Localizer.Token(1713);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    this.habitable = true;
                    break;
                case 18:
                    this.Type = "Steppe";
                    this.planetComposition = Localizer.Token(1714);
                    this.Fertility = RandomMath.RandomBetween(0.4f, 1.4f);
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(2000f, 4000f);
                    this.habitable = true;
                    break;
                case 19:
                    this.Type = "Swamp";
                    this.habitable = true;
                    this.planetComposition = Localizer.Token(1712);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(1000f, 3000f);
                    this.Fertility = RandomMath.RandomBetween(0.8f, 2f);
                    this.hasEarthLikeClouds = true;
                    break;
                case 20:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    this.Type = "Oceanic";
                    this.planetComposition = Localizer.Token(1716);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(3000f, 6000f);
                    this.Fertility = RandomMath.RandomBetween(2f, 5f);
                    break;
                case 22:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1717);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
                case 23:
                    this.Type = "Volcanic";
                    this.planetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1719);
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(100f, 500f);
                    this.Fertility = 0.0f;
                    break;
                case 25:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1720);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
                case 26:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1721);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
                case 29:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1722);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(12000f, 20000f);
                    this.Fertility = RandomMath.RandomBetween(1f, 3f);
                    break;
            }
       
            if (!this.habitable)
                this.MineralRichness = 0.0f;
            if (this.Type == "Barren")
            {
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        double num2 = (double)RandomMath.RandomBetween(0.0f, 100f);
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, false));
                    }
                }
            }
            if (this.Type == "Terran")
            {
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        int num2 = (int)RandomMath.RandomBetween(0.0f, 100f);
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num2 > 25));
                    }
                }
            }
            if (this.Type == "Oceanic" || this.Type == "Desert" || this.Type == "Tundra")
            {
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        int num2 = (int)RandomMath.RandomBetween(0.0f, 100f);
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num2 > 50));
                    }
                }
            }
            if (!(this.Type == "Steppe") && !(this.Type == "Swamp"))
                return;
            for (int x = 0; x < 7; ++x)
            {
                for (int y = 0; y < 5; ++y)
                {
                    int num2 = (int)RandomMath.RandomBetween(0.0f, 100f);
                    this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num2 > 33));
                }
            }
        }

        public void Terraform()
        {
            switch (this.planetType)
            {
                case 7:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1704);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(0.0f, 500f);
                    this.hasEarthLikeClouds = false;
                    this.habitable = true;
                    break;
                case 11:
                    this.Type = "Tundra";
                    this.planetComposition = Localizer.Token(1724);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(4000f, 8000f);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    break;
                case 14:
                    this.Type = "Desert";
                    this.planetComposition = Localizer.Token(1725);
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(1000f, 3000f);
                    this.habitable = true;
                    break;
                case 18:
                    this.Type = "Steppe";
                    this.planetComposition = Localizer.Token(1726);
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(2000f, 4000f);
                    this.habitable = true;
                    break;
                case 19:
                    this.Type = "Swamp";
                    this.planetComposition = Localizer.Token(1727);
                    this.habitable = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(1000f, 3000f);
                    this.hasEarthLikeClouds = true;
                    break;
                case 21:
                    this.Type = "Oceanic";
                    this.planetComposition = Localizer.Token(1728);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(3000f, 6000f);
                    break;
                case 22:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1717);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    this.MaxPopulation = (float)(int)RandomMath.RandomBetween(6000f, 10000f);
                    break;
            }
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                switch (this.Type)
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
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) > 25)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Swamp":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < 45)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Ocean":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < 35)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Desert":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < 25)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Steppe":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < 33)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Tundra":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < 50)
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
            this.UpdateDescription();
            lock (GlobalStats.ObjectManagerLocker)
                Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
            this.SO = new SceneObject(((ReadOnlyCollection<ModelMesh>)ResourceManager.GetModel("Model/SpaceObjects/planet_" + (object)this.planetType).Meshes)[0]);
            this.SO.ObjectType = ObjectType.Dynamic;
            this.SO.World = Matrix.Identity * Matrix.CreateScale(3f) 
                * Matrix.CreateScale(this.scale) 
                * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
            this.RingWorld = Matrix.Identity 
                * Matrix.CreateRotationX(ringTilt.ToRadians()) 
                * Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
            lock (GlobalStats.ObjectManagerLocker)
                Planet.universeScreen.ScreenManager.inter.ObjectManager.Submit((ISceneObject)this.SO);
        }

        public void LoadAttributes()
        {
            switch (this.planetType)
            {
                case 1:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1700);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    break;
                case 2:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1702);
                    this.habitable = true;
                    break;
                case 4:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1702);
                    this.habitable = true;
                    break;
                case 5:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1703);
                    this.habitable = true;
                    break;
                case 6:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1704);
                    this.habitable = true;
                    break;
                case 8:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1703);
                    this.habitable = true;
                    break;
                case 9:
                    this.Type = "Volcanic";
                    this.planetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    this.Type = "Tundra";
                    this.planetComposition = Localizer.Token(1707);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    break;
                case 12:
                    this.Type = "Gas Giant";
                    this.habitable = false;
                    this.planetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1709);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    break;
                case 14:
                    this.Type = "Desert";
                    this.planetComposition = Localizer.Token(1710);
                    this.habitable = true;
                    break;
                case 15:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    this.planetType = 26;
                    break;
                case 16:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1712);
                    this.habitable = true;
                    break;
                case 17:
                    this.Type = "Ice";
                    this.planetComposition = Localizer.Token(1713);
                    this.habitable = true;
                    break;
                case 18:
                    this.Type = "Steppe";
                    this.planetComposition = Localizer.Token(1714);
                    this.hasEarthLikeClouds = true;
                    this.habitable = true;
                    break;
                case 19:
                    this.Type = "Swamp";
                    this.planetComposition = "";
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    break;
                case 20:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    this.Type = "Oceanic";
                    this.planetComposition = Localizer.Token(1716);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    break;
                case 22:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1717);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    break;
                case 23:
                    this.Type = "Volcanic";
                    this.planetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    this.Type = "Barren";
                    this.planetComposition = Localizer.Token(1719);
                    this.habitable = true;
                    break;
                case 25:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1720);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    break;
                case 26:
                    this.Type = "Gas Giant";
                    this.planetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1721);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    break;
                case 29:
                    this.Type = "Terran";
                    this.planetComposition = Localizer.Token(1722);
                    this.habitable = true;
                    this.hasEarthLikeClouds = true;
                    break;
            }
        }

        public bool AssignTroopToNearestAvailableTile(Troop t, PlanetGridSquare tile)
        {
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops && (planetGridSquare.building == null || planetGridSquare.building != null && planetGridSquare.building.CombatStrength == 0) && (Math.Abs(tile.x - planetGridSquare.x) <= 1 && Math.Abs(tile.y - planetGridSquare.y) <= 1))
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in this.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.TroopsHere.Add(t);
                        this.TroopsHere.Add(t);
                        t.SetPlanet(this);
                        return true;

                    }
                }
            }
            return false;

        }
        

        public bool AssignTroopToTile(Troop t)
        {
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops && (planetGridSquare.building == null || planetGridSquare.building != null && planetGridSquare.building.CombatStrength == 0))
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count);
                PlanetGridSquare planetGridSquare = list[index];
                foreach (PlanetGridSquare eventLocation in this.TilesList)
                {
                    if (eventLocation == planetGridSquare)
                    {
                        eventLocation.TroopsHere.Add(t);
                        this.TroopsHere.Add(t);
                        t.SetPlanet(this);
                        if (eventLocation.building == null || string.IsNullOrEmpty(eventLocation.building.EventTriggerUID) || (eventLocation.TroopsHere.Count <= 0 || eventLocation.TroopsHere[0].GetOwner().isFaction))
                            return true;
                        ResourceManager.EventsDict[eventLocation.building.EventTriggerUID].TriggerPlanetEvent(this, eventLocation.TroopsHere[0].GetOwner(), eventLocation, Empire.Universe.PlayerEmpire, Planet.universeScreen);
                    }
                }
            }
            return false;
        }

        public bool AssignBuildingToTile(Building b)
        {
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (planetGridSquare.Habitable && planetGridSquare.building == null)
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count + 0.97f);
                if (index > list.Count - 1)
                    index = list.Count - 1;
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in this.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.building = b;
                        return true;
                    }
                }
            }
            else if (b.Name == "Outpost" || !string.IsNullOrEmpty(b.EventTriggerUID))
            {
                list.Clear();
                foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                {
                    if (planetGridSquare.building == null)
                        list.Add(planetGridSquare);
                }
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count + 0.97f);
                if (index > list.Count - 1)
                    index = list.Count - 1;
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in list)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.building = b;
                        planetGridSquare2.Habitable = true;
                        return true;
                    }
                }
            }
            else if (b.Name == "Biospheres")
            {
                list.Clear();
                foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                {
                    if (planetGridSquare.building == null)
                        list.Add(planetGridSquare);
                }
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count + 0.97f);
                if (index > list.Count - 1)
                    index = list.Count - 1;
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in list)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.building = b;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool AssignBuildingToTileOnColonize(Building b)
        {
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (planetGridSquare.Habitable && planetGridSquare.building == null)
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count + 0.97f);
                if (index > list.Count - 1)
                    index = list.Count - 1;
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in this.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.building = b;
                        return true;
                    }
                }
            }
            else
            {
                list.Clear();
                foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                {
                    if (planetGridSquare.building == null)
                        list.Add(planetGridSquare);
                }
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count + 0.97f);
                if (index > list.Count - 1)
                    index = list.Count - 1;
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in list)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.building = b;
                        planetGridSquare2.Habitable = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public PlanetGridSquare AssignBuildingToRandomTile(Building b)
        {
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (planetGridSquare.building == null)
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in this.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.building = b;
                        this.BuildingList.Add(b);
                        return planetGridSquare2;
                    }
                }
            }
            return (PlanetGridSquare)null;
        }

        public void AssignBuildingToSpecificTile(Building b, PlanetGridSquare pgs)
        {
            if (pgs.building != null)
                this.BuildingList.Remove(pgs.building);
            pgs.building = b;
            this.BuildingList.Add(b);
        }

        public bool TryBiosphereBuild(Building b, QueueItem qi)
        {
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (!planetGridSquare.Habitable && planetGridSquare.building == null && (!planetGridSquare.Biosphere && planetGridSquare.QItem == null))
                    list.Add(planetGridSquare);
            }
            if (b.Name == "Biospheres" && list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in this.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        qi.Building = b;
                        qi.isBuilding = true;
                        qi.Cost = b.Cost;
                        qi.productionTowards = 0.0f;
                        planetGridSquare2.QItem = qi;
                        qi.pgs = planetGridSquare2;
                        qi.NotifyOnEmpty = false;
                        this.ConstructionQueue.Add(qi);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool AssignBuildingToTile(Building b, QueueItem qi)
        {
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            if (b.Name == "Biospheres") 
                return false;
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                bool flag = true;
                foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                {
                    if (queueItem.pgs == planetGridSquare)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag && planetGridSquare.Habitable && planetGridSquare.building == null)
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, (float)list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in this.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.QItem = qi;
                        qi.pgs = planetGridSquare2;
                        return true;
                    }
                }
            }
            else if (b.CanBuildAnywhere)
            {
                PlanetGridSquare planetGridSquare1 = this.TilesList[(int)RandomMath.RandomBetween(0.0f, (float)this.TilesList.Count)];
                foreach (PlanetGridSquare planetGridSquare2 in this.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.QItem = qi;
                        qi.pgs = planetGridSquare2;
                        return true;
                    }
                }
            }
            return false;
        }

        public void AddBasedShip(Ship ship)
        {
            this.BasedShips.Add(ship);
        }

        private void DoViewedCombat(float elapsedTime)
        {
            this.ActiveCombats.thisLock.EnterReadLock();
            foreach (Combat combat in (List<Combat>)this.ActiveCombats)
            {
                if (combat.Attacker.TroopsHere.Count == 0 && combat.Attacker.building == null)
                {
                    this.ActiveCombats.QueuePendingRemoval(combat);
                    break;
                }
                else
                {
                    if (combat.Attacker.TroopsHere.Count > 0)
                    {
                        if (combat.Attacker.TroopsHere[0].Strength <= 0)
                        {
                            this.ActiveCombats.QueuePendingRemoval(combat);
                            break;
                        }
                    }
                    else if (combat.Attacker.building != null && combat.Attacker.building.Strength <= 0)
                    {
                        this.ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    if (combat.Defender.TroopsHere.Count == 0 && combat.Defender.building == null)
                    {
                        this.ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    else
                    {
                        if (combat.Defender.TroopsHere.Count > 0)
                        {
                            if (combat.Defender.TroopsHere[0].Strength <= 0)
                            {
                                this.ActiveCombats.QueuePendingRemoval(combat);
                                break;
                            }
                        }
                        else if (combat.Defender.building != null && combat.Defender.building.Strength <= 0)
                        {
                            this.ActiveCombats.QueuePendingRemoval(combat);
                            break;
                        }
                        float num1;
                        int num2;
                        int num3;
                        if (combat.Attacker.TroopsHere.Count > 0)
                        {
                            num1 = combat.Attacker.TroopsHere[0].Strength;
                            num2 = combat.Attacker.TroopsHere[0].GetHardAttack();
                            num3 = combat.Attacker.TroopsHere[0].GetSoftAttack();
                        }
                        else
                        {
                            num1 = combat.Attacker.building.Strength;
                            num2 = combat.Attacker.building.HardAttack;
                            num3 = combat.Attacker.building.SoftAttack;
                        }
                        string str = combat.Defender.TroopsHere.Count <= 0 ? "Hard" : combat.Defender.TroopsHere[0].TargetType;
                        combat.Timer -= elapsedTime;
                        int num4 = 0;
                        if ((double)combat.Timer < 3.0 && combat.phase == 1)
                        {
                            for (int index = 0; index < num1; ++index)
                            {
                                if ((double)RandomMath.RandomBetween(0.0f, 100f) < (str == "Soft" ? (double)num3 : (double)num2))
                                    ++num4;
                            }
                            if (num4 > 0 && (combat.Defender.TroopsHere.Count > 0 || combat.Defender.building != null && combat.Defender.building.Strength > 0))
                            {
                                AudioManager.PlayCue("sd_troop_attack_hit");
                                CombatScreen.SmallExplosion smallExplosion = new CombatScreen.SmallExplosion(1);
                                smallExplosion.grid = combat.Defender.TroopClickRect;
                                lock (GlobalStats.ExplosionLocker)
                                    (Planet.universeScreen.workersPanel as CombatScreen).Explosions.Add(smallExplosion);
                                if (combat.Defender.TroopsHere.Count > 0)
                                {
                                    combat.Defender.TroopsHere[0].Strength -= num4;
                                    if (combat.Defender.TroopsHere[0].Strength <= 0)
                                    {
                                        this.TroopsHere.Remove(combat.Defender.TroopsHere[0]);
                                        combat.Defender.TroopsHere.Clear();
                                        this.ActiveCombats.QueuePendingRemoval(combat);
                                        AudioManager.PlayCue("Explo1");
                                        lock (GlobalStats.ExplosionLocker)
                                            (Planet.universeScreen.workersPanel as CombatScreen).Explosions.Add(new CombatScreen.SmallExplosion(4)
                                            {
                                                grid = combat.Defender.TroopClickRect
                                            });
                                        if (combat.Attacker.TroopsHere.Count > 0)
                                        {
                                            combat.Attacker.TroopsHere[0].AddKill();
                                        }
                                    }
                                }
                                else
                                {
                                    combat.Defender.building.Strength -= num4;
                                    combat.Defender.building.CombatStrength -= num4;
                                    if (combat.Defender.building.Strength <= 0)
                                    {
                                        this.BuildingList.Remove(combat.Defender.building);
                                        combat.Defender.building = (Building)null;
                                    }
                                }
                            }
                            else if (num4 == 0)
                                AudioManager.PlayCue("sd_troop_attack_miss");
                            combat.phase = 2;
                        }
                        else if (combat.phase == 2)
                            this.ActiveCombats.QueuePendingRemoval(combat);
                    }
                }
            }
            this.ActiveCombats.thisLock.ExitReadLock();

        }

        private void DoCombatUnviewed(float elapsedTime)
        {
            this.ActiveCombats.thisLock.EnterReadLock();
            foreach (Combat combat in (List<Combat>)this.ActiveCombats)
            {
                if (combat.Attacker.TroopsHere.Count == 0 && combat.Attacker.building == null)
                {
                    this.ActiveCombats.QueuePendingRemoval(combat);
                    break;
                }
                else
                {
                    if (combat.Attacker.TroopsHere.Count > 0)
                    {
                        if (combat.Attacker.TroopsHere[0].Strength <= 0)
                        {
                            this.ActiveCombats.QueuePendingRemoval(combat);
                            break;
                        }
                    }
                    else if (combat.Attacker.building != null && combat.Attacker.building.Strength <= 0)
                    {
                        this.ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    if (combat.Defender.TroopsHere.Count == 0 && combat.Defender.building == null)
                    {
                        this.ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    else
                    {
                        if (combat.Defender.TroopsHere.Count > 0)
                        {
                            if (combat.Defender.TroopsHere[0].Strength <= 0)
                            {
                                this.ActiveCombats.QueuePendingRemoval(combat);
                                break;
                            }
                        }
                        else if (combat.Defender.building != null && combat.Defender.building.Strength <= 0)
                        {
                            this.ActiveCombats.QueuePendingRemoval(combat);
                            break;
                        }
                        float num1;
                        int num2;
                        int num3;
                        if (combat.Attacker.TroopsHere.Count > 0)
                        {
                            num1 = combat.Attacker.TroopsHere[0].Strength;
                            num2 = combat.Attacker.TroopsHere[0].GetHardAttack();
                            num3 = combat.Attacker.TroopsHere[0].GetSoftAttack();
                        }
                        else
                        {
                            num1 = combat.Attacker.building.Strength;
                            num2 = combat.Attacker.building.HardAttack;
                            num3 = combat.Attacker.building.SoftAttack;
                        }
                        string str = combat.Defender.TroopsHere.Count <= 0 ? "Hard" : combat.Defender.TroopsHere[0].TargetType;
                        combat.Timer -= elapsedTime;
                        int num4 = 0;
                        if ((double)combat.Timer < 3.0 && combat.phase == 1)
                        {
                            for (int index = 0; index < num1; ++index)
                            {
                                if ((double)RandomMath.RandomBetween(0.0f, 100f) < (str == "Soft" ? (double)num3 : (double)num2))
                                    ++num4;
                            }
                            if (num4 > 0 && (combat.Defender.TroopsHere.Count > 0 || combat.Defender.building != null && combat.Defender.building.Strength > 0))
                            {
                                if (combat.Defender.TroopsHere.Count > 0)
                                {
                                    combat.Defender.TroopsHere[0].Strength -= num4;
                                    if (combat.Defender.TroopsHere[0].Strength <= 0)
                                    {
                                        this.TroopsHere.Remove(combat.Defender.TroopsHere[0]);
                                        combat.Defender.TroopsHere.Clear();
                                        this.ActiveCombats.QueuePendingRemoval(combat);
                                        if (combat.Attacker.TroopsHere.Count > 0)
                                        {
                                            combat.Attacker.TroopsHere[0].AddKill();
                                        }
                                    }
                                }
                                else
                                {
                                    combat.Defender.building.Strength -= num4;
                                    combat.Defender.building.CombatStrength -= num4;
                                    if (combat.Defender.building.Strength <= 0)
                                    {
                                        this.BuildingList.Remove(combat.Defender.building);
                                        combat.Defender.building = (Building)null;
                                    }
                                }
                            }
                            combat.phase = 2;
                        }
                        else if (combat.phase == 2)
                            this.ActiveCombats.QueuePendingRemoval(combat);
                    }
                }
            }
            this.ActiveCombats.thisLock.ExitReadLock();
        }

        public void DoCombats(float elapsedTime)
        {
            if (Planet.universeScreen.LookingAtPlanet)
            {
                if (Planet.universeScreen.workersPanel is CombatScreen)
                {
                    if ((Planet.universeScreen.workersPanel as CombatScreen).p == this)
                        this.DoViewedCombat(elapsedTime);
                }
                else
                {
                    this.DoCombatUnviewed(elapsedTime);
                    this.ActiveCombats.ApplyPendingRemovals();
                }
            }
            else
            {
                this.DoCombatUnviewed(elapsedTime);
                this.ActiveCombats.ApplyPendingRemovals();
            }
            if (this.ActiveCombats.Count > 0)
                this.CombatTimer = 10f;
            if (this.TroopsHere.Count <= 0 || this.Owner == null)
                return;
            bool flag = false;
            int num1 = 0;
            int num2 = 0;
            Empire index = (Empire)null;
            
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                planetGridSquare.TroopsHere.thisLock.EnterReadLock();
                foreach (Troop troop in planetGridSquare.TroopsHere)
                {
                    if (troop.GetOwner() != null && troop.GetOwner() != this.Owner)
                    {
                        ++num2;
                        index = troop.GetOwner();
                        if (index.isFaction)
                            flag = true;
                    }
                    else
                        ++num1;
                }
                planetGridSquare.TroopsHere.thisLock.ExitReadLock();
                if (planetGridSquare.building != null && planetGridSquare.building.CombatStrength > 0)
                    ++num1;
            }
            
            if (num2 > this.numInvadersLast && this.numInvadersLast == 0)
            {
                if (Empire.Universe.PlayerEmpire == this.Owner)
                    Planet.universeScreen.NotificationManager.AddEnemyTroopsLandedNotification(this, index, this.Owner);
                else if (index == Empire.Universe.PlayerEmpire && !this.Owner.isFaction && !Empire.Universe.PlayerEmpire.GetRelations(this.Owner).AtWar)
                {
                    if (Empire.Universe.PlayerEmpire.GetRelations(this.Owner).Treaty_NAPact)
                    {
                        Planet.universeScreen.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.Owner, Empire.Universe.PlayerEmpire, "Invaded NA Pact", this.system));
                        Empire.Universe.PlayerEmpire.GetGSAI().DeclareWarOn(this.Owner, WarType.ImperialistWar);
                        this.Owner.GetRelations(Empire.Universe.PlayerEmpire).Trust -= 50f;
                        this.Owner.GetRelations(Empire.Universe.PlayerEmpire).Anger_DiplomaticConflict += 50f;
                    }
                    else
                    {
                        Planet.universeScreen.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.Owner, Empire.Universe.PlayerEmpire, "Invaded Start War", this.system));
                        Empire.Universe.PlayerEmpire.GetGSAI().DeclareWarOn(this.Owner, WarType.ImperialistWar);
                        this.Owner.GetRelations(Empire.Universe.PlayerEmpire).Trust -= 25f;
                        this.Owner.GetRelations(Empire.Universe.PlayerEmpire).Anger_DiplomaticConflict += 25f;
                    }
                }
            }
            this.numInvadersLast = num2;
            if (num2 <= 0 || num1 != 0 )//|| this.Owner == null)
                return;
            if (index.TryGetRelations(this.Owner, out Relationship rel))
            {
                if (rel.AtWar && rel.ActiveWar != null)
                    ++rel.ActiveWar.ColoniestWon;
            }
            else if (Owner.TryGetRelations(index, out Relationship relship) && relship.AtWar && relship.ActiveWar != null)
                ++relship.ActiveWar.ColoniesLost;
            this.ConstructionQueue.Clear();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                planetGridSquare.QItem = (QueueItem)null;
            this.Owner.RemovePlanet(this);
            if (index == Empire.Universe.PlayerEmpire && this.Owner == EmpireManager.GetEmpireByName("Cordrazine Collective"))
                GlobalStats.IncrementCordrazineCapture();
            if (this.ExploredDict[Empire.Universe.PlayerEmpire] && !flag)
                Planet.universeScreen.NotificationManager.AddConqueredNotification(this, index, this.Owner);
            else if (this.ExploredDict[Empire.Universe.PlayerEmpire])
            {
                lock (GlobalStats.OwnedPlanetsLock)
                {
                    Planet.universeScreen.NotificationManager.AddPlanetDiedNotification(this, Empire.Universe.PlayerEmpire);
                    bool local_7 = true;
                    
                    if (this.Owner != null)
                    {
                        foreach (Planet item_3 in this.system.PlanetList)
                        {
                            if (item_3.Owner == this.Owner && item_3 != this)
                                local_7 = false;
                        }
                        if (local_7)
                            this.system.OwnerList.Remove(this.Owner);
                    }
                    this.Owner = (Empire)null;
                }
                this.ConstructionQueue.Clear();
                return;
            }
            if (index.data.Traits.Assimilators)
            {
                if ((double)index.data.Traits.DiplomacyMod < (double)this.Owner.data.Traits.DiplomacyMod)
                    index.data.Traits.DiplomacyMod = this.Owner.data.Traits.DiplomacyMod;
                if ((double)index.data.Traits.DodgeMod < (double)this.Owner.data.Traits.DodgeMod)
                    index.data.Traits.DodgeMod = this.Owner.data.Traits.DodgeMod;
                if ((double)index.data.Traits.EnergyDamageMod < (double)this.Owner.data.Traits.EnergyDamageMod)
                    index.data.Traits.EnergyDamageMod = this.Owner.data.Traits.EnergyDamageMod;
                if ((double)index.data.Traits.ConsumptionModifier > (double)this.Owner.data.Traits.ConsumptionModifier)
                    index.data.Traits.ConsumptionModifier = this.Owner.data.Traits.ConsumptionModifier;
                if ((double)index.data.Traits.GroundCombatModifier < (double)this.Owner.data.Traits.GroundCombatModifier)
                    index.data.Traits.GroundCombatModifier = this.Owner.data.Traits.GroundCombatModifier;
                if ((double)this.Owner.data.Traits.Mercantile > 0.0)
                    index.data.Traits.Mercantile = this.Owner.data.Traits.Mercantile;
                if (index.data.Traits.PassengerModifier < this.Owner.data.Traits.PassengerModifier)
                    index.data.Traits.PassengerModifier = this.Owner.data.Traits.PassengerModifier;
                if ((double)index.data.Traits.ProductionMod < (double)this.Owner.data.Traits.ProductionMod)
                    index.data.Traits.ProductionMod = this.Owner.data.Traits.ProductionMod;
                if ((double)index.data.Traits.RepairMod < (double)this.Owner.data.Traits.RepairMod)
                    index.data.Traits.RepairMod = this.Owner.data.Traits.RepairMod;
                if ((double)index.data.Traits.ResearchMod < (double)this.Owner.data.Traits.ResearchMod)
                    index.data.Traits.ResearchMod = this.Owner.data.Traits.ResearchMod;
                if ((double)index.data.Traits.ShipCostMod > (double)this.Owner.data.Traits.ShipCostMod)
                    index.data.Traits.ShipCostMod = this.Owner.data.Traits.ShipCostMod;
                if ((double)index.data.Traits.PopGrowthMin < (double)this.Owner.data.Traits.PopGrowthMin)
                    index.data.Traits.PopGrowthMin = this.Owner.data.Traits.PopGrowthMin;
                if ((double)index.data.Traits.PopGrowthMax > (double)this.Owner.data.Traits.PopGrowthMax)
                    index.data.Traits.PopGrowthMax = this.Owner.data.Traits.PopGrowthMax;
                if ((double)index.data.Traits.ModHpModifier < (double)this.Owner.data.Traits.ModHpModifier)
                    index.data.Traits.ModHpModifier = this.Owner.data.Traits.ModHpModifier;
                if ((double)index.data.Traits.TaxMod < (double)this.Owner.data.Traits.TaxMod)
                    index.data.Traits.TaxMod = this.Owner.data.Traits.TaxMod;
                if ((double)index.data.Traits.MaintMod > (double)this.Owner.data.Traits.MaintMod)
                    index.data.Traits.MaintMod = this.Owner.data.Traits.MaintMod;
                if ((double)index.data.SpyModifier < (double)this.Owner.data.SpyModifier)
                    index.data.SpyModifier = this.Owner.data.SpyModifier;
                if ((double)index.data.Traits.Spiritual < (double)this.Owner.data.Traits.Spiritual)
                    index.data.Traits.Spiritual = this.Owner.data.Traits.Spiritual;
            }
            if (index.isFaction)
                return;

            foreach (KeyValuePair<Guid, Ship> keyValuePair in this.Shipyards)
            {
                if (keyValuePair.Value.loyalty != index && keyValuePair.Value.TroopList.Where(loyalty => loyalty.GetOwner() != index).Count() > 0)
                    continue;
                keyValuePair.Value.loyalty = index;
                this.Owner.RemoveShip(keyValuePair.Value);      //Transfer to new owner's ship list. Fixes platforms changing loyalty after game load bug      -Gretman
                index.AddShip(keyValuePair.Value);
                System.Diagnostics.Debug.WriteLine("Owner of platform tethered to " + this.Name + " changed from " + this.Owner.PortraitName + "  to " + index.PortraitName);
            }
            this.Owner = index;
            this.TurnsSinceTurnover = 0;
            this.Owner.AddPlanet(this);
            this.ConstructionQueue.Clear();
            this.system.OwnerList.Clear();
            
            foreach (Planet planet in this.system.PlanetList)
            {
                if (planet.Owner != null && !this.system.OwnerList.Contains(planet.Owner))
                    this.system.OwnerList.Add(planet.Owner);
            }         
            this.colonyType = this.Owner.AssessColonyNeeds(this);
            this.GovernorOn = true;
        }

        public void DoTroopTimers(float elapsedTime)
        {
            //foreach (Building building in this.BuildingList)
            for (int x = 0; x < this.BuildingList.Count;x++ )
            {
                Building building = this.BuildingList[x];
                if (building == null)
                    continue;
                building.AttackTimer -= elapsedTime;
                if ((double)building.AttackTimer < 0.0)
                {
                    building.AvailableAttackActions = 1;
                    building.AttackTimer = 10f;
                }
            }
            List<Troop> list = new List<Troop>();
            //foreach (Troop troop in this.TroopsHere)
            for (int x = 0; x < this.TroopsHere.Count;x++ )
            {
                Troop troop = this.TroopsHere[x];
                if (troop == null)
                    continue;
                if (troop.Strength <= 0)
                {
                    list.Add(troop);
                    foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                        planetGridSquare.TroopsHere.Remove(troop);
                }
                troop.Launchtimer -= elapsedTime;
                troop.MoveTimer -= elapsedTime;
                troop.MovingTimer -= elapsedTime;
                if ((double)troop.MoveTimer < 0.0)
                {
                    ++troop.AvailableMoveActions;
                    if (troop.AvailableMoveActions > troop.MaxStoredActions)
                        troop.AvailableMoveActions = troop.MaxStoredActions;
                    troop.MoveTimer = (float)troop.MoveTimerBase;
                }
                troop.AttackTimer -= elapsedTime;
                if ((double)troop.AttackTimer < 0.0)
                {
                    ++troop.AvailableAttackActions;
                    if (troop.AvailableAttackActions > troop.MaxStoredActions)
                        troop.AvailableAttackActions = troop.MaxStoredActions;
                    troop.AttackTimer = (float)troop.AttackTimerBase;
                }
            }
            foreach (Troop troop in list)
                this.TroopsHere.Remove(troop);
        }

        private void MakeCombatDecisions()
        {
            bool enemyTroopsFound = false;
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (planetGridSquare.TroopsHere.Count > 0 && planetGridSquare.TroopsHere[0].GetOwner() != this.Owner || planetGridSquare.building != null && !string.IsNullOrEmpty(planetGridSquare.building.EventTriggerUID))
                {
                    enemyTroopsFound = true;
                    break;
                }
            }
            if (!enemyTroopsFound)
                return;
            List<PlanetGridSquare> list = new List<PlanetGridSquare>();
            for (int index = 0; index < this.TilesList.Count; ++index)
            {
                PlanetGridSquare pgs = this.TilesList[index];
                bool hasAttacked = false;
                if (pgs.TroopsHere.Count > 0)
                {
                    if (pgs.TroopsHere[0].AvailableAttackActions > 0)
                    {
                        if (pgs.TroopsHere[0].GetOwner() != Empire.Universe.PlayerEmpire || !Planet.universeScreen.LookingAtPlanet || (!(Planet.universeScreen.workersPanel is CombatScreen) || (Planet.universeScreen.workersPanel as CombatScreen).p != this) || GlobalStats.AutoCombat)
                        {
                            {
                                foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                                {
                                    if (CombatScreen.TroopCanAttackSquare(pgs, planetGridSquare, this))
                                    {
                                        hasAttacked = true;
                                        if (pgs.TroopsHere[0].AvailableAttackActions > 0)
                                        {
                                            --pgs.TroopsHere[0].AvailableAttackActions;
                                            --pgs.TroopsHere[0].AvailableMoveActions;
                                            if (planetGridSquare.x > pgs.x)
                                                pgs.TroopsHere[0].facingRight = true;
                                            else if (planetGridSquare.x < pgs.x)
                                                pgs.TroopsHere[0].facingRight = false;
                                            CombatScreen.StartCombat(pgs, planetGridSquare, this);
                                            break;
                                        }
                                        else
                                            break;
                                    }
                                }
                            }
                        }
                        else
                            continue;
                    }
                    try
                    {
                        if (!hasAttacked && pgs.TroopsHere.Count > 0 && pgs.TroopsHere[0].AvailableMoveActions > 0)
                        {
                            foreach (PlanetGridSquare planetGridSquare in (IEnumerable<PlanetGridSquare>)Enumerable.OrderBy<PlanetGridSquare, int>((IEnumerable<PlanetGridSquare>)this.TilesList, (Func<PlanetGridSquare, int>)(tile => Math.Abs(tile.x - pgs.x) + Math.Abs(tile.y - pgs.y))))
                            {
                                if (planetGridSquare != pgs)
                                {
                                    if (planetGridSquare.TroopsHere.Count > 0)
                                    {
                                        if (planetGridSquare.TroopsHere[0].GetOwner() != pgs.TroopsHere[0].GetOwner())
                                        {
                                            if (planetGridSquare.x > pgs.x)
                                            {
                                                if (planetGridSquare.y > pgs.y)
                                                {
                                                    if (this.TryTroopMove(1, 1, pgs))
                                                        break;
                                                }
                                                if (planetGridSquare.y < pgs.y)
                                                {
                                                    if (this.TryTroopMove(1, -1, pgs))
                                                        break;
                                                }
                                                if (!this.TryTroopMove(1, 0, pgs))
                                                {
                                                    if (!this.TryTroopMove(1, -1, pgs))
                                                    {
                                                        if (this.TryTroopMove(1, 1, pgs))
                                                            break;
                                                    }
                                                    else
                                                        break;
                                                }
                                                else
                                                    break;
                                            }
                                            else if (planetGridSquare.x < pgs.x)
                                            {
                                                if (planetGridSquare.y > pgs.y)
                                                {
                                                    if (this.TryTroopMove(-1, 1, pgs))
                                                        break;
                                                }
                                                if (planetGridSquare.y < pgs.y)
                                                {
                                                    if (this.TryTroopMove(-1, -1, pgs))
                                                        break;
                                                }
                                                if (!this.TryTroopMove(-1, 0, pgs))
                                                {
                                                    if (!this.TryTroopMove(-1, -1, pgs))
                                                    {
                                                        if (this.TryTroopMove(-1, 1, pgs))
                                                            break;
                                                    }
                                                    else
                                                        break;
                                                }
                                                else
                                                    break;
                                            }
                                            else
                                            {
                                                if (planetGridSquare.y > pgs.y)
                                                {
                                                    if (this.TryTroopMove(0, 1, pgs))
                                                        break;
                                                }
                                                if (planetGridSquare.y < pgs.y)
                                                {
                                                    if (this.TryTroopMove(0, -1, pgs))
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    else if (planetGridSquare.building != null && (planetGridSquare.building.CombatStrength > 0 || !string.IsNullOrEmpty(planetGridSquare.building.EventTriggerUID)) && (this.Owner != pgs.TroopsHere[0].GetOwner() || !string.IsNullOrEmpty(planetGridSquare.building.EventTriggerUID)))
                                    {
                                        if (planetGridSquare.x > pgs.x)
                                        {
                                            if (planetGridSquare.y > pgs.y)
                                            {
                                                if (this.TryTroopMove(1, 1, pgs))
                                                    break;
                                            }
                                            if (planetGridSquare.y < pgs.y)
                                            {
                                                if (this.TryTroopMove(1, -1, pgs))
                                                    break;
                                            }
                                            if (!this.TryTroopMove(1, 0, pgs))
                                            {
                                                if (!this.TryTroopMove(1, -1, pgs))
                                                {
                                                    if (this.TryTroopMove(1, 1, pgs))
                                                        break;
                                                }
                                                else
                                                    break;
                                            }
                                            else
                                                break;
                                        }
                                        else if (planetGridSquare.x < pgs.x)
                                        {
                                            if (planetGridSquare.y > pgs.y)
                                            {
                                                if (this.TryTroopMove(-1, 1, pgs))
                                                    break;
                                            }
                                            if (planetGridSquare.y < pgs.y)
                                            {
                                                if (this.TryTroopMove(-1, -1, pgs))
                                                    break;
                                            }
                                            if (!this.TryTroopMove(-1, 0, pgs))
                                            {
                                                if (!this.TryTroopMove(-1, -1, pgs))
                                                {
                                                    if (this.TryTroopMove(-1, 1, pgs))
                                                        break;
                                                }
                                                else
                                                    break;
                                            }
                                            else
                                                break;
                                        }
                                        else
                                        {
                                            if (planetGridSquare.y > pgs.y)
                                            {
                                                if (!this.TryTroopMove(0, 1, pgs))
                                                {
                                                    if (!this.TryTroopMove(1, 1, pgs))
                                                    {
                                                        if (this.TryTroopMove(-1, 1, pgs))
                                                            break;
                                                    }
                                                    else
                                                        break;
                                                }
                                                else
                                                    break;
                                            }
                                            if (planetGridSquare.y < pgs.y)
                                            {
                                                if (!this.TryTroopMove(0, -1, pgs))
                                                {
                                                    if (!this.TryTroopMove(1, -1, pgs))
                                                    {
                                                        if (this.TryTroopMove(-1, -1, pgs))
                                                            break;
                                                    }
                                                    else
                                                        break;
                                                }
                                                else
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                    catch { }
                }
                    
                else if (pgs.building != null && pgs.building.CombatStrength > 0 && (this.Owner != Empire.Universe.PlayerEmpire || !Planet.universeScreen.LookingAtPlanet || (!(Planet.universeScreen.workersPanel is CombatScreen) || (Planet.universeScreen.workersPanel as CombatScreen).p != this) || GlobalStats.AutoCombat) && pgs.building.AvailableAttackActions > 0)
                {
                    foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                    {
                        if (CombatScreen.TroopCanAttackSquare(pgs, planetGridSquare, this))
                        {
                            --pgs.building.AvailableAttackActions;
                            CombatScreen.StartCombat(pgs, planetGridSquare, this);
                            break;
                        }
                    }
                }
                
            }
            
        }

        private bool TryTroopMove(int changex, int changey, PlanetGridSquare start)
        {
            foreach (PlanetGridSquare eventLocation in this.TilesList)
            {
                if (eventLocation.x == start.x + changex && eventLocation.y == start.y + changey)
                {
                    Troop troop = null;

                    eventLocation.TroopsHere.thisLock.EnterReadLock();
                    if (start.TroopsHere.Count > 0)
                    {
                        troop = start.TroopsHere[0];
                    }
                    

                    if (eventLocation.building != null && eventLocation.building.CombatStrength > 0 || eventLocation.TroopsHere.Count > 0)
                    {
                        eventLocation.TroopsHere.thisLock.ExitReadLock();
                        return false;
                    }
                    if (troop != null)
                    {
                        if (changex > 0)
                            troop.facingRight = true;
                        else if (changex < 0)
                            troop.facingRight = false;
                        troop.SetFromRect(start.TroopClickRect);
                        troop.MovingTimer = 0.75f;
                        --troop.AvailableMoveActions;
                        troop.MoveTimer = (float)troop.MoveTimerBase;
                        eventLocation.TroopsHere.thisLock.ExitReadLock();
                        eventLocation.TroopsHere.Add(troop);
                        start.TroopsHere.Clear();
                    }
                    if (eventLocation.building == null || string.IsNullOrEmpty(eventLocation.building.EventTriggerUID) || (eventLocation.TroopsHere.Count <= 0 || eventLocation.TroopsHere[0].GetOwner().isFaction))
                    {
                        if (eventLocation.TroopsHere.thisLock.IsReadLockHeld)
                        eventLocation.TroopsHere.thisLock.ExitReadLock();
                        return true;
                    }
                    ResourceManager.EventsDict[eventLocation.building.EventTriggerUID].TriggerPlanetEvent(this, eventLocation.TroopsHere[0].GetOwner(), eventLocation, Empire.Universe.PlayerEmpire, Planet.universeScreen);
                    
                }
            }
            return false;
        }

        public void Update(float elapsedTime)
        {
            this.DecisionTimer -= elapsedTime;
            this.CombatTimer -= elapsedTime;
            this.RecentCombat = this.CombatTimer > 0.0f;
            List<Guid> list = new List<Guid>();
            foreach (KeyValuePair<Guid, Ship> keyValuePair in this.Shipyards)
            {
                if (!keyValuePair.Value.Active 
                    || keyValuePair.Value.ModuleSlotList.Count == 0
                    //|| keyValuePair.Value.loyalty != this.Owner
                    )
                    list.Add(keyValuePair.Key);
            }
            Ship remove;
            foreach (Guid key in list)
                this.Shipyards.TryRemove(key,out remove);
            if (!Planet.universeScreen.Paused)
            {
                
                if (this.TroopsHere.Count > 0)
                {
                    //try
                    {
                        this.DoCombats(elapsedTime);
                        if (this.DecisionTimer <= 0)
                        {
                            this.MakeCombatDecisions();
                            this.DecisionTimer = 0.5f;
                        }
                    }
                    //catch
                    {
                    }
                }
                if (this.TroopsHere.Count != 0 || this.BuildingList.Count != 0)
                    this.DoTroopTimers(elapsedTime);
            }
            for (int index1 = 0; index1 < this.BuildingList.Count; ++index1)
            {
                //try
                {
                    Building building = this.BuildingList[index1];
                    if (building.isWeapon)
                    {
                        building.WeaponTimer -= elapsedTime;
                        if (building.WeaponTimer < 0 && this.system.ShipList.Count>0)
                        {
                            if (this.Owner != null)
                            {
                                Ship target = null;
                                Ship troop = null;
                                float currentD = 0;
                                float previousD = building.theWeapon.Range + 1000f;
                                //float currentT = 0;
                                float previousT = building.theWeapon.Range + 1000f;
                                //this.system.ShipList.thisLock.EnterReadLock();
                                for (int index2 = 0; index2 < this.system.ShipList.Count; ++index2)
                                {
                                    Ship ship = this.system.ShipList[index2];
                                    if (ship.loyalty == this.Owner || (!ship.loyalty.isFaction && this.Owner.GetRelations(ship.loyalty).Treaty_NAPact) )
                                        continue;
                                    currentD = Vector2.Distance(this.Position, ship.Center);                                   
                                    if (ship.GetShipData().Role == ShipData.RoleName.troop && currentD  < previousT)
                                    {
                                        previousT = currentD;
                                        troop = ship;
                                        continue;
                                    }
                                    if(currentD < previousD && troop ==null)
                                    {
                                        previousD = currentD;
                                        target = ship;
                                    }

                                }
                              //  this.system.ShipList.thisLock.ExitReadLock();
                                //if (ship.loyalty != this.Owner && (ship.loyalty.isFaction || this.Owner.GetRelations()[ship.loyalty].AtWar) && Vector2.Distance(this.Position, ship.Center) < building.theWeapon.Range)
                                //Ship ship = null;
                                if (troop != null)
                                    target = troop;
                                if(target != null)
                                {
                                    building.theWeapon.Center = this.Position;
                                    building.theWeapon.FireFromPlanet(target.Center + target.Velocity - this.Position, this, target.GetRandomInternalModule(building.theWeapon));
                                    building.WeaponTimer = building.theWeapon.fireDelay;
                                    break;
                                }


                            }
                        }
                    }
                }
                //catch
                //{
                //}
            }
            for (int index = 0; index < this.Projectiles.Count; ++index)
            {
                Projectile projectile = this.Projectiles[index];
                if (projectile.Active)
                {
                    if (elapsedTime > 0)
                        projectile.Update(elapsedTime);
                }
                else
                    this.Projectiles.QueuePendingRemoval(projectile);
            }
            this.Projectiles.ApplyPendingRemovals();
            this.UpdatePosition(elapsedTime);
        }

   
        //added by gremlin affectnearbyships
        private void AffectNearbyShips()
        {
            float RepairPool = this.developmentLevel * this.RepairPerTurn * 20;
            if(this.HasShipyard)
            {
                foreach(Ship ship in this.Shipyards.Values)
                {                    
                        RepairPool += ship.RepairRate;                    
                }
            }
            for (int i = 0; i < this.system.ShipList.Count; i++)
            {
                Ship ship = this.system.ShipList[i];
                if(ship != null && ship.loyalty.isFaction)
                {
                    ship.Ordinance = ship.OrdinanceMax;
                    if (ship.HasTroopBay )
                    {
                        if (this.Population >0)
                        {
                            if (ship.TroopCapacity > ship.TroopList.Count)
                            {
                                string redshirtType = "Wyvern";
                                Troop xeno = ResourceManager.TroopsDict[redshirtType];
                                xeno = ResourceManager.CreateTroop(xeno, ship.loyalty);
                                ship.TroopList.Add(ResourceManager.CreateTroop(xeno, ship.loyalty));
                            }
                            if (this.Owner != null && this.Population > 0)
                            {
                                this.Population *= .5f;
                                this.Population -= 1000;
                                this.ProductionHere *= .5f;
                                this.FoodHere *= .5f;
                            }
                            if (this.Population < 0)
                                this.Population = 0;
                        }
                        else if (this.ParentSystem.combatTimer < -30 && ship.TroopCapacity > ship.TroopList.Count)
                        {
                            string redshirtType = "Wyvern";
                            Troop xeno = ResourceManager.TroopsDict[redshirtType];
                            xeno = ResourceManager.CreateTroop(xeno, ship.loyalty);
                            ship.TroopList.Add(ResourceManager.CreateTroop(xeno, ship.loyalty));
                            this.ParentSystem.combatTimer = 0;
                        }
                        

                        
                    }
                }
                if (ship != null && ship.loyalty == this.Owner && this.HasShipyard && Vector2.Distance(this.Position, ship.Position) <= 5000f)
                {
                    ship.PowerCurrent = ship.PowerStoreMax;
                    ship.Ordinance = ship.OrdinanceMax;
                    if (GlobalStats.HardcoreRuleset)
                    {
                        foreach (KeyValuePair<string, float> maxGood in ship.GetMaxGoods())
                        {
                            if (ship.GetCargo()[maxGood.Key] >= maxGood.Value)
                            {
                                continue;
                            }
                            while (this.ResourcesDict[maxGood.Key] > 0f && ship.GetCargo()[maxGood.Key] < maxGood.Value)
                            {
                                if (maxGood.Value - ship.GetCargo()[maxGood.Key] < 1f)
                                {
                                    Dictionary<string, float> resourcesDict = this.ResourcesDict;
                                    Dictionary<string, float> strs = resourcesDict;
                                    string key = maxGood.Key;
                                    string str = key;
                                    resourcesDict[key] = strs[str] - (maxGood.Value - ship.GetCargo()[maxGood.Key]);
                                    Dictionary<string, float> cargo = ship.GetCargo();
                                    Dictionary<string, float> strs1 = cargo;
                                    string key1 = maxGood.Key;
                                    string str1 = key1;
                                    cargo[key1] = strs1[str1] + (maxGood.Value - ship.GetCargo()[maxGood.Key]);
                                }
                                else
                                {
                                    Dictionary<string, float> resourcesDict1 = this.ResourcesDict;
                                    Dictionary<string, float> strs2 = resourcesDict1;
                                    string key2 = maxGood.Key;
                                    resourcesDict1[key2] = strs2[key2] - 1f;
                                    Dictionary<string, float> cargo1 = ship.GetCargo();
                                    Dictionary<string, float> strs3 = cargo1;
                                    string str2 = maxGood.Key;
                                    cargo1[str2] = strs3[str2] + 1f;
                                }
                            }
                        }
                    }
                    //Modified by McShooterz: Repair based on repair pool, if no combat in system                 
                    if (!ship.InCombat && RepairPool > 0 && (ship.Health < ship.HealthMax || ship.shield_percent <90))
                    {
                        //bool repairing = false;
                        ship.shipStatusChanged = true;
                        foreach (ModuleSlot slot in ship.ModuleSlotList) // .Where(slot => slot.module.ModuleType != ShipModuleType.Dummy && slot.module.Health != slot.module.HealthMax))
                        {
                            if (slot.module.ModuleType == ShipModuleType.Dummy)
                                continue;
                            //repairing = true;
                            if(ship.loyalty.data.Traits.ModHpModifier >0 )
                            {
                                float test = ResourceManager.ShipModulesDict[slot.module.UID].HealthMax;
                                slot.module.HealthMax = test + test * ship.loyalty.data.Traits.ModHpModifier; 
                            }
                            if (slot.module.Health < slot.module.HealthMax)
                            {
                                if (slot.module.HealthMax - slot.module.Health > RepairPool)
                                {
                                    slot.module.Repair(RepairPool);
                                    RepairPool = 0;
                                    break;
                                }
                                else
                                {
                                    RepairPool -= slot.module.HealthMax - slot.module.Health;
                                    slot.module.Repair(slot.module.HealthMax);
                                }
                            }
                        }                        
                        if (RepairPool > 0)
                        {
                            float shieldrepair = .2f * RepairPool;
                            if (ship.shield_max - ship.shield_power > shieldrepair)
                                ship.shield_power += shieldrepair;
                            else
                            {
                                shieldrepair = ship.shield_max - ship.shield_power;
                                ship.shield_power = ship.shield_max;
                                
                            }
                            RepairPool = -shieldrepair;
                        }
                    }
                    else if(ship.GetAI().State == AIState.Resupply)
                    {
                
                        ship.GetAI().OrderQueue.Clear();
                    
                        ship.GetAI().Target = null;
                        ship.GetAI().PotentialTargets.Clear();
                        ship.GetAI().HasPriorityOrder = false;
                        ship.GetAI().State = AIState.AwaitingOrders;

                    }
                    //auto load troop:
                    if ((this.ParentSystem.combatTimer <= 0 || !ship.InCombat) && this.TroopsHere.Count() > 0 && this.TroopsHere.Where(troop => troop.GetOwner() != this.Owner).Count() == 0)
                    {
                        foreach (var pgs in this.TilesList)
                        {
                            if (ship.TroopCapacity ==0 || ship.TroopList.Count >= ship.TroopCapacity) 
                                break;
                            pgs.TroopsHere.thisLock.EnterWriteLock();
                            if (pgs.TroopsHere.Count > 0 && pgs.TroopsHere[0].GetOwner() == this.Owner)
                            {                                
                                Troop troop = pgs.TroopsHere[0];
                                ship.TroopList.Add(troop);
                                pgs.TroopsHere.Clear();
                                this.TroopsHere.Remove(troop);
                            }
                            pgs.TroopsHere.thisLock.ExitWriteLock();
                        }
                    }
                }
            }
        }

        public void TerraformExternal(float amount)
        {
            this.Fertility += amount;
            if ((double)this.Fertility <= 0.0)
            {
                this.Fertility = 0.0f;
                lock (GlobalStats.ObjectManagerLocker)
                    Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                this.planetType = 7;
                this.Terraform();
            }
            else if (this.Type == "Barren" && (double)this.Fertility > 0.01)
            {
                lock (GlobalStats.ObjectManagerLocker)
                    Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                this.planetType = 14;
                this.Terraform();
            }
            else if (this.Type == "Desert" && (double)this.Fertility > 0.35)
            {
                lock (GlobalStats.ObjectManagerLocker)
                    Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                this.planetType = 18;
                this.Terraform();
            }
            else if (this.Type == "Ice" && (double)this.Fertility > 0.35)
            {
                lock (GlobalStats.ObjectManagerLocker)
                    Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                this.planetType = 19;
                this.Terraform();
            }
            else if (this.Type == "Swamp" && (double)this.Fertility > 0.75)
            {
                lock (GlobalStats.ObjectManagerLocker)
                    Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                this.planetType = 21;
                this.Terraform();
            }
            else if (this.Type == "Steppe" && (double)this.Fertility > 0.6)
            {
                lock (GlobalStats.ObjectManagerLocker)
                    Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                this.planetType = 11;
                this.Terraform();
            }
            else
            {
                if (!(this.Type == "Tundra") || (double)this.Fertility <= 0.95)
                    return;
                lock (GlobalStats.ObjectManagerLocker)
                    Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                this.planetType = 22;
                this.Terraform();
            }
        }

        public void UpdateOwnedPlanet()
        {
            ++this.TurnsSinceTurnover;
            --this.Crippled_Turns;
            if (this.Crippled_Turns < 0)
                this.Crippled_Turns = 0;
            this.ConstructionQueue.ApplyPendingRemovals();
            this.UpdateDevelopmentStatus();
            this.Description = this.DevelopmentStatus;
            this.AffectNearbyShips();
            this.TerraformPoints += this.TerraformToAdd;
            if (  this.TerraformPoints > 0.0f &&  this.Fertility < 1.0)
            {
                this.Fertility += this.TerraformToAdd;
                if (this.Type == "Barren" &&  this.Fertility > 0.01)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                    this.planetType = 14;
                    this.Terraform();
                }
                else if (this.Type == "Desert" &&  this.Fertility > 0.35)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                    this.planetType = 18;
                    this.Terraform();
                }
                else if (this.Type == "Ice" &&  this.Fertility > 0.35)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                    this.planetType = 19;
                    this.Terraform();
                }
                else if (this.Type == "Swamp" &&  this.Fertility > 0.75)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                    this.planetType = 21;
                    this.Terraform();
                }
                else if (this.Type == "Steppe" &&  this.Fertility > 0.6)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                    this.planetType = 11;
                    this.Terraform();
                }
                else if (this.Type == "Tundra" &&  this.Fertility > 0.95)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                    this.planetType = 22;
                    this.Terraform();
                }
                if ( this.Fertility > 1.0)
                    this.Fertility = 1f;
            }
            if (GovernorOn)
                DoGoverning();
            UpdateIncomes(false);

            // notification about empty queue
            if (GlobalStats.ExtraNotiofications && Owner != null && Owner.isPlayer)
            {
                if (ConstructionQueue.Count == 0 && !queueEmptySent)
                {
                    if (colonyType == ColonyType.Colony || colonyType == ColonyType.Core || colonyType == ColonyType.Industrial || !GovernorOn)
                    {
                        queueEmptySent = true;
                        universeScreen.NotificationManager.AddEmptyQueueNotification(this);
                    }
                }
                else if (ConstructionQueue.Count > 0)
                {
                    queueEmptySent = false;
                }
            }
            //if ((double)this.ShieldStrengthCurrent < (double)this.ShieldStrengthMax)
            //{
            //    ++this.ShieldStrengthCurrent;
            //    if ((double)this.ShieldStrengthCurrent > (double)this.ShieldStrengthMax)
            //        this.ShieldStrengthCurrent = this.ShieldStrengthMax;
            //}
            //if ((double)this.ShieldStrengthCurrent > (double)this.ShieldStrengthMax)
            //    this.ShieldStrengthCurrent = this.ShieldStrengthMax;
            //added by gremlin Planetary Shield Change
            if (ShieldStrengthCurrent < this.ShieldStrengthMax)
            {
                Planet shieldStrengthCurrent = this;

                if (!RecentCombat)
                {

                    if (ShieldStrengthCurrent > ShieldStrengthMax / 10)
                    {
                        shieldStrengthCurrent.ShieldStrengthCurrent += shieldStrengthCurrent.ShieldStrengthMax / 10;
                    }
                    else
                    {
                        shieldStrengthCurrent.ShieldStrengthCurrent++;
                    }
                }
                if (ShieldStrengthCurrent > ShieldStrengthMax)
                    ShieldStrengthCurrent = ShieldStrengthMax;
            }

            //this.UpdateTimer = 10f;
            HarvestResources();
            ApplyProductionTowardsConstruction();
            GrowPopulation();
            HealTroops();
            if (FoodHere > MAX_STORAGE)
                FoodHere = MAX_STORAGE;
            if (ProductionHere > MAX_STORAGE)
                ProductionHere = MAX_STORAGE;
        }

        private float CalculateCyberneticPercentForSurplus(float desiredSurplus)
        {
            // replacing while loop with singal fromula, should save some clock cycles
            //float Surplus = 0.0f;
            //while ((double)Surplus < 1.0)
            //{
            //    Surplus += 0.01f;
            //    float num2 = (float)((double)Surplus * (double)this.Population / 1000.0 * ((double)this.MineralRichness + (double)this.PlusProductionPerColonist)) + this.PlusFlatProductionPerTurn;
            //    //float num3 = num2 - this.consumption;
            //    //if ((double)(num3 - this.Owner.data.TaxRate * num3) >= (double)desiredSurplus)

            //    //taking taxes out of production first then taking out consumption to fix starvation at high tax rates
            //    //Allium Sativum trying to fix issue #332
            //    float num3 = num2 * (1 - this.Owner.data.TaxRate);
            //    if ((double)(num3 - this.consumption) >= (double)desiredSurplus) 
            //    {
            //        this.ps = Planet.GoodState.EXPORT;
            //        return Surplus;
            //    }
            //}
            //this.fs = Planet.GoodState.IMPORT;
            //return Surplus;

            float NoDivByZero = .0000001f;
            float Surplus = (float)((this.consumption + desiredSurplus - this.PlusFlatProductionPerTurn) / ((this.Population / 1000.0) * (this.MineralRichness + this.PlusProductionPerColonist)) * (1 - this.Owner.data.TaxRate)+NoDivByZero);
            if (Surplus < 1.0f)
            {
               // this.ps = Planet.GoodState.EXPORT;
                if (Surplus < 0)
                    return 0.0f;
                return Surplus;
            }
            else
            {
               // this.ps = Planet.GoodState.IMPORT;
                return 1.0f;
            }
        }

        private float CalculateFarmerPercentForSurplus(float desiredSurplus)
        {
            float Surplus = 0.0f;
            float NoDivByZero = .0000001f;
            if(this.Owner.data.Traits.Cybernetic >0)
            {

                Surplus = Surplus = (float)((this.consumption + desiredSurplus - this.PlusFlatProductionPerTurn) / ((this.Population / 1000.0) * (this.MineralRichness + this.PlusProductionPerColonist)) * (1 - this.Owner.data.TaxRate) + NoDivByZero);
                    //(float)((this.consumption + desiredSurplus - this.PlusFlatProductionPerTurn) / 
                    //((this.Population / 1000.0) * (this.MineralRichness + this.PlusProductionPerColonist)) * 
                    //(1 - (this.Owner.data.TaxRate == 1 ? .9 : this.Owner.data.TaxRate)) + NoDivByZero);
                if (Surplus < 1.0f)
                {
                    // this.ps = Planet.GoodState.EXPORT;
                    if (Surplus < 0)
                        return 0.0f;
                    return Surplus;
                }
                else
                {
                    // this.ps = Planet.GoodState.IMPORT;
                    return 1.0f;
                }
            }
            
            if ((double)this.Fertility == 0.0)
                return 0.0f;
            // replacing while loop with singal fromula, should save some clock cycles

           
            Surplus = (float)((this.consumption + desiredSurplus - this.FlatFoodAdded) / ((this.Population / 1000.0) * (this.Fertility + this.PlusFoodPerColonist) * (1 + this.FoodPercentAdded)+NoDivByZero));
            if (Surplus < 1)
            {
                if (Surplus < 0)
                    return 0.0f;
                return Surplus;
            }
            else
            {
                //this.fs = Planet.GoodState.IMPORT;
                //if you cant reach the desired surplus, produce as much as you can
                return 1.0f;
            }
        }

        private bool DetermineIfSelfSufficient()
        {
            //float num = (float)(1.0 * (double)this.Population / 1000.0 * ((double)this.Fertility + (double)this.PlusFoodPerColonist)) + this.FlatFoodAdded;
            //return (double)(num + this.FoodPercentAdded * num - this.consumption) > 0.0;
             float NoDivByZero = .0000001f;
            return (float)((this.consumption - this.FlatFoodAdded) / ((this.Population / 1000.0) * (this.Fertility + this.PlusFoodPerColonist) * (1 + this.FoodPercentAdded)+NoDivByZero)) < 1;
        }

        public float GetDefendingTroopStrength()
        {
            float num = 0;
            foreach (Troop troop in this.TroopsHere)
            {
                if (troop.GetOwner() == this.Owner)
                    num += troop.Strength;
            }
            return num;
        }

        public int GetDefendingTroopCount()
        {
            int num = 0;
            foreach (Troop troop in this.TroopsHere)
            {
                if (troop.GetOwner() == this.Owner)
                    num++;
            }
            return num;
        }



        public List<Building> GetBuildingsWeCanBuildHere()
        {
            if (this.Owner == null)
                return new List<Building>();
            this.BuildingsCanBuild.Clear();
            bool flag1 = true;
            foreach (Building building in this.BuildingList)
            {
                if (building.Name == "Capital City" || building.Name == "Outpost")
                {
                    flag1 = false;
                    break;
                }
            }
            foreach (KeyValuePair<string, bool> keyValuePair in this.Owner.GetBDict())
            {
                if (keyValuePair.Value)
                {
                    Building building1 = ResourceManager.BuildingsDict[keyValuePair.Key];
                    bool flag2 = true;
                    if(this.Owner.data.Traits.Cybernetic >0)
                    {
                        if(building1.PlusFlatFoodAmount >0 || building1.PlusFoodPerColonist >0)
                        {
                            continue;
                        }
                    }
                    if (!flag1 && (building1.Name == "Outpost" || building1.Name == "Capital City"))
                        flag2 = false;
                    if (building1.BuildOnlyOnce)
                    {
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            foreach (Building building2 in planet.BuildingList)
                            {
                                if (planet.Name == building1.Name)
                                {
                                    flag2 = false;
                                    break;
                                }
                            }
                            if (flag2)
                            {
                                foreach (QueueItem queueItem in (List<QueueItem>)planet.ConstructionQueue)
                                {
                                    if (queueItem.isBuilding && queueItem.Building.Name == building1.Name)
                                    {
                                        flag2 = false;
                                        break;
                                    }
                                }
                                if (!flag2)
                                    break;
                            }
                            else
                                break;
                        }
                    }
                    if (flag2)
                    {
                        foreach (Building building2 in this.BuildingList)
                        {
                            if (building2.Name == building1.Name && building1.Name != "Biospheres" && building2.Unique)
                            {
                                flag2 = false;
                                break;
                            }
                        }
                        for (int index = 0; index < this.ConstructionQueue.Count; ++index)
                        {
                            QueueItem queueItem = this.ConstructionQueue[index];
                            if (queueItem.isBuilding && queueItem.Building.Name == building1.Name && (building1.Name != "Biospheres" && queueItem.Building.Unique))
                            {
                                flag2 = false;
                                break;
                            }
                        }
                        if(building1.Name == "Biosphers")
                        {
                            foreach(PlanetGridSquare tile in this.TilesList)
                            {
                                if (!tile.Habitable)
                                    break;
                                flag2 = false;

                            }
                        }
                    }
                    if (flag2)
                        this.BuildingsCanBuild.Add(building1);
                }
            }
            return this.BuildingsCanBuild;
        }
        public void AddBuildingToCQ(Building b)
        {
            this.AddBuildingToCQ(b, false);
        }
        public void AddBuildingToCQ(Building b, bool PlayerAdded)
        {
            int count = this.ConstructionQueue.Count;
            QueueItem qi = new QueueItem();
            qi.IsPlayerAdded = PlayerAdded;
            qi.isBuilding = true;
            qi.Building = b;
            qi.Cost = b.Cost;// ResourceManager.BuildingsDict[b.Name].Cost;   //GetBuilding(b.Name).Cost;
            qi.productionTowards = 0.0f;
            qi.NotifyOnEmpty = false;
            Building terraformer=null;
            ResourceManager.BuildingsDict.TryGetValue("Terraformer",out terraformer);
            
            if (terraformer == null)
            {
                foreach(KeyValuePair<string,bool> bdict in this.Owner.GetBDict())
                {
                    if (!bdict.Value)
                        continue;
                    ResourceManager.BuildingsDict.TryGetValue("Terraformer", out terraformer);
                    if (terraformer.PlusTerraformPoints > 0)
                        break;

                }
            }
            if (this.AssignBuildingToTile(b, qi))
                this.ConstructionQueue.Add(qi);

            else if (this.Owner.data.Traits.Cybernetic <=0 && this.Owner.GetBDict()[terraformer.Name] && this.Fertility < 1.0 && this.WeCanAffordThis(terraformer, this.colonyType))
            {
                bool flag = true;
                foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                {
                    if (queueItem.isBuilding && queueItem.Building.Name == terraformer.Name)
                        flag = false;
                }
                foreach (Building building in this.BuildingList)
                {
                    if (building.Name == terraformer.Name)
                        flag = false;
                }
                if (!flag)
                    return;
                this.AddBuildingToCQ(ResourceManager.GetBuilding(terraformer.Name),false);
            }
            else
            {
                if (!this.Owner.GetBDict()["Biospheres"])
                    return;
                this.TryBiosphereBuild(ResourceManager.GetBuilding("Biospheres"), qi);
            }
        }

        public bool BuildingInQueue(string UID)
        {
            for (int index = 0; index < this.ConstructionQueue.Count; ++index)
            {
                if (this.ConstructionQueue[index].isBuilding && this.ConstructionQueue[index].Building.Name == UID)
                    return true;
            }
            return false;
        }

        public bool WeCanAffordThis(Building building, Planet.ColonyType governor)
        {
            if (governor == ColonyType.TradeHub)
                return true;
            if (building == null)
                return false;
            if (building.IsPlayerAdded)
                return true;
            Empire empire = this.Owner;
            float buildingMaintenance = empire.GetTotalBuildingMaintenance();
            float grossTaxes = empire.GrossTaxes;
            //bool playeradded = ;
          
            bool itsHere = this.BuildingList.Contains(building);
            
            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
            {
                if (queueItem.isBuilding)
                {
                    buildingMaintenance += this.Owner.data.Traits.MaintMod * queueItem.Building.Maintenance;
                    bool added =queueItem.Building == building;
                    if (added)
                    {
                        //if(queueItem.IsPlayerAdded)
                        //{
                        //    playeradded = true;
                        //}
                        itsHere = true;
                    }
                    
                }
                
            }
            buildingMaintenance += building.Maintenance + building.Maintenance * this.Owner.data.Traits.MaintMod;
            
            bool LowPri = buildingMaintenance / grossTaxes < .25f;
            bool MedPri = buildingMaintenance / grossTaxes < .60f;
            bool HighPri = buildingMaintenance / grossTaxes < .80f;
            float income = this.GrossMoneyPT + this.Owner.data.Traits.TaxMod * this.GrossMoneyPT - (this.TotalMaintenanceCostsPerTurn + this.TotalMaintenanceCostsPerTurn * this.Owner.data.Traits.MaintMod);           
            float maintCost = this.GrossMoneyPT + this.Owner.data.Traits.TaxMod * this.GrossMoneyPT -building.Maintenance- (this.TotalMaintenanceCostsPerTurn + this.TotalMaintenanceCostsPerTurn * this.Owner.data.Traits.MaintMod);
            bool makingMoney = maintCost > 0;
      
            int defensiveBuildings = this.BuildingList.Where(combat => combat.SoftAttack > 0 || combat.PlanetaryShieldStrengthAdded >0 ||combat.theWeapon !=null ).Count();           
           int possibleoffensiveBuilding = this.BuildingsCanBuild.Where(b => b.PlanetaryShieldStrengthAdded > 0 || b.SoftAttack > 0 || b.theWeapon != null).Count();
           bool isdefensive = building.SoftAttack > 0 || building.PlanetaryShieldStrengthAdded > 0 || building.isWeapon ;
           float defenseratio =0;
            if(defensiveBuildings+possibleoffensiveBuilding >0)
                defenseratio = (float)(defensiveBuildings + 1) / (float)(defensiveBuildings + possibleoffensiveBuilding + 1);
            SystemCommander SC;
            bool needDefense =false;
            
            if (this.Owner.data.TaxRate > .5f)
                makingMoney = false;
            //dont scrap buildings if we can use treasury to pay for it. 
            if (building.AllowInfantry && !this.BuildingList.Contains(building) && (this.AllowInfantry || governor == ColonyType.Military))
                return false;

            //determine defensive needs.
            if (this.Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(this.system, out SC))
            {
                if (makingMoney)
                    needDefense = SC.RankImportance >= defenseratio *10; ;// / (defensiveBuildings + offensiveBuildings+1)) >defensiveNeeds;
            }
            
            if (!string.IsNullOrEmpty(building.ExcludesPlanetType) && building.ExcludesPlanetType == this.Type)
                return false;
            

            if (itsHere && building.Unique && (makingMoney || building.Maintenance < this.Owner.Money * .001))
                return true;

            if (building.PlusTaxPercentage * this.GrossMoneyPT >= building.Maintenance 
                || building.CreditsPerColonist * (this.Population / 1000f) >= building.Maintenance 

                
                ) 
                return true;
            if (building.Name == "Outpost" || building.WinsGame  )
                return true;
            //dont build +food if you dont need to
            if (this.Owner.data.Traits.Cybernetic <= 0 && building.PlusFlatFoodAmount > 0)// && this.Fertility == 0)
            {

                if (this.NetFoodPerTurn > 0 && this.FarmerPercentage < .3 || this.BuildingList.Contains(building))

                    return false;
                else
                    return true;
               
            }
            if(income > building.Maintenance && this.Population >=1000 ) 
            {
                if (building.PlusFoodPerColonist > 0 && this.FarmerPercentage > .5f && this.Fertility >= 1)
                {
                    return true;
                }

            }
            if(this.Owner.data.Traits.Cybernetic >0)
            {
                if(this.NetProductionPerTurn-consumption <0)
                {
                    if(building.PlusFlatProductionAmount >0 && (this.WorkerPercentage >.5 || income >building.Maintenance*2))
                    {
                        return true;
                    }
                    if (building.PlusProdPerColonist > 0 && building.PlusProdPerColonist * (this.Population / 1000) > building.Maintenance *(2-this.WorkerPercentage))
                    {
                        if (income > this.ShipBuildingModifier * 2)
                            return true;

                    }
                    if (building.PlusProdPerRichness * this.MineralRichness > building.Maintenance )
                        return true;
                }
            }
            if(building.PlusTerraformPoints >0)
            {
                if (!makingMoney || this.Owner.data.Traits.Cybernetic>0||this.BuildingList.Contains(building) || this.BuildingInQueue(building.Name))
                    return false;
                
            }
            if(!makingMoney || this.developmentLevel <3)
            {
                if (building.Icon == "Biospheres")
                    return false;
            }
                
            bool iftrue = false;
            switch  (governor)
            {
                case ColonyType.Agricultural:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && this.GetMaxProductionPotential()>20 )
                        {
                            return true;
                        }
                        if (this.Fertility > 0 && building.MinusFertilityOnBuild > 0 && this.Owner.data.Traits.Cybernetic <=0)
                            return false;
                        if (HighPri)
                        {
                            if (building.PlusFlatFoodAmount > 0
                                || (building.PlusFoodPerColonist > 0 && this.Population > 500f)
                                
                                //|| this.developmentLevel > 4
                                || ((building.MaxPopIncrease > 0
                                || building.PlusFlatPopulation > 0 || building.PlusTerraformPoints > 0) && this.Population > this.MaxPopulation * .5f)
                                || building.PlusFlatFoodAmount > 0
                                || building.PlusFlatProductionAmount > 0
                                || building.StorageAdded > 0 
                               // || (this.Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount>0))
                                || (needDefense && isdefensive && this.developmentLevel > 3)
                                )
                                return true;
                                //iftrue = true;
                            
                        }
                        if (!iftrue && MedPri && this.developmentLevel > 2 && makingMoney)
                        {
                            if (
                                building.Name == "Biospheres"||
                                ( building.PlusTerraformPoints > 0 && this.Fertility <3)
                                || building.MaxPopIncrease > 0 
                                || building.PlusFlatPopulation > 0
                                || this.developmentLevel > 3
                                  || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && this.MaxPopulation > 999)
                                || (needDefense && isdefensive )

                                )
                                return true;
                        }
                        if (LowPri && this.developmentLevel > 4 && makingMoney)
                        {
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Core:
                    #region MyRegion
                    {
                        if (this.Fertility > 0 && building.MinusFertilityOnBuild > 0 && this.Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {

                            if (building.StorageAdded > 0
                                || (this.Owner.data.Traits.Cybernetic <=0 && (building.PlusTerraformPoints > 0 && this.Fertility < 1) && this.MaxPopulation > 2000)
                                || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && this.Population == this.MaxPopulation && income > building.Maintenance)                             
                                || (this.Owner.data.Traits.Cybernetic <=0 && building.PlusFlatFoodAmount > 0)
                                || (this.Owner.data.Traits.Cybernetic <=0 && building.PlusFoodPerColonist > 0)                                
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness >0
                                || building.PlusProdPerColonist >0
                                || building.PlusFlatResearchAmount>0
                                || (building.PlusResearchPerColonist>0 && this.Population / 1000 > 1)
                                //|| building.Name == "Biospheres"                                
                                
                                || (needDefense && isdefensive && this.developmentLevel >3)                                
                                || (this.Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                return true;
                        }
                        if (MedPri && this.developmentLevel > 3 &&makingMoney )
                        {
                            if (this.developmentLevel > 2 && needDefense && (building.theWeapon != null || building.Strength > 0))
                                return true;
                            iftrue = true;
                        }
                        if (!iftrue && LowPri && this.developmentLevel > 4 && makingMoney && income > building.Maintenance)
                        {
                            
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion

                case ColonyType.Industrial:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && this.GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (HighPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0
                                || (this.Owner.data.Traits  .Cybernetic <=0 && this.Fertility < 1f && building.PlusFlatFoodAmount > 0)                             
                                || building.StorageAdded > 0
                                || (needDefense && isdefensive && this.developmentLevel > 3)
                                )
                                return true;
                        }
                        if (MedPri && this.developmentLevel > 2 && makingMoney)
                        {
                            if (building.PlusResearchPerColonist * this.Population/1000 >building.Maintenance
                            || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && this.Population == this.MaxPopulation && income > building.Maintenance)
                            || (this.Owner.data.Traits.Cybernetic <= 0 && building.PlusTerraformPoints > 0 && this.Fertility < 1 && this.Population == this.MaxPopulation && this.MaxPopulation > 2000 && income>building.Maintenance)
                               || (building.PlusFlatFoodAmount > 0 && this.NetFoodPerTurn <0)
                                ||building.PlusFlatResearchAmount >0
                                || (building.PlusResearchPerColonist >0 && this.MaxPopulation >999)
                                )
                               
                            {
                                iftrue = true;
                            }

                        }
                        if (!iftrue && LowPri && this.developmentLevel > 3 && makingMoney && income >building.Maintenance)
                        {
                            if (needDefense && isdefensive && this.developmentLevel >2)
                                return true;
                            
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Military:
                    #region MyRegion
                    {
                        if (this.Fertility > 0 && building.MinusFertilityOnBuild > 0 && this.Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {
                            if (building.isWeapon
                                || building.IsSensor
                                || building.Defense > 0
                                || (this.Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (this.MineralRichness < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlanetaryShieldStrengthAdded > 0
                                || (building.AllowShipBuilding  && this.GrossProductionPerTurn >1)
                                || (building.ShipRepair > 0&& this.GrossProductionPerTurn >1)
                                || building.Strength > 0
                                || (building.AllowInfantry && this.GrossProductionPerTurn >1)
                                || needDefense &&(building.theWeapon !=null || building.Strength >0)
                                || (this.Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                iftrue = true;
                        }
                        if (!iftrue && MedPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0)
                                iftrue = true;
                        }
                        if (!iftrue && LowPri && this.developmentLevel > 4)
                        {
                            //if(building.Name!= "Biospheres")
                            iftrue = true;

                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Research:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && this.GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (this.Fertility > 0 && building.MinusFertilityOnBuild > 0 && this.Owner.data.Traits.Cybernetic <= 0)
                            return false;

                        if (HighPri)
                        {
                            if (building.PlusFlatResearchAmount > 0
                                || (this.Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (this.Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlusFlatProductionAmount >0
                                || building.PlusResearchPerColonist > 0
                                || (this.Owner.data.Traits.Cybernetic > 0 && (building.PlusFlatProductionAmount > 0 || building.PlusProdPerColonist > 0 ))
                                || (needDefense && isdefensive && this.developmentLevel > 3)
                                )
                                return true;

                        }
                        if ( MedPri && this.developmentLevel > 3 && makingMoney)
                        {
                            if (((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && this.Population > this.MaxPopulation * .5f)
                            || this.Owner.data.Traits.Cybernetic <=0 &&( (building.PlusTerraformPoints > 0 && this.Fertility < 1 && this.Population > this.MaxPopulation * .5f && this.MaxPopulation > 2000)
                                || (building.PlusFlatFoodAmount > 0 && this.NetFoodPerTurn < 0))
                                )
                                return true;
                        }
                        if ( LowPri && this.developmentLevel > 4 && makingMoney)
                        {
                            if (needDefense && isdefensive && this.developmentLevel >2)
                                
                            return true;
                        }
                        break;
                    } 
                    #endregion
            }
            return iftrue;

        }
        private void SetExportState(ColonyType colonyType)
        {

            bool FSexport = false;
            bool PSexport = false;
            int pc = this.Owner.GetPlanets().Count;
            
            
            
            
            bool exportPSFlag = true;
            bool exportFSFlag = true;
             float exportPTrack = this.Owner.exportPTrack;
         float exportFTrack = this.Owner.exportFTrack;
     
            
            //foreach (Planet planet in this.Owner.GetPlanets())
            //{
            //    pc++;
            //    //                if (this.ExportPSWeight < 0 || this.ExportFSWeight < 0)
            //    if (planet.fs == GoodState.IMPORT)
            //    {
            //        importFSNeed += planet.MAX_STORAGE - planet.FoodHere;
            //        FSexport = true;
            //    }
            //    if (planet.fs == GoodState.EXPORT)
            //        exportFSNeed += planet.FoodHere * (this.ExportFSWeight * -.01f);

            //    //if (planet.ExportFSWeight < exportFTrack)
            //    exportFTrack += planet.ExportFSWeight;
            //    if (planet.ps == GoodState.IMPORT)
            //    {
            //        importPSNeed += planet.MAX_STORAGE - planet.ProductionHere;
            //        PSexport = true;
            //    }
            //    if (planet.ps == GoodState.EXPORT)
            //        exportPSNeed += planet.ProductionHere * (this.ExportFSWeight * -.01f);

            //    //if (planet.ExportPSWeight < exportPTrack)
            //    exportPTrack += planet.ExportPSWeight;
            //    Storage += planet.MAX_STORAGE;


            //}
            if (pc == 1)
            {
                FSexport = false;
                PSexport = false;
            }
            exportFSFlag = exportFTrack / pc *2 >= this.ExportFSWeight;
            exportPSFlag = exportPTrack / pc *2  >= this.ExportPSWeight;
            
            if (!exportFSFlag || this.Owner.averagePLanetStorage >= this.MAX_STORAGE ) //|| this.ExportFSWeight > exportFTrack //exportFSNeed <= 0 ||
                FSexport = true;
            
            if ( !exportPSFlag || this.Owner.averagePLanetStorage >= this.MAX_STORAGE ) //|| this.ExportPSWeight > exportPTrack //exportPSNeed <= 0 ||
                PSexport = true;
            //this.ExportFSWeight = 0;
            //this.ExportPSWeight = 0;
            float PRatio = this.ProductionHere /this.MAX_STORAGE;
            float FRatio = this.FoodHere /this.MAX_STORAGE;

            int queueCount = this.ConstructionQueue.Count;
            switch (colonyType)
            {
                
                case ColonyType.Colony:               
                case ColonyType.Industrial:
                    if (this.Population >=1000 &&this.MaxPopulation >= this.Population)
                    {
                        if (PRatio < .9 && queueCount > 0) //&& FSexport
                            this.ps = GoodState.IMPORT;
                        else if (queueCount == 0)
                        {
                            this.ps = GoodState.EXPORT;
                        }
                        else
                            this.ps = GoodState.STORE;

                    }
                    else if (queueCount > 0 || this.Owner.data.Traits.Cybernetic > 0)
                    {
                        if (PRatio < .5f ) //&& PSexport
                            this.ps = GoodState.IMPORT;
                        else if (!PSexport && PRatio >.5)
                            this.ps = GoodState.EXPORT;
                        else
                            this.ps = GoodState.STORE;
                    } 
                    else
                    {
                        if (PRatio > .5f && !PSexport)
                            this.ps = GoodState.EXPORT;
                        else if (PRatio > .5f && PSexport)
                            this.ps = GoodState.STORE;
                        else this.ps = GoodState.EXPORT;
                        
                    }
                    
                if(this.NetFoodPerTurn <0)
                        this.fs = Planet.GoodState.IMPORT;
                    else if (FRatio > .75f)
                        this.fs = Planet.GoodState.STORE;
                    else
                        this.fs = Planet.GoodState.IMPORT;
                break;

                    
                case ColonyType.Agricultural:
                if (PRatio > .75 && !PSexport)
                    this.ps = Planet.GoodState.EXPORT;
                else if (PRatio < .5 && PSexport)
                    this.ps = Planet.GoodState.IMPORT;
                else
                    this.ps = GoodState.STORE;


                if (this.NetFoodPerTurn >0 )
                    this.fs = Planet.GoodState.EXPORT;
                else if (this.NetFoodPerTurn < 0)
                        this.fs = Planet.GoodState.IMPORT;
                    else if(FRatio> .75f )
                    this.fs = Planet.GoodState.STORE;
                else
                    this.fs = Planet.GoodState.IMPORT;

                break;
                
                case ColonyType.Research:

                    {
                        if (PRatio > .75f && !PSexport)
                            this.ps = Planet.GoodState.EXPORT;
                        else if (PRatio < .5f) //&& PSexport
                            this.ps = Planet.GoodState.IMPORT;
                        else
                            this.ps = GoodState.STORE;

                        if (this.NetFoodPerTurn < 0)
                            this.fs = Planet.GoodState.IMPORT;
                        else if (this.NetFoodPerTurn < 0)
                            this.fs = Planet.GoodState.IMPORT;
                        else
                        if (FRatio > .75f && !FSexport)
                            this.fs = Planet.GoodState.EXPORT;
                        else if ( FRatio < .75) //FSexport &&
                            this.fs = Planet.GoodState.IMPORT;
                        else
                            this.fs = GoodState.STORE;

                        break;
                    }

                case ColonyType.Core:                
                if(this.MaxPopulation > this.Population *.75f && this.Population >this.developmentLevel *1000)
                {

                    if (PRatio > .33f )
                        this.ps = GoodState.EXPORT;
                    else if ( PRatio < .33 )
                        this.ps = GoodState.STORE;
                    else
                        this.ps = GoodState.IMPORT;
                }
                else
                {
                    if (PRatio > .75 && !FSexport)
                        this.ps = GoodState.EXPORT;
                    else if (PRatio < .5) //&& FSexport
                            this.ps = GoodState.IMPORT;
                    else this.ps = GoodState.STORE;
                }

                    if (this.NetFoodPerTurn < 0)
                        this.fs = Planet.GoodState.IMPORT;
                    else if(FRatio > .25)
                    this.fs = GoodState.EXPORT;
                else if (this.NetFoodPerTurn > this.developmentLevel * .5)
                    this.fs = GoodState.STORE;
                else
                    this.fs = GoodState.IMPORT;
                        

                break;
                case ColonyType.Military:
                case ColonyType.TradeHub:
                if (this.fs != GoodState.STORE)
                    if (FRatio > .50)
                        this.fs = GoodState.EXPORT;
                    else
                        this.fs = GoodState.IMPORT;
                if (this.ps != GoodState.STORE)
                    if (PRatio > .50)
                        this.ps = GoodState.EXPORT;
                    else
                        this.ps = GoodState.IMPORT;

                break;

                default:
                    break;
            }
            if(!PSexport)
                this.PSexport = true;
            else
            {
                this.PSexport = false;
            }

            //if(!FSexport)
            //    this.FSexport = true;
            //else
            //{
            //    this.FSexport = false;
            //}
            //if(this.developmentLevel>1 && this.ps == GoodState.EXPORT && !PSexport)
            //{
                
            //    this.ps = GoodState.STORE;
            //}

            //if(this.developmentLevel>1 && this.fs == GoodState.EXPORT && !FSexport)
            //{
                
            //    this.fs = GoodState.STORE;
            //}

           
        }
        public void DoGoverning()
        {

            float income = this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn;
            if (this.colonyType == Planet.ColonyType.Colony)
                return;
            this.GetBuildingsWeCanBuildHere();
            Building cheapestFlatfood =
                this.BuildingsCanBuild.Where(flatfood => flatfood.PlusFlatFoodAmount > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
 
            Building cheapestFlatprod = this.BuildingsCanBuild.Where(flat => flat.PlusFlatProductionAmount > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
            Building cheapestFlatResearch = this.BuildingsCanBuild.Where(flat => flat.PlusFlatResearchAmount > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
            if (this.Owner.data.Traits.Cybernetic > 0)
            {
                cheapestFlatfood = cheapestFlatprod;// this.BuildingsCanBuild.Where(flat => flat.PlusProdPerColonist > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
            }
            Building pro = cheapestFlatprod;
            Building food = cheapestFlatfood;
            Building res = cheapestFlatResearch;
            bool noMoreBiospheres = true;
            foreach(PlanetGridSquare pgs in this.TilesList)
            {
                if(pgs.Habitable)
                    continue;
                noMoreBiospheres = false;
                break;
            }
            int buildingsinQueue = this.ConstructionQueue.Where(isbuilding => isbuilding.isBuilding).Count();
            bool needsBiospheres = this.ConstructionQueue.Where(isbuilding => isbuilding.isBuilding && isbuilding.Building.Name == "Biospheres").Count() != buildingsinQueue;
            bool StuffInQueueToBuild = this.ConstructionQueue.Count >0;// .Where(building => building.isBuilding || (building.Cost - building.productionTowards > this.ProductionHere)).Count() > 0;
            bool ForgetReseachAndBuild =
     string.IsNullOrEmpty(this.Owner.ResearchTopic) || StuffInQueueToBuild || (this.developmentLevel < 3 && (this.ProductionHere + 1) / (this.MAX_STORAGE + 1) < .9f);
            if (this.colonyType == ColonyType.Research && string.IsNullOrEmpty(this.Owner.ResearchTopic))
            {
                this.colonyType = ColonyType.Industrial;
            }
            if ( !true && this.Owner.data.Traits.Cybernetic < 0) //no longer needed
            #region cybernetic
            {

                this.FarmerPercentage = 0.0f;

                float surplus = this.GrossProductionPerTurn - this.consumption;
                surplus = surplus * (1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                this.WorkerPercentage = this.CalculateCyberneticPercentForSurplus(surplus);
                if ((double)this.WorkerPercentage > 1.0)
                    this.WorkerPercentage = 1f;
                this.ResearcherPercentage = 1f - this.WorkerPercentage;
                if (this.ResearcherPercentage < 0f)
                    ResearcherPercentage = 0f;
                //if (this.ProductionHere > this.MAX_STORAGE * 0.25f && (double)this.GetNetProductionPerTurn() > 1.0)// &&
                //    this.ps = Planet.GoodState.EXPORT;
                //else
                //    this.ps = Planet.GoodState.IMPORT;
                float buildingCount = 0.0f;
                foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                {
                    if (queueItem.isBuilding)
                        ++buildingCount;
                    if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                        ++buildingCount;
                }
                bool flag1 = true;
                foreach (Building building in this.BuildingList)
                {
                    if (building.Name == "Outpost" || building.Name == "Capital City")
                        flag1 = false;
                }
                if (flag1)
                {
                    bool flag2 = false;
                    foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                    {
                        if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                        {
                            flag2 = true;
                            break;
                        }
                    }
                    if (!flag2)
                        this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"),false);
                }
                bool flag3 = false;
                foreach (Building building1 in this.BuildingsCanBuild)
                {
                    if ((double)building1.PlusFlatProductionAmount > 0.0
                        || (double)building1.PlusProdPerColonist > 0.0
                        || (building1.Name == "Space Port" || (double)building1.PlusProdPerRichness > 0.0) || building1.Name == "Outpost")
                    {
                        int num2 = 0;
                        foreach (Building building2 in this.BuildingList)
                        {
                            if (building2 == building1)
                                ++num2;
                        }
                        flag3 = num2 <= 9;
                        break;
                    }
                }
                bool flag4 = true;
                if (flag3)
                {
                    foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                    {
                        if (queueItem.isBuilding
                            && ((double)queueItem.Building.PlusFlatProductionAmount > 0.0
                            || (double)queueItem.Building.PlusProdPerColonist > 0.0
                            || (double)queueItem.Building.PlusProdPerRichness > 0.0))
                        {
                            flag4 = false;
                            break;
                        }
                    }
                }
                if (this.Owner != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty)
                    && this.Shipyards.Where(ship => ship.Value.GetShipData().IsShipyard).Count() == 0
                    && this.Owner.ShipsWeCanBuild.Contains(this.Owner.data.DefaultShipyard) && (double)this.GrossMoneyPT > 5.0
                    && (double)this.NetProductionPerTurn > 6.0)
                {
                    bool hasShipyard = false;
                    foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                    {
                        if (queueItem.isShip && queueItem.sData.IsShipyard)
                        {
                            hasShipyard = true;
                            break;
                        }
                    }
                    if (!hasShipyard)
                        this.ConstructionQueue.Add(new QueueItem()
                        {
                            isShip = true,
                            sData = ResourceManager.ShipsDict[this.Owner.data.DefaultShipyard].GetShipData(),
                            Cost = ResourceManager.ShipsDict[this.Owner.data.DefaultShipyard].GetCost(this.Owner)
                        });
                }
                if ((double)buildingCount < 2.0 && flag4)
                {
                    this.GetBuildingsWeCanBuildHere();
                    Building b = (Building)null;
                    float num2 = 99999f;
                    foreach (Building building in this.BuildingsCanBuild)
                    {
                        if ((building.PlusTerraformPoints <= 0.0) //(building.Name == "Terraformer") 
                            && building.PlusFlatFoodAmount <= 0.0f
                            && (building.PlusFoodPerColonist <= 0.0f && !(building.Name == "Biospheres"))
                            && (building.PlusFlatPopulation <= 0.0f || this.Population / this.MaxPopulation <= 0.25f))
                        {
                            if (building.PlusFlatProductionAmount > 0.0f
                                || building.PlusProdPerColonist > 0.0f
                                || building.PlusTaxPercentage > 0.0f
                                || building.PlusProdPerRichness > 0.0f
                                || building.CreditsPerColonist > 0.0f
                                || (building.Name == "Space Port" || building.Name == "Outpost"))
                            {
                                if ((building.Cost + 1) / (this.GetNetProductionPerTurn() + 1) < 150 || this.ProductionHere > building.Cost * .5) //(building.Name == "Space Port") &&
                                {
                                    float num3 = building.Cost;
                                    b = building;
                                    break;
                                }
                            }
                            else if (building.Cost < num2 && (!(building.Name == "Space Port") || this.BuildingList.Count >= 2) || ((building.Cost + 1) / (this.GetNetProductionPerTurn() + 1) < 150 || this.ProductionHere > building.Cost * .5))
                            {
                                num2 = building.Cost;
                                b = building;
                            }
                        }
                    }
                    if (b != null && (this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn > 0.0f || (b.CreditsPerColonist > 0 || this.PlusTaxPercentage > 0)))//((double)this.Owner.EstimateIncomeAtTaxRate(0.4f) - (double)b.Maintenance > 0.0 ))
                    {
                        bool flag2 = true;
                        if (b.BuildOnlyOnce)
                        {
                            for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                            {
                                if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                {
                                    flag2 = false;
                                    break;
                                }
                            }
                        }
                        if (flag2)
                            this.AddBuildingToCQ(b,false);
                    }
                    else if (buildingCount < 2.0 && this.Owner.GetBDict()["Biospheres"] && this.MineralRichness >= .5f)
                    {
                        if (this.Owner == Empire.Universe.PlayerEmpire)
                        {
                            if (this.Population / (this.MaxPopulation + this.MaxPopBonus) > 0.949999f && (this.Owner.EstimateIncomeAtTaxRate(this.Owner.data.TaxRate) -  ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || this.Owner.Money > this.Owner.GrossTaxes * 3))
                                this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                        }
                        else if (this.Population / (this.MaxPopulation + this.MaxPopBonus) > 0.949999988079071 && (this.Owner.EstimateIncomeAtTaxRate(0.5f) -  ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || this.Owner.Money > this.Owner.GrossTaxes * 3))
                            this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                    }
                }
                for (int index = 0; index < this.ConstructionQueue.Count; ++index)
                {
                    QueueItem queueItem1 = this.ConstructionQueue[index];
                    if (index == 0 && queueItem1.isBuilding && this.ProductionHere > this.MAX_STORAGE * .5)
                    {
                        if (queueItem1.Building.Name == "Outpost"
                            ||  queueItem1.Building.PlusFlatProductionAmount > 0.0f
                            ||  queueItem1.Building.PlusProdPerRichness > 0.0f
                            ||  queueItem1.Building.PlusProdPerColonist > 0.0f
                            //|| (double)queueItem1.Building.PlusTaxPercentage > 0.0
                            //|| (double)queueItem1.Building.CreditsPerColonist > 0.0
                            )
                        {
                            this.ApplyAllStoredProduction(0);
                        }
                        break;
                    }
                    else if (queueItem1.isBuilding
                        && ( queueItem1.Building.PlusFlatProductionAmount > 0.0f
                        ||   queueItem1.Building.PlusProdPerColonist > 0.0f
                        || (queueItem1.Building.Name == "Outpost"
                        ||  queueItem1.Building.PlusProdPerRichness > 0.0f
                        //|| queueItem1.Building.PlusFlatFoodAmount >0f
                        )))
                    {
                        LinkedList<QueueItem> linkedList = new LinkedList<QueueItem>();
                        foreach (QueueItem queueItem2 in (List<QueueItem>)this.ConstructionQueue)
                            linkedList.AddLast(queueItem2);
                        linkedList.Remove(queueItem1);
                        linkedList.AddFirst(queueItem1);
                        this.ConstructionQueue.Clear();
                        foreach (QueueItem queueItem2 in linkedList)
                            this.ConstructionQueue.Add(queueItem2);
                    }
                }
            }
            #endregion
            else
            {

                switch (this.colonyType)
                {
                    case Planet.ColonyType.Core:
                        #region Core
                        {
                            #region Resource control
                            //Determine Food needs first
                            //if (this.DetermineIfSelfSufficient())
                            #region MyRegion
                            {
                                //this.fs = GoodState.EXPORT;
                                //Determine if excess food
                               
                                float surplus = (this.NetFoodPerTurn * (string.IsNullOrEmpty(this.Owner.ResearchTopic) ? 1 : .5f)) * (1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                                if(this.Owner.data.Traits.Cybernetic >0)
                                {
                                    surplus = this.GrossProductionPerTurn - this.consumption;
                                    surplus = surplus * ((string.IsNullOrEmpty(this.Owner.ResearchTopic) ? 1 : .5f)) * (1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                                        //(1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                                }
                                this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(surplus);
                                if ( FarmerPercentage == 1 && StuffInQueueToBuild)
                                    this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0);
                                if (this.FarmerPercentage == 1 && StuffInQueueToBuild)
                                    this.FarmerPercentage = .9f;
                                this.WorkerPercentage =
                                (1f - this.FarmerPercentage) *
                                (ForgetReseachAndBuild ? 1 : (1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1)));
   
                                float Remainder = 1f - FarmerPercentage;
                                //Research is happening
                                this.WorkerPercentage = (Remainder * (string.IsNullOrEmpty(this.Owner.ResearchTopic) ? 1 : (1 - (this.ProductionHere ) / (this.MAX_STORAGE ))));
                                if (this.ProductionHere / this.MAX_STORAGE > .9 && !StuffInQueueToBuild)
                                    this.WorkerPercentage = 0;
                                this.ResearcherPercentage = Remainder - this.WorkerPercentage;
                                if (this.Owner.data.Traits.Cybernetic > 0)
                                {
                                    this.WorkerPercentage += this.FarmerPercentage;
                                    this.FarmerPercentage = 0;
                                }
                            }
                            #endregion
                            this.SetExportState(this.colonyType);
                            //if (this.NetProductionPerTurn > 3f || this.developmentLevel > 2)
                            //{
                            //    if (this.ProductionHere > this.MAX_STORAGE * 0.33f)
                            //        this.ps = Planet.GoodState.EXPORT;
                            //    else if (this.ConstructionQueue.Count == 0)
                            //        this.ps = Planet.GoodState.STORE;
                            //    else
                            //        this.ps = Planet.GoodState.IMPORT;
                            //}
                            ////Not enough production or development
                            //else if (MAX_STORAGE * .75f > this.ProductionHere)
                            //{
                            //    this.ps = Planet.GoodState.IMPORT;
                            //}
                            //else if (MAX_STORAGE == this.ProductionHere)
                            //{
                            //    this.ps = GoodState.EXPORT;
                            //}
                            //else
                            //    this.ps = GoodState.STORE;
                            if (this.Owner != Empire.Universe.PlayerEmpire
                                && this.Shipyards.Where(ship => ship.Value.GetShipData().IsShipyard).Count() == 0
                                && this.Owner.ShipsWeCanBuild.Contains(this.Owner.data.DefaultShipyard)

                                )
                            // && (double)this.Owner.MoneyLastTurn > 5.0 && (double)this.NetProductionPerTurn > 4.0)
                            {
                                bool hasShipyard = false;
                                foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                                {
                                    if (queueItem.isShip && queueItem.sData.IsShipyard)
                                    {
                                        hasShipyard = true;
                                        break;
                                    }
                                }
                                if (!hasShipyard && this.developmentLevel > 2)
                                    this.ConstructionQueue.Add(new QueueItem()
                                    {
                                        isShip = true,
                                        sData = ResourceManager.ShipsDict[this.Owner.data.DefaultShipyard].GetShipData(),
                                        Cost = ResourceManager.ShipsDict[this.Owner.data.DefaultShipyard].GetCost(this.Owner) * UniverseScreen.GamePaceStatic
                                    });
                            }
                            #endregion
                            byte num5 = 0;
                            bool flag5 = false;
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name != "Biospheres")
                                    ++num5;
                                if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                    ++num5;
                                if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                    flag5 = true;
                            }
                            bool flag6 = true;
                            foreach (Building building in this.BuildingList)
                            {
                                if (building.Name == "Outpost" || building.Name == "Capital City")
                                    flag6 = false;
                                if (building.Name == "Terraformer")
                                    flag5 = true;
                            }
                            if (flag6)
                            {
                                bool flag1 = false;
                                foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                                {
                                    if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                    {
                                        flag1 = true;
                                        break;
                                    }
                                }
                                if (!flag1)
                                    this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"),false);
                            }
                            if (num5 < 2)
                            {
                                this.GetBuildingsWeCanBuildHere();

                                foreach (PlanetGridSquare PGS in this.TilesList)
                                {
                                    bool qitemTest = PGS.QItem != null;
                                    if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                        pro = null;
                                    if (PGS.building != cheapestFlatfood && !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                        food = cheapestFlatfood;

                                    if (PGS.building != cheapestFlatResearch && !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                        res = cheapestFlatResearch;

                                }

                                Building buildthis = null;
                                buildthis = pro;
                                buildthis = pro ?? food ?? res;

                                if (buildthis != null)
                                {
                                    num5++;
                                    this.AddBuildingToCQ(buildthis,false);
                                }

                            }
                            if (num5 < 2)
                            {
                                float coreCost = 99999f;
                                this.GetBuildingsWeCanBuildHere();
                                Building b = (Building)null;
                                foreach (Building building in this.BuildingsCanBuild)
                                {
                                    if (!WeCanAffordThis(building, this.colonyType))
                                        continue;
                                    //if you dont want it to be built put it here.
                                    //this first if is the low pri build spot. 
                                    //the second if will override items that make it through this if. 
                                    if (cheapestFlatfood == null && cheapestFlatprod == null &&
                                        //(building.PlusFlatPopulation <= 0.0f || this.Population <= 1000.0)
                                        //&& 
                                        ( (building.MinusFertilityOnBuild <= 0.0f ||this.Owner.data.Traits.Cybernetic > 0) && !(building.Name == "Biospheres") )
                                        //&& (!(building.Name == "Terraformer") || !flag5 && this.Fertility < 1.0)
                                        && ( building.PlusTerraformPoints < 0 || !flag5 && (this.Fertility < 1.0 && this.Owner.data.Traits.Cybernetic <= 0 ))

                                        //&& (building.PlusFlatPopulation <= 0.0
                                        //|| (this.Population / this.MaxPopulation <= 0.25 && this.developmentLevel >2 && !noMoreBiospheres))
                                        //||(this.Owner.data.Traits.Cybernetic >0 && building.PlusProdPerRichness >0)
                                        )
                                    {

                                        b = building;
                                        coreCost = b.Cost;
                                        break;
                                    }
                                    else if (building.Cost < coreCost && ((building.Name != "Biospheres" && building.PlusTerraformPoints <=0 ) || this.Population / this.MaxPopulation <= 0.25 && this.developmentLevel > 2 && !noMoreBiospheres))
                                    {
                                        b = building;
                                        coreCost = b.Cost;
                                    }
                                }
                                //if you want it to be built with priority put it here.
                                if (b != null &&// cheapestFlatfood == null && cheapestFlatprod == null &&
                                   (//b.CreditsPerColonist > 0 || b.PlusTaxPercentage > 0
                                   //||
                                    b.PlusFlatProductionAmount > 0 || b.PlusProdPerRichness > 0 || b.PlusProdPerColonist > 0
                                   || b.PlusFoodPerColonist > 0 || b.PlusFlatFoodAmount > 0
                                   || b.CreditsPerColonist >0 || b.PlusTaxPercentage >0
                                   || cheapestFlatfood == b || cheapestFlatprod ==b || cheapestFlatResearch ==b
                                   //|| b.PlusFlatResearchAmount > 0 || b.PlusResearchPerColonist > 0
                                   //|| b.StorageAdded > 0
                                   ))//&& !b.AllowShipBuilding)))//  ((double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0 || this.Owner.Money > this.Owner.GrossTaxes * 3)) //this.WeCanAffordThis(b,this.colonyType)) //
                                {
                                    bool flag1 = true;
                                    if (b.BuildOnlyOnce)
                                    {
                                        for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                                        {
                                            if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                            {
                                                flag1 = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (flag1)
                                        this.AddBuildingToCQ(b,false);
                                }
                                    //if it must be built with high pri put it here. 
                                else if (b != null
                                    //&& ((double)b.PlusFlatProductionAmount > 0.0 || (double)b.PlusProdPerColonist > 0.0)
                                    // && WeCanAffordThis(b,this.colonyType)
                                    )
                                {
                                    bool flag1 = true;
                                    if (b.BuildOnlyOnce)
                                    {
                                        for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                                        {
                                            if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                            {
                                                flag1 = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (flag1)
                                        this.AddBuildingToCQ(b);
                                }
                                else if (this.Owner.GetBDict()["Biospheres"] &&  this.MineralRichness >= 1.0f && ((this.Owner.data.Traits.Cybernetic > 0 && this.GrossProductionPerTurn > this.consumption) || this.Owner.data.Traits.Cybernetic <=0 &&  this.Fertility >= 1.0))
                                {
                                    if (this.Owner == Empire.Universe.PlayerEmpire)
                                    {
                                        if ( this.Population / ( this.MaxPopulation +  this.MaxPopBonus) > 0.94999f && ( this.Owner.EstimateIncomeAtTaxRate(this.Owner.data.TaxRate) - ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || this.Owner.Money > this.Owner.GrossTaxes * 3))
                                            this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                                    }
                                    else if ( this.Population / ( this.MaxPopulation +  this.MaxPopBonus) > 0.94999f && ( this.Owner.EstimateIncomeAtTaxRate(0.5f) -  ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || this.Owner.Money > this.Owner.GrossTaxes * 3))
                                        this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                                }
                            }

                            for (int index = 0; index < this.ConstructionQueue.Count; ++index)
                            {
                                QueueItem queueItem1 = this.ConstructionQueue[index];
                                if (index == 0 && queueItem1.isBuilding)
                                {
                                    if (queueItem1.Building.Name == "Outpost" ) //|| (double)queueItem1.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem1.Building.PlusProdPerRichness > 0.0 || (double)queueItem1.Building.PlusProdPerColonist > 0.0)
                                    {
                                        this.ApplyAllStoredProduction(0);
                                    }
                                    break;
                                }
                                else if (queueItem1.isBuilding && ( queueItem1.Building.PlusFlatProductionAmount > 0.0f ||  queueItem1.Building.PlusProdPerColonist > 0.0f || queueItem1.Building.Name == "Outpost"))
                                {
                                    LinkedList<QueueItem> linkedList = new LinkedList<QueueItem>();
                                    foreach (QueueItem queueItem2 in (List<QueueItem>)this.ConstructionQueue)
                                        linkedList.AddLast(queueItem2);
                                    linkedList.Remove(queueItem1);
                                    linkedList.AddFirst(queueItem1);
                                    this.ConstructionQueue.Clear();
                                    foreach (QueueItem queueItem2 in linkedList)
                                        this.ConstructionQueue.Add(queueItem2);
                                }
                            }


                            break;
                        }
                        #endregion
                    case Planet.ColonyType.Industrial:
                        #region Industrial
                        //this.fs = Planet.GoodState.IMPORT;

                        this.FarmerPercentage = 0.0f;
                        this.WorkerPercentage = 1f;
                        this.ResearcherPercentage = 0.0f;

                        //? true : .75f;
                        //this.ps = (double)this.ProductionHere >= 20.0 ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                        float IndySurplus = (this.NetFoodPerTurn) *//(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? .5f : .25f)) * 
                            (1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                        if (this.Owner.data.Traits.Cybernetic > 0)
                        {
                            IndySurplus = this.GrossProductionPerTurn - this.consumption;
                            IndySurplus = IndySurplus * (1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                            //(1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                        }
                        //if ((double)this.FoodHere <= (double)this.consumption)
                        {

                            this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(IndySurplus);
                            this.FarmerPercentage *= (this.FoodHere / MAX_STORAGE) > .25 ? .5f : 1;
                            if ( FarmerPercentage == 1 && StuffInQueueToBuild)
                                this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0);
                            this.WorkerPercentage =
                                (1f - this.FarmerPercentage)   //(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? 1f :
                                * (ForgetReseachAndBuild ? 1 :
                             (1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1)));
                            if (this.ProductionHere / this.MAX_STORAGE >.75 && !StuffInQueueToBuild)
                                this.WorkerPercentage = 0;
 
                            this.ResearcherPercentage = 1 - this.FarmerPercentage - this.WorkerPercentage;// 0.0f;
                            if (this.Owner.data.Traits.Cybernetic > 0)
                            {
                                this.WorkerPercentage += this.FarmerPercentage;
                                this.FarmerPercentage = 0;
                            }
                        }
                        this.SetExportState(this.colonyType);
                        

                        float num6 = 0.0f;
                        foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
                                ++num6;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++num6;
                        }
                        bool flag7 = true;
                        foreach (Building building in this.BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                flag7 = false;
                        }
                        if (flag7)
                        {
                            bool flag1 = false;
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    flag1 = true;
                                    break;
                                }
                            }
                            if (!flag1)
                                this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"));
                        }

                        bool flag8 = false;
                        this.GetBuildingsWeCanBuildHere();
                        if (num6 < 2)
                        {

                            this.GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in this.TilesList)
                            {
                                bool qitemTest = PGS.QItem != null;
                                if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                    pro = null;
                                if (PGS.building != cheapestFlatfood && !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                    food = cheapestFlatfood;

                                if (PGS.building != cheapestFlatResearch && !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                    res = cheapestFlatResearch;

                            }
                            Building buildthis = null;
                            buildthis = pro;
                            buildthis = pro ?? food ?? res;

                            if (buildthis != null)
                            {
                                num6++;
                                this.AddBuildingToCQ(buildthis);
                            }

                        }
                        {


                            double num1 = 0;
                            foreach (Building building1 in this.BuildingsCanBuild)
                            {
                                
                                    if ( building1.PlusFlatProductionAmount > 0.0
                                        ||  building1.PlusProdPerColonist > 0.0
                                        ||  building1.PlusProdPerRichness > 0.0
                                        )
                                {

                                    foreach (Building building2 in this.BuildingList)
                                    {
                                        if (building2 == building1)
                                            ++num1;
                                    }
                                    flag8 = num1 <= 9;
                                    break;
                                }
                            }
                        }
                        bool flag9 = true;
                        if (flag8)
                        {
                            this.ConstructionQueue.thisLock.EnterReadLock();
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isBuilding
                                    && ( queueItem.Building.PlusFlatProductionAmount > 0.0
                                    ||  queueItem.Building.PlusProdPerColonist > 0.0
                                    ||  queueItem.Building.PlusProdPerRichness > 0.0)                                    
                                    )
                                {
                                    flag9 = false;
                                    break;
                                }
                            }
                            this.ConstructionQueue.thisLock.ExitReadLock();
                        }
                        if (flag9 &&  num6 < 2f)
                        {
                            float indycost = 99999f;
                            Building b = (Building)null;
                            foreach (Building building in this.BuildingsCanBuild)//.OrderBy(cost=> cost.Cost))
                            {
                                if (!this.WeCanAffordThis(building, this.colonyType))
                                    continue;
                                if ( building.PlusFlatProductionAmount > 0.0f
                                    ||  building.PlusProdPerColonist > 0.0f
                                    || ( building.PlusProdPerRichness > 0.0f
                                    

                                    )
                                    )//this.WeCanAffordThis(b,this.colonyType) )//
                                {
                                    indycost = building.Cost;
                                    b = building;
                                    break;
                                }
                                else if (indycost > building.Cost)//building.Name!="Biospheres" || developmentLevel >2 )
                                    indycost = building.Cost;
                                b = building;
                            }
                            if (b != null) //(this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn > 0.0 || (b.CreditsPerColonist > 0 || this.PlusTaxPercentage > 0))) // ((double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0 || this.Owner.Money > this.Owner.GrossTaxes * 3)) //this.WeCanAffordThis(b, this.colonyType)) //
                            {
                                bool flag1 = true;
                                if (b.BuildOnlyOnce)
                                {
                                    for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                                    {
                                        if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                        {
                                            flag1 = false;
                                            break;
                                        }
                                    }
                                }
                                if (flag1)
                                {
                                    this.AddBuildingToCQ(b);

                                    ++num6;
                                }
                            }
                        //    else if (b != null && ((double)b.PlusFlatProductionAmount > 0.0
                        //        || (double)b.PlusProdPerColonist > 0.0
                        //        || (double)b.PlusProdPerRichness > 0.0 && (this.MineralRichness > 1.5 || this.Owner.data.Traits.Cybernetic >0))) //this.WeCanAffordThis(b, this.colonyType))//
                        //    {
                        //        bool flag1 = true;
                        //        if (b.BuildOnlyOnce)
                        //        {
                        //            for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                        //            {
                        //                if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                        //                {
                        //                    flag1 = false;
                        //                    break;
                        //                }
                        //            }
                        //        }
                        //        if (flag1)
                        //            this.AddBuildingToCQ(b);
                        //    }
                        //}
                        //if ((double)num6 < 2)
                        //{
                        //    Building b = (Building)null;


                        //    float num1 = 99999f;
                        //    foreach (Building building in this.GetBuildingsWeCanBuildHere().OrderByDescending(industry => this.MaxPopulation < 4 ? industry.PlusFlatFoodAmount : industry.PlusFoodPerColonist).ThenBy(maintenance => maintenance.Maintenance))
                        //    {
                        //        if (this.WeCanAffordThis(building, this.colonyType) && (int)(building.Cost * .05f) < num1)   //
                        //        {
                        //            b = building;
                        //            num1 = (int)(building.Cost * .05f);

                        //        }

                        //    }
                        //    if (b != null)
                        //    {
                        //        bool flag1 = true;
                        //        if (b.BuildOnlyOnce)
                        //        {
                        //            for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                        //            {
                        //                if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                        //                {
                        //                    flag1 = false;
                        //                    break;
                        //                }
                        //            }
                        //        }
                        //        if (flag1)
                        //            this.AddBuildingToCQ(b);
                        //    }
                        }
                        break;
                        #endregion
                    case Planet.ColonyType.Research:
                        #region Research
                        //this.fs = Planet.GoodState.IMPORT;
                        //this.ps = Planet.GoodState.IMPORT;
                        this.FarmerPercentage = 0.0f;
                        this.WorkerPercentage = 0.0f;
                        this.ResearcherPercentage = 1f;
                        //StuffInQueueToBuild =
                        //    this.ConstructionQueue.Where(building => building.isBuilding
                        //        && (building.Building.PlusFlatFoodAmount > 0
                        //        || building.Building.PlusFlatProductionAmount > 0
                        //        || building.Building.PlusFlatResearchAmount > 0
                        //        || building.Building.PlusResearchPerColonist > 0
                        //        //|| building.Building.Name == "Biospheres"
                        //        )).Count() > 0;  //(building.Cost > this.NetProductionPerTurn * 10)).Count() > 0;
                        ForgetReseachAndBuild =
                            string.IsNullOrEmpty(this.Owner.ResearchTopic); //|| StuffInQueueToBuild; //? 1 : .5f;
                        //IndySurplus = 0;
                        //this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(IndySurplus);
                        //if (this.Owner.data.Traits.Cybernetic <= 0 && FarmerPercentage == 1 & StuffInQueueToBuild)
                        //    this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0);
                        //if (this.FarmerPercentage == 1 && StuffInQueueToBuild)
                        //    this.FarmerPercentage = .9f;
                        //this.WorkerPercentage =
                        //(1f - this.FarmerPercentage) *
                        //(ForgetReseachAndBuild ? 1 : ((1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1)) * .25f));
                        //if (this.ProductionHere / this.MAX_STORAGE > .9 && !StuffInQueueToBuild)
                        //    this.WorkerPercentage = 0;
                        //this.ResearcherPercentage = 1f - this.FarmerPercentage - this.WorkerPercentage;

                        IndySurplus = (this.NetFoodPerTurn) * ((this.MAX_STORAGE - this.FoodHere*2f ) / this.MAX_STORAGE);
    //(1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                        this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(IndySurplus);

                        this.WorkerPercentage =(1f - this.FarmerPercentage);

                        if (StuffInQueueToBuild)
                            this.WorkerPercentage *= ((this.MAX_STORAGE - this.ProductionHere) / this.MAX_STORAGE);
                        else
                            this.WorkerPercentage = 0;

                        this.ResearcherPercentage = 1f - this.FarmerPercentage - this.WorkerPercentage;

                        if (this.Owner.data.Traits.Cybernetic > 0)
                        {
                            this.WorkerPercentage += this.FarmerPercentage;
                            this.FarmerPercentage = 0;
                        }


                        if (this.Owner.data.Traits.Cybernetic > 0)
                        {
                            this.WorkerPercentage += this.FarmerPercentage;
                            this.FarmerPercentage = 0;
                        }
                        this.SetExportState(this.colonyType);
                        //    if ((double)this.FoodHere <= (double)this.consumption)
                        //{
                        //    this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.0f);
                        //    this.ResearcherPercentage = 1f - this.FarmerPercentage;
                        //}
                        float num8 = 0.0f;
                        foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                        {
                            if (queueItem.isBuilding )
                                ++num8;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++num8;
                        }
                        bool flag10 = true;
                        foreach (Building building in this.BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                flag10 = false;
                        }
                        if (flag10)
                        {
                            bool flag1 = false;
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    flag1 = true;
                                    break;
                                }
                            }
                            if (!flag1)
                                this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"));
                        }
                        if (num8 < 2.0)
                        {
                            this.GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in this.TilesList)
                            {
                                bool qitemTest = PGS.QItem != null;
                                if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                    pro = null;
                                if (PGS.building != cheapestFlatfood && !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                    food = cheapestFlatfood;

                                if (PGS.building != cheapestFlatResearch && !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                    res = cheapestFlatResearch;
                                

                            }
                            Building buildthis = null;
                            buildthis = pro;
                            buildthis = pro ?? food ?? res;

                            if (buildthis != null && WeCanAffordThis(buildthis, this.colonyType))
                            {
                                num8++;
                                this.AddBuildingToCQ(buildthis);
                            }
                        }
                        if (num8 < 2.0)
                        {
                            this.GetBuildingsWeCanBuildHere();
                            Building b = (Building)null;
                            float num1 = 99999f;
                            foreach (Building building in this.BuildingsCanBuild)
                            {
                                if (!WeCanAffordThis(building, this.colonyType))
                                    continue;
                                if (building.Name == "Outpost") //this.WeCanAffordThis(building,this.colonyType)) //
                                {
                                    //float num2 = building.Cost;
                                    b = building;
                                    break;
                                }
                                else if (num8 <2 && building.Cost < num1 && (building.Name != "Biospheres" || (num8 ==0 && developmentLevel >2 && !noMoreBiospheres) ))
                                //&& 
                                //( (double)building.PlusResearchPerColonist > 0.0 
                                //|| (double)building.PlusFlatResearchAmount > 0.0
                                //|| (double)building.CreditsPerColonist > 0.0
                                //|| building.StorageAdded > 0
                                ////|| (building.PlusTaxPercentage > 0 && !building.AllowShipBuilding)))
                                //))
                                {
                                    num1 = building.Cost;
                                    b = building;
                                    num8++;
                                }

                                if (b != null && num8 <2) // (this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn > 0.0 
                                //|| (b.CreditsPerColonist > 0 || this.PlusTaxPercentage > 0))) //((double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0 || this.Owner.Money > this.Owner.GrossTaxes *3))
                                {
                                    bool flag1 = true;

                                    if (b.BuildOnlyOnce)
                                    {
                                        for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                                        {
                                            if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                            {
                                                flag1 = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (flag1)
                                    {
                                        this.AddBuildingToCQ(b);
                                        num8++;
                                    }
                                }
                            }
                            
                        }
                        break;
                        #endregion
                    case Planet.ColonyType.Agricultural:
                        #region Agricultural
                        //this.fs = Planet.GoodState.EXPORT;
                        //this.ps = Planet.GoodState.IMPORT;
                        this.FarmerPercentage = 1f;
                        this.WorkerPercentage = 0.0f;
                        this.ResearcherPercentage = 0.0f;
                        //if ((this.Owner.data.Traits.Cybernetic <= 0 ? this.FoodHere:this.ProductionHere) == this.MAX_STORAGE)
                        //{
                        //    this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.0f);
                        //    float num1 = 1f - this.FarmerPercentage;
                        //    float farmmod =((this.MAX_STORAGE - this.FoodHere*.25f) / this.MAX_STORAGE);
                        //    this.FarmerPercentage *= farmmod <= 0 ? 1 : farmmod;
                        //    //Added by McShooterz: No research percentage if not researching
                        //    if (!string.IsNullOrEmpty(this.Owner.ResearchTopic))
                        //    {
                        //        this.WorkerPercentage = num1 / 2f;
                        //        this.ResearcherPercentage = num1 / 2f;
                        //    }
                        //    else
                        //    {
                        //        this.WorkerPercentage = num1;
                        //        this.ResearcherPercentage = 0.0f;
                        //    }
                        //}
                        //if ( this.ProductionHere /  this.MAX_STORAGE > 0.85f)
                        //{
                        //    float num1 = 1f - this.FarmerPercentage;
                        //    this.WorkerPercentage = 0.0f;
                        //    //Added by McShooterz: No research percentage if not researching
                        //    if (!string.IsNullOrEmpty(this.Owner.ResearchTopic))
                        //    {
                        //        this.ResearcherPercentage = num1;
                        //    }
                        //    else
                        //    {
                        //        this.FarmerPercentage = 1f;
                        //        this.ResearcherPercentage = 0.0f;
                        //    }
                        //}
                
                        this.SetExportState(this.colonyType);
                        StuffInQueueToBuild = this.ConstructionQueue.Where(building => building.isBuilding || (building.Cost > this.NetProductionPerTurn * 10)).Count() > 0;
                        ForgetReseachAndBuild =
                            string.IsNullOrEmpty(this.Owner.ResearchTopic) ; //? 1 : .5f;
                        IndySurplus = (this.NetFoodPerTurn) * ((this.MAX_STORAGE - this.FoodHere ) / this.MAX_STORAGE);
    //(1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                        this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(IndySurplus);

                        this.WorkerPercentage =(1f - this.FarmerPercentage);

                        if (StuffInQueueToBuild)
                            this.WorkerPercentage *= ((this.MAX_STORAGE - this.ProductionHere) / this.MAX_STORAGE);
                        else
                            this.WorkerPercentage = 0;

                        this.ResearcherPercentage = 1f - this.FarmerPercentage - this.WorkerPercentage;

                        if (this.Owner.data.Traits.Cybernetic > 0)
                        {
                            this.WorkerPercentage += this.FarmerPercentage;
                            this.FarmerPercentage = 0;
                        }

                        float num9 = 0.0f;
                        //bool flag11 = false;
                        foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
                                ++num9;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++num9;
                            //if (queueItem.isBuilding && queueItem.Building.Name == "Terraformer")
                            //    flag11 = true;
                        }
                        bool flag12 = true;
                        foreach (Building building in this.BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                flag12 = false;
                            //if (building.Name == "Terraformer" && this.Fertility >= 1.0)
                            //    flag11 = true;
                        }
                        if (flag12)
                        {
                            bool flag1 = false;
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    flag1 = true;
                                    break;
                                }
                            }
                            if (!flag1)
                                this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"));
                        }
                        if ( num9 < 2 )
                        {
                            this.GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in this.TilesList)
                            {
                                bool qitemTest = PGS.QItem != null;
                                if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                    pro = null;
                                if (PGS.building != cheapestFlatfood && !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                    food = cheapestFlatfood;

                                if (PGS.building != cheapestFlatResearch && !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                    res = cheapestFlatResearch;

                            }
                            Building buildthis = null;
                            buildthis = pro;
                            buildthis = pro ?? food ?? res;

                            if (buildthis != null)
                            {
                                num9++;
                                this.AddBuildingToCQ(buildthis);
                            }
                        }

                        if ( num9 < 2.0f)
                        {
                            this.GetBuildingsWeCanBuildHere();
                            Building b = (Building)null;
                            float num1 = 99999f;
                            foreach (Building building in this.BuildingsCanBuild.OrderBy(cost => cost.Cost))
                            {
                                if (!WeCanAffordThis(building, this.colonyType))
                                    continue;
                                if (building.Name == "Outpost") //this.WeCanAffordThis(building,this.colonyType)) //
                                {//(double)building.PlusFlatProductionAmount > 0.0 ||
                                    float num2 = building.Cost;
                                    b = building;
                                    break;
                                }
                                else if ( building.Cost <  num1 
                                    && cheapestFlatfood == null 
                                    && cheapestFlatprod == null 
                                   && cheapestFlatResearch == null &&(building.Name == "Biospheres"  && !noMoreBiospheres))

                                {
                                    num1 = building.Cost;
                                    b = building;
                                }
                                else if (building.Cost < num1 && (building.Name != "Biospheres" || (num9 == 0 && developmentLevel > 2 && !noMoreBiospheres)))

                                {

                                    num1 = building.Cost;
                                    b = building;
                                }
                            }
                            if (b != null)//&& (this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn > 0.0 || (b.CreditsPerColonist > 0 || this.PlusTaxPercentage > 0))) //((double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0 || this.Owner.Money > this.Owner.GrossTaxes *3))
                            {
                                bool flag1 = true;
                                if (b.BuildOnlyOnce)
                                {
                                    for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                                    {
                                        if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                        {
                                            flag1 = false;
                                            break;
                                        }
                                    }
                                }
                                if (flag1)
                                    this.AddBuildingToCQ(b);
                            }
                        }
                        break;
                        #endregion
                    case Planet.ColonyType.Military:
                        #region Military                        
                        this.FarmerPercentage = 0.0f;
                        this.WorkerPercentage = 1f;
                        this.ResearcherPercentage = 0.0f;
                        if ( this.FoodHere <=  this.consumption)
                        {
                            this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.01f);
                            this.WorkerPercentage = 1f - this.FarmerPercentage;
                        
                        }

                        this.WorkerPercentage =(1f - this.FarmerPercentage);

                        if (StuffInQueueToBuild)
                            this.WorkerPercentage *= ((this.MAX_STORAGE - this.ProductionHere) / this.MAX_STORAGE);
                        else
                            this.WorkerPercentage = 0;

                        this.ResearcherPercentage = 1f - this.FarmerPercentage - this.WorkerPercentage;

                        if (this.Owner.data.Traits.Cybernetic > 0)
                        {
                            this.WorkerPercentage += this.FarmerPercentage;
                            this.FarmerPercentage = 0;
                        }


                        if (this.Owner.data.Traits.Cybernetic > 0)
                        {
                            this.WorkerPercentage += this.FarmerPercentage;
                            this.FarmerPercentage = 0;
                        }
                        if(!this.Owner.isPlayer && this.fs == GoodState.STORE)
                        {
                            this.fs = GoodState.IMPORT;
                            this.ps = GoodState.IMPORT;
                               
                        }
                        this.SetExportState(this.colonyType);
                        //this.ps = (double)this.ProductionHere >= 20.0 ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                        float buildingCount = 0.0f;
                        foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
                                ++buildingCount;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++buildingCount;
                        }
                        bool missingOutpost = true;
                        foreach (Building building in this.BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                missingOutpost = false;
                        }
                        if (missingOutpost)
                        {
                            bool hasOutpost = false;
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    hasOutpost = true;
                                    break;
                                }
                            }
                            if (!hasOutpost)
                                this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"));
                        }
                        if (this.Owner != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty)
                            && this.Shipyards.Where(ship => ship.Value.GetShipData().IsShipyard).Count() == 0
                            && this.Owner.ShipsWeCanBuild.Contains(this.Owner.data.DefaultShipyard) &&  this.GrossMoneyPT > 3.0)
                        {
                            bool hasShipyard = false;
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isShip && queueItem.sData.IsShipyard)
                                {
                                    hasShipyard = true;
                                    break;
                                }
                            }
                            if (!hasShipyard)
                                this.ConstructionQueue.Add(new QueueItem()
                                {
                                    isShip = true,
                                    sData = ResourceManager.ShipsDict[this.Owner.data.DefaultShipyard].GetShipData(),
                                    Cost = ResourceManager.ShipsDict[this.Owner.data.DefaultShipyard].GetCost(this.Owner)
                                });
                        }
                        if ( buildingCount < 2.0f)
                        {
                            this.GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in this.TilesList)
                            {
                                bool qitemTest = PGS.QItem != null;
                                if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                    pro = null;
                                if (PGS.building != cheapestFlatfood && !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                    food = cheapestFlatfood;

                                if (PGS.building != cheapestFlatResearch && !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                    res = cheapestFlatResearch;

                            }
                            Building buildthis = null;
                            buildthis = pro;
                            buildthis = pro ?? food ?? res;

                            if (buildthis != null)
                            {
                                buildingCount++;
                                this.AddBuildingToCQ(buildthis);
                            }
                            
                            
                            
                            
                            this.GetBuildingsWeCanBuildHere();
                            Building b = (Building)null;
                            float num1 = 99999f;
                            foreach (Building building in this.BuildingsCanBuild.OrderBy(cost => cost.Cost))
                            {
                                if (!WeCanAffordThis(building, this.colonyType))
                                    continue;
                                if (building.Name == "Outpost") //this.WeCanAffordThis(building,this.colonyType)) //
                                {//(double)building.PlusFlatProductionAmount > 0.0 ||
                                    float num2 = building.Cost;
                                    b = building;
                                    break;
                                }
                                else if (building.Cost < num1
                                    && cheapestFlatfood == null
                                    && cheapestFlatprod == null
                                   && cheapestFlatResearch == null && (building.Name == "Biospheres" && !noMoreBiospheres))
                                {
                                    num1 = building.Cost;
                                    b = building;
                                }
                                else if (building.Cost < num1 && (building.Name != "Biospheres" || (buildingCount == 0 && developmentLevel > 2 && !noMoreBiospheres)))
                                {

                                    num1 = building.Cost;
                                    b = building;
                                }
                            }
                            if (b != null)//&& (this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn > 0.0 || (b.CreditsPerColonist > 0 || this.PlusTaxPercentage > 0))) //((double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0 || this.Owner.Money > this.Owner.GrossTaxes *3))
                            {
                                bool flag1 = true;
                                if (b.BuildOnlyOnce)
                                {
                                    for (int index = 0; index < this.Owner.GetPlanets().Count; ++index)
                                    {
                                        if (this.Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                        {
                                            flag1 = false;
                                            break;
                                        }
                                    }
                                }
                                if (flag1)
                                    this.AddBuildingToCQ(b);
                            }
                        }
                        break;
                        #endregion

                    case Planet.ColonyType.TradeHub:
                        #region TradeHub
                        {

                            //this.fs = Planet.GoodState.IMPORT;

                            this.FarmerPercentage = 0.0f;
                            this.WorkerPercentage = 1f;
                            this.ResearcherPercentage = 0.0f;

                            //? true : .75f;
                            this.ps =  this.ProductionHere >= 20  ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                            float IndySurplus2 = (this.NetFoodPerTurn) *//(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? .5f : .25f)) * 
                                (1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                            if (this.Owner.data.Traits.Cybernetic > 0)
                            {
                                IndySurplus = this.GrossProductionPerTurn - this.consumption;
                                IndySurplus = IndySurplus * (1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                                //(1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                            }
                            //if ((double)this.FoodHere <= (double)this.consumption)
                            {

                                this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(IndySurplus2);
                                if (FarmerPercentage == 1 && StuffInQueueToBuild)
                                    this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0);
                                this.WorkerPercentage =
                                    (1f - this.FarmerPercentage)   //(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? 1f :
                                    * (ForgetReseachAndBuild ? 1 :
                                 (1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1)));
                                if (this.ProductionHere / this.MAX_STORAGE > .75 && !StuffInQueueToBuild)
                                    this.WorkerPercentage = 0;
                                this.ResearcherPercentage = 1 - this.FarmerPercentage - this.WorkerPercentage;// 0.0f;
                                if (this.Owner.data.Traits.Cybernetic > 0)
                                {
                                    this.WorkerPercentage += this.FarmerPercentage;
                                    this.FarmerPercentage = 0;
                                }
                                this.SetExportState(this.colonyType);

                            }
                            break;
                        }
                        #endregion
                }
            }

            if (this.ConstructionQueue.Count < 5 && !this.system.CombatInSystem && this.developmentLevel > 2 && this.colonyType != ColonyType.Research) //  this.ProductionHere > this.MAX_STORAGE * .75f)
            #region Troops and platforms
            {
                //Added by McShooterz: Colony build troops
                #region MyRegion
                if (this.Owner.isPlayer && this.colonyType == ColonyType.Military)
                {
                    bool addTroop = false;
                    foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                    {
                        if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops)
                        {
                            addTroop = true;
                            break;
                        }
                    }
                    if (addTroop && this.AllowInfantry)
                    {
                        foreach (KeyValuePair<string, Troop> troop in ResourceManager.TroopsDict)
                        {
                            if (this.Owner.WeCanBuildTroop(troop.Key))
                            {

                                QueueItem qi = new QueueItem();
                                qi.isTroop = true;
                                qi.troop = troop.Value;
                                qi.Cost = troop.Value.GetCost();
                                qi.productionTowards = 0f;
                                qi.NotifyOnEmpty = false;
                                this.ConstructionQueue.Add(qi);
                                break;
                            }
                        }
                    }


                }
                #endregion
                //Added by McShooterz: build defense platforms

                if (this.HasShipyard && !this.system.CombatInSystem 
                     && (!this.Owner.isPlayer || this.colonyType == ColonyType.Military))
                {

                    SystemCommander SCom;
                    if (this.Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(this.system, out SCom))
                    {
                        float DefBudget;
                        DefBudget = this.Owner.data.DefenseBudget * SCom.PercentageOfValue;

                        float maxProd = this.GetMaxProductionPotential();
                        //bool buildStation =false;
                        float platformUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.platform].Upkeep;
                        float stationUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.station].Upkeep;
                        string station = this.Owner.GetGSAI().GetStarBase();
                        //if (DefBudget >= 1 && !string.IsNullOrEmpty(station))
                        //    buildStation = true;
                        int PlatformCount = 0;
                        int stationCount = 0;
                        foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                        {
                            if (!queueItem.isShip)
                                continue;
                            if (queueItem.sData.Role == ShipData.RoleName.platform)
                            {
                                if (DefBudget - platformUpkeep < -platformUpkeep * .5) //|| (buildStation && DefBudget > stationUpkeep))
                                {
                                    this.ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                DefBudget -= platformUpkeep;
                                PlatformCount++;
                            }
                            if (queueItem.sData.Role == ShipData.RoleName.station)
                            {
                                if (DefBudget - stationUpkeep < -stationUpkeep)
                                {
                                    this.ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                DefBudget -= stationUpkeep;
                                stationCount++;
                            }
                        }
                        foreach (Ship platform in this.Shipyards.Values)
                        {
                            if (platform.BaseStrength <= 0)
                                continue;
                            if (platform.GetAI().State == AIState.Scrap)
                                continue;
                            if (platform.shipData.Role == ShipData.RoleName.station)
                            {
                                stationUpkeep = platform.GetMaintCost();
                                if (DefBudget - stationUpkeep < -stationUpkeep)
                                {

                                    platform.GetAI().OrderScrapShip();
                                    continue;
                                }
                                DefBudget -= stationUpkeep;
                                stationCount++;
                            }
                            if (platform.shipData.Role == ShipData.RoleName.platform)//|| (buildStation && DefBudget < 5))
                            {
                                platformUpkeep = platform.GetMaintCost();
                                if (DefBudget - platformUpkeep < -platformUpkeep)
                                {
                                    platform.GetAI().OrderScrapShip();

                                    continue;
                                }
                                DefBudget -= platformUpkeep;
                                PlatformCount++;
                            }

                        }
                        //this.Shipyards.Where(ship => ship.Value.Weapons.Count() > 0 && ship.Value.Role==ShipData.RoleName.platform).Count();


                        if (DefBudget > stationUpkeep && maxProd > 10.0
&& stationCount < (int)(SCom.RankImportance * .5f) //(int)(SCom.PercentageOfValue * this.developmentLevel)
&& stationCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                        {
                            // string platform = this.Owner.GetGSAI().GetStarBase();
                            if (!string.IsNullOrEmpty(station))
                            {
                                Ship ship = ResourceManager.ShipsDict[station];
                                if (ship.GetCost(this.Owner) / this.GrossProductionPerTurn < 10)
                                this.ConstructionQueue.Add(new QueueItem()
                                   {
                                       isShip = true,
                                       sData = ship.GetShipData(),
                                       Cost = ship.GetCost(this.Owner)
                                   });
                            }
                            DefBudget -= stationUpkeep;
                        }
                        if (DefBudget > platformUpkeep && maxProd > 1.0
                            && PlatformCount < SCom.RankImportance //(int)(SCom.PercentageOfValue * this.developmentLevel)
                            && PlatformCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                        {
                            string platform = this.Owner.GetGSAI().GetDefenceSatellite();
                            if (!string.IsNullOrEmpty(platform))
                            {
                                Ship ship = ResourceManager.ShipsDict[platform];
                                this.ConstructionQueue.Add(new QueueItem()
                                {
                                    isShip = true,
                                    sData = ship.GetShipData(),
                                    Cost = ship.GetCost(this.Owner)
                                });
                            }

                        }

                    }
                }

            }
            #endregion
            //if (this.Population > 3000.0 || this.Population / (this.MaxPopulation + this.MaxPopBonus) > 0.75)
            #region Scrap
            //if (this.colonyType!= ColonyType.TradeHub)
            {
                //List<Building> list = new List<Building>();
                //foreach (Building building in this.BuildingList)
                //{
                //    if ((double)building.PlusFlatPopulation > 0.0 && (double)building.Maintenance > 0.0 && building.Name != "Biospheres")
                //    //
                //        list.Add(building);
                //}
                //foreach (Building b in list)
                //    this.ScrapBuilding(b);

                List<Building> list1 = new List<Building>();
                if ( this.Fertility >= 1 )
                {

                    foreach (Building building in this.BuildingList)
                    {
                        if (building.PlusTerraformPoints > 0.0f &&  building.Maintenance > 0 )
                            list1.Add(building);
                    }
                }

                //finances
                //if (this.Owner.Money*.5f < this.Owner.GrossTaxes*(1-this.Owner.data.TaxRate) && this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn < 0)
                {
                    this.ConstructionQueue.thisLock.EnterReadLock();
                    foreach (PlanetGridSquare PGS in this.TilesList)
                    {
                        bool qitemTest = PGS.QItem != null;
                        if (qitemTest && PGS.QItem.IsPlayerAdded)
                            continue;
                        if (PGS.building != null && PGS.building.IsPlayerAdded)
                            continue;
                        if ((qitemTest && PGS.QItem.Building.Name == "Biospheres") || (PGS.building != null && PGS.building.Name == "Biospheres"))
                            continue;
                        if ((PGS.building != null && PGS.building.PlusFlatProductionAmount > 0) || (PGS.building != null && PGS.building.PlusFlatProductionAmount > 0))
                            continue;
                        if ((PGS.building != null && PGS.building.PlusFlatFoodAmount > 0) || (PGS.building != null && PGS.building.PlusFlatFoodAmount > 0))
                            continue;
                        if ((PGS.building != null && PGS.building.PlusFlatResearchAmount > 0) || (PGS.building != null && PGS.building.PlusFlatResearchAmount > 0))
                            continue;
                        if (PGS.building != null && !qitemTest && PGS.building.Scrappable && !WeCanAffordThis(PGS.building, this.colonyType)) // queueItem.isBuilding && !WeCanAffordThis(queueItem.Building, this.colonyType))
                        {
                            this.ScrapBuilding(PGS.building);

                        }
                        if (qitemTest && !WeCanAffordThis(PGS.QItem.Building, this.colonyType))
                        {
                            this.ProductionHere += PGS.QItem.productionTowards;
                            this.ConstructionQueue.QueuePendingRemoval(PGS.QItem);
                            PGS.QItem = null;

                        }
                    }
                    //foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                    //{
                    //    if(queueItem.Building == cheapestFlatfood || queueItem.Building == cheapestFlatprod || queueItem.Building == cheapestFlatResearch)
                    //        continue;
                    //    if (queueItem.isBuilding &&  !WeCanAffordThis(queueItem.Building, this.colonyType))
                    //    {
                    //        this.ProductionHere += queueItem.productionTowards;
                    //        this.ConstructionQueue.QueuePendingRemoval(queueItem);
                    //    }
                    //}
                    this.ConstructionQueue.thisLock.ExitReadLock();
                    this.ConstructionQueue.ApplyPendingRemovals();
                    //foreach (Building building in this.BuildingList)
                    //{
                    //    if (building.Name != "Biospheres" && !WeCanAffordThis(building, this.colonyType))
                    //        //
                    //        list1.Add(building);
                    //}

                }

                //foreach (Building b in list1.OrderBy(maintenance=> maintenance.Maintenance))
                //{

                //    if (b == cheapestFlatprod || b == cheapestFlatfood || b == cheapestFlatResearch)
                //        continue;
                //    this.ScrapBuilding(b);
                //}
            #endregion
            }
        }
        public bool GoodBuilding (Building b)
        {
            return true;
        }

        public void ScrapBuilding(Building b)
        {
            //if (b.IsPlayerAdded)
            //    return;

            Building building1 = (Building)null;
            foreach (Building building2 in this.BuildingList)
            {
                if (b == building2)
                    building1 = building2;
            }
            this.BuildingList.Remove(building1);
            this.ProductionHere += ResourceManager.BuildingsDict[b.Name].Cost / 2f;
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
            {
                if (planetGridSquare.building != null && planetGridSquare.building == building1)
                    planetGridSquare.building = (Building)null;
            }
        }


        public bool ApplyStoredProduction(int Index)
        {
            
            if (this.Crippled_Turns > 0 || this.RecentCombat || (this.ConstructionQueue.Count <= 0 || this.Owner == null ))//|| this.Owner.Money <=0))
                return false;
            if (this.Owner != null && !this.Owner.isPlayer && this.Owner.data.Traits.Cybernetic > 0)
                return false;
            
            QueueItem item = this.ConstructionQueue[Index];
            float amountToRush = this.GetMaxProductionPotential();//(int)(this.ProductionHere * .25f); //item.Cost - item.productionTowards;
            amountToRush = amountToRush < 5 ? 5 : amountToRush;
            float amount = amountToRush < this.ProductionHere ? amountToRush : this.ProductionHere;
            if (amount < 1)
            {
                return false;
            }
                this.ProductionHere -= amount;
                this.ApplyProductiontoQueue(amount, Index);
                
                return true;
       }

        public void ApplyAllStoredProduction(int Index)
        {
            if (this.Crippled_Turns > 0 || this.RecentCombat || (this.ConstructionQueue.Count <= 0 || this.Owner == null )) //|| this.Owner.Money <= 0))
                return;
           
            float amount = this.ProductionHere;
            this.ProductionHere = 0f;
            this.ApplyProductiontoQueue(amount, Index);
            
        }

        private void ApplyProductionTowardsConstruction()
        {
            if (this.Crippled_Turns > 0 || this.RecentCombat)
                return;
            /*
             * timeToEmptyMax = maxs/maxp; = 2

maxp =max production =50
maxs = max storage =100
cs = current stoage; = 50

time2ec = cs/maxp = 1

take10 = time2ec /10; = .1
output = maxp * take10 = 5
             * */
            
            float maxp = this.GetMaxProductionPotential() *(1-this.FarmerPercentage); //this.NetProductionPerTurn; //
            if (maxp < 5)
                maxp = 5;
            float StorageRatio = 0;
                //float TimeToEmpty = cs / maxp;
            float take10Turns = 0;//TimeToEmpty / (this.ps == GoodState.EXPORT ? 20 : 10);
            //bool NoNeedToStore = this.Owner.GetPlanets().Count == 1;
                StorageRatio= this.ProductionHere / this.MAX_STORAGE;
            take10Turns = maxp * StorageRatio;

               
                if (!this.PSexport)
                    take10Turns *= (StorageRatio < .75f ? this.ps == GoodState.EXPORT ? .5f : this.ps == GoodState.STORE ? .25f : 1 : 1);
                    //take10Turns = maxp;
            if (!this.GovernorOn || this.colonyType == ColonyType.Colony)
                {
                    take10Turns = this.NetProductionPerTurn; ;
                }
                float normalAmount =  take10Turns;// maxp* take10Turns;



            normalAmount = normalAmount < 0 ? 0 : normalAmount;
            normalAmount = normalAmount >
                this.ProductionHere ? ProductionHere : normalAmount;
            ProductionHere -= normalAmount;

            this.ApplyProductiontoQueue(normalAmount,0);
            this.ProductionHere += this.NetProductionPerTurn > 0.0f ? this.NetProductionPerTurn : 0.0f;

            //fbedard: apply all remaining production on Planet with no governor
            if (this.ps != GoodState.EXPORT && this.colonyType == Planet.ColonyType.Colony && this.Owner.isPlayer )
            {
                normalAmount = this.ProductionHere;
                this.ProductionHere = 0f;
                this.ApplyProductiontoQueue(normalAmount, 0);                
            }

            //return;
            //if (this.ProductionHere > this.MAX_STORAGE * 0.6f && this.GovernorOn)
            //{
            //    float amount = this.ProductionHere - (this.MAX_STORAGE * 0.6f);
            //    this.ProductionHere = this.MAX_STORAGE * 0.6f;
            //    this.ApplyProductiontoQueue(normalAmount + amount, 0);
            //}
            //else
            //{
            //    //Only store 25% if exporting
            //    if (this.ps == GoodState.EXPORT)
            //    {
            //        this.ApplyProductiontoQueue(normalAmount * 0.75f, 0);
            //        this.ProductionHere += 0.25f * normalAmount;
            //    }
            //    else
            //        this.ApplyProductiontoQueue(normalAmount, 0);
            //}
            //Lost production converted into 50% money
            //The Doctor: disabling until this can be more smoothly integrated into the economic predictions, UI and AI
            /*
            if (this.ProductionHere > this.MAX_STORAGE)
            {
                this.Owner.Money += (this.ProductionHere - this.MAX_STORAGE) * 0.5f;
                this.ProductionHere = this.MAX_STORAGE;
            } */
        }

        public void ApplyProductiontoQueue(float howMuch, int whichItem)
        {
            if (this.Crippled_Turns > 0 || this.RecentCombat || howMuch <= 0.0)
            {
                if (howMuch > 0 && this.Crippled_Turns <=0)
                ProductionHere += howMuch;
                return;
            }
            this.planetLock.EnterWriteLock();
            float cost = 0;
            if (this.ConstructionQueue.Count > 0 && this.ConstructionQueue.Count > whichItem)
            {                
                QueueItem item = this.ConstructionQueue[whichItem];
                cost = item.Cost;
                if (item.isShip)
                    //howMuch += howMuch * this.ShipBuildingModifier;
                    cost *= this.ShipBuildingModifier;
                cost -= item.productionTowards;
                if (howMuch < cost)
                {
                    item.productionTowards += howMuch;
                    //if (item.productionTowards >= cost)
                    //    this.ProductionHere += item.productionTowards - cost;
                }
                else
                {
                    
                    howMuch -= cost;
                    item.productionTowards = item.Cost;// *this.ShipBuildingModifier;
                    this.ProductionHere += howMuch;
                }
                this.ConstructionQueue[whichItem] = item;
            }
            else
                this.ProductionHere += howMuch;
            this.planetLock.ExitWriteLock();
            for (int index1 = 0; index1 < this.ConstructionQueue.Count; ++index1)
            {
                QueueItem queueItem = this.ConstructionQueue[index1];

               //Added by gremlin remove exess troops from queue 
                if (queueItem.isTroop)
                {

                    int space = 0;
                    foreach (PlanetGridSquare tilesList in this.TilesList)
                    {
                        if (tilesList.TroopsHere.Count >= tilesList.number_allowed_troops || tilesList.building != null && (tilesList.building == null || tilesList.building.CombatStrength != 0))
                        {
                            continue;
                        }
                        space++;
                    }

                    if (space < 1)
                    {
                        if (queueItem.productionTowards == 0)
                        {
                            this.ConstructionQueue.Remove(queueItem);
                        }
                        else
                        {
                            ProductionHere += queueItem.productionTowards;
                            if (ProductionHere > MAX_STORAGE)
                                ProductionHere = MAX_STORAGE;
                            if (queueItem.pgs != null)
                                queueItem.pgs.QItem = null;
                            ConstructionQueue.Remove(queueItem);
                        }
                    }
                }
                //if ((double)queueItem.productionTowards >= (double)queueItem.Cost && queueItem.NotifyOnEmpty == false)
                //    this.queueEmptySent = true;
                //else if ((double)queueItem.productionTowards >= (double)queueItem.Cost)
                //    this.queueEmptySent = false;

                if (queueItem.isBuilding &&  queueItem.productionTowards >= queueItem.Cost)
                {
                    Building building = ResourceManager.GetBuilding(queueItem.Building.Name);
                    if(queueItem.IsPlayerAdded)
                    building.IsPlayerAdded = queueItem.IsPlayerAdded;
                    this.BuildingList.Add(building);
                    this.Fertility -= ResourceManager.GetBuilding(queueItem.Building.Name).MinusFertilityOnBuild;
                    if (this.Fertility < 0.0)
                        this.Fertility = 0.0f;
                    if (queueItem.pgs != null)
                    {
                        if (queueItem.Building != null && queueItem.Building.Name == "Biospheres")
                        {
                            queueItem.pgs.Habitable = true;
                            queueItem.pgs.Biosphere = true;
                            queueItem.pgs.building = (Building)null;
                            queueItem.pgs.QItem = (QueueItem)null;
                        }
                        else
                        {
                            queueItem.pgs.building = building;
                            queueItem.pgs.QItem = (QueueItem)null;
                        }
                    }
                    if (queueItem.Building.Name == "Space Port")
                    {
                        this.Station.planet = this;
                        this.Station.ParentSystem = this.system;
                        this.Station.LoadContent(Planet.universeScreen.ScreenManager);
                        this.HasShipyard = true;
                    }
                    if (queueItem.Building.AllowShipBuilding)
                        this.HasShipyard = true;
                    if (building.EventOnBuild != null && this.Owner != null && this.Owner == Empire.Universe.PlayerEmpire)
                        Planet.universeScreen.ScreenManager.AddScreen((GameScreen)new EventPopup(Planet.universeScreen, Empire.Universe.PlayerEmpire, ResourceManager.EventsDict[building.EventOnBuild], ResourceManager.EventsDict[building.EventOnBuild].PotentialOutcomes[0], true));
                    this.ConstructionQueue.QueuePendingRemoval(queueItem);
                }
                else if (queueItem.isShip && !ResourceManager.ShipsDict.ContainsKey(queueItem.sData.Name))
                {
                    this.ConstructionQueue.QueuePendingRemoval(queueItem);
                    this.ProductionHere += queueItem.productionTowards;
                    if ( this.ProductionHere >  this.MAX_STORAGE)
                        this.ProductionHere = this.MAX_STORAGE;
                }
                else if (queueItem.isShip && queueItem.productionTowards >= queueItem.Cost)
                {
                    Ship shipAt;
                    if (queueItem.isRefit)
                        shipAt = ResourceManager.CreateShipAt(queueItem.sData.Name, this.Owner, this, true, !string.IsNullOrEmpty(queueItem.RefitName) ? queueItem.RefitName : queueItem.sData.Name, queueItem.sData.Level);
                    else
                        shipAt = ResourceManager.CreateShipAt(queueItem.sData.Name, this.Owner, this, true);
                    this.ConstructionQueue.QueuePendingRemoval(queueItem);
                    using (List<string>.Enumerator enumerator = Enumerable.ToList<string>((IEnumerable<string>)shipAt.GetMaxGoods().Keys).GetEnumerator())
                    {
                        //label_35:
                        while (enumerator.MoveNext())
                        {
                            string current = enumerator.Current;
                            while (true)
                            {
                                if ( this.ResourcesDict[current] > 0.0f &&  shipAt.GetCargo()[current] <  shipAt.GetMaxGoods()[current])
                                {
                                    Dictionary<string, float> dictionary;
                                    string index2;
                                    (dictionary = this.ResourcesDict)[index2 = current] = dictionary[index2] - 1f;
                                    shipAt.AddGood(current, 1);
                                }
                                else
                                    break;
                                //goto label_35;
                            }
                        }
                    }
                    if (queueItem.sData.Role == ShipData.RoleName.station || queueItem.sData.Role == ShipData.RoleName.platform)
                    {
                        int num = this.Shipyards.Count / 9;
                        shipAt.Position = this.Position + MathExt.PointOnCircle((float)(this.Shipyards.Count * 40), (float)(2000 + 2000 * num * this.scale));
                        shipAt.Center = shipAt.Position;
                        shipAt.TetherToPlanet(this);
                        this.Shipyards.TryAdd(shipAt.guid, shipAt);
                    }
                    if (queueItem.Goal != null)
                    {
                        if (queueItem.Goal.GoalName == "BuildConstructionShip")
                        {
                            shipAt.GetAI().OrderDeepSpaceBuild(queueItem.Goal);
                            //shipAt.shipData.Role = ShipData.RoleName.construction;
                            shipAt.isConstructor = true;
                            shipAt.VanityName = "Construction Ship";
                        }
                        else if (queueItem.Goal.GoalName != "BuildDefensiveShips" && queueItem.Goal.GoalName != "BuildOffensiveShips" && queueItem.Goal.GoalName != "FleetRequisition")
                        {
                            ++queueItem.Goal.Step;
                        }
                        else
                        {
                            if (this.Owner != Empire.Universe.PlayerEmpire)
                                this.Owner.ForcePoolAdd(shipAt);
                            queueItem.Goal.ReportShipComplete(shipAt);
                        }
                    }
                    else if ((queueItem.sData.Role != ShipData.RoleName.station || queueItem.sData.Role == ShipData.RoleName.platform) && this.Owner != Empire.Universe.PlayerEmpire)
                        this.Owner.ForcePoolAdd(shipAt);
                }
                else if (queueItem.isTroop &&  queueItem.productionTowards >= queueItem.Cost)
                {
                    Troop troop = ResourceManager.CreateTroop(queueItem.troop, this.Owner);
                    if (this.AssignTroopToTile(troop))
                    {

                        troop.SetOwner(this.Owner);
                        if (queueItem.Goal != null)
                        {
                            Goal step = queueItem.Goal;
                            step.Step = step.Step + 1;
                        }
                        this.ConstructionQueue.QueuePendingRemoval(queueItem);
                    }
                }
            }
            this.ConstructionQueue.ApplyPendingRemovals();
        }

        public int EstimatedTurnsTillComplete(QueueItem qItem)
        {
            int num = (int)Math.Ceiling((double)(int)((qItem.Cost - qItem.productionTowards) / this.NetProductionPerTurn));
            if (this.NetProductionPerTurn > 0.0)
                return num;
            else
                return 999;
        }

        public float GetMaxProductionPotential()
        {
            float num1 = 0.0f;
            float num2 =  this.MineralRichness *  this.Population / 1000;
            for (int index = 0; index < this.BuildingList.Count; ++index)
            {
                Building building = this.BuildingList[index];
                if (building.PlusProdPerRichness > 0.0)
                    num1 += building.PlusProdPerRichness * this.MineralRichness;
                num1 += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0.0)
                    num2 += building.PlusProdPerColonist;
            }
            float num3 = num2 + num1 *this.Population / 1000;
            float num4 = num3;
            if (this.Owner.data.Traits.Cybernetic > 0)
                return num4 + this.Owner.data.Traits.ProductionMod * num4 - this.consumption;
            return num4 + this.Owner.data.Traits.ProductionMod * num4;
        }

        public void InitializeSliders(Empire o)
        {
            if (o.data.Traits.Cybernetic == 1 || this.Type == "Barren")
            {
                this.FarmerPercentage = 0.0f;
                this.WorkerPercentage = 0.5f;
                this.ResearcherPercentage = 0.5f;
            }
            else
            {
                this.FarmerPercentage = 0.55f;
                this.ResearcherPercentage = 0.2f;
                this.WorkerPercentage = 0.25f;
            }
        }

        public bool CanBuildInfantry()
        {
            for (int i = 0; i < this.BuildingList.Count; i++)
            {
                if (this.BuildingList[i].AllowInfantry)
                    return true;
            }
            return false;
        }

        public void UpdateIncomes(bool LoadUniverse)
        {
            if (this.Owner == null)
                return;
            this.PlusFlatPopulationPerTurn = 0f;
            this.ShieldStrengthMax = 0f;
            this.TotalMaintenanceCostsPerTurn = 0f;
            this.StorageAdded = 0;
            this.AllowInfantry = false;
            this.TotalDefensiveStrength = 0;
            this.GrossFood = 0f;
            this.PlusResearchPerColonist = 0f;
            this.PlusFlatResearchPerTurn = 0f;
            this.PlusFlatProductionPerTurn = 0f;
            this.PlusProductionPerColonist = 0f;
            this.FlatFoodAdded = 0f;
            this.PlusFoodPerColonist = 0f;
            
            this.PlusFlatPopulationPerTurn = 0f;
            this.ShipBuildingModifier = 0f;
            this.CommoditiesPresent.Clear();
            float shipbuildingmodifier = 1f;
            List<Guid> list = new List<Guid>();
            float shipyards =1;
            
            if (!LoadUniverse)
            foreach (KeyValuePair<Guid, Ship> keyValuePair in this.Shipyards)
            {
                if (keyValuePair.Value == null)
                    list.Add(keyValuePair.Key);
                    
                else if (keyValuePair.Value.Active && keyValuePair.Value.GetShipData().IsShipyard)
                {

					if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.ShipyardBonus > 0)
                    {
                        shipbuildingmodifier *= (1 - (GlobalStats.ActiveModInfo.ShipyardBonus / shipyards)); //+= GlobalStats.ActiveModInfo.ShipyardBonus;
                    }
                    else
                    {
                        shipbuildingmodifier *= (1-(.25f/shipyards));
                    }
                    shipyards += .2f;
                }
                else if (!keyValuePair.Value.Active)
                    list.Add(keyValuePair.Key);
            }
            this.ShipBuildingModifier = shipbuildingmodifier;
            Ship remove;
            foreach (Guid key in list)
            {
                
                this.Shipyards.TryRemove(key, out remove);
            }
            this.PlusCreditsPerColonist = 0f;
            this.MaxPopBonus = 0f;
            this.PlusTaxPercentage = 0f;
            this.TerraformToAdd = 0f;
            bool shipyard =false;            
            for (int index = 0; index < this.BuildingList.Count; ++index)
            {
                Building building = this.BuildingList[index];
                if (building.WinsGame)
                    this.HasWinBuilding = true;
                //if (building.NameTranslationIndex == 458)
                if (building.AllowShipBuilding || building.Name == "Space Port" )
                    shipyard= true;
                
                if (building.PlusFlatPopulation > 0)
                    this.PlusFlatPopulationPerTurn += building.PlusFlatPopulation;
                this.ShieldStrengthMax += building.PlanetaryShieldStrengthAdded;
                this.PlusCreditsPerColonist += building.CreditsPerColonist;
                if (building.PlusTerraformPoints > 0)
                    this.TerraformToAdd += building.PlusTerraformPoints;
                if (building.Strength > 0)
                    this.TotalDefensiveStrength += building.CombatStrength;
                this.PlusTaxPercentage += building.PlusTaxPercentage;
                if (building.IsCommodity)
                    this.CommoditiesPresent.Add(building.Name);
                if (building.AllowInfantry)
                    this.AllowInfantry = true;
                if (building.StorageAdded > 0)
                    this.StorageAdded += building.StorageAdded;
                if (building.PlusFoodPerColonist > 0)
                    this.PlusFoodPerColonist += building.PlusFoodPerColonist;
                if (building.PlusResearchPerColonist > 0)
                    this.PlusResearchPerColonist += building.PlusResearchPerColonist;
                if (building.PlusFlatResearchAmount > 0)
                    this.PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                if (building.PlusProdPerRichness > 0)
                    this.PlusFlatProductionPerTurn += building.PlusProdPerRichness * this.MineralRichness;
                this.PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0)
                    this.PlusProductionPerColonist += building.PlusProdPerColonist;
                if (building.MaxPopIncrease > 0)
                    this.MaxPopBonus += building.MaxPopIncrease;
                if (building.Maintenance > 0)
                    this.TotalMaintenanceCostsPerTurn += building.Maintenance;
                this.FlatFoodAdded += building.PlusFlatFoodAmount;
                this.RepairPerTurn += building.ShipRepair;
                //Repair if no combat
                if(!this.RecentCombat)
                {
                    building.CombatStrength = Ship_Game.ResourceManager.BuildingsDict[building.Name].CombatStrength;
                    building.Strength = Ship_Game.ResourceManager.BuildingsDict[building.Name].Strength;
                }
            }
            //Added by Gretman -- This will keep a planet from still having sheilds even after the shield building has been scrapped.
            if (this.ShieldStrengthCurrent > this.ShieldStrengthMax) this.ShieldStrengthCurrent = this.ShieldStrengthMax;

            if (shipyard && (this.colonyType != ColonyType.Research || this.Owner.isPlayer))
                this.HasShipyard = true;
            else
                this.HasShipyard = false;
            //Research
            this.NetResearchPerTurn =  ( this.ResearcherPercentage *  this.Population / 1000) * this.PlusResearchPerColonist + this.PlusFlatResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn + this.ResearchPercentAdded * this.NetResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn + this.Owner.data.Traits.ResearchMod * this.NetResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn - this.Owner.data.TaxRate * this.NetResearchPerTurn;
            //Food
            this.NetFoodPerTurn =  ( this.FarmerPercentage *  this.Population / 1000 * ( this.Fertility +  this.PlusFoodPerColonist)) + this.FlatFoodAdded;
            this.NetFoodPerTurn = this.NetFoodPerTurn + this.FoodPercentAdded * this.NetFoodPerTurn;
            this.GrossFood = this.NetFoodPerTurn;
            //Production
            this.NetProductionPerTurn =  (this.WorkerPercentage * this.Population / 1000f * ( this.MineralRichness +  this.PlusProductionPerColonist)) + this.PlusFlatProductionPerTurn;
            this.NetProductionPerTurn = this.NetProductionPerTurn + this.Owner.data.Traits.ProductionMod * this.NetProductionPerTurn;
            if (this.Owner.data.Traits.Cybernetic > 0)
                this.NetProductionPerTurn = this.NetProductionPerTurn - this.Owner.data.TaxRate * (this.NetProductionPerTurn -this.consumption) ;
            else
                this.NetProductionPerTurn = this.NetProductionPerTurn - this.Owner.data.TaxRate * this.NetProductionPerTurn;
            
            this.GrossProductionPerTurn =  (  this.Population / 1000  * ( this.MineralRichness +  this.PlusProductionPerColonist)) + this.PlusFlatProductionPerTurn;
            this.GrossProductionPerTurn = this.GrossProductionPerTurn + this.Owner.data.Traits.ProductionMod * this.GrossProductionPerTurn;


            if (this.Station != null && !LoadUniverse)
            {
                if (!this.HasShipyard)
                    this.Station.SetVisibility(false, Planet.universeScreen.ScreenManager, this);
                else
                    this.Station.SetVisibility(true, Planet.universeScreen.ScreenManager, this);
            }
            
            this.consumption =  ( this.Population / 1000 +  this.Owner.data.Traits.ConsumptionModifier * this.Population / 1000);
            if(this.Owner.data.Traits.Cybernetic >0)
            {
                if(this.Population >0.1 && this.NetProductionPerTurn <=0)
                {

                }
            }
            //Money
            this.GrossMoneyPT = this.Population / 1000f;
            this.GrossMoneyPT += this.PlusTaxPercentage * this.GrossMoneyPT;
            //this.GrossMoneyPT += this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            //this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
            this.MAX_STORAGE = (float)this.StorageAdded;
            if (this.MAX_STORAGE < 10)
                this.MAX_STORAGE = 10f;
        }

        private void HarvestResources()
        {
            this.unfed = 0.0f;
            if (this.Owner.data.Traits.Cybernetic > 0 )
            {
                this.FoodHere = 0.0f;
                this.NetProductionPerTurn -= this.consumption;

                 if(this.NetProductionPerTurn <0f)
                   this.ProductionHere += this.NetProductionPerTurn;
                
              if ( this.ProductionHere >  this.MAX_STORAGE)
                {
                    this.unfed = 0.0f;
                    this.ProductionHere = this.MAX_STORAGE;
                }
                else if ( this.ProductionHere < 0 )
                {
                    
                    this.unfed = this.ProductionHere;
                    this.ProductionHere = 0.0f;
                }
            }
            else
            {
                this.NetFoodPerTurn -= this.consumption;
                this.FoodHere += this.NetFoodPerTurn;
                if ( this.FoodHere >  this.MAX_STORAGE)
                {
                    this.unfed = 0.0f;
                    this.FoodHere = this.MAX_STORAGE;
                }
                else if ( this.FoodHere < 0 )
                {
                    this.unfed = this.FoodHere;
                    this.FoodHere = 0.0f;
                }
            }
            foreach (Building building1 in this.BuildingList)
            {
                if (building1.ResourceCreated != null)
                {
                    if (building1.ResourceConsumed != null)
                    {
                        if ( this.ResourcesDict[building1.ResourceConsumed] >=  building1.ConsumptionPerTurn)
                        {
                            Dictionary<string, float> dictionary1;
                            string index1;
                            (dictionary1 = this.ResourcesDict)[index1 = building1.ResourceConsumed] = dictionary1[index1] - building1.ConsumptionPerTurn;
                            Dictionary<string, float> dictionary2;
                            string index2;
                            (dictionary2 = this.ResourcesDict)[index2 = building1.ResourceCreated] = dictionary2[index2] + building1.OutputPerTurn;
                        }
                    }
                    else if (building1.CommodityRequired != null)
                    {
                        if (this.CommoditiesPresent.Contains(building1.CommodityRequired))
                        {
                            foreach (Building building2 in this.BuildingList)
                            {
                                if (building2.IsCommodity && building2.Name == building1.CommodityRequired)
                                {
                                    Dictionary<string, float> dictionary;
                                    string index;
                                    (dictionary = this.ResourcesDict)[index = building1.ResourceCreated] = dictionary[index] + building1.OutputPerTurn;
                                }
                            }
                        }
                    }
                    else
                    {
                        Dictionary<string, float> dictionary;
                        string index;
                        (dictionary = this.ResourcesDict)[index = building1.ResourceCreated] = dictionary[index] + building1.OutputPerTurn;
                    }
                }
            }
        }

        public int GetGoodAmount(string good)
        {
            return (int)this.ResourcesDict[good];
        }

        private void GrowPopulation()
        {
            if (this.Owner == null)
                return;
            
            float num1 = this.Owner.data.BaseReproductiveRate * this.Population;
            if ( num1 >  this.Owner.data.Traits.PopGrowthMax * 1000  &&  this.Owner.data.Traits.PopGrowthMax != 0 )
                num1 = this.Owner.data.Traits.PopGrowthMax * 1000f;
            if ( num1 <  this.Owner.data.Traits.PopGrowthMin * 1000 )
                num1 = this.Owner.data.Traits.PopGrowthMin * 1000f;
            float num2 = num1 + this.PlusFlatPopulationPerTurn;
            float num3 = num2 + this.Owner.data.Traits.ReproductionMod * num2;
            if ( Math.Abs(this.unfed) <= 0 )
            {

                this.Population += num3;
                if ( this.Population +  num3 >  this.MaxPopulation +  this.MaxPopBonus)
                    this.Population = this.MaxPopulation + this.MaxPopBonus;
            }
            else
                this.Population += this.unfed * 10f;
            if ( this.Population >= 100.0)
                return;
            this.Population = 100f;
        }

        public float CalculateGrowth(float EstimatedFoodGain)
        {
            if (this.Owner != null)
            {
                float num1 = this.Owner.data.BaseReproductiveRate * this.Population;
                if ( num1 >  this.Owner.data.Traits.PopGrowthMax)
                    num1 = this.Owner.data.Traits.PopGrowthMax;
                if ( num1 < this.Owner.data.Traits.PopGrowthMin)
                    num1 = this.Owner.data.Traits.PopGrowthMin;
                float num2 = num1 + this.PlusFlatPopulationPerTurn;
                float num3 = num2 + this.Owner.data.Traits.ReproductionMod * num2;
                if (this.Owner.data.Traits.Cybernetic > 0)
                {
                    if ( this.ProductionHere + this.NetProductionPerTurn -  this.consumption <= 0.1)
                        return -(Math.Abs(this.ProductionHere + this.NetProductionPerTurn - this.consumption) / 10f);
                    if ( this.Population <  this.MaxPopulation +  this.MaxPopBonus &&  this.Population +  num3 <  this.MaxPopulation +  this.MaxPopBonus)
                        return this.Owner.data.BaseReproductiveRate * this.Population;
                }
                else
                {
                    if ( this.FoodHere +  this.NetFoodPerTurn -  this.consumption <= 0f)
                        return -(Math.Abs(this.FoodHere + this.NetFoodPerTurn - this.consumption) / 10f);
                    if ( this.Population <  this.MaxPopulation +  this.MaxPopBonus &&   this.Population +  num3 <  this.MaxPopulation +  this.MaxPopBonus)
                        return this.Owner.data.BaseReproductiveRate * this.Population;
                }
            }
            return 0.0f;
        }

        public void InitializeUpdate()
        {
            this.shield = new Shield();
            this.shield.World = Matrix.Identity * Matrix.CreateScale(2f) * Matrix.CreateRotationZ(0.0f) * Matrix.CreateTranslation(this.Position.X, this.Position.Y, 2500f);
            this.shield.Center = new Vector3(this.Position.X, this.Position.Y, 2500f);
            this.shield.displacement = 0.0f;
            this.shield.texscale = 2.8f;
            this.shield.Rotation = 0.0f;
            ShieldManager.PlanetaryShieldList.Add(this.shield);
            //this.initializing = true;
            this.UpdateDescription();
            this.SO = new SceneObject(((ReadOnlyCollection<ModelMesh>)ResourceManager.GetModel("Model/SpaceObjects/planet_" + (object)this.planetType).Meshes)[0]);
            this.SO.ObjectType = ObjectType.Dynamic;
            this.SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(this.scale) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
            this.RingWorld = Matrix.Identity * Matrix.CreateRotationX(this.ringTilt.ToRadians()) * Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
            
            //this.initializing = false;
        }

        public void AddGood(string UID, int Amount)
        {
            if (this.ResourcesDict.ContainsKey(UID))
            {
                Dictionary<string, float> dictionary;
                string index;
                (dictionary = this.ResourcesDict)[index = UID] = dictionary[index] + (float)Amount;
            }
            else
                this.ResourcesDict.Add(UID, (float)Amount);
        }

        private  void UpdatePosition(float elapsedTime)
        {
            this.Zrotate += this.ZrotateAmount * elapsedTime;
            if (!Planet.universeScreen.Paused)
            {
                this.OrbitalAngle += (float)Math.Asin(15.0 /  this.OrbitalRadius);
                if ( this.OrbitalAngle >= 360.0f)
                    this.OrbitalAngle -= 360f;
            }
            this.PosUpdateTimer -= elapsedTime;
            if ( this.PosUpdateTimer <= 0.0f || this.system.isVisible)
            {
                PosUpdateTimer = 5f;
                Position = ParentSystem.Position.PointOnCircle(OrbitalAngle, OrbitalRadius);
            }
            if (this.system.isVisible)
            {
                BoundingSphere boundingSphere = new BoundingSphere(new Vector3(this.Position, 0.0f), 300000f);
                Parallel.Invoke(() =>
                {
                    this.SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(this.scale) * Matrix.CreateRotationZ(-this.Zrotate) * Matrix.CreateRotationX(-45f.ToRadians()) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
                },
                () =>
                {
                    this.cloudMatrix = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(this.scale) * Matrix.CreateRotationZ((float)(-(double)this.Zrotate / 1.5)) * Matrix.CreateRotationX(-45f.ToRadians()) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
                },
                () =>
                {
                    this.RingWorld = Matrix.Identity * Matrix.CreateRotationX(this.ringTilt.ToRadians()) * Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
                }
                );
                


                
                this.SO.Visibility = ObjectVisibility.Rendered;
            }
            else
                this.SO.Visibility = ObjectVisibility.None;
        }



        private void UpdateDescription()
        {
            if (this.SpecialDescription != null)
            {
                this.Description = this.SpecialDescription;
            }
            else
            {
                this.Description = "";
                Planet planet1 = this;
                string str1 = planet1.Description + this.Name + " " + this.planetComposition + ". ";
                planet1.Description = str1;
                if ( this.Fertility > 2 )
                {
                    if (this.planetType == 21)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1729);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 13 || this.planetType == 22)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1730);
                        planet2.Description = str2;
                    }
                    else
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1731);
                        planet2.Description = str2;
                    }
                }
                else if ( this.Fertility > 1 )
                {
                    if (this.planetType == 19)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1732);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 21)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1733);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 13 || this.planetType == 22)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1734);
                        planet2.Description = str2;
                    }
                    else
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1735);
                        planet2.Description = str2;
                    }
                }
                else if ( this.Fertility > 0.6f)
                {
                    if (this.planetType == 14)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1736);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 21)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1737);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 17)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1738);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 19)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1739);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 18)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1740);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 11)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1741);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 13 || this.planetType == 22)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1742);
                        planet2.Description = str2;
                    }
                    else
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1743);
                        planet2.Description = str2;
                    }
                }
                else
                {
                    double num = (double)RandomMath.RandomBetween(1f, 2f);
                    if (this.planetType == 9 || this.planetType == 23)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1744);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 20 || this.planetType == 15)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1745);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 17)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1746);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 18)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1747);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 11)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1748);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 14)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1749);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 2 || this.planetType == 6 || this.planetType == 10)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1750);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 3 || this.planetType == 4 || this.planetType == 16)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1751);
                        planet2.Description = str2;
                    }
                    else if (this.planetType == 1)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1752);
                        planet2.Description = str2;
                    }
                    else if (this.habitable)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description ?? "";
                        planet2.Description = str2;
                    }
                    else
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1753);
                        planet2.Description = str2;
                    }
                }
                if ( this.Fertility < 0.6f &&  this.MineralRichness >= 2 && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1754);
                    planet2.Description = str2;
                    if ( this.MineralRichness > 3 )
                    {
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1755);
                        planet3.Description = str3;
                    }
                    else if ( this.MineralRichness >= 2 )
                    {
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1756);
                        planet3.Description = str3;
                    }
                    else
                    {
                        if ( this.MineralRichness < 1 )
                            return;
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1757);
                        planet3.Description = str3;
                    }
                }
                else if ( this.MineralRichness > 3  && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1758);
                    planet2.Description = str2;
                }
                else if ( this.MineralRichness >= 2  && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + this.Name + Localizer.Token(1759);
                    planet2.Description = str2;
                }
                else if ( this.MineralRichness >= 1 && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + this.Name + Localizer.Token(1760);
                    planet2.Description = str2;
                }
                else
                {
                    if ( this.MineralRichness >= 1 || !this.habitable)
                        return;
                    if (this.planetType == 14)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + this.Name + Localizer.Token(1761);
                        planet2.Description = str2;
                    }
                    else
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + this.Name + Localizer.Token(1762);
                        planet2.Description = str2;
                    }
                }
            }
        }

        private void UpdateDevelopmentStatus()
        {
            this.Density = this.Population / 1000f;
            float num = this.MaxPopulation / 1000f;
            if (this.Density <= 0.5f)
            {
                this.developmentLevel = 1;
                this.DevelopmentStatus = Localizer.Token(1763);
                if ( num >= 2  && this.Type != "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1764);
                    planet.DevelopmentStatus = str;
                }
                else if ( num >= 2f && this.Type == "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1765);
                    planet.DevelopmentStatus = str;
                }
                else if (num < 0 && this.Type != "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1766);
                    planet.DevelopmentStatus = str;
                }
                else if ( num < 0.5f && this.Type == "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1767);
                    planet.DevelopmentStatus = str;
                }
            }
            else if ( this.Density > 0.5f &&  this.Density <= 2 )
            {
                this.developmentLevel = 2;
                this.DevelopmentStatus = Localizer.Token(1768);
                if ( num >= 2 )
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1769);
                    planet.DevelopmentStatus = str;
                }
                else if ( num < 2 )
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1770);
                    planet.DevelopmentStatus = str;
                }
            }
            else if ( this.Density > 2.0 && this.Density <= 5.0)
            {
                this.developmentLevel = 3;
                this.DevelopmentStatus = Localizer.Token(1771);
                if ((double)num >= 5.0)
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1772);
                    planet.DevelopmentStatus = str;
                }
                else if ((double)num < 5.0)
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1773);
                    planet.DevelopmentStatus = str;
                }
            }
            else if ((double)this.Density > 5.0 && (double)this.Density <= 10.0)
            {
                this.developmentLevel = 4;
                this.DevelopmentStatus = Localizer.Token(1774);
            }
            else if ((double)this.Density > 10.0)
            {
                this.developmentLevel = 5;
                this.DevelopmentStatus = Localizer.Token(1775);
            }
            if ((double)this.NetProductionPerTurn >= 10.0 && this.HasShipyard)
            {
                Planet planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1776);
                planet.DevelopmentStatus = str;
            }
            else if ((double)this.Fertility >= 2.0 && (double)this.NetFoodPerTurn > (double)this.MaxPopulation)
            {
                Planet planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1777);
                planet.DevelopmentStatus = str;
            }
            else if ((double)this.NetResearchPerTurn > 5.0)
            {
                Planet planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1778);
                planet.DevelopmentStatus = str;
            }
            if (!this.AllowInfantry || this.TroopsHere.Count <= 6)
                return;
            Planet planet1 = this;
            string str1 = planet1.DevelopmentStatus + Localizer.Token(1779);
            planet1.DevelopmentStatus = str1;
        }

        //added by gremlin: get a planets ground combat strength
        public float GetGroundStrength(Empire empire)
        {
            float num = 0;
            if (this.Owner == empire)
                num += this.BuildingList.Sum(offense => offense.CombatStrength);
            this.TroopsHere.thisLock.EnterReadLock();
            num += this.TroopsHere.Where(empiresTroops => empiresTroops.GetOwner() == empire).Sum(strength => strength.Strength);
            this.TroopsHere.thisLock.ExitReadLock();
            return num;


        }
        public int GetPotentialGroundTroops()
        {
            int num = 0;
            
            foreach(PlanetGridSquare PGS in this.TilesList)
            {
                num += PGS.number_allowed_troops;
                
            }
            return num; //(int)(this.TilesList.Sum(spots => spots.number_allowed_troops));// * (.25f + this.developmentLevel*.2f));


        }
        public float GetGroundStrengthOther(Empire AllButThisEmpire)
        {
            //float num = 0;
            //if (this.Owner == null || this.Owner != empire)
            //    num += this.BuildingList.Sum(offense => offense.CombatStrength > 0 ? offense.CombatStrength : 1);
            //this.TroopsHere.thisLock.EnterReadLock();
            //num += this.TroopsHere.Where(empiresTroops => empiresTroops.GetOwner() == null || empiresTroops.GetOwner() != empire).Sum(strength => strength.Strength);
            //this.TroopsHere.thisLock.ExitReadLock();
            //return num;
            float EnemyTroopStrength = 0f;
            this.TroopsHere.ForEach(trooper =>
            {

                if (trooper.GetOwner() != AllButThisEmpire)
                {
                    EnemyTroopStrength += trooper.Strength;
                }
            });
            for(int i =0; i<this.BuildingList.Count;i++)
            {
                Building b;
                try
                {
                    b = this.BuildingList[i];
                }
                catch
                {
                    continue;
                }
                if (b == null)
                    continue;
                if(b.CombatStrength>0)
                EnemyTroopStrength += b.Strength + b.CombatStrength;
            }
          
            return EnemyTroopStrength;
            //foreach (PlanetGridSquare pgs in this.TilesList)
            //{
            //    pgs.TroopsHere.thisLock.EnterReadLock();
            //    Troop troop = null;
            //    while (pgs.TroopsHere.Count > 0)
            //    {
            //        if (pgs.TroopsHere.Count > 0)
            //            troop = pgs.TroopsHere[0];

            //        if (troop == null && this.Owner != AllButThisEmpire)
            //        {
            //            if (pgs.building == null || pgs.building.CombatStrength <= 0)
            //            {

            //                break;
            //            }
            //            EnemyTroopStrength = EnemyTroopStrength + (pgs.building.CombatStrength + (pgs.building.Strength)); //+ (pgs.building.isWeapon ? pgs.building.theWeapon.DamageAmount:0));
            //            break;
            //        }
            //        else if (troop != null && troop.GetOwner() != AllButThisEmpire)
            //        {
            //            EnemyTroopStrength = EnemyTroopStrength + troop.Strength;
            //        }
            //        if (this.Owner == AllButThisEmpire || pgs.building == null || pgs.building.CombatStrength <= 0)
            //        {

            //            break;
            //        }
            //        EnemyTroopStrength = EnemyTroopStrength + pgs.building.CombatStrength + pgs.building.Strength;
            //        break;
            //    }
            //    pgs.TroopsHere.thisLock.ExitReadLock();
            //}
            //return EnemyTroopStrength ;
        }
        public bool TroopsHereAreEnemies(Empire empire)
        {
            bool enemies = false;
            this.TroopsHere.thisLock.EnterReadLock();
            foreach (Troop trooper in this.TroopsHere)
            {
                if (!empire.TryGetRelations(trooper.GetOwner(), out Relationship trouble) || trouble.AtWar)
                {
                    enemies=true;
                    break;
                }

            }
            this.TroopsHere.thisLock.ExitReadLock();
            return enemies;
        }
        public bool EventsOnBuildings()
        {
            bool events = false;            
            foreach (Building building in this.BuildingList)
            {
                if (building.EventTriggerUID !=null && !building.EventWasTriggered)
                {
                    events = true;
                    break;
                }

            }            
            return events;
        }
        public int GetGroundLandingSpots()
        {
            int spotCount =this.TilesList.Where(spots => spots.building == null).Sum(spots => spots.number_allowed_troops);            
            int troops = this.TroopsHere.Where(owner=> owner.GetOwner() == this.Owner) .Count();
            return spotCount -troops;


        }

        //Added by McShooterz: heal builds and troops every turn
        public void HealTroops()
        {
            if (this.RecentCombat)
                return;
            //heal troops
            this.TroopsHere.thisLock.EnterReadLock();
            foreach (Troop troop in this.TroopsHere)
            {
                troop.Strength = troop.GetStrengthMax();
            }
            this.TroopsHere.thisLock.ExitReadLock();
        }

        public enum ColonyType
        {
            Core,
            Colony,
            Industrial,
            Research,
            Agricultural,
            Military,
            TradeHub,
        }

        public enum Richness
        {
            UltraPoor,
            Poor,
            Average,
            Rich,
            UltraRich,
        }

        public enum GoodState
        {
            STORE,
            IMPORT,
            EXPORT,
        }

        public class OrbitalDrop
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
            public PlanetGridSquare Target;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Planet() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.ActiveCombats != null)
                        this.ActiveCombats.Dispose();
                    if (this.OrbitalDropList != null)
                        this.OrbitalDropList.Dispose();
                    if (this.ConstructionQueue != null)
                        this.ConstructionQueue.Dispose();
                    if (this.BasedShips != null)
                        this.BasedShips.Dispose();
                    if (this.Projectiles != null)
                        this.Projectiles.Dispose();

                }
                this.ActiveCombats = null;
                this.OrbitalDropList = null;
                this.ConstructionQueue = null;
                this.BasedShips = null;
                this.Projectiles = null;
                this.disposed = true;
            }
        }
    }
}

/* Old core governor sliders calcs
 * this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.5f);
                            if ((double)this.NetFoodPerTurn - (double)this.consumption < 0.0 && (double)this.FoodHere / (double)this.MAX_STORAGE < 0.75)
                            {
                                this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.01f);
                                float num = 1f - this.FarmerPercentage;
                                //Added by McShooterz: No research percentage if not researching
                                if (this.Owner.ResearchTopic != "")
                                {
                                    if (this.ConstructionQueue.Count() != 0 || this.ProductionHere < this.MAX_STORAGE)
                                    {
                                        this.WorkerPercentage = (float)(num * 2.0 / 5.0);
                                        this.ResearcherPercentage = (float)(num * 3.0 / 5.0);
                                    }
                                    else
                                    {
                                        this.WorkerPercentage = 0f;
                                        this.ResearcherPercentage = num;
                                    }
                                }
                                else
                                {
                                    this.WorkerPercentage = num;
                                    this.ResearcherPercentage = 0.0f;
                                }
                            }
                            else
                                this.fs = (double)this.FoodHere >= (double)this.MAX_STORAGE * 0.25 ? Planet.GoodState.EXPORT : Planet.GoodState.STORE;
                            if ((double)this.Population / ((double)this.MaxPopulation + (double)this.MaxPopBonus) < 0.95)
                                this.fs = Planet.GoodState.IMPORT;
                            if (this.DetermineIfSelfSufficient() && (double)this.FoodHere > 1.0 && (double)this.Population / ((double)this.MaxPopulation + (double)this.MaxPopBonus) < 0.95)
                            {
                                this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(-0.5f);
                                float num = 1f - this.FarmerPercentage;
                                //Added by McShooterz: No research percentage if not researching
                                if (this.Owner.ResearchTopic != "")
                                {
                                    if (this.ConstructionQueue.Count() != 0 || this.ProductionHere < this.MAX_STORAGE)
                                    {
                                        this.WorkerPercentage = num / 2f;
                                        this.ResearcherPercentage = num / 2f;
                                    }
                                    else
                                    {
                                        this.WorkerPercentage = 0f;
                                        this.ResearcherPercentage = num;
                                    }
                                }
                                else
                                {
                                    this.WorkerPercentage = num;
                                    this.ResearcherPercentage = 0.0f;
                                }
                                this.fs = Planet.GoodState.IMPORT;
                            }
                            else if ((double)this.ProductionHere / (double)this.MAX_STORAGE > 0.75)
                            {
                                if ((double)this.FoodHere / (double)this.MAX_STORAGE < 0.75)
                                {
                                    this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.01f);
                                    float num = 1f - this.FarmerPercentage;
                                    //Added by McShooterz: No research percentage if not researching
                                    if (this.Owner.ResearchTopic != "")
                                    {
                                        if (this.ConstructionQueue.Count() != 0 || this.ProductionHere < this.MAX_STORAGE)
                                        {
                                            this.WorkerPercentage = num / 2f;
                                            this.ResearcherPercentage = num / 2f;
                                        }
                                        else
                                        {
                                            this.WorkerPercentage = 0f;
                                            this.ResearcherPercentage = num;
                                        }
                                    }
                                    else
                                    {
                                        this.WorkerPercentage = num;
                                        this.ResearcherPercentage = 0.0f;
                                    }
                                }
                                else
                                {
                                    this.FarmerPercentage = 0.0f;
                                    //Added by McShooterz: No research percentage if not researching
                                    if (this.Owner.ResearchTopic != "")
                                    {
                                        if (this.ConstructionQueue.Count() != 0 || this.ProductionHere < this.MAX_STORAGE)
                                        {
                                            this.WorkerPercentage = 0.5f;
                                            this.ResearcherPercentage = 0.5f;
                                        }
                                        else
                                        {
                                            this.WorkerPercentage = 0f;
                                            this.ResearcherPercentage = 1f;
                                        }
                                    }
                                    else
                                    {
                                        this.WorkerPercentage = 1f;
                                        this.ResearcherPercentage = 0f;
                                    }
                                }
                            }
                            else if ((double)this.FoodHere / (double)this.MAX_STORAGE > 0.75)
                            {
                                this.FarmerPercentage = 0.0f;
                                //Added by McShooterz: No research percentage if not researching
                                if (this.Owner.ResearchTopic != "")
                                {
                                    if (this.ConstructionQueue.Count() != 0 || this.ProductionHere < this.MAX_STORAGE)
                                    {
                                        this.WorkerPercentage = 0.7f;
                                        this.ResearcherPercentage = 0.3f;
                                    }
                                    else
                                    {
                                        this.WorkerPercentage = 0f;
                                        this.ResearcherPercentage = 1f;
                                    }
                                }
                                else
                                {
                                    this.WorkerPercentage = 1f;
                                    this.ResearcherPercentage = 0f;
                                }
                            }
                            else
                            {
                                this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.01f);
                                float num = 1f - this.FarmerPercentage;
                                //Added by McShooterz: No research percentage if not researching
                                if (this.Owner.ResearchTopic != "")
                                {
                                    if (this.ConstructionQueue.Count() != 0 || this.ProductionHere < this.MAX_STORAGE)
                                    {
                                        this.WorkerPercentage = (float)((double)num / 4.0 * 3.0);
                                        this.ResearcherPercentage = num / 4f;
                                    }
                                    else
                                    {
                                        this.WorkerPercentage = 0f;
                                        this.ResearcherPercentage = num;
                                    }
                                }
                                else
                                {
                                    this.WorkerPercentage = num;
                                    this.ResearcherPercentage = 0.0f;
                                }
                            }
 */
