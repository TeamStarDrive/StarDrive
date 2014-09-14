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
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace Ship_Game
{
    public class Planet
    {
        public bool GovBuildings = true;
        public bool GovSliders = true;
        public BatchRemovalCollection<Combat> ActiveCombats = new BatchRemovalCollection<Combat>();
        public Guid guid = Guid.NewGuid();
        public List<PlanetGridSquare> TilesList = new List<PlanetGridSquare>();
        public string Special = "None";
        public BatchRemovalCollection<Planet.OrbitalDrop> OrbitalDropList = new BatchRemovalCollection<Planet.OrbitalDrop>();
        public Planet.GoodState fs = Planet.GoodState.IMPORT;
        public Planet.GoodState ps = Planet.GoodState.IMPORT;
        public Dictionary<Empire, bool> ExploredDict = new Dictionary<Empire, bool>();
        public List<Building> BuildingList = new List<Building>();
        public SpaceStation Station = new SpaceStation();
        public Dictionary<Guid, Ship> Shipyards = new Dictionary<Guid, Ship>();
        public List<Troop> TroopsHere = new List<Troop>();
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
        private Dictionary<string, float> ResourcesDict = new Dictionary<string, float>();
        private float PosUpdateTimer = 1f;
        public float MAX_STORAGE = 10f;
        public string DevelopmentStatus = "Undeveloped";
        public List<string> Guardians = new List<string>();
        public bool FoodLocked;
        public bool ProdLocked;
        public bool ResLocked;
        public bool RecentCombat;
        public int Crippled_Turns;
        public int numGuardians;
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
        public float ObjectRadius;
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
        public float TotalOreExtracted;
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
        public float PlusFlatProductionPerTurn;
        public float ProductionPercentAdded;
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
        private float PlusCreditsPerColonist;
        public bool HasWinBuilding;
        public float ShipBuildingModifier;
        public float consumption;
        private float unfed;
        private Shield shield;
        public float FoodHere;
        public int developmentLevel;
        public bool CorsairPresence;
        public bool queueEmptySent ;
        public float RepairPerTurn = 50;
        public List<string> PlanetFleets = new List<string>();
        
        public Planet()
        {
            foreach (KeyValuePair<string, Good> keyValuePair in ResourceManager.GoodsDict)
                this.AddGood(keyValuePair.Key, 0);
        }

        public void DropBombORIG(Bomb bomb)
        {
            if (bomb.owner == this.Owner)
                return;
            if (this.Owner != null && !this.Owner.GetRelations()[bomb.owner].AtWar && (this.TurnsSinceTurnover > 10 && EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) == bomb.owner))
                this.Owner.GetGSAI().DeclareWarOn(bomb.owner, WarType.DefensiveWar);
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
                this.shield.Rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Position, new Vector2(bomb.Position.X, bomb.Position.Y)));
                this.shield.displacement = 0.0f;
                this.shield.texscale = 2.8f;
                this.shield.Radius = this.SO.WorldBoundingSphere.Radius + 100f;
                this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                this.shield.texscale = 2.8f;
                this.shield.texscale = (float)(2.79999995231628 - 0.185000002384186 * (double)RandomMath.RandomBetween(1f, 10f));
                this.shield.Center = new Vector3(this.Position.X, this.Position.Y, 2500f);
                this.shield.pointLight.World = bomb.GetWorld();
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
                        this.Owner.GetPlanets().Remove(this);
                        if (this.ExploredDict[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)])
                        {
                            Planet.universeScreen.NotificationManager.AddPlanetDiedNotification(this, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty));
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
                                this.TroopsHere[index].SetOwner(bomb.owner);
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
            if (bomb.owner == this.Owner)
            {
                return;
            }
            if (this.Owner != null && !this.Owner.GetRelations()[bomb.owner].AtWar && this.TurnsSinceTurnover > 10 && EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) == bomb.owner)
            {
                this.Owner.GetGSAI().DeclareWarOn(bomb.owner, WarType.DefensiveWar);
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
                    foreach (PlanetGridSquare pgs in this.TilesList)
                    {
                        if (pgs.building == null && pgs.TroopsHere.Count <= 0)
                        {
                            continue;
                        }
                        PotentialHits.Add(pgs);
                    }
                    if (PotentialHits.Count <= 0)
                    {
                        hit = false;
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


                        //Added Code here
                        od.Target.Habitable = false;
                        od.Target.highlighted = false;
                        od.Target.Biosphere = false;
                        //Building Wasteland = new Building;
                        //Wasteland.Name="Fissionables";
                        //od.Target.building=Wasteland;






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
                        this.Owner.GetPlanets().Remove(this);
                        if (this.ExploredDict[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)])
                        {
                            Planet.universeScreen.NotificationManager.AddPlanetDiedNotification(this, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty));
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
                                    this.TroopsHere[i].SetOwner(bomb.owner);
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
                this.shield.Rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Position, new Vector2(bomb.Position.X, bomb.Position.Y)));
                this.shield.displacement = 0f;
                this.shield.texscale = 2.8f;
                this.shield.Radius = this.SO.WorldBoundingSphere.Radius + 100f;
                this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                this.shield.texscale = 2.8f;
                this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                this.shield.Center = new Vector3(this.Position.X, this.Position.Y, 2500f);
                this.shield.pointLight.World = bomb.GetWorld();
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
            if (!(this.Type == "Terran"))
                return this.Type;
            switch (this.planetType)
            {
                case 1:
                    return "Terran";
                case 13:
                    return "Terran_2";
                case 22:
                    return "Terran_3";
                default:
                    return "Terran";
            }
        }

        public string GetTypeTranslation()
        {
            switch (this.Type)
            {
                case "Terran":
                    return Localizer.Token(1447);
                case "Barren":
                    return Localizer.Token(1448);
                case "Gas Giant":
                    return Localizer.Token(1449);
                case "Volcanic":
                    return Localizer.Token(1450);
                case "Tundra":
                    return Localizer.Token(1451);
                case "Desert":
                    return Localizer.Token(1452);
                case "Steppe":
                    return Localizer.Token(1453);
                case "Swamp":
                    return Localizer.Token(1454);
                case "Ice":
                    return Localizer.Token(1455);
                case "Oceanic":
                    return Localizer.Token(1456);
                default:
                    return "";
            }
        }

        public void SetPlanetAttributes()
        {
            this.hasEarthLikeClouds = false;
            float num1 = RandomMath.RandomBetween(0.0f, 100f);
            if ((double)num1 >= 92.5)
            {
                //this.richness = Planet.Richness.UltraRich;
                this.MineralRichness = RandomMath.RandomBetween(2f, 2.5f);
            }
            else if ((double)num1 >= 85.0)
            {
                //this.richness = Planet.Richness.Rich;
                this.MineralRichness = RandomMath.RandomBetween(1.5f, 2f);
            }
            else if ((double)num1 >= 25.0)
            {
                //this.richness = Planet.Richness.Average;
                this.MineralRichness = RandomMath.RandomBetween(0.75f, 1.5f);
            }
            else if ((double)num1 >= 12.5)
            {
                this.MineralRichness = RandomMath.RandomBetween(0.25f, 0.75f);
                //this.richness = Planet.Richness.Poor;
            }
            else if ((double)num1 < 12.5)
            {
                this.MineralRichness = RandomMath.RandomBetween(0.1f, 0.25f);
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
            if (!this.habitable)
                this.MineralRichness = 0.0f;
            if (this.Type == "Barren")
            {
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        double num3 = (double)RandomMath.RandomBetween(0.0f, 100f);
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, false));
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
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 < 15));
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
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 > 25));
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
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 > 50));
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
                        this.TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, (Building)null, num3 > 33));
                    }
                }
            }
            if ((double)RandomMath.RandomBetween(0.0f, 100f) <= 15.0 && this.habitable)
            {
                List<string> list = new List<string>();
                foreach (KeyValuePair<string, Building> keyValuePair in ResourceManager.BuildingsDict)
                {
                    if (keyValuePair.Value.EventTriggerUID != "" && !keyValuePair.Value.NoRandomSpawn)
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
            this.SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(this.scale) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
            this.RingWorld = Matrix.Identity * Matrix.CreateRotationX(MathHelper.ToRadians(this.ringTilt)) * Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
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
                        if (eventLocation.building == null || !(eventLocation.building.EventTriggerUID != "") || (eventLocation.TroopsHere.Count <= 0 || eventLocation.TroopsHere[0].GetOwner().isFaction))
                            return true;
                        ResourceManager.EventsDict[eventLocation.building.EventTriggerUID].TriggerPlanetEvent(this, eventLocation.TroopsHere[0].GetOwner(), eventLocation, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty), Planet.universeScreen);
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
            else if (b.Name == "Outpost" || b.EventTriggerUID != "")
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
            ship.SetHome(this);
            this.BasedShips.Add(ship);
        }

        private void DoViewedCombat(float elapsedTime)
        {
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
        }

        private void DoCombatUnviewed(float elapsedTime)
        {
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
                if (planetGridSquare.building != null && planetGridSquare.building.CombatStrength > 0)
                    ++num1;
            }
            if (num2 > this.numInvadersLast && this.numInvadersLast == 0)
            {
                if (EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) == this.Owner)
                    Planet.universeScreen.NotificationManager.AddEnemyTroopsLandedNotification(this, index, this.Owner);
                else if (index == EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) && !this.Owner.isFaction && !EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty).GetRelations()[this.Owner].AtWar)
                {
                    if (EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty).GetRelations()[this.Owner].Treaty_NAPact)
                    {
                        Planet.universeScreen.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.Owner, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty), "Invaded NA Pact", this.system));
                        EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty).GetGSAI().DeclareWarOn(this.Owner, WarType.ImperialistWar);
                        this.Owner.GetRelations()[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)].Trust -= 50f;
                        this.Owner.GetRelations()[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)].Anger_DiplomaticConflict += 50f;
                    }
                    else
                    {
                        Planet.universeScreen.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.Owner, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty), "Invaded Start War", this.system));
                        EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty).GetGSAI().DeclareWarOn(this.Owner, WarType.ImperialistWar);
                        this.Owner.GetRelations()[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)].Trust -= 25f;
                        this.Owner.GetRelations()[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)].Anger_DiplomaticConflict += 25f;
                    }
                }
            }
            this.numInvadersLast = num2;
            if (num2 <= 0 || num1 != 0 || this.Owner == null)
                return;
            if (index.GetRelations().ContainsKey(this.Owner))
            {
                if (index.GetRelations()[this.Owner].AtWar && index.GetRelations()[this.Owner].ActiveWar != null)
                    ++index.GetRelations()[this.Owner].ActiveWar.ColoniestWon;
            }
            else if (this.Owner.GetRelations().ContainsKey(index) && this.Owner.GetRelations()[index].AtWar && this.Owner.GetRelations()[index].ActiveWar != null)
                ++this.Owner.GetRelations()[index].ActiveWar.ColoniesLost;
            this.ConstructionQueue.Clear();
            foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                planetGridSquare.QItem = (QueueItem)null;
            this.Owner.GetPlanets().Remove(this);
            if (index == EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) && this.Owner == EmpireManager.GetEmpireByName("Cordrazine Collective"))
                GlobalStats.IncrementCordrazineCapture();
            if (this.ExploredDict[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)] && !flag)
                Planet.universeScreen.NotificationManager.AddConqueredNotification(this, index, this.Owner);
            else if (this.ExploredDict[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)])
            {
                lock (GlobalStats.OwnedPlanetsLock)
                {
                    Planet.universeScreen.NotificationManager.AddPlanetDiedNotification(this, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty));
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
            this.Owner = index;
            this.TurnsSinceTurnover = 0;
            this.Owner.AddPlanet(this);
            this.ConstructionQueue.Clear();
            this.system.OwnerList.Clear();
            foreach (KeyValuePair<Guid, Ship> keyValuePair in this.Shipyards)
                keyValuePair.Value.loyalty = this.Owner;
            foreach (Planet planet in this.system.PlanetList)
            {
                if (planet.Owner != null && !this.system.OwnerList.Contains(planet.Owner))
                    this.system.OwnerList.Add(planet.Owner);
            }
            if (index.isFaction)
                return;
            this.Owner.GetGSAI().ReformulateWarGoals();
           
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
                if (planetGridSquare.TroopsHere.Count > 0 && planetGridSquare.TroopsHere[0].GetOwner() != this.Owner || planetGridSquare.building != null && planetGridSquare.building.EventTriggerUID != "")
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
                        if (pgs.TroopsHere[0].GetOwner() != EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) || !Planet.universeScreen.LookingAtPlanet || (!(Planet.universeScreen.workersPanel is CombatScreen) || (Planet.universeScreen.workersPanel as CombatScreen).p != this) || GlobalStats.AutoCombat)
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
                                    else if (planetGridSquare.building != null && (planetGridSquare.building.CombatStrength > 0 || planetGridSquare.building.EventTriggerUID != "") && (this.Owner != pgs.TroopsHere[0].GetOwner() || planetGridSquare.building.EventTriggerUID != ""))
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
                    
                else if (pgs.building != null && pgs.building.CombatStrength > 0 && (this.Owner != EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) || !Planet.universeScreen.LookingAtPlanet || (!(Planet.universeScreen.workersPanel is CombatScreen) || (Planet.universeScreen.workersPanel as CombatScreen).p != this) || GlobalStats.AutoCombat) && pgs.building.AvailableAttackActions > 0)
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
                    if (eventLocation.building != null && eventLocation.building.CombatStrength > 0 || eventLocation.TroopsHere.Count > 0)
                        return false;
                    if (changex > 0)
                        start.TroopsHere[0].facingRight = true;
                    else if (changex < 0)
                        start.TroopsHere[0].facingRight = false;
                    start.TroopsHere[0].SetFromRect(start.TroopClickRect);
                    start.TroopsHere[0].MovingTimer = 0.75f;
                    --start.TroopsHere[0].AvailableMoveActions;
                    start.TroopsHere[0].MoveTimer = (float)start.TroopsHere[0].MoveTimerBase;
                    eventLocation.TroopsHere.Add(start.TroopsHere[0]);
                    start.TroopsHere.Clear();
                    if (eventLocation.building == null || !(eventLocation.building.EventTriggerUID != "") || (eventLocation.TroopsHere.Count <= 0 || eventLocation.TroopsHere[0].GetOwner().isFaction))
                        return true;
                    ResourceManager.EventsDict[eventLocation.building.EventTriggerUID].TriggerPlanetEvent(this, eventLocation.TroopsHere[0].GetOwner(), eventLocation, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty), Planet.universeScreen);
                }
            }
            return false;
        }

        public void Update(float elapsedTime)
        {
            this.DecisionTimer -= elapsedTime;
            this.CombatTimer -= elapsedTime;
            this.RecentCombat = (double)this.CombatTimer > 0.0;
            List<Guid> list = new List<Guid>();
            foreach (KeyValuePair<Guid, Ship> keyValuePair in this.Shipyards)
            {
                if (!keyValuePair.Value.Active || keyValuePair.Value.ModuleSlotList.Count == 0)
                    list.Add(keyValuePair.Key);
            }
            foreach (Guid key in list)
                this.Shipyards.Remove(key);
            if (!Planet.universeScreen.Paused)
            {
                if (this.TroopsHere.Count > 0)
                {
                    //try
                    {
                        this.DoCombats(elapsedTime);
                        if ((double)this.DecisionTimer <= 0.0)
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
                try
                {
                    Building building = this.BuildingList[index1];
                    if (building.isWeapon)
                    {
                        building.WeaponTimer -= elapsedTime;
                        if ((double)building.WeaponTimer < 0.0)
                        {
                            if (this.Owner != null)
                            {
                                for (int index2 = 0; index2 < this.system.ShipList.Count; ++index2)
                                {
                                    Ship ship = this.system.ShipList[index2];
                                    if (ship.loyalty != this.Owner && (ship.loyalty.isFaction || this.Owner.GetRelations()[ship.loyalty].AtWar) && (double)Vector2.Distance(this.Position, ship.Center) < (double)building.theWeapon.Range)
                                    {
                                        building.theWeapon.FireFromPlanet(ship.Center - this.Position, this, (GameplayObject)ship);
                                        building.WeaponTimer = building.theWeapon.fireDelay;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            for (int index = 0; index < this.Projectiles.Count; ++index)
            {
                Projectile projectile = this.Projectiles[index];
                if (projectile.Active)
                {
                    if ((double)elapsedTime > 0.0)
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
            for (int i = 0; i < this.system.ShipList.Count; i++)
            {
                Ship item = this.system.ShipList[i];
                if (item != null && item.loyalty == this.Owner && this.HasShipyard && Vector2.Distance(this.Position, item.Position) <= 5000f)
                {
                    item.PowerCurrent = item.PowerStoreMax;
                    item.Ordinance = item.OrdinanceMax;
                    if (GlobalStats.HardcoreRuleset)
                    {
                        foreach (KeyValuePair<string, float> maxGood in item.GetMaxGoods())
                        {
                            if (item.GetCargo()[maxGood.Key] >= maxGood.Value)
                            {
                                continue;
                            }
                            while (this.ResourcesDict[maxGood.Key] > 0f && item.GetCargo()[maxGood.Key] < maxGood.Value)
                            {
                                if (maxGood.Value - item.GetCargo()[maxGood.Key] < 1f)
                                {
                                    Dictionary<string, float> resourcesDict = this.ResourcesDict;
                                    Dictionary<string, float> strs = resourcesDict;
                                    string key = maxGood.Key;
                                    string str = key;
                                    resourcesDict[key] = strs[str] - (maxGood.Value - item.GetCargo()[maxGood.Key]);
                                    Dictionary<string, float> cargo = item.GetCargo();
                                    Dictionary<string, float> strs1 = cargo;
                                    string key1 = maxGood.Key;
                                    string str1 = key1;
                                    cargo[key1] = strs1[str1] + (maxGood.Value - item.GetCargo()[maxGood.Key]);
                                }
                                else
                                {
                                    Dictionary<string, float> resourcesDict1 = this.ResourcesDict;
                                    Dictionary<string, float> strs2 = resourcesDict1;
                                    string key2 = maxGood.Key;
                                    resourcesDict1[key2] = strs2[key2] - 1f;
                                    Dictionary<string, float> cargo1 = item.GetCargo();
                                    Dictionary<string, float> strs3 = cargo1;
                                    string str2 = maxGood.Key;
                                    cargo1[str2] = strs3[str2] + 1f;
                                }
                            }
                        }
                    }
                    //Modified by McShooterz: Repair based on repair pool, if no combat in system                 
                    if (RepairPool > 0 && item.Health < item.HealthMax && !this.ParentSystem.CombatInSystem)
                    {
                        foreach (ModuleSlot slot in item.ModuleSlotList.Where(slot => slot.module.ModuleType != ShipModuleType.Dummy && slot.module.Health < slot.module.HealthMax))
                        {
                            if (slot.module.HealthMax - slot.module.Health > RepairPool)
                            {
                                slot.module.Repair(RepairPool);
                                break;
                            }
                            else
                            {
                                RepairPool -= slot.module.HealthMax - slot.module.Health;
                                slot.module.Repair(slot.module.HealthMax);
                            }
                        }
                    }
                    if ((this.ParentSystem.combatTimer <= 0 || item.InCombatTimer <= 0) && this.TroopsHere.Count() > 0 && this.TroopsHere.Where(troop => troop.GetOwner() != this.Owner).Count() == 0)
                    {
                        foreach (var pgs in this.TilesList)
                        {
                            if (item.TroopCapacity ==0 || item.TroopList.Count >= item.TroopCapacity) 
                                break;
                            if (pgs.TroopsHere.Count > 0 && pgs.TroopsHere[0].GetOwner() == this.Owner)
                            {
                                Troop troop = pgs.TroopsHere[0];
                                item.TroopList.Add(troop);
                                pgs.TroopsHere.Clear();
                                this.TroopsHere.Remove(troop);
                            }
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
            if ((double)this.TerraformPoints > 0.0 && (double)this.Fertility < 1.0)
            {
                this.Fertility += this.TerraformToAdd;
                if (this.Type == "Barren" && (double)this.Fertility > 0.01)
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
                else if (this.Type == "Tundra" && (double)this.Fertility > 0.95)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Planet.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.SO);
                    this.planetType = 22;
                    this.Terraform();
                }
                if ((double)this.Fertility > 1.0)
                    this.Fertility = 1f;
            }
            this.UpdateIncomes();
            if (this.GovernorOn)
                this.DoGoverning();
            this.UpdateIncomes();
            // ADDED BY SHAHMATT (notification about empty queue)
            if (GlobalStats.ExtraNotiofications && this.Owner == EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) && this.ConstructionQueue.Count <= 0 && !this.queueEmptySent)
            {
                if (this.colonyType == Planet.ColonyType.Colony || this.colonyType == Planet.ColonyType.Core || this.colonyType == Planet.ColonyType.Industrial || !this.GovernorOn)
                {
                    this.queueEmptySent = true;
                    Notification cNote = new Notification()
                    {
                        RelevantEmpire = this.Owner,
                        Message = string.Concat(this.Name, " is not producing anything."),
                        ReferencedItem1 = this, //this.system,
                        IconPath = string.Concat("Planets/", this.planetType),//"UI/icon_warning_money",
                        Action = "SnapToPlanet", //"SnapToSystem",
                        ClickRect = new Rectangle(Planet.universeScreen.NotificationManager.NotificationArea.X, Planet.universeScreen.NotificationManager.NotificationArea.Y, 64, 64),
                        DestinationRect = new Rectangle(Planet.universeScreen.NotificationManager.NotificationArea.X, Planet.universeScreen.NotificationManager.NotificationArea.Y + Planet.universeScreen.NotificationManager.NotificationArea.Height - (Planet.universeScreen.NotificationManager.NotificationList.Count + 1) * 70, 64, 64)
                    };
                    AudioManager.PlayCue("sd_ui_notification_warning");
                    lock (GlobalStats.NotificationLocker)
                    {
                        Planet.universeScreen.NotificationManager.NotificationList.Add(cNote);
                    }
                }
            }
            else if (GlobalStats.ExtraNotiofications && this.Owner == EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) && this.ConstructionQueue.Count > 0)
            {
                this.queueEmptySent = false;
            }
            // END OF ADDED BY SHAHMATT
            //if ((double)this.ShieldStrengthCurrent < (double)this.ShieldStrengthMax)
            //{
            //    ++this.ShieldStrengthCurrent;
            //    if ((double)this.ShieldStrengthCurrent > (double)this.ShieldStrengthMax)
            //        this.ShieldStrengthCurrent = this.ShieldStrengthMax;
            //}
            //if ((double)this.ShieldStrengthCurrent > (double)this.ShieldStrengthMax)
            //    this.ShieldStrengthCurrent = this.ShieldStrengthMax;
            //added by gremlin Planetary Shield Change
            if (this.ShieldStrengthCurrent < this.ShieldStrengthMax)
            {
                Planet shieldStrengthCurrent = this;
                shieldStrengthCurrent.ShieldStrengthCurrent = shieldStrengthCurrent.ShieldStrengthCurrent + 1f;
                if (this.ShieldStrengthCurrent > this.ShieldStrengthMax)
                {
                    this.ShieldStrengthCurrent = this.ShieldStrengthMax;
                }
                if (this.ShieldStrengthCurrent > this.ShieldStrengthMax / 10 && !this.RecentCombat)
                {
                    shieldStrengthCurrent.ShieldStrengthCurrent += shieldStrengthCurrent.ShieldStrengthMax / 10;
                }
            }

            //this.UpdateTimer = 10f;
            this.HarvestResources();
            this.ApplyProductionTowardsConstruction();
            this.GrowPopulation();
            //Added by McShooterz         
            this.HealBuildingsAndTroops();
            if ((double)this.FoodHere > (double)this.MAX_STORAGE)
                this.FoodHere = this.MAX_STORAGE;
            if ((double)this.ProductionHere <= (double)this.MAX_STORAGE)
                return;
            this.ProductionHere = this.MAX_STORAGE;
        }

        private float CalculateCyberneticPercentForSurplus(float desiredSurplus)
        {
            float Surplus = 0.0f;
            while ((double)Surplus < 1.0)
            {
                Surplus += 0.01f;
                float num2 = (float)((double)Surplus * (double)this.Population / 1000.0 * ((double)this.MineralRichness + (double)this.PlusProductionPerColonist)) + this.PlusFlatProductionPerTurn;
                float num3 = num2 + this.ProductionPercentAdded * num2 - this.consumption;
                if ((double)(num3 - this.Owner.data.TaxRate * num3) >= (double)desiredSurplus)
                {
                    this.ps = Planet.GoodState.EXPORT;
                    return Surplus;
                }
            }
            this.fs = Planet.GoodState.IMPORT;
            return Surplus;
        }

        private float CalculateFarmerPercentForSurplus(float desiredSurplus)
        {
            float Surplus = 0.0f;
            if ((double)this.Fertility == 0.0)
                return 0.0f;
            while ((double)Surplus < 1.0)
            {
                Surplus += 0.01f;
                float num2 = (float)((double)Surplus * (double)this.Population / 1000.0 * ((double)this.Fertility + (double)this.PlusFoodPerColonist)) + this.FlatFoodAdded;
                if ((double)(num2 + this.FoodPercentAdded * num2 - this.consumption) >= (double)desiredSurplus)
                    return Surplus;
            }
            this.fs = Planet.GoodState.IMPORT;
            return 0.5f;
        }

        private bool DetermineIfSelfSufficient()
        {
            float num = (float)(1.0 * (double)this.Population / 1000.0 * ((double)this.Fertility + (double)this.PlusFoodPerColonist)) + this.FlatFoodAdded;
            return (double)(num + this.FoodPercentAdded * num - this.consumption) > 0.0;
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
                    }
                    if (flag2)
                        this.BuildingsCanBuild.Add(building1);
                }
            }
            return this.BuildingsCanBuild;
        }

        public void AddBuildingToCQ(Building b)
        {
            int count = this.ConstructionQueue.Count;
            QueueItem qi = new QueueItem();
            qi.isBuilding = true;
            qi.Building = b;
            qi.Cost = ResourceManager.GetBuilding(b.Name).Cost;
            qi.productionTowards = 0.0f;
            qi.NotifyOnEmpty = false;
            if (this.AssignBuildingToTile(b, qi))
                this.ConstructionQueue.Add(qi);
            else if (this.Owner.GetBDict()["Terraformer"] && (double)this.Fertility < 1.0)
            {
                bool flag = true;
                foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                {
                    if (queueItem.isBuilding && queueItem.Building.Name == "Terraformer")
                        flag = false;
                }
                foreach (Building building in this.BuildingList)
                {
                    if (building.Name == "Terraformer")
                        flag = false;
                }
                if (!flag)
                    return;
                this.AddBuildingToCQ(ResourceManager.GetBuilding("Terraformer"));
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

        private float AdjustResearchForProfit()
        {
            //return 0.0f;
            //added by gremlin pre15b code + custom to prevent low tax issues.
            if (this.Owner.data.TaxRate <= .15f) //this.Owner.Money > this.Owner.GetPlanets().Count * 200 ||
            {
                return 0f;
            }
            float single = this.EstimateNetWithWorkerPct(this.Owner.data.TaxRate, this.WorkerPercentage, this.ResearcherPercentage);
            float taxMod = single + this.Owner.data.Traits.TaxMod * single - (this.TotalMaintenanceCostsPerTurn + this.TotalMaintenanceCostsPerTurn * this.Owner.data.Traits.MaintMod);
            float researcherPercentage = this.ResearcherPercentage / 10f;
            for (int i = 0; i < 10 && taxMod <= 0f; i++)
            {
                Planet workerPercentage = this;
                workerPercentage.WorkerPercentage = workerPercentage.WorkerPercentage + researcherPercentage;
                Planet planet = this;
                planet.ResearcherPercentage = planet.ResearcherPercentage - researcherPercentage;
                single = this.EstimateNetWithWorkerPct(this.Owner.data.TaxRate, this.WorkerPercentage, this.ResearcherPercentage);
                taxMod = single + this.Owner.data.Traits.TaxMod * single - (this.TotalMaintenanceCostsPerTurn + this.TotalMaintenanceCostsPerTurn * this.Owner.data.Traits.MaintMod);
            }
            this.EstimateTaxes(this.Owner.data.TaxRate);
            return taxMod;
        }

        public void DoGoverning()
        {
            if (this.colonyType == Planet.ColonyType.Colony)
                return;
            if (this.Owner.data.Traits.Cybernetic > 0)
            {
                this.FarmerPercentage = 0.0f;
                this.WorkerPercentage = this.CalculateCyberneticPercentForSurplus(2f);
                if ((double)this.WorkerPercentage > 1.0)
                    this.WorkerPercentage = 1f;
                this.ResearcherPercentage = 1f - this.WorkerPercentage;
                if ((double)this.NetProductionPerTurn > 5.0 && this.ProductionHere > this.MAX_STORAGE * 0.5f)
                    this.ps = Planet.GoodState.EXPORT;
                else
                    this.ps = Planet.GoodState.IMPORT;
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
                        this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"));
                }
                bool flag3 = false;
                foreach (Building building1 in this.BuildingsCanBuild)
                {
                    if ((double)building1.PlusFlatProductionAmount > 0.0 || (double)building1.PlusProdPerColonist > 0.0 || (building1.Name == "Space Port" || (double)building1.PlusProdPerRichness > 0.0) || building1.Name == "Outpost")
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
                        if (queueItem.isBuilding && ((double)queueItem.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem.Building.PlusProdPerColonist > 0.0 || (double)queueItem.Building.PlusProdPerRichness > 0.0))
                        {
                            flag4 = false;
                            break;
                        }
                    }
                }
                if (this.Owner != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && this.Shipyards.Where(ship => ship.Value.GetShipData().IsShipyard).Count() == 0 && this.Owner.WeCanBuildThis("Shipyard") && (double)this.GrossMoneyPT > 5.0 && (double)this.NetProductionPerTurn > 6.0)
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
                            sData = ResourceManager.ShipsDict["Shipyard"].GetShipData(),
                            Cost = ResourceManager.ShipsDict["Shipyard"].GetCost(this.Owner)
                        });
                }
                if ((double)buildingCount < 2.0 && flag4)
                {
                    this.GetBuildingsWeCanBuildHere();
                    Building b = (Building)null;
                    float num2 = 99999f;
                    foreach (Building building in this.BuildingsCanBuild)
                    {
                        if (!(building.Name == "Terraformer") && (double)building.PlusFlatFoodAmount <= 0.0 && ((double)building.PlusFoodPerColonist <= 0.0 && !(building.Name == "Biospheres")) && ((double)building.PlusFlatPopulation <= 0.0 || (double)this.Population / (double)this.MaxPopulation <= 0.25))
                        {
                            if ((double)building.PlusFlatProductionAmount > 0.0 || (double)building.PlusProdPerColonist > 0.0 || (building.Name == "Space Port" || building.Name == "Outpost"))
                            {
                                if (!(building.Name == "Space Port") || (double)this.GetNetProductionPerTurn() >= 2.0)
                                {
                                    float num3 = building.Cost;
                                    b = building;
                                    break;
                                }
                            }
                            else if ((double)building.Cost < (double)num2 && (!(building.Name == "Space Port") || this.BuildingList.Count >= 2))
                            {
                                num2 = building.Cost;
                                b = building;
                            }
                        }
                    }
                    if (b != null && (double)this.Owner.EstimateIncomeAtTaxRate(0.4f) - (double)b.Maintenance > 0.0)
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
                            this.AddBuildingToCQ(b);
                    }
                    else if (this.Owner.GetBDict()["Biospheres"] && (double)this.MineralRichness >= 1.0)
                    {
                        if (this.Owner == EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty))
                        {
                            if ((double)this.Population / ((double)this.MaxPopulation + (double)this.MaxPopBonus) > 0.949999988079071 && (double)this.Owner.EstimateIncomeAtTaxRate(this.Owner.data.TaxRate) - (double)ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0)
                                this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                        }
                        else if ((double)this.Population / ((double)this.MaxPopulation + (double)this.MaxPopBonus) > 0.949999988079071 && (double)this.Owner.EstimateIncomeAtTaxRate(0.5f) - (double)ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0)
                            this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                    }
                }
                for (int index = 0; index < this.ConstructionQueue.Count; ++index)
                {
                    QueueItem queueItem1 = this.ConstructionQueue[index];
                    if (index == 0 && queueItem1.isBuilding)
                    {
                        if (queueItem1.Building.Name == "Outpost" || (double)queueItem1.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem1.Building.PlusProdPerRichness > 0.0 || (double)queueItem1.Building.PlusProdPerColonist > 0.0)
                        {
                            this.ApplyAllStoredProduction(0);
                        }
                        break;
                    }
                    else if (queueItem1.isBuilding && ((double)queueItem1.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem1.Building.PlusProdPerColonist > 0.0 || (queueItem1.Building.Name == "Outpost" || (double)queueItem1.Building.PlusProdPerRichness > 0.0)))
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
            else
            {
                switch (this.colonyType)
                {
                    case Planet.ColonyType.Core:
                        this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.5f);
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
                                    this.WorkerPercentage = 1.0f;
                                    this.ResearcherPercentage = 0.0f;
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
                                this.WorkerPercentage = 1.0f;
                                this.ResearcherPercentage = 0.0f;
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
                        if ((double)this.GrossMoneyPT - (double)this.TotalMaintenanceCostsPerTurn < 0.0 && ((double)this.MineralRichness >= 0.75 || (double)this.PlusProductionPerColonist >= 1.0) && (double)this.ResearcherPercentage > 0.0)
                        {
                            double num4 = (double)this.AdjustResearchForProfit();
                        }
                        if (this.Owner != EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty) && this.Shipyards.Where(ship => ship.Value.GetShipData().IsShipyard).Count() == 0 && this.Owner.WeCanBuildThis("Shipyard") && (double)this.Owner.MoneyLastTurn > 5.0 && (double)this.NetProductionPerTurn > 4.0)
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
                                    sData = ResourceManager.ShipsDict["Shipyard"].GetShipData(),
                                    Cost = ResourceManager.ShipsDict["Shipyard"].GetCost(this.Owner) * UniverseScreen.GamePaceStatic
                                });
                        }
                        foreach (Building building in this.BuildingList)
                        {
                            if (building.Name == "Space Port")
                            {
                                this.ps = Planet.GoodState.EXPORT;
                                break;
                            }
                            else
                                this.ps = Planet.GoodState.IMPORT;
                        }
                        if ((double)this.NetProductionPerTurn < 2.0 && (double)this.Population / 1000.0 < 1.0)
                            this.ps = Planet.GoodState.IMPORT;
                        float num5 = 0.0f;
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
                                this.AddBuildingToCQ(ResourceManager.GetBuilding("Outpost"));
                        }
                        if ((double)num5 < 2.0)
                        {
                            this.GetBuildingsWeCanBuildHere();
                            Building b = (Building)null;
                            foreach (Building building in this.BuildingsCanBuild)
                            {
                                if (((double)building.PlusFlatPopulation <= 0.0 || (double)this.Population <= 1000.0) && ((double)building.MinusFertilityOnBuild <= 0.0 && !(building.Name == "Biospheres")) && (!(building.Name == "Terraformer") || !flag5 && (double)this.Fertility < 1.0) && (!(building.Name == "Deep Core Mine") && ((double)building.PlusFlatPopulation <= 0.0 || (double)this.Population / (double)this.MaxPopulation <= 0.25)))
                                {
                                    if ((double)building.PlusFlatProductionAmount > 0.0 || (double)building.PlusProdPerColonist > 0.0 || building.Name == "Outpost")
                                    {
                                        float num2 = building.Cost;
                                        b = building;
                                        break;
                                    }
                                    else if ((double)building.Cost < 99999f)
                                    {
                                        b = building;
                                    }
                                }
                            }
                            if (b != null && (double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0)
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
                            else if (b != null && ((double)b.PlusFlatProductionAmount > 0.0 || (double)b.PlusProdPerColonist > 0.0))
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
                            else if (this.Owner.GetBDict()["Biospheres"] && (double)this.MineralRichness >= 1.0 && (double)this.Fertility >= 1.0)
                            {
                                if (this.Owner == EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty))
                                {
                                    if ((double)this.Population / ((double)this.MaxPopulation + (double)this.MaxPopBonus) > 0.949999988079071 && (double)this.Owner.EstimateIncomeAtTaxRate(this.Owner.data.TaxRate) - (double)ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0)
                                        this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                                }
                                else if ((double)this.Population / ((double)this.MaxPopulation + (double)this.MaxPopBonus) > 0.949999988079071 && (double)this.Owner.EstimateIncomeAtTaxRate(0.5f) - (double)ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0)
                                    this.TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                            }
                        }
                        for (int index = 0; index < this.ConstructionQueue.Count; ++index)
                        {
                            QueueItem queueItem1 = this.ConstructionQueue[index];
                            if (index == 0 && queueItem1.isBuilding)
                            {
                                if (queueItem1.Building.Name == "Outpost" || (double)queueItem1.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem1.Building.PlusProdPerRichness > 0.0 || (double)queueItem1.Building.PlusProdPerColonist > 0.0)
                                {
                                    this.ApplyAllStoredProduction(0);
                                }
                                break;
                            }
                            else if (queueItem1.isBuilding && ((double)queueItem1.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem1.Building.PlusProdPerColonist > 0.0 || queueItem1.Building.Name == "Outpost"))
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
                    case Planet.ColonyType.Industrial:
                        this.fs = Planet.GoodState.IMPORT;
                        this.FarmerPercentage = 0.0f;
                        this.WorkerPercentage = 1f;
                        this.ResearcherPercentage = 0.0f;
                        this.ps = (double)this.ProductionHere >= 20.0 ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                        if ((double)this.FoodHere <= (double)this.consumption)
                        {
                            this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.01f);
                            this.WorkerPercentage = 1f - this.FarmerPercentage;
                            this.ResearcherPercentage = 0.0f;
                        }
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
                        foreach (Building building1 in this.BuildingsCanBuild)
                        {
                            if ((double)building1.PlusFlatProductionAmount > 0.0 || (double)building1.PlusProdPerColonist > 0.0 || (double)building1.PlusProdPerRichness > 0.0)
                            {
                                int num1 = 0;
                                foreach (Building building2 in this.BuildingList)
                                {
                                    if (building2 == building1)
                                        ++num1;
                                }
                                flag8 = num1 <= 9;
                                break;
                            }
                        }
                        bool flag9 = true;
                        if (flag8)
                        {
                            foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                            {
                                if (queueItem.isBuilding && ((double)queueItem.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem.Building.PlusProdPerColonist > 0.0 || (double)queueItem.Building.PlusProdPerRichness > 0.0))
                                {
                                    flag9 = false;
                                    break;
                                }
                            }
                        }
                        if (flag9 && (double)num6 < 2.0)
                        {
                            Building b = (Building)null;
                            foreach (Building building in this.BuildingsCanBuild)
                            {
                                if ((double)building.PlusFlatProductionAmount > 0.0 || (double)building.PlusProdPerColonist > 0.0 || ((double)building.PlusProdPerRichness > 0.0 || building.StorageAdded > 0))
                                {
                                    double num1 = (double)building.Cost;
                                    b = building;
                                    break;
                                }
                            }
                            if (b != null && (double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0)
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
                            else if (b != null && ((double)b.PlusFlatProductionAmount > 0.0 || (double)b.PlusProdPerColonist > 0.0 || (double)b.PlusProdPerRichness > 0.0 && (double)this.MineralRichness > 1.5))
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
                        if ((double)num6 < 2.0)
                        {
                            Building b = (Building)null;
                            float num1 = 99999f;
                            foreach (Building building in this.GetBuildingsWeCanBuildHere())
                            {
                                if ((double)building.PlusFlatProductionAmount > 0.0 || (double)building.PlusTaxPercentage > 0.0 || ((double)building.PlusProdPerColonist > 0.0 || building.Name == "Space Port") || ((double)building.PlusProdPerColonist > 0.0 || building.Name == "Outpost"))
                                {
                                    if (!(building.Name == "Space Port") || (double)this.GetNetProductionPerTurn() >= 2.0)
                                    {
                                        float num2 = building.Cost;
                                        b = building;
                                        break;
                                    }
                                }
                                else if ((double)building.Cost < (double)num1 && (building.CombatStrength > 0 || (double)building.PlusFlatProductionAmount > 0.0 || ((double)building.PlusFlatResearchAmount > 0.0 || (double)building.PlusFlatFoodAmount > 0.0) || (building.Name == "Space Port" || (double)building.PlusProdPerColonist > 0.0 || (building.Name == "Outpost" || (double)building.CreditsPerColonist > 0.0)) || building.StorageAdded > 0) && (!(building.Name == "Space Port") || this.BuildingList.Count >= 2))
                                {
                                    num1 = building.Cost;
                                    b = building;
                                }
                            }
                            if (b != null && (double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0)
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
                    case Planet.ColonyType.Research:
                        this.fs = Planet.GoodState.IMPORT;
                        this.ps = Planet.GoodState.IMPORT;
                        this.FarmerPercentage = 0.0f;
                        this.WorkerPercentage = 0.0f;
                        this.ResearcherPercentage = 1f;
                        if ((double)this.FoodHere <= (double)this.consumption)
                        {
                            this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.01f);
                            this.ResearcherPercentage = 1f - this.FarmerPercentage;
                        }
                        if ((double)this.GrossMoneyPT - (double)this.TotalMaintenanceCostsPerTurn < 0.0 && ((double)this.MineralRichness >= 0.75 || (double)this.PlusProductionPerColonist >= 1.0) && (double)this.ResearcherPercentage > 0.0)
                        {
                            double num7 = (double)this.AdjustResearchForProfit();
                        }
                        float num8 = 0.0f;
                        foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
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
                        if ((double)num8 < 2.0)
                        {
                            this.GetBuildingsWeCanBuildHere();
                            Building b = (Building)null;
                            float num1 = 99999f;
                            foreach (Building building in this.BuildingsCanBuild)
                            {
                                if ((double)building.PlusFlatProductionAmount > 0.0 || building.Name == "Outpost")
                                {
                                    float num2 = building.Cost;
                                    b = building;
                                    break;
                                }
                                else if ((double)building.Cost < (double)num1 && (building.CombatStrength > 0 || (double)building.PlusResearchPerColonist > 0.0 || ((double)building.PlusFlatResearchAmount > 0.0 || (double)building.PlusFlatFoodAmount > 0.0) || ((double)building.PlusFlatProductionAmount > 0.0 || building.StorageAdded > 0 || (building.Name == "Outpost" || (double)building.CreditsPerColonist > 0.0)) || (building.StorageAdded > 0 || (double)building.PlusTaxPercentage > 0.0)))
                                {
                                    num1 = building.Cost;
                                    b = building;
                                }
                            }
                            if (b != null && (double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0)
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
                    case Planet.ColonyType.Agricultural:
                        this.fs = Planet.GoodState.EXPORT;
                        this.ps = Planet.GoodState.IMPORT;
                        this.FarmerPercentage = 1f;
                        this.WorkerPercentage = 0.0f;
                        this.ResearcherPercentage = 0.0f;
                        if ((double)this.FoodHere == (double)this.MAX_STORAGE)
                        {
                            this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.0f);
                            float num1 = 1f - this.FarmerPercentage;
                            //Added by McShooterz: No research percentage if not researching
                            if (this.Owner.ResearchTopic != "")
                            {
                                this.WorkerPercentage = num1 / 2f;
                                this.ResearcherPercentage = num1 / 2f;
                            }
                            else
                            {
                                this.WorkerPercentage = num1;
                                this.ResearcherPercentage = 0.0f;
                            }
                        }
                        if ((double)this.ProductionHere / (double)this.MAX_STORAGE > 0.850000023841858)
                        {
                            float num1 = 1f - this.FarmerPercentage;
                            this.WorkerPercentage = 0.0f;
                            //Added by McShooterz: No research percentage if not researching
                            if (this.Owner.ResearchTopic != "")
                            {
                                this.ResearcherPercentage = num1;
                            }
                            else
                            {
                                this.FarmerPercentage = 1f;
                                this.ResearcherPercentage = 0.0f;
                            }
                        }
                        float num9 = 0.0f;
                        bool flag11 = false;
                        foreach (QueueItem queueItem in (List<QueueItem>)this.ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
                                ++num9;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++num9;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Terraformer")
                                flag11 = true;
                        }
                        bool flag12 = true;
                        foreach (Building building in this.BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                flag12 = false;
                            if (building.Name == "Terraformer")
                                flag11 = true;
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
                        if ((double)num9 < 2.0)
                        {
                            this.GetBuildingsWeCanBuildHere();
                            Building b = (Building)null;
                            float num1 = 99999f;
                            foreach (Building building in this.BuildingsCanBuild)
                            {
                                if ((double)building.PlusFlatProductionAmount > 0.0 || building.Name == "Outpost")
                                {
                                    float num2 = building.Cost;
                                    b = building;
                                    break;
                                }
                                else if ((double)building.Cost < (double)num1 && (building.CombatStrength > 0 || (double)building.PlusFoodPerColonist > 0.0 || ((double)building.PlusFlatFoodAmount > 0.0 || (double)building.PlusFlatProductionAmount > 0.0) || ((double)building.PlusFlatResearchAmount > 0.0 || building.StorageAdded > 0 || (building.Name == "Outpost" || (double)building.CreditsPerColonist > 0.0)) || (building.StorageAdded > 0 || !flag11 && building.Name == "Terraformer" && (double)this.Fertility < 1.0) || (double)building.PlusTaxPercentage > 0.0))
                                {
                                    num1 = building.Cost;
                                    b = building;
                                }
                            }
                            if (b != null && (double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0)
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
                    case Planet.ColonyType.Military:
                        this.fs = Planet.GoodState.IMPORT;
                        if ((double)this.MAX_STORAGE - (double)this.FoodHere < 25.0)
                            this.fs = Planet.GoodState.EXPORT;
                        this.FarmerPercentage = 0.0f;
                        this.WorkerPercentage = 1f;
                        this.ResearcherPercentage = 0.0f;
                        if ((double)this.FoodHere <= (double)this.consumption)
                        {
                            this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.01f);
                            this.WorkerPercentage = 1f - this.FarmerPercentage;
                            this.ResearcherPercentage = 0.0f;
                        }
                        this.ps = (double)this.ProductionHere >= 20.0 ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
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
                        if (this.Owner != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && this.Shipyards.Where(ship => ship.Value.GetShipData().IsShipyard).Count() == 0 && this.Owner.WeCanBuildThis("Shipyard") && (double)this.GrossMoneyPT > 3.0)
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
                                    sData = ResourceManager.ShipsDict["Shipyard"].GetShipData(),
                                    Cost = ResourceManager.ShipsDict["Shipyard"].GetCost(this.Owner)
                                });
                        }
                        if ((double)buildingCount < 2.0)
                        {
                            this.GetBuildingsWeCanBuildHere();
                            Building b = (Building)null;
                            float num1 = 99999f;
                            foreach (Building building in this.BuildingsCanBuild)
                            {
                                if ((double)building.PlusFlatProductionAmount > 0.0 || (double)building.PlusFlatResearchAmount > 0.0 || ((double)building.PlusFlatFoodAmount > 0.0 || building.Name == "Space Port") || ((double)building.PlusProdPerColonist > 0.0 || building.Name == "Outpost" || ((double)building.CreditsPerColonist > 0.0 || building.CombatStrength > 0)))
                                {
                                    if (!(building.Name == "Space Port") || (double)this.GetNetProductionPerTurn() >= 2.0)
                                    {
                                        float num2 = building.Cost;
                                        b = building;
                                        break;
                                    }
                                }
                                else if ((double)building.Cost < (double)num1 && ((double)building.PlusFlatProductionAmount > 0.0 || (double)building.PlusFlatResearchAmount > 0.0 || ((double)building.PlusFlatFoodAmount > 0.0 || building.Name == "Space Port") || ((double)building.PlusProdPerColonist > 0.0 || building.Name == "Outpost" || ((double)building.CreditsPerColonist > 0.0 || building.CombatStrength > 0)) || ((double)building.PlusTaxPercentage > 0.0 || building.StorageAdded > 0)) && (building.CombatStrength <= 0 && (!(building.Name == "Space Port") || this.BuildingList.Count >= 2)))
                                {
                                    num1 = building.Cost;
                                    b = building;
                                }
                            }
                            if (b != null && (double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0)
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
                            else if (b != null && ((double)b.PlusFlatProductionAmount > 0.0 || (double)b.PlusProdPerColonist > 0.0))
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
                }
            }
            //Added by McShooterz: Colony build troops
            if (this.CanBuildInfantry() && this.ConstructionQueue.Count == 0 && this.ProductionHere > this.MAX_STORAGE*.75f && this.Owner == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
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
                if (addTroop)
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
            //Added by McShooterz: build defense platforms
            if (this.ConstructionQueue.Count == 0 && this.Owner.data.TaxRate <.40f && (double)this.NetProductionPerTurn > 4.0 && this.Shipyards.Where(ship => ship.Value.Weapons.Count() > 0).Count() < (this.developmentLevel - 1) * 2)
            {
                string platform = this.Owner.GetGSAI().GetDefenceSatellite();
                if (platform != "")
                    this.ConstructionQueue.Add(new QueueItem()
                    {
                        isShip = true,
                        sData = ResourceManager.ShipsDict[platform].GetShipData(),
                        Cost = ResourceManager.ShipsDict[platform].GetCost(this.Owner)
                    });
            }
            if ((double)this.Population > 3000.0 || (double)this.Population / ((double)this.MaxPopulation + (double)this.MaxPopBonus) > 0.75)
            {
                List<Building> list = new List<Building>();
                foreach (Building building in this.BuildingList)
                {
                    if ((double)building.PlusFlatPopulation > 0.0 && (double)building.Maintenance > 0.0)
                        list.Add(building);
                }
                foreach (Building b in list)
                    this.ScrapBuilding(b);
            }
            if ((double)this.Fertility < 1.0)
                return;
            List<Building> list1 = new List<Building>();
            foreach (Building building in this.BuildingList)
            {
                if ((double)building.PlusTerraformPoints > 0.0 && (double)building.Maintenance > 0.0)
                    list1.Add(building);
            }
            foreach (Building b in list1)
                this.ScrapBuilding(b);
        }

        public void ScrapBuilding(Building b)
        {
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
            if (this.Crippled_Turns > 0 || this.RecentCombat || (this.ConstructionQueue.Count <= 0 || this.Owner == null || this.Owner.Money <=0))
                return false;

            float amount = this.ProductionHere > 10f ? 10f : this.ProductionHere;
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
            if (this.Crippled_Turns > 0 || this.RecentCombat || (this.ConstructionQueue.Count <= 0 || this.Owner == null || this.Owner.Money <= 0))
                return;
            float amount = this.ProductionHere;
            this.ProductionHere = 0f;
            this.ApplyProductiontoQueue(amount, Index);
        }

        private void ApplyProductionTowardsConstruction()
        {
            if (this.ProductionHere > this.MAX_STORAGE * 0.6f)
            {
                float amount = this.ProductionHere - (this.MAX_STORAGE * 0.6f);
                this.ProductionHere = this.MAX_STORAGE * 0.6f;
                this.ApplyProductiontoQueue(this.NetProductionPerTurn + amount, 0);
            }
            else
                if (this.NetProductionPerTurn > 5.0)
                {
                    this.ApplyProductiontoQueue(this.NetProductionPerTurn * 0.75f, 0);
                    this.ProductionHere += 0.25f * this.NetProductionPerTurn;
                }
                else
                    this.ApplyProductiontoQueue(this.NetProductionPerTurn, 0);
            if ((double)this.ProductionHere > (double)this.MAX_STORAGE)
                this.ProductionHere = this.MAX_STORAGE;
        }

        public void ApplyProductiontoQueue(float howMuch, int whichItem)
        {
            if (this.Crippled_Turns > 0 || this.RecentCombat || (double)howMuch < 0.0)
                return;      
            if (this.ConstructionQueue.Count > 0 && this.ConstructionQueue.Count > whichItem)
            {
                QueueItem item = this.ConstructionQueue[whichItem];
                if (item.isShip)
                    howMuch += howMuch * this.ShipBuildingModifier;
                if ((double)item.productionTowards + (double)howMuch < (double)item.Cost)
                {
                    this.ConstructionQueue[whichItem].productionTowards += howMuch;
                    if ((double)item.productionTowards >= (double)item.Cost)
                        this.ProductionHere += item.productionTowards - item.Cost;
                }
                else
                {
                    howMuch -= item.Cost - item.productionTowards;
                    item.productionTowards = item.Cost;
                    this.ProductionHere += howMuch;
                }
                this.ConstructionQueue[whichItem] = item;
            }
            else
                this.ProductionHere += howMuch;
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
                if ((double)queueItem.productionTowards >= (double)queueItem.Cost && queueItem.NotifyOnEmpty == false)
                    this.queueEmptySent = true;
                else if ((double)queueItem.productionTowards >= (double)queueItem.Cost)
                    this.queueEmptySent = false;

                if (queueItem.isBuilding && (double)queueItem.productionTowards >= (double)queueItem.Cost)
                {
                    Building building = ResourceManager.GetBuilding(queueItem.Building.Name);
                    this.BuildingList.Add(building);
                    this.Fertility -= ResourceManager.GetBuilding(queueItem.Building.Name).MinusFertilityOnBuild;
                    if ((double)this.Fertility < 0.0)
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
                    if (building.EventOnBuild != null && this.Owner != null && this.Owner == EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty))
                        Planet.universeScreen.ScreenManager.AddScreen((GameScreen)new EventPopup(Planet.universeScreen, EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty), ResourceManager.EventsDict[building.EventOnBuild], ResourceManager.EventsDict[building.EventOnBuild].PotentialOutcomes[0], true));
                    this.ConstructionQueue.QueuePendingRemoval(queueItem);
                }
                else if (queueItem.isShip && !ResourceManager.ShipsDict.ContainsKey(queueItem.sData.Name))
                {
                    this.ConstructionQueue.QueuePendingRemoval(queueItem);
                    this.ProductionHere += queueItem.productionTowards;
                    if ((double)this.ProductionHere > (double)this.MAX_STORAGE)
                        this.ProductionHere = this.MAX_STORAGE;
                }
                else if (queueItem.isShip && (double)queueItem.productionTowards >= (double)queueItem.Cost)
                {
                    Ship shipAt;
                    if (queueItem.isRefit)
                        shipAt = ResourceManager.CreateShipAt(queueItem.sData.Name, this.Owner, this, true, queueItem.RefitName != "" ? queueItem.RefitName : queueItem.sData.Name, queueItem.sData.Level);
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
                                if ((double)this.ResourcesDict[current] > 0.0 && (double)shipAt.GetCargo()[current] < (double)shipAt.GetMaxGoods()[current])
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
                    if (queueItem.sData.Role == "station" || queueItem.sData.Role == "platform")
                    {
                        int num = this.Shipyards.Count / 9;
                        shipAt.Position = this.Position + HelperFunctions.GeneratePointOnCircle((float)(this.Shipyards.Count * 40), Vector2.Zero, (float)(2000 + 2000 * num * this.scale));
                        shipAt.Center = shipAt.Position;
                        shipAt.TetherToPlanet(this);
                        this.Shipyards.Add(shipAt.guid, shipAt);
                    }
                    if (queueItem.Goal != null)
                    {
                        if (queueItem.Goal.GoalName == "BuildConstructionShip")
                        {
                            shipAt.GetAI().OrderDeepSpaceBuild(queueItem.Goal);
                            shipAt.Role = "construction";
                            shipAt.VanityName = "Construction Ship";
                        }
                        else if (queueItem.Goal.GoalName != "BuildDefensiveShips" && queueItem.Goal.GoalName != "BuildOffensiveShips" && queueItem.Goal.GoalName != "FleetRequisition")
                        {
                            ++queueItem.Goal.Step;
                        }
                        else
                        {
                            if (this.Owner != EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty))
                                this.Owner.ForcePoolAdd(shipAt);
                            queueItem.Goal.ReportShipComplete(shipAt);
                        }
                    }
                    else if ((queueItem.sData.Role != "station" || queueItem.sData.Role == "platform") && this.Owner != EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty))
                        this.Owner.ForcePoolAdd(shipAt);
                }
                else if (queueItem.isTroop && (double)queueItem.productionTowards >= (double)queueItem.Cost)
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
            int num = (int)Math.Ceiling((double)(int)((double)(qItem.Cost - qItem.productionTowards) / (double)this.NetProductionPerTurn));
            if ((double)this.NetProductionPerTurn > 0.0)
                return num;
            else
                return 999;
        }

        public float GetMaxProductionPotential()
        {
            float num1 = 0.0f;
            float num2 = (float)((double)this.MineralRichness * (double)this.Population / 1000.0);
            for (int index = 0; index < this.BuildingList.Count; ++index)
            {
                Building building = this.BuildingList[index];
                if ((double)building.PlusProdPerRichness > 0.0)
                    num1 += building.PlusProdPerRichness * this.MineralRichness;
                num1 += building.PlusFlatProductionAmount;
                if ((double)building.PlusProdPerColonist > 0.0)
                    num2 += building.PlusProdPerColonist;
            }
            float num3 = num1 + (float)((double)num2 * (double)this.Population / 1000.0);
            float num4 = num3 + this.ProductionPercentAdded * num3;
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

        public float EstimateTaxes(float Rate)
        {
            this.TotalMaintenanceCostsPerTurn = 0.0f;
            this.GrossFood = 0.0f;
            this.PlusResearchPerColonist = 0.0f;
            this.PlusFlatResearchPerTurn = 0.0f;
            this.PlusFlatProductionPerTurn = 0.0f;
            this.PlusProductionPerColonist = 0.0f;
            this.FlatFoodAdded = 0.0f;
            this.PlusFoodPerColonist = 0.0f;
            this.PlusCreditsPerColonist = 0.0f;
            this.MaxPopBonus = 0.0f;
            this.PlusTaxPercentage = 0.0f;
            this.GrossMoneyPT = 0.0f;
            foreach (Building building in this.BuildingList)
            {
                this.PlusTaxPercentage += building.PlusTaxPercentage;
                this.PlusCreditsPerColonist += building.CreditsPerColonist;
                if ((double)building.PlusFoodPerColonist > 0.0)
                    this.PlusFoodPerColonist += building.PlusFoodPerColonist;
                if ((double)building.PlusResearchPerColonist > 0.0)
                    this.PlusResearchPerColonist += building.PlusResearchPerColonist;
                if ((double)building.PlusFlatResearchAmount > 0.0)
                    this.PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                if ((double)building.PlusProdPerRichness > 0.0)
                    this.PlusFlatProductionPerTurn += building.PlusProdPerRichness * this.MineralRichness;
                this.PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                if ((double)building.PlusProdPerColonist > 0.0)
                    this.PlusProductionPerColonist += building.PlusProdPerColonist;
                if ((double)building.Maintenance > 0.0)
                    this.TotalMaintenanceCostsPerTurn += building.Maintenance;
                if ((double)building.MaxPopIncrease > 0.0)
                    this.MaxPopBonus += building.MaxPopIncrease;
            }
            foreach (Troop troop in this.TroopsHere)
            {
                if (troop.Strength > 0 && troop.GetOwner() == this.Owner)
                    this.TotalDefensiveStrength += (int)troop.Strength;
            }
            this.NetResearchPerTurn = (float)((double)this.ResearcherPercentage * (double)this.Population / 1000.0) * this.PlusResearchPerColonist + this.PlusFlatResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn + this.ResearchPercentAdded * this.NetResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn + this.Owner.data.Traits.ResearchMod * this.NetResearchPerTurn;
            this.GrossMoneyPT += Rate * this.NetResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn - Rate * this.NetResearchPerTurn;
            this.NetFoodPerTurn = (float)((double)this.FarmerPercentage * (double)this.Population / 1000.0 * ((double)this.Fertility + (double)this.PlusFoodPerColonist)) + this.FlatFoodAdded;
            this.NetFoodPerTurn = this.NetFoodPerTurn + this.FoodPercentAdded * this.NetFoodPerTurn;
            this.GrossFood = this.NetFoodPerTurn;
            this.NetProductionPerTurn = (float)((double)this.WorkerPercentage * (double)this.Population / 1000.0 * ((double)this.MineralRichness + (double)this.PlusProductionPerColonist)) + this.PlusFlatProductionPerTurn;
            this.NetProductionPerTurn = this.NetProductionPerTurn + this.ProductionPercentAdded * this.NetProductionPerTurn;
            this.NetProductionPerTurn = this.NetProductionPerTurn + this.Owner.data.Traits.ProductionMod * this.NetProductionPerTurn;
            this.GrossMoneyPT += Rate * this.NetProductionPerTurn;
            this.NetProductionPerTurn = this.NetProductionPerTurn - Rate * this.NetProductionPerTurn;
            this.GrossMoneyPT = this.GrossMoneyPT + this.PlusTaxPercentage * this.GrossMoneyPT;
            this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
            this.GrossMoneyPT = this.GrossMoneyPT + this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            return this.GrossMoneyPT;
        }

        public float EstimateNetWithWorkerPct(float Rate, float workerpct, float respct)
        {
            this.TotalMaintenanceCostsPerTurn = 0.0f;
            this.GrossFood = 0.0f;
            this.PlusResearchPerColonist = 0.0f;
            this.PlusFlatResearchPerTurn = 0.0f;
            this.PlusFlatProductionPerTurn = 0.0f;
            this.PlusProductionPerColonist = 0.0f;
            this.FlatFoodAdded = 0.0f;
            this.PlusFoodPerColonist = 0.0f;
            this.PlusCreditsPerColonist = 0.0f;
            this.MaxPopBonus = 0.0f;
            this.PlusTaxPercentage = 0.0f;
            this.GrossMoneyPT = 0.0f;
            foreach (Building building in this.BuildingList)
            {
                this.PlusTaxPercentage += building.PlusTaxPercentage;
                this.PlusCreditsPerColonist += building.CreditsPerColonist;
                if ((double)building.PlusFoodPerColonist > 0.0)
                    this.PlusFoodPerColonist += building.PlusFoodPerColonist;
                if ((double)building.PlusResearchPerColonist > 0.0)
                    this.PlusResearchPerColonist += building.PlusResearchPerColonist;
                if ((double)building.PlusFlatResearchAmount > 0.0)
                    this.PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                if ((double)building.PlusProdPerRichness > 0.0)
                    this.PlusFlatProductionPerTurn += building.PlusProdPerRichness * this.MineralRichness;
                this.PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                if ((double)building.PlusProdPerColonist > 0.0)
                    this.PlusProductionPerColonist += building.PlusProdPerColonist;
                if ((double)building.Maintenance > 0.0)
                    this.TotalMaintenanceCostsPerTurn += building.Maintenance;
                if ((double)building.MaxPopIncrease > 0.0)
                    this.MaxPopBonus += building.MaxPopIncrease;
            }
            foreach (Troop troop in this.TroopsHere)
            {
                if (troop.Strength > 0 && troop.GetOwner() == this.Owner)
                    this.TotalDefensiveStrength += (int)troop.Strength;
            }
            this.NetResearchPerTurn = (float)((double)respct * (double)this.Population / 1000.0) * this.PlusResearchPerColonist + this.PlusFlatResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn + this.ResearchPercentAdded * this.NetResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn + this.Owner.data.Traits.ResearchMod * this.NetResearchPerTurn;
            this.GrossMoneyPT += Rate * this.NetResearchPerTurn;
            this.NetResearchPerTurn = this.NetResearchPerTurn - Rate * this.NetResearchPerTurn;
            this.NetFoodPerTurn = (float)((double)this.FarmerPercentage * (double)this.Population / 1000.0 * ((double)this.Fertility + (double)this.PlusFoodPerColonist)) + this.FlatFoodAdded;
            this.NetFoodPerTurn = this.NetFoodPerTurn + this.FoodPercentAdded * this.NetFoodPerTurn;
            this.GrossFood = this.NetFoodPerTurn;
            this.NetProductionPerTurn = (float)((double)workerpct * (double)this.Population / 1000.0 * ((double)this.MineralRichness + (double)this.PlusProductionPerColonist)) + this.PlusFlatProductionPerTurn;
            this.NetProductionPerTurn = this.NetProductionPerTurn + this.ProductionPercentAdded * this.NetProductionPerTurn;
            this.NetProductionPerTurn = this.NetProductionPerTurn + this.Owner.data.Traits.ProductionMod * this.NetProductionPerTurn;
            this.GrossMoneyPT += Rate * this.NetProductionPerTurn;
            this.NetProductionPerTurn = this.NetProductionPerTurn - Rate * this.NetProductionPerTurn;
            this.GrossMoneyPT = this.GrossMoneyPT + this.PlusTaxPercentage * this.GrossMoneyPT;
            this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
            this.GrossMoneyPT = this.GrossMoneyPT + this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            return this.GrossMoneyPT;
        }

        public bool CanBuildInfantry()
        {
            try
            {
                foreach (Building building in this.BuildingList)
                {
                    if (building.AllowInfantry)
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool CanBuildShips()
        {
            try
            {
                foreach (Building building in this.BuildingList)
                {
                    //if (building.NameTranslationIndex == 458)
                    if (building.AllowShipBuilding || building.Name =="Space Port")
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public void UpdateIncomes()
        {
            try
            {
                if (this.Owner == null)
                    return;
                this.PlusFlatPopulationPerTurn = 0.0f;
                this.ShieldStrengthMax = 0.0f;
                this.TotalMaintenanceCostsPerTurn = 0.0f;
                this.StorageAdded = 0;
                this.AllowInfantry = false;
                this.TotalDefensiveStrength = 0;
                this.GrossFood = 0.0f;
                this.PlusResearchPerColonist = 0.0f;
                this.PlusFlatResearchPerTurn = 0.0f;
                this.PlusFlatProductionPerTurn = 0.0f;
                this.PlusProductionPerColonist = 0.0f;
                this.FlatFoodAdded = 0.0f;
                this.PlusFoodPerColonist = 0.0f;
                this.HasShipyard = false;
                this.PlusFlatPopulationPerTurn = 0.0f;
                this.ShipBuildingModifier = 0.0f;
                this.CommoditiesPresent.Clear();
                List<Guid> list = new List<Guid>();
                foreach (KeyValuePair<Guid, Ship> keyValuePair in this.Shipyards)
                {
                    if (keyValuePair.Value == null)
                        list.Add(keyValuePair.Key);
                    else if (keyValuePair.Value.Active && keyValuePair.Value.GetShipData().IsShipyard)
                    {
                        if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.ShipyardBonus > 0)
                        {
                            this.ShipBuildingModifier += GlobalStats.ActiveMod.mi.ShipyardBonus;
                        }
                        else
                        {
                            this.ShipBuildingModifier += 0.25f;
                        }
                    }
                    else if (!keyValuePair.Value.Active)
                        list.Add(keyValuePair.Key);
                }
                foreach (Guid key in list)
                    this.Shipyards.Remove(key);
                this.PlusCreditsPerColonist = 0.0f;
                this.MaxPopBonus = 0.0f;
                this.PlusTaxPercentage = 0.0f;
                this.GrossMoneyPT = 0.0f;
                this.TerraformToAdd = 0.0f;
                for (int index = 0; index < this.BuildingList.Count; ++index)
                {
                    Building building = this.BuildingList[index];
                    if (building.WinsGame)
                        this.HasWinBuilding = true;
                    //if (building.NameTranslationIndex == 458)
                    if (building.AllowShipBuilding || building.Name == "Space Port")
                        this.HasShipyard = true;
                    if ((double)building.PlusFlatPopulation > 0.0)
                        this.PlusFlatPopulationPerTurn += building.PlusFlatPopulation;
                    this.ShieldStrengthMax += building.PlanetaryShieldStrengthAdded;
                    this.PlusCreditsPerColonist += building.CreditsPerColonist;
                    if ((double)building.PlusTerraformPoints > 0.0)
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
                    if ((double)building.PlusFoodPerColonist > 0.0)
                        this.PlusFoodPerColonist += building.PlusFoodPerColonist;
                    if ((double)building.PlusResearchPerColonist > 0.0)
                        this.PlusResearchPerColonist += building.PlusResearchPerColonist;
                    if ((double)building.PlusFlatResearchAmount > 0.0)
                        this.PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                    if ((double)building.PlusProdPerRichness > 0.0)
                        this.PlusFlatProductionPerTurn += building.PlusProdPerRichness * this.MineralRichness;
                    this.PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                    if ((double)building.PlusProdPerColonist > 0.0)
                        this.PlusProductionPerColonist += building.PlusProdPerColonist;
                    if ((double)building.MaxPopIncrease > 0.0)
                        this.MaxPopBonus += building.MaxPopIncrease;
                    if ((double)building.Maintenance > 0.0)
                        this.TotalMaintenanceCostsPerTurn += building.Maintenance;
                    this.FlatFoodAdded += building.PlusFlatFoodAmount;
                    this.RepairPerTurn += building.ShipRepair;
                }
                for (int index = 0; index < this.TroopsHere.Count; ++index)
                {
                    Troop troop = this.TroopsHere[index];
                }
                this.NetResearchPerTurn = (float)((double)this.ResearcherPercentage * (double)this.Population / 1000.0) * this.PlusResearchPerColonist + this.PlusFlatResearchPerTurn;
                this.NetResearchPerTurn = this.NetResearchPerTurn + this.ResearchPercentAdded * this.NetResearchPerTurn;
                this.NetResearchPerTurn = this.NetResearchPerTurn + this.Owner.data.Traits.ResearchMod * this.NetResearchPerTurn;
                this.GrossMoneyPT += this.Owner.data.TaxRate * this.NetResearchPerTurn;
                this.NetResearchPerTurn = this.NetResearchPerTurn - this.Owner.data.TaxRate * this.NetResearchPerTurn;
                this.NetFoodPerTurn = (float)((double)this.FarmerPercentage * (double)this.Population / 1000.0 * ((double)this.Fertility + (double)this.PlusFoodPerColonist)) + this.FlatFoodAdded;
                this.NetFoodPerTurn = this.NetFoodPerTurn + this.FoodPercentAdded * this.NetFoodPerTurn;
                this.GrossFood = this.NetFoodPerTurn;
                this.NetProductionPerTurn = (float)((double)this.WorkerPercentage * (double)this.Population / 1000.0 * ((double)this.MineralRichness + (double)this.PlusProductionPerColonist)) + this.PlusFlatProductionPerTurn;
                this.NetProductionPerTurn = this.NetProductionPerTurn + this.ProductionPercentAdded * this.NetProductionPerTurn;
                this.NetProductionPerTurn = this.NetProductionPerTurn + this.Owner.data.Traits.ProductionMod * this.NetProductionPerTurn;
                this.GrossMoneyPT += this.Owner.data.TaxRate * this.NetProductionPerTurn;
                this.NetProductionPerTurn = this.NetProductionPerTurn - this.Owner.data.TaxRate * this.NetProductionPerTurn;
                double num1 = (double)this.TotalOreExtracted;
                int num2 = 0;
                foreach (PlanetGridSquare planetGridSquare in this.TilesList)
                {
                    if (planetGridSquare.Habitable)
                        ++num2;
                }
                if (this.Station != null)
                {
                    if (!this.HasShipyard)
                        this.Station.SetVisibility(false, Planet.universeScreen.ScreenManager, this);
                    else
                        this.Station.SetVisibility(true, Planet.universeScreen.ScreenManager, this);
                }
                this.consumption = (float)((double)this.Population / 1000.0 + (double)this.Owner.data.Traits.ConsumptionModifier * (double)this.Population / 1000.0);
                this.GrossMoneyPT = this.GrossMoneyPT + this.PlusTaxPercentage * this.GrossMoneyPT;
                this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
                this.MAX_STORAGE = (float)this.StorageAdded;
                if ((double)this.MAX_STORAGE >= 10.0)
                    return;
                this.MAX_STORAGE = 10f;
            }
            catch
            {
            }
        }

        private void HarvestResources()
        {
            this.unfed = 0.0f;
            if (this.Owner.data.Traits.Cybernetic > 0)
            {
                this.FoodHere = 0.0f;
                this.NetProductionPerTurn -= this.consumption;
                if ((double)this.NetProductionPerTurn < 0.0)
                    this.ProductionHere += this.NetProductionPerTurn;
                if ((double)this.ProductionHere > (double)this.MAX_STORAGE)
                {
                    this.unfed = 0.0f;
                    this.ProductionHere = this.MAX_STORAGE;
                }
                else if ((double)this.ProductionHere < 0.0)
                {
                    this.unfed = this.ProductionHere;
                    this.ProductionHere = 0.0f;
                }
            }
            else
            {
                this.NetFoodPerTurn -= this.consumption;
                this.FoodHere += this.NetFoodPerTurn;
                if ((double)this.FoodHere > (double)this.MAX_STORAGE)
                {
                    this.unfed = 0.0f;
                    this.FoodHere = this.MAX_STORAGE;
                }
                else if ((double)this.FoodHere < 0.0)
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
                        if ((double)this.ResourcesDict[building1.ResourceConsumed] >= (double)building1.ConsumptionPerTurn)
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
            if ((double)this.ProductionHere + (double)this.NetProductionPerTurn < (double)this.MAX_STORAGE || this.ConstructionQueue.Count > 0)
                this.TotalOreExtracted += this.NetProductionPerTurn;
            this.Owner.Research += this.NetResearchPerTurn;
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
            if ((double)num1 > (double)this.Owner.data.Traits.PopGrowthMax * 1000.0 && (double)this.Owner.data.Traits.PopGrowthMax != 0.0)
                num1 = this.Owner.data.Traits.PopGrowthMax * 1000f;
            if ((double)num1 < (double)this.Owner.data.Traits.PopGrowthMin * 1000.0)
                num1 = this.Owner.data.Traits.PopGrowthMin * 1000f;
            float num2 = num1 + this.PlusFlatPopulationPerTurn;
            float num3 = num2 + this.Owner.data.Traits.ReproductionMod * num2;
            if ((double)Math.Abs(this.unfed) <= 0.0)
            {
                this.Population += num3;
                if ((double)this.Population + (double)num3 > (double)this.MaxPopulation + (double)this.MaxPopBonus)
                    this.Population = this.MaxPopulation + this.MaxPopBonus;
            }
            else
                this.Population += this.unfed * 10f;
            if ((double)this.Population >= 100.0)
                return;
            this.Population = 100f;
        }

        public float CalculateGrowth(float EstimatedFoodGain)
        {
            if (this.Owner != null)
            {
                float num1 = this.Owner.data.BaseReproductiveRate * this.Population;
                if ((double)num1 > (double)this.Owner.data.Traits.PopGrowthMax)
                    num1 = this.Owner.data.Traits.PopGrowthMax;
                if ((double)num1 < (double)this.Owner.data.Traits.PopGrowthMin)
                    num1 = this.Owner.data.Traits.PopGrowthMin;
                float num2 = num1 + this.PlusFlatPopulationPerTurn;
                float num3 = num2 + this.Owner.data.Traits.ReproductionMod * num2;
                if (this.Owner.data.Traits.Cybernetic > 0)
                {
                    if ((double)this.ProductionHere + (double)this.NetProductionPerTurn - (double)this.consumption <= 0.0)
                        return -(Math.Abs(this.ProductionHere + this.NetProductionPerTurn - this.consumption) / 10f);
                    if ((double)this.Population < (double)this.MaxPopulation + (double)this.MaxPopBonus && (double)this.Population + (double)num3 < (double)this.MaxPopulation + (double)this.MaxPopBonus)
                        return this.Owner.data.BaseReproductiveRate * this.Population;
                }
                else
                {
                    if ((double)this.FoodHere + (double)this.NetFoodPerTurn - (double)this.consumption <= 0.0)
                        return -(Math.Abs(this.FoodHere + this.NetFoodPerTurn - this.consumption) / 10f);
                    if ((double)this.Population < (double)this.MaxPopulation + (double)this.MaxPopBonus && (double)this.Population + (double)num3 < (double)this.MaxPopulation + (double)this.MaxPopBonus)
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
            this.RingWorld = Matrix.Identity * Matrix.CreateRotationX(MathHelper.ToRadians(this.ringTilt)) * Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
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
                this.OrbitalAngle += (float)Math.Asin(15.0 / (double)this.OrbitalRadius);
                if ((double)this.OrbitalAngle >= 360.0)
                    this.OrbitalAngle -= 360f;
            }
            this.PosUpdateTimer -= elapsedTime;
            if ((double)this.PosUpdateTimer <= 0.0 || this.system.isVisible)
            {
                this.PosUpdateTimer = 5f;
                this.Position = this.GeneratePointOnCircle(this.OrbitalAngle, this.ParentSystem.Position, this.OrbitalRadius);
            }
            if (this.system.isVisible)
            {
                BoundingSphere boundingSphere = new BoundingSphere(new Vector3(this.Position, 0.0f), 300000f);
                Parallel.Invoke(() =>
                {
                    this.SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(this.scale) * Matrix.CreateRotationZ(-this.Zrotate) * Matrix.CreateRotationX(MathHelper.ToRadians(-45f)) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
                },
                () =>
                {
                    this.cloudMatrix = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(this.scale) * Matrix.CreateRotationZ((float)(-(double)this.Zrotate / 1.5)) * Matrix.CreateRotationX(MathHelper.ToRadians(-45f)) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
                },
                () =>
                {
                    this.RingWorld = Matrix.Identity * Matrix.CreateRotationX(MathHelper.ToRadians(this.ringTilt)) * Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f));
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
                if ((double)this.Fertility > 2.0)
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
                else if ((double)this.Fertility > 1.0)
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
                else if ((double)this.Fertility > 0.6)
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
                if ((double)this.Fertility < 0.6 && (double)this.MineralRichness >= 2.0 && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1754);
                    planet2.Description = str2;
                    if ((double)this.MineralRichness > 3.0)
                    {
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1755);
                        planet3.Description = str3;
                    }
                    else if ((double)this.MineralRichness >= 2.0)
                    {
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1756);
                        planet3.Description = str3;
                    }
                    else
                    {
                        if ((double)this.MineralRichness < 1.0)
                            return;
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1757);
                        planet3.Description = str3;
                    }
                }
                else if ((double)this.MineralRichness > 3.0 && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1758);
                    planet2.Description = str2;
                }
                else if ((double)this.MineralRichness >= 2.0 && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + this.Name + Localizer.Token(1759);
                    planet2.Description = str2;
                }
                else if ((double)this.MineralRichness >= 1.0 && this.habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + this.Name + Localizer.Token(1760);
                    planet2.Description = str2;
                }
                else
                {
                    if ((double)this.MineralRichness >= 1.0 || !this.habitable)
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

        public int TurnsUntilOutOfFood()
        {
            return 0;
        }

        private void UpdateDevelopmentStatus()
        {
            this.Density = this.Population / 1000f;
            float num = this.MaxPopulation / 1000f;
            if ((double)this.Density <= 0.5)
            {
                this.developmentLevel = 1;
                this.DevelopmentStatus = Localizer.Token(1763);
                if ((double)num >= 2.0 && this.Type != "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1764);
                    planet.DevelopmentStatus = str;
                }
                else if ((double)num >= 2.0 && this.Type == "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1765);
                    planet.DevelopmentStatus = str;
                }
                else if ((double)num < 0.5 && this.Type != "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1766);
                    planet.DevelopmentStatus = str;
                }
                else if ((double)num < 0.5 && this.Type == "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1767);
                    planet.DevelopmentStatus = str;
                }
            }
            else if ((double)this.Density > 0.5 && (double)this.Density <= 2.0)
            {
                this.developmentLevel = 2;
                this.DevelopmentStatus = Localizer.Token(1768);
                if ((double)num >= 2.0)
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1769);
                    planet.DevelopmentStatus = str;
                }
                else if ((double)num < 2.0)
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1770);
                    planet.DevelopmentStatus = str;
                }
            }
            else if ((double)this.Density > 2.0 && (double)this.Density <= 5.0)
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
            num += this.TroopsHere.Where(empiresTroops => empiresTroops.GetOwner() == empire).Sum(strength => strength.Strength);
            return num;


        }
        public int GetPotentialGroundTroops(Empire empire)
        {
            //int num = 0;
            //if (this.Owner == empire)
            //    num += this.BuildingList.Sum(offense => offense.CombatStrength);
            //num += this.TroopsHere.Where(empiresTroops => empiresTroops.GetOwner() == empire).Sum(strength => strength.Strength);
            //return num;
            return  (int)(this.TilesList.Sum(spots => spots.number_allowed_troops));// * (.25f + this.developmentLevel*.2f));


        }
        public float GetGroundStrengthOther(Empire empire)
        {
            float num = 0;
            if (this.Owner == null || this.Owner != empire)
                num += this.BuildingList.Sum(offense => offense.CombatStrength);
            num += this.TroopsHere.Where(empiresTroops => empiresTroops.GetOwner()==null ||empiresTroops.GetOwner() != empire).Sum(strength => strength.Strength);
            return num;


        }
        public int GetGroundLandingSpots()
        {
            return (int)(this.TilesList.Sum(spots => spots.number_allowed_troops)-this.TroopsHere.Count );


        }

        //Added by McShooterz: heal builds and troops every turn
        public void HealBuildingsAndTroops()
        {
            if (this.RecentCombat)
                return;
            //heal troops
            //Gremlin Dont heal enemy troops
            foreach (Troop troop in this.TroopsHere)
            {
                if (troop.GetOwner() != this.Owner)
                    continue;
                if(troop.StrengthMax>0)
                    troop.Strength = troop.GetStrengthMax();
            }
            //Repair buildings
            foreach (Building building in this.BuildingList)
            {
                building.CombatStrength = Ship_Game.ResourceManager.BuildingsDict[building.Name].CombatStrength;
                building.Strength = Ship_Game.ResourceManager.BuildingsDict[building.Name].Strength;
            }
        }

        private Vector2 GeneratePointOnCircle(float angle, Vector2 center, float radius)
        {
            return this.findPointFromAngleAndDistance(center, angle, radius);
        }

        private Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
        {
            Vector2 vector2 = new Vector2(0.0f, 0.0f);
            float num1 = angle;
            float num2 = distance;
            int num3 = 0;
            float num4 = 0.0f;
            float num5 = 0.0f;
            if ((double)num1 > 360.0)
                num1 -= 360f;
            if ((double)num1 < 90.0)
            {
                float num6 = (float)((double)(90f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 1;
            }
            else if ((double)num1 > 90.0 && (double)num1 < 180.0)
            {
                float num6 = (float)((double)(num1 - 90f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 2;
            }
            else if ((double)num1 > 180.0 && (double)num1 < 270.0)
            {
                float num6 = (float)((double)(270f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 3;
            }
            else if ((double)num1 > 270.0 && (double)num1 < 360.0)
            {
                float num6 = (float)((double)(num1 - 270f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 4;
            }
            if ((double)num1 == 0.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y - num2;
            }
            if ((double)num1 == 90.0)
            {
                vector2.X = position.X + num2;
                vector2.Y = position.Y;
            }
            if ((double)num1 == 180.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y + num2;
            }
            if ((double)num1 == 270.0)
            {
                vector2.X = position.X - num2;
                vector2.Y = position.Y;
            }
            if (num3 == 1)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y - num4;
            }
            else if (num3 == 2)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 3)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 4)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y - num4;
            }
            return vector2;
        }

        public enum ColonyType
        {
            Core,
            Colony,
            Industrial,
            Research,
            Agricultural,
            Military,
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
    }
}
