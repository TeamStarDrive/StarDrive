using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public enum PlanetType
    {
        Other,
        Barren,
        Terran,
    }

    public enum SunZone
    {
        Near,
        Habital,
        Far,
        VeryFar,
        Any
    }

    public sealed class Planet : Explorable, IDisposable
    {
        public bool GovBuildings = true;
        public bool GovSliders = true;
        public BatchRemovalCollection<Combat> ActiveCombats = new BatchRemovalCollection<Combat>();
        public Guid guid = Guid.NewGuid();
        public Array<PlanetGridSquare> TilesList = new Array<PlanetGridSquare>(35);
        public BatchRemovalCollection<OrbitalDrop> OrbitalDropList = new BatchRemovalCollection<OrbitalDrop>();
        public GoodState fs = GoodState.STORE;
        public GoodState ps = GoodState.STORE;
        public Planet.GoodState GetGoodState(string good)
        {
            switch (good)
            {
                case "Food":
                    return fs;
                case "Production":
                    return ps;
            }
            return 0;
        }
        public Array<Building> BuildingList = new Array<Building>();
        public SpaceStation Station = new SpaceStation();
        public Map<Guid, Ship> Shipyards = new Map<Guid, Ship>();
        public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
        public BatchRemovalCollection<QueueItem> ConstructionQueue = new BatchRemovalCollection<QueueItem>();
        private float ZrotateAmount = 0.03f;
        public BatchRemovalCollection<Ship> BasedShips = new BatchRemovalCollection<Ship>();
        public bool GovernorOn = true;
        private AudioEmitter emit = new AudioEmitter();
        private float DecisionTimer = 0.5f;
        public BatchRemovalCollection<Projectile> Projectiles = new BatchRemovalCollection<Projectile>();
        private Array<Building> BuildingsCanBuild = new Array<Building>();
        public float FarmerPercentage = 0.34f;
        public float WorkerPercentage = 0.33f;
        public float ResearcherPercentage = 0.33f;
        public Array<string> CommoditiesPresent = new Array<string>();
        private Map<string, float> ResourcesDict = new Map<string, float>(StringComparer.OrdinalIgnoreCase);
        private float PosUpdateTimer = 1f;
        public float MAX_STORAGE = 10f;
        public string DevelopmentStatus = "Undeveloped";
        public Array<string> Guardians = new Array<string>();
        public bool FoodLocked;
        public bool ProdLocked;
        public bool ResLocked;
        public bool RecentCombat;
        public int Crippled_Turns;
        public ColonyType colonyType;
        public float ShieldStrengthCurrent;
        public float ShieldStrengthMax;
        private int TurnsSinceTurnover;
        public bool isSelected;
        public Vector2 Center;
        public string SpecialDescription;
        public bool HasShipyard;
        public SolarSystem ParentSystem; 
        public Matrix cloudMatrix;        
        public bool hasEarthLikeClouds;
        public string Name;
        public string Description;
        public Empire Owner;
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
        public float GetNetGoodProd(string good)
        {
            switch (good)
            {
                case "Food":
                    return NetFoodPerTurn;
                case "Production":
                    return NetProductionPerTurn;
            }
            return 0;
        }
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
        private Shield Shield;
        public float FoodHere;
        public float GetGoodHere(string good)
        {
            switch (good)
            {
                case "Food":
                    return FoodHere;
                case "Production":
                    return ProductionHere;
            }
            return 0;
        }
        public int developmentLevel;
        public bool CorsairPresence;
        public bool queueEmptySent = true;
        public float RepairPerTurn = 50;
        public Array<string> PlanetFleets = new Array<string>();
        private bool PSexport = false;
        //private bool FSexport = false;
        public bool UniqueHab = false;
        public int uniqueHabPercent;
        public float ExportPSWeight =0;
        public float ExportFSWeight = 0;

        public float TradeIncomingColonists =0;

        public void SetExportWeight(string goodType, float weight)
        {
            switch (goodType)
            {
                case "Food":
                    ExportFSWeight = weight;
                    break;
                case "Production":
                    ExportPSWeight = weight;
                    break;

            }   
        }
        public float GetExportWeight(string goodType)
        {
            switch (goodType)
            {
                case "Food":
                    return ExportFSWeight;
                case "Production":
                    return ExportPSWeight;                    
            }
            return 0;
        }
        private AudioEmitter Emitter;
        private float InvisibleRadius;
        public float GravityWellRadius { get; private set; }
        public SunZone Zone { get; private set; }
        private float HabitalTileChance = 10;
        public float ObjectRadius
        {
            get { return SO != null ? SO.WorldBoundingSphere.Radius : InvisibleRadius; }
            set { InvisibleRadius = SO != null ? SO.WorldBoundingSphere.Radius : value; }
        }
        
        public Planet()
        {
            foreach (KeyValuePair<string, Good> keyValuePair in ResourceManager.GoodsDict)
                AddGood(keyValuePair.Key, 0);
            HasShipyard = false;            
        }

        public Goods ImportPriority()
        {
            if (NetFoodPerTurn <= 0 || FarmerPercentage > .5f)
            {
                if (ConstructingGoodsBuilding(Goods.Food))
                    return Goods.Production;
                return Goods.Food;
            }
            if (ConstructionQueue.Count > 0) return Goods.Production;
            if (ps == GoodState.IMPORT) return Goods.Production;
            if (fs == GoodState.IMPORT) return Goods.Food;
            return Goods.Food;
        }

        public bool ConstructingGoodsBuilding(Goods goods)
        {
            if (ConstructionQueue.IsEmpty) return false;
            switch (goods)
            {
                case Goods.Production:
                    foreach (var item in ConstructionQueue)
                    {
                        if (item.isBuilding && item.Building.ProducesProduction)
                        {
                            return true;                            
                        }
                    }
                    break;
                case Goods.Food:
                    foreach (var item in ConstructionQueue)
                    {
                        if (item.isBuilding && item.Building.ProducesFood)
                        {
                            return true;
                        }
                    }
                    break;
                case Goods.Colonists:
                    break;
                default:
                    break;
            }
            return false;
        }
 

        public Planet(SolarSystem system, float randomAngle, float ringRadius, string name, float ringMax, Empire owner = null)
        {                        
            var newOrbital = this;

            Name = name;
            OrbitalAngle = randomAngle;
            ParentSystem = system;
                
            
            SunZone sunZone;
            float zoneSize = ringMax;
            if (ringRadius < zoneSize * .15f)
                sunZone = SunZone.Near;
            else if (ringRadius < zoneSize * .25f)
                sunZone = SunZone.Habital;
            else if (ringRadius < zoneSize * .7f)
                sunZone = SunZone.Far;
            else
                sunZone = SunZone.VeryFar;
            if (owner != null && owner.Capital == null && sunZone >= SunZone.Habital)
            {
                planetType = RandomMath.IntBetween(0, 1) == 0 ? 27 : 29;
                owner.SpawnHomePlanet(newOrbital);
                Name = ParentSystem.Name + " " + NumberToRomanConvertor.NumberToRoman(1);
            }
            else            
            {
                GenerateType(sunZone);
                newOrbital.SetPlanetAttributes(true);
            }
            
            float zoneBonus = ((int)sunZone +1) * .2f * ((int)sunZone +1);
            float scale = RandomMath.RandomBetween(0f, zoneBonus) + .9f;
            if (newOrbital.planetType == 2 || newOrbital.planetType == 6 || newOrbital.planetType == 10 ||
                newOrbital.planetType == 12 || newOrbital.planetType == 15 || newOrbital.planetType == 20 ||
                newOrbital.planetType == 26)
                scale += 2.5f;

            float planetRadius       = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
            newOrbital.ObjectRadius  = planetRadius;
            newOrbital.OrbitalRadius = ringRadius + planetRadius;
            Vector2 planetCenter     = MathExt.PointOnCircle(randomAngle, ringRadius);
            newOrbital.Center        = planetCenter;
            newOrbital.scale         = scale;            
            newOrbital.planetTilt    = RandomMath.RandomBetween(45f, 135f);


            GenerateMoons(newOrbital);

            if (RandomMath.RandomBetween(1f, 100f) < 15f)
            {
                newOrbital.hasRings = true;
                newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
            }
        }

        public float EmpireFertility(Empire empire) =>
            (empire.data?.Traits.Cybernetic ?? 0) > 0 ? MineralRichness : Fertility;
            

        public float EmpireBaseValue(Empire empire) => 
            (CommoditiesPresent.Count +
            EmpireFertility(empire)
            * MineralRichness) 
            * (MaxPopulation / 1000f);

        public bool NeedsFood()
        {
            if (Owner?.isFaction ?? true) return false;
            bool cyber = Owner.data.Traits.Cybernetic > 0;
            float food = cyber ? ProductionHere : FoodHere;
            bool badProduction = cyber ? NetProductionPerTurn <= 0 && WorkerPercentage > .5f : 
                (NetFoodPerTurn <= 0 && FarmerPercentage >.5f);
            return food / MAX_STORAGE < .10f || badProduction;
        }

        private void GenerateMoons(Planet newOrbital)
        {
            int moonCount = (int) Math.Ceiling(ObjectRadius * .004f);
            moonCount = (int) Math.Round(RandomMath.AvgRandomBetween(-moonCount * .75f, moonCount));
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

        private void GenerateType(SunZone sunZone)
        {            
            for (int x = 0; x < 5; x++)
            {                
                Type = "";
                planetComposition = "";
                hasEarthLikeClouds = false;
                habitable = false;
                MaxPopulation = 0;
                Fertility = 0;
                planetType = RandomMath.IntBetween(1, 24);
                TilesList.Clear();
                ApplyPlanetType();
                if (Zone == sunZone || (Zone == SunZone.Any && sunZone == SunZone.Near)) break;
                if (x > 2 && Zone == SunZone.Any) break;
            }
        }

        private void PlayPlanetSfx(string sfx, Vector3 position)
        {
            if (Emitter == null)
                Emitter = new AudioEmitter();
            Emitter.Position = position;
            GameAudio.PlaySfxAsync(sfx, Emitter);
        }

        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
        }

        //added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb)
        {
            if (bomb.Owner == Owner)
            {
                return;
            }
            if (Owner != null && !Owner.GetRelations(bomb.Owner).AtWar && TurnsSinceTurnover > 10 && Empire.Universe.PlayerEmpire == bomb.Owner)
            {
                Owner.GetGSAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
            }
            CombatTimer = 10f;
            if (ShieldStrengthCurrent <= 0f)
            {
                float ran = RandomMath.RandomBetween(0f, 100f);
                bool hit = true;
                if (ran < 75f)
                {
                    hit = false;
                }
                Population -= 1000f * ResourceManager.WeaponsDict[bomb.WeaponName].BombPopulationKillPerHit;

                if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView && ParentSystem.isVisible)
                {
                    PlayPlanetSfx("sd_bomb_impact_01", bomb.Position);

                    ExplosionManager.AddExplosionNoFlames(bomb.Position, 200f, 7.5f, 0.6f);
                    Empire.Universe.flash.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    for (int i = 0; i < 50; i++)
                    {
                        Empire.Universe.explosionParticles.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    }
                }
                Planet.OrbitalDrop od = new Planet.OrbitalDrop();
                Array<PlanetGridSquare> PotentialHits = new Array<PlanetGridSquare>();
                if (hit)
                {
                    int buildingcount = 0;
                    foreach (PlanetGridSquare pgs in TilesList)
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
                        if (BuildingList.Count > 0)
                            BuildingList.Clear();
                    }
                    else
                    {
                        int ranhit = (int)RandomMath.RandomBetween(0f, PotentialHits.Count + 1f);
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
                    foreach (PlanetGridSquare pgs in TilesList)
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
                    item.Strength = item.Strength - (int)RandomMath.RandomBetween(ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Min, ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Max);
                    if (od.Target.TroopsHere[0].Strength <= 0)
                    {
                        TroopsHere.Remove(od.Target.TroopsHere[0]);
                        od.Target.TroopsHere.Clear();
                    }
                }
                else if (od.Target.building != null)
                {
                    Building target = od.Target.building;
                    target.Strength = target.Strength - (int)RandomMath.RandomBetween(ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMin, ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMax);
                    if (od.Target.building.CombatStrength > 0)
                    {
                        od.Target.building.CombatStrength = od.Target.building.Strength;
                    }
                    if (od.Target.building.Strength <= 0)
                    {
                        BuildingList.Remove(od.Target.building);
                        od.Target.building = null;

                        bool flag = od.Target.Biosphere;
                        //Added Code here
                        od.Target.Habitable = false;
                        od.Target.highlighted = false;
                        od.Target.Biosphere = false;
                        if (flag)
                        {
                            foreach (Building bios in BuildingList)
                            {
                                if (bios.Name == "Biospheres")
                                {
                                    od.Target.building = bios;
                                    break;
                                }
                            }
                            if (od.Target.building != null)
                            {
                                Population -= od.Target.building.MaxPopIncrease;
                                BuildingList.Remove(od.Target.building);
                                od.Target.building = null;
                            }
                        }
                    }
                }
                if (Empire.Universe.workersPanel is CombatScreen && Empire.Universe.LookingAtPlanet && (Empire.Universe.workersPanel as CombatScreen).p == this)
                {
                    GameAudio.PlaySfxAsync("Explo1");
                    CombatScreen.SmallExplosion exp1 = new CombatScreen.SmallExplosion(4);
                    exp1.grid = od.Target.ClickRect;
                    lock (GlobalStats.ExplosionLocker)
                    {
                        (Empire.Universe.workersPanel as CombatScreen).Explosions.Add(exp1);
                    }
                }
                if (Population <= 0f)
                {
                    Population = 0f;
                    if (Owner != null)
                    {
                        Owner.RemovePlanet(this);
                        if (IsExploredBy(Empire.Universe.PlayerEmpire))
                        {
                            Empire.Universe.NotificationManager.AddPlanetDiedNotification(this, Empire.Universe.PlayerEmpire);
                        }
                        bool removeowner = true;
                        if (Owner != null)
                        {
                            foreach (Planet other in ParentSystem.PlanetList)
                            {
                                if (other.Owner != Owner || other == this)
                                {
                                    continue;
                                }
                                removeowner = false;
                            }
                            if (removeowner)
                            {
                                ParentSystem.OwnerList.Remove(Owner);
                            }
                        }
                        ConstructionQueue.Clear();
                        Owner = null;
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
                        if (Owner != null && Owner == EmpireManager.Cordrazine)
                        {
                            for (int i = 0; i < TroopsHere.Count; i++)
                            {
                                if (TroopsHere[i].GetOwner() == EmpireManager.Cordrazine && TroopsHere[i].TargetType == "Soft")
                                {
                                    if (SteamManager.SetAchievement("Owlwoks_Freed"))
                                    {
                                        SteamManager.SaveAllStatAndAchievementChanges();
                                    }
                                    TroopsHere[i].SetOwner(bomb.Owner);
                                    TroopsHere[i].Name = Localizer.Token(EmpireManager.Cordrazine.data.TroopNameIndex);
                                    TroopsHere[i].Description = Localizer.Token(EmpireManager.Cordrazine.data.TroopDescriptionIndex);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    PlayPlanetSfx("sd_impact_shield_01", Shield.Center);
                }
                Shield.Rotation = Center.RadiansToTarget(new Vector2(bomb.Position.X, bomb.Position.Y));
                Shield.displacement = 0f;
                Shield.texscale = 2.8f;
                Shield.Radius = SO.WorldBoundingSphere.Radius + 100f;
                Shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                Shield.texscale = 2.8f;
                Shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                Shield.Center = new Vector3(Center.X, Center.Y, 2500f);
                Shield.pointLight.World = bomb.World;
                Shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                Shield.pointLight.Radius = 50f;
                Shield.pointLight.Intensity = 8f;
                Shield.pointLight.Enabled = true;
                Vector3 vel = Vector3.Normalize(bomb.Position - Shield.Center);
                if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    Empire.Universe.flash.AddParticleThreadB(bomb.Position, Vector3.Zero);
                    for (int i = 0; i < 200; i++)
                    {
                        Empire.Universe.sparks.AddParticleThreadB(bomb.Position, vel * new Vector3(RandomMath.RandomBetween(-25f, 25f), RandomMath.RandomBetween(-25f, 25f), RandomMath.RandomBetween(-25f, 25f)));
                    }
                }
                Planet shieldStrengthCurrent = this;
                shieldStrengthCurrent.ShieldStrengthCurrent = shieldStrengthCurrent.ShieldStrengthCurrent - ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Max;
                if (ShieldStrengthCurrent < 0f)
                {
                    ShieldStrengthCurrent = 0f;
                    return;
                }
            }
        }

        public float GetNetFoodPerTurn()
        {
            if (Owner != null && Owner.data.Traits.Cybernetic == 1)
                return NetFoodPerTurn;
            else
                return NetFoodPerTurn - consumption;
        }

        public float GetNetProductionPerTurn()
        {
            if (Owner != null && Owner.data.Traits.Cybernetic == 1)
                return NetProductionPerTurn - consumption;
            else
                return NetProductionPerTurn;
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
            return habitable ? " None" : " Uninhabitable";
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

        public void SetPlanetAttributes(bool setType = true)
        {
            hasEarthLikeClouds = false;
            float richness = RandomMath.RandomBetween(0.0f, 100f);
            if (richness >= 92.5f)      MineralRichness = RandomMath.RandomBetween(2.00f, 2.50f);
            else if (richness >= 85.0f) MineralRichness = RandomMath.RandomBetween(1.50f, 2.00f);
            else if (richness >= 25.0f) MineralRichness = RandomMath.RandomBetween(0.75f, 1.50f);
            else if (richness >= 12.5f) MineralRichness = RandomMath.RandomBetween(0.25f, 0.75f);
            else if (richness  < 12.5f) MineralRichness = RandomMath.RandomBetween(0.10f, 0.25f);

            if (setType) ApplyPlanetType();
            if (!habitable)
                MineralRichness = 0.0f;
                       

            AddEventsAndCommodities();
        }



        private void ApplyPlanetType()
        {
            HabitalTileChance = 20;
            switch (planetType)
            {
                case 1:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1700);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    MaxPopulation = (int) RandomMath.RandomBetween(4000f, 8000f);                    
                    Fertility = RandomMath.RandomBetween(0.8f, 1.5f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 3:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1702);
                    habitable = true;
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 4:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1703);
                    habitable = true;
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 5:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1704);
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 6:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 7:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1704);
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 8:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1703);
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 9:
                    Type = "Volcanic";
                    planetComposition = Localizer.Token(1705);
                    Zone = SunZone.Any;
                    break;
                case 10:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1706);
                    Zone = SunZone.Far;
                    break;
                case 11:
                    Type = "Tundra";
                    planetComposition = Localizer.Token(1707);
                    MaxPopulation = (int) RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.AvgRandomBetween(0.5f, 1f);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    Zone = SunZone.Far;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Type = "Gas Giant";
                    habitable = false;
                    planetComposition = Localizer.Token(1708);
                    Zone = SunZone.Far;
                    break;
                case 13:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1709);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int) RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.AvgRandomBetween(1f, 3f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Type = "Desert";
                    planetComposition = Localizer.Token(1710);
                    HabitalTileChance = RandomMath.AvgRandomBetween(10f, 45f);
                    MaxPopulation = (int)HabitalTileChance * 100;                    
                    Fertility = RandomMath.AvgRandomBetween(-2f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    habitable = true;                    
                    Zone = SunZone.Near;                    
                    break;
                case 15:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    planetType = 26;
                    Zone = SunZone.Far;
                    break;
                case 16:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1712);
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 17:
                    Type = "Ice";
                    planetComposition = Localizer.Token(1713);
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    Zone = SunZone.VeryFar;
                    break;
                case 18:
                    Type = "Steppe";
                    planetComposition = Localizer.Token(1714);                    
                    hasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f,45f);
                    MaxPopulation = (int)HabitalTileChance * 200;
                    Fertility = RandomMath.AvgRandomBetween(0f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    habitable = true;
                    Zone = SunZone.Habital;                    
                    break;
                case 19:
                    Type = "Swamp";
                    habitable = true;
                    planetComposition = Localizer.Token(1715);
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = HabitalTileChance * 200;                    
                    Fertility = RandomMath.AvgRandomBetween(-2f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    hasEarthLikeClouds = true;
                    Zone = SunZone.Near;
                    break;
                case 20:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 21:
                    Type = "Oceanic";
                    planetComposition = Localizer.Token(1716);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = HabitalTileChance * 100 + 1500;                                        
                    Fertility = RandomMath.AvgRandomBetween(-3f, 5f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 22:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1717);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(0f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 23:
                    Type = "Volcanic";
                    planetComposition = Localizer.Token(1718);
                    Zone = SunZone.Near;
                    break;
                case 24:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1719);
                    habitable = true;
                    MaxPopulation = (int) RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 25:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1720);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-.50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 26:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 27:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1721);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 29:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1722);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(50f, 80f);
                    MaxPopulation = HabitalTileChance * 150f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
            }
        }

        private void AddEventsAndCommodities()
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

        private void AddTileEvents()
        {
            if (RandomMath.RandomBetween(0.0f, 100f) <= 15 && habitable)
            {
                Array<string> list = new Array<string>();
                foreach (var kv in ResourceManager.BuildingsDict)
                {
                    if (!string.IsNullOrEmpty(kv.Value.EventTriggerUID) && !kv.Value.NoRandomSpawn)
                        list.Add(kv.Key);
                }
                int index = (int) RandomMath.RandomBetween(0f, list.Count + 0.85f);
                if (index >= list.Count)
                    index = list.Count - 1;
                var b = AssignBuildingToRandomTile(ResourceManager.CreateBuilding(list[index]));
                BuildingList.Add(b.building);
                Log.Info($"Event building : {b.building.Name} : created on {Name}");
            }
        }

        private void SetTileHabitability(float habChance)
        {            
            {
                if (UniqueHab)
                {
                    habChance = uniqueHabPercent;
                }
                bool habitable = false;
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        if (habChance > 0)
                            habitable = RandomMath.RandomBetween(0, 100) < habChance;

                        TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, null, habitable));
                    }
                }
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
                    var pgs = AssignBuildingToRandomTile(ResourceManager.CreateBuilding(randItem.BuildingID));
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
            switch (planetType)
            {
                case 1:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1700);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 2f);
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1702);
                    habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 4:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1702);
                    habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 5:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    break;
                case 6:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    break;
                case 8:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    break;
                case 9:
                    Type = "Volcanic";
                    planetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Type = "Tundra";
                    planetComposition = Localizer.Token(1707);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 0.9f);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Type = "Gas Giant";
                    habitable = false;
                    planetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1709);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(0.8f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Type = "Desert";
                    planetComposition = Localizer.Token(1710);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(0.2f, 1.8f);
                    habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 15:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    planetType = 26;
                    break;
                case 16:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    break;
                case 17:
                    Type = "Ice";
                    planetComposition = Localizer.Token(1713);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    habitable = true;
                    HabitalTileChance = 10;
                    break;
                case 18:
                    Type = "Steppe";
                    planetComposition = Localizer.Token(1714);
                    Fertility = RandomMath.RandomBetween(0.4f, 1.4f);
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(2000f, 4000f);
                    habitable = true;
                    HabitalTileChance = 50;
                    break;
                case 19:
                    Type = "Swamp";
                    habitable = true;
                    planetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(1f, 5f);
                    hasEarthLikeClouds = true;
                    HabitalTileChance = 20;
                    break;
                case 20:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Type = "Oceanic";
                    planetComposition = Localizer.Token(1716);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(3000f, 6000f);
                    Fertility = RandomMath.RandomBetween(2f, 5f);
                    HabitalTileChance = 20;
                    break;
                case 22:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1717);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 23:
                    Type = "Volcanic";
                    planetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1719);
                    habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 25:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1720);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 2f);
                    HabitalTileChance = 90;
                    break;
                case 26:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1721);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 60;
                    break;
                case 29:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1722);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 50;
                    break;
            }
       
            if (!habitable)
                MineralRichness = 0.0f;
            else
            {
                if (Fertility > 0)
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        double num2 = RandomMath.RandomBetween(0.0f, 100f);
                        bool habitableTile = (int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance;
                        TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, null, habitableTile));

                    }
                }
            }

            
        }

        public void Terraform()
        {
            switch (planetType)
            {
                case 7:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(0.0f, 500f);
                    hasEarthLikeClouds = false;
                    habitable = true;
                    break;
                case 11:
                    Type = "Tundra";
                    planetComposition = Localizer.Token(1724);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    break;
                case 14:
                    Type = "Desert";
                    planetComposition = Localizer.Token(1725);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    habitable = true;
                    break;
                case 18:
                    Type = "Steppe";
                    planetComposition = Localizer.Token(1726);
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(2000f, 4000f);
                    habitable = true;
                    break;
                case 19:
                    Type = "Swamp";
                    planetComposition = Localizer.Token(1727);
                    habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    hasEarthLikeClouds = true;
                    break;
                case 21:
                    Type = "Oceanic";
                    planetComposition = Localizer.Token(1728);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(3000f, 6000f);
                    break;
                case 22:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1717);
                    habitable = true;
                    hasEarthLikeClouds = true;
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

        public void LoadAttributes()
        {
            switch (planetType)
            {
                case 1:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1700);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    break;
                case 2:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1702);
                    habitable = true;
                    break;
                case 4:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1702);
                    habitable = true;
                    break;
                case 5:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1703);
                    habitable = true;
                    break;
                case 6:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1704);
                    habitable = true;
                    break;
                case 8:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1703);
                    habitable = true;
                    break;
                case 9:
                    Type = "Volcanic";
                    planetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Type = "Tundra";
                    planetComposition = Localizer.Token(1707);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    break;
                case 12:
                    Type = "Gas Giant";
                    habitable = false;
                    planetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1709);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    break;
                case 14:
                    Type = "Desert";
                    planetComposition = Localizer.Token(1710);
                    habitable = true;
                    break;
                case 15:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    planetType = 26;
                    break;
                case 16:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1712);
                    habitable = true;
                    break;
                case 17:
                    Type = "Ice";
                    planetComposition = Localizer.Token(1713);
                    habitable = true;
                    break;
                case 18:
                    Type = "Steppe";
                    planetComposition = Localizer.Token(1714);
                    hasEarthLikeClouds = true;
                    habitable = true;
                    break;
                case 19:
                    Type = "Swamp";
                    planetComposition = "";
                    habitable = true;
                    hasEarthLikeClouds = true;
                    break;
                case 20:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Type = "Oceanic";
                    planetComposition = Localizer.Token(1716);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    break;
                case 22:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1717);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    break;
                case 23:
                    Type = "Volcanic";
                    planetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Type = "Barren";
                    planetComposition = Localizer.Token(1719);
                    habitable = true;
                    break;
                case 25:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1720);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    break;
                case 26:
                    Type = "Gas Giant";
                    planetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1721);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    break;
                case 29:
                    Type = "Terran";
                    planetComposition = Localizer.Token(1722);
                    habitable = true;
                    hasEarthLikeClouds = true;
                    break;
            }
        }

        public bool AssignTroopToNearestAvailableTile(Troop t, PlanetGridSquare tile)
        {
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops 
                    && (planetGridSquare.building == null || planetGridSquare.building != null && planetGridSquare.building.CombatStrength == 0) 
                    && (Math.Abs(tile.x - planetGridSquare.x) <= 1 && Math.Abs(tile.y - planetGridSquare.y) <= 1))
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.TroopsHere.Add(t);
                        TroopsHere.Add(t);
                        t.SetPlanet(this);
                        return true;

                    }
                }
            }
            return false;

        }
        

        public bool AssignTroopToTile(Troop t)
        {
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops && (planetGridSquare.building == null || planetGridSquare.building != null && planetGridSquare.building.CombatStrength == 0))
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
                PlanetGridSquare planetGridSquare = list[index];
                foreach (PlanetGridSquare eventLocation in TilesList)
                {
                    if (eventLocation == planetGridSquare)
                    {
                        eventLocation.TroopsHere.Add(t);
                        TroopsHere.Add(t);
                        t.SetPlanet(this);
                        if (eventLocation.building == null || string.IsNullOrEmpty(eventLocation.building.EventTriggerUID) || (eventLocation.TroopsHere.Count <= 0 || eventLocation.TroopsHere[0].GetOwner().isFaction))
                            return true;
                        ResourceManager.EventsDict[eventLocation.building.EventTriggerUID].TriggerPlanetEvent(this, eventLocation.TroopsHere[0].GetOwner(), eventLocation, Empire.Universe);
                    }
                }
            }
            return false;
        }

        public bool AssignBuildingToTile(Building b)
        {
            if (AssignBuildingToRandomTile(b, true) != null)
                return true;
            PlanetGridSquare targetPGS;
            if (!string.IsNullOrEmpty(b.EventTriggerUID))
            {
                targetPGS = AssignBuildingToRandomTile(b);
                if (targetPGS != null)                
                    return targetPGS.Habitable = true;                    
                
            }
            if (b.Name == "Outpost" || !string.IsNullOrEmpty(b.EventTriggerUID))
            {
                targetPGS = AssignBuildingToRandomTile(b);
                if (targetPGS != null)
                    return targetPGS.Habitable = true;
            }
            if (b.Name == "Biospheres")
                return AssignBuildingToRandomTile(b) != null;                    
            return false;            
        }

        public bool AssignBuildingToTileOnColonize(Building b)
        {
            if (AssignBuildingToRandomTile(b, true) != null) return true;
            if (AssignBuildingToRandomTile(b) != null) return true;
            return false;
        }

        public PlanetGridSquare AssignBuildingToRandomTile(Building b, bool habitable = false)
        {
            PlanetGridSquare[] list;
            if (!habitable)
                list = TilesList.FilterBy(planetGridSquare => planetGridSquare.building == null);
            else
                list = TilesList.FilterBy(planetGridSquare => planetGridSquare.building == null && planetGridSquare.Habitable == true);
            if (list.Length == 0)
                return null;

            int index = RandomMath.InRange(list.Length-1);             
            var targetPGS = TilesList.Find(pgs => pgs == list[index]);
            targetPGS.building = b;
            return targetPGS;

        }

        public void AssignBuildingToSpecificTile(Building b, PlanetGridSquare pgs)
        {
            if (pgs.building != null)
                BuildingList.Remove(pgs.building);
            pgs.building = b;
            BuildingList.Add(b);
        }

        public bool TryBiosphereBuild(Building b, QueueItem qi)
        {            
            if (qi.isBuilding == false &&  (FarmerPercentage > .5f || NetFoodPerTurn < 0))
                return false;
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                if (!planetGridSquare.Habitable && planetGridSquare.building == null && (!planetGridSquare.Biosphere && planetGridSquare.QItem == null))
                    list.Add(planetGridSquare);
            }
            if (b.Name != "Biospheres" || list.Count <= 0) return false;

            int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
            PlanetGridSquare planetGridSquare1 = list[index];
            foreach (PlanetGridSquare planetGridSquare2 in TilesList)
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
                    ConstructionQueue.Add(qi);
                    return true;
                }
            }
            return false;
        }

        public bool AssignBuildingToTile(Building b, QueueItem qi)
        {
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            if (b.Name == "Biospheres") 
                return false;
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                bool flag = true;
                foreach (QueueItem queueItem in ConstructionQueue)
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
                int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in TilesList)
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
                PlanetGridSquare planetGridSquare1 = TilesList[(int)RandomMath.RandomBetween(0.0f, TilesList.Count)];
                foreach (PlanetGridSquare planetGridSquare2 in TilesList)
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
            BasedShips.Add(ship);
        }

        private void DoViewedCombat(float elapsedTime)
        {
            using (ActiveCombats.AcquireReadLock())
            foreach (Combat combat in ActiveCombats)
            {
                if (combat.Attacker.TroopsHere.Count == 0 && combat.Attacker.building == null)
                {
                        ActiveCombats.QueuePendingRemoval(combat);
                    break;
                }
                else
                {
                    if (combat.Attacker.TroopsHere.Count > 0)
                    {
                        if (combat.Attacker.TroopsHere[0].Strength <= 0)
                        {
                                ActiveCombats.QueuePendingRemoval(combat);
                            break;
                        }
                    }
                    else if (combat.Attacker.building != null && combat.Attacker.building.Strength <= 0)
                    {
                            ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    if (combat.Defender.TroopsHere.Count == 0 && combat.Defender.building == null)
                    {
                            ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    else
                    {
                        if (combat.Defender.TroopsHere.Count > 0)
                        {
                            if (combat.Defender.TroopsHere[0].Strength <= 0)
                            {
                                    ActiveCombats.QueuePendingRemoval(combat);
                                break;
                            }
                        }
                        else if (combat.Defender.building != null && combat.Defender.building.Strength <= 0)
                        {
                                ActiveCombats.QueuePendingRemoval(combat);
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
                        if (combat.Timer < 3.0 && combat.phase == 1)
                        {
                            for (int index = 0; index < num1; ++index)
                            {
                                if (RandomMath.RandomBetween(0.0f, 100f) < (str == "Soft" ? num3 : (double)num2))
                                    ++num4;
                            }
                            if (num4 > 0 && (combat.Defender.TroopsHere.Count > 0 || combat.Defender.building != null && combat.Defender.building.Strength > 0))
                            {
                                GameAudio.PlaySfxAsync("sd_troop_attack_hit");
                                CombatScreen.SmallExplosion smallExplosion = new CombatScreen.SmallExplosion(1);
                                smallExplosion.grid = combat.Defender.TroopClickRect;
                                lock (GlobalStats.ExplosionLocker)
                                    (Empire.Universe.workersPanel as CombatScreen).Explosions.Add(smallExplosion);
                                if (combat.Defender.TroopsHere.Count > 0)
                                {
                                    combat.Defender.TroopsHere[0].Strength -= num4;
                                    if (combat.Defender.TroopsHere[0].Strength <= 0)
                                    {
                                            TroopsHere.Remove(combat.Defender.TroopsHere[0]);
                                        combat.Defender.TroopsHere.Clear();
                                            ActiveCombats.QueuePendingRemoval(combat);
                                        GameAudio.PlaySfxAsync("Explo1");
                                        lock (GlobalStats.ExplosionLocker)
                                            (Empire.Universe.workersPanel as CombatScreen).Explosions.Add(new CombatScreen.SmallExplosion(4)
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
                                            BuildingList.Remove(combat.Defender.building);
                                        combat.Defender.building = null;
                                    }
                                }
                            }
                            else if (num4 == 0)
                                GameAudio.PlaySfxAsync("sd_troop_attack_miss");
                            combat.phase = 2;
                        }
                        else if (combat.phase == 2)
                                ActiveCombats.QueuePendingRemoval(combat);
                    }
                }
            }
        }

        private void DoCombatUnviewed(float elapsedTime)
        {
            using (ActiveCombats.AcquireReadLock())
            foreach (Combat combat in ActiveCombats)
            {
                if (combat.Attacker.TroopsHere.Count == 0 && combat.Attacker.building == null)
                {
                        ActiveCombats.QueuePendingRemoval(combat);
                    break;
                }
                else
                {
                    if (combat.Attacker.TroopsHere.Count > 0)
                    {
                        if (combat.Attacker.TroopsHere[0].Strength <= 0)
                        {
                                ActiveCombats.QueuePendingRemoval(combat);
                            break;
                        }
                    }
                    else if (combat.Attacker.building != null && combat.Attacker.building.Strength <= 0)
                    {
                            ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    if (combat.Defender.TroopsHere.Count == 0 && combat.Defender.building == null)
                    {
                            ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }
                    else
                    {
                        if (combat.Defender.TroopsHere.Count > 0)
                        {
                            if (combat.Defender.TroopsHere[0].Strength <= 0)
                            {
                                    ActiveCombats.QueuePendingRemoval(combat);
                                break;
                            }
                        }
                        else if (combat.Defender.building != null && combat.Defender.building.Strength <= 0)
                        {
                                ActiveCombats.QueuePendingRemoval(combat);
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
                        if (combat.Timer < 3.0 && combat.phase == 1)
                        {
                            for (int index = 0; index < num1; ++index)
                            {
                                if (RandomMath.RandomBetween(0.0f, 100f) < (str == "Soft" ? num3 : (double)num2))
                                    ++num4;
                            }
                            if (num4 > 0 && (combat.Defender.TroopsHere.Count > 0 || combat.Defender.building != null && combat.Defender.building.Strength > 0))
                            {
                                if (combat.Defender.TroopsHere.Count > 0)
                                {
                                    combat.Defender.TroopsHere[0].Strength -= num4;
                                    if (combat.Defender.TroopsHere[0].Strength <= 0)
                                    {
                                            TroopsHere.Remove(combat.Defender.TroopsHere[0]);
                                        combat.Defender.TroopsHere.Clear();
                                            ActiveCombats.QueuePendingRemoval(combat);
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
                                            BuildingList.Remove(combat.Defender.building);
                                        combat.Defender.building = null;
                                    }
                                }
                            }
                            combat.phase = 2;
                        }
                        else if (combat.phase == 2)
                                ActiveCombats.QueuePendingRemoval(combat);
                    }
                }
            }
        }

        public void DoCombats(float elapsedTime)
        {
            if (Empire.Universe.LookingAtPlanet)
            {
                if (Empire.Universe.workersPanel is CombatScreen)
                {
                    if ((Empire.Universe.workersPanel as CombatScreen).p == this)
                        DoViewedCombat(elapsedTime);
                }
                else
                {
                    DoCombatUnviewed(elapsedTime);
                    ActiveCombats.ApplyPendingRemovals();
                }
            }
            else
            {
                DoCombatUnviewed(elapsedTime);
                ActiveCombats.ApplyPendingRemovals();
            }
            if (ActiveCombats.Count > 0)
                CombatTimer = 10f;
            if (TroopsHere.Count <= 0 || Owner == null)
                return;
            bool flag = false;
            int num1 = 0;
            int num2 = 0;
            Empire index = null;
            
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                using (planetGridSquare.TroopsHere.AcquireReadLock())
                foreach (Troop troop in planetGridSquare.TroopsHere)
                {
                    if (troop.GetOwner() != null && troop.GetOwner() != Owner)
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
            
            if (num2 > numInvadersLast && numInvadersLast == 0)
            {
                if (Empire.Universe.PlayerEmpire == Owner)
                    Empire.Universe.NotificationManager.AddEnemyTroopsLandedNotification(this, index, Owner);
                else if (index == Empire.Universe.PlayerEmpire && !Owner.isFaction && !Empire.Universe.PlayerEmpire.GetRelations(Owner).AtWar)
                {
                    if (Empire.Universe.PlayerEmpire.GetRelations(Owner).Treaty_NAPact)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Owner, Empire.Universe.PlayerEmpire, "Invaded NA Pact", ParentSystem));
                        Empire.Universe.PlayerEmpire.GetGSAI().DeclareWarOn(Owner, WarType.ImperialistWar);
                        Owner.GetRelations(Empire.Universe.PlayerEmpire).Trust -= 50f;
                        Owner.GetRelations(Empire.Universe.PlayerEmpire).Anger_DiplomaticConflict += 50f;
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Owner, Empire.Universe.PlayerEmpire, "Invaded Start War", ParentSystem));
                        Empire.Universe.PlayerEmpire.GetGSAI().DeclareWarOn(Owner, WarType.ImperialistWar);
                        Owner.GetRelations(Empire.Universe.PlayerEmpire).Trust -= 25f;
                        Owner.GetRelations(Empire.Universe.PlayerEmpire).Anger_DiplomaticConflict += 25f;
                    }
                }
            }
            numInvadersLast = num2;
            if (num2 <= 0 || num1 != 0 )//|| this.Owner == null)
                return;
            if (index.TryGetRelations(Owner, out Relationship rel))
            {
                if (rel.AtWar && rel.ActiveWar != null)
                    ++rel.ActiveWar.ColoniestWon;
            }
            else if (Owner.TryGetRelations(index, out Relationship relship) && relship.AtWar && relship.ActiveWar != null)
                ++relship.ActiveWar.ColoniesLost;
            ConstructionQueue.Clear();
            foreach (PlanetGridSquare planetGridSquare in TilesList)
                planetGridSquare.QItem = null;
            Owner.RemovePlanet(this);
            if (index == Empire.Universe.PlayerEmpire && Owner == EmpireManager.Cordrazine)
                GlobalStats.IncrementCordrazineCapture();

            if (IsExploredBy(Empire.Universe.PlayerEmpire))
            {
                if (!flag)
                    Empire.Universe.NotificationManager.AddConqueredNotification(this, index, Owner);
                else
                {
                    lock (GlobalStats.OwnedPlanetsLock)
                    {
                        Empire.Universe.NotificationManager.AddPlanetDiedNotification(this, Empire.Universe.PlayerEmpire);
                        bool local_7 = true;
                    
                        if (Owner != null)
                        {
                            foreach (Planet item_3 in ParentSystem.PlanetList)
                            {
                                if (item_3.Owner == Owner && item_3 != this)
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

            if (index.data.Traits.Assimilators)
            {
                TraitLess(ref index.data.Traits.DiplomacyMod, ref Owner.data.Traits.DiplomacyMod);
                TraitLess(ref index.data.Traits.DodgeMod, ref Owner.data.Traits.DodgeMod);
                TraitLess(ref index.data.Traits.EnergyDamageMod, ref Owner.data.Traits.EnergyDamageMod);
                TraitMore(ref index.data.Traits.ConsumptionModifier, ref Owner.data.Traits.ConsumptionModifier);
                TraitLess(ref index.data.Traits.GroundCombatModifier, ref Owner.data.Traits.GroundCombatModifier);
                TraitLess(ref index.data.Traits.Mercantile, ref Owner.data.Traits.Mercantile);
                TraitLess(ref index.data.Traits.PassengerModifier, ref Owner.data.Traits.PassengerModifier);
                TraitLess(ref index.data.Traits.ProductionMod, ref Owner.data.Traits.ProductionMod);
                TraitLess(ref index.data.Traits.RepairMod, ref Owner.data.Traits.RepairMod);
                TraitLess(ref index.data.Traits.ResearchMod, ref Owner.data.Traits.ResearchMod);
                TraitLess(ref index.data.Traits.ShipCostMod, ref Owner.data.Traits.ShipCostMod);
                TraitLess(ref index.data.Traits.PopGrowthMin, ref Owner.data.Traits.PopGrowthMin);
                TraitMore(ref index.data.Traits.PopGrowthMax, ref Owner.data.Traits.PopGrowthMax);
                TraitLess(ref index.data.Traits.ModHpModifier, ref Owner.data.Traits.ModHpModifier);
                TraitLess(ref index.data.Traits.TaxMod, ref Owner.data.Traits.TaxMod);
                TraitMore(ref index.data.Traits.MaintMod, ref Owner.data.Traits.MaintMod);
                TraitLess(ref index.data.SpyModifier, ref Owner.data.SpyModifier);
                TraitLess(ref index.data.Traits.Spiritual, ref Owner.data.Traits.Spiritual);

            }
            if (index.isFaction)
                return;

            foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
            {
                if (keyValuePair.Value.loyalty != index && keyValuePair.Value.TroopList.Where(loyalty => loyalty.GetOwner() != index).Count() > 0)
                    continue;
                keyValuePair.Value.loyalty = index;
                Owner.RemoveShip(keyValuePair.Value);      //Transfer to new owner's ship list. Fixes platforms changing loyalty after game load bug      -Gretman
                index.AddShip(keyValuePair.Value);
                Log.Info("Owner of platform tethered to {0} changed from {1} to {2}", Name, Owner.PortraitName, index.PortraitName);
            }
            Owner = index;
            TurnsSinceTurnover = 0;
            Owner.AddPlanet(this);
            ConstructionQueue.Clear();
            ParentSystem.OwnerList.Clear();
            
            foreach (Planet planet in ParentSystem.PlanetList)
            {
                if (planet.Owner != null && !ParentSystem.OwnerList.Contains(planet.Owner))
                    ParentSystem.OwnerList.Add(planet.Owner);
            }
            colonyType = Owner.AssessColonyNeeds(this);
            GovernorOn = true;
        }

        private void TraitLess(ref float invaderValue, ref float ownerValue) => invaderValue = Math.Max(invaderValue, ownerValue);
        private void TraitMore(ref float invaderValue, ref float ownerValue) => invaderValue = Math.Min(invaderValue, ownerValue);


        public void DoTroopTimers(float elapsedTime)
        {
            //foreach (Building building in this.BuildingList)
            for (int x = 0; x < BuildingList.Count;x++ )
            {
                Building building = BuildingList[x];
                if (building == null)
                    continue;
                building.AttackTimer -= elapsedTime;
                if (building.AttackTimer < 0.0)
                {
                    building.AvailableAttackActions = 1;
                    building.AttackTimer = 10f;
                }
            }
            Array<Troop> list = new Array<Troop>();
            //foreach (Troop troop in this.TroopsHere)
            for (int x = 0; x < TroopsHere.Count;x++ )
            {
                Troop troop = TroopsHere[x];
                if (troop == null)
                    continue;
                if (troop.Strength <= 0)
                {
                    list.Add(troop);
                    foreach (PlanetGridSquare planetGridSquare in TilesList)
                        planetGridSquare.TroopsHere.Remove(troop);
                }
                troop.Launchtimer -= elapsedTime;
                troop.MoveTimer -= elapsedTime;
                troop.MovingTimer -= elapsedTime;
                if (troop.MoveTimer < 0.0)
                {
                    ++troop.AvailableMoveActions;
                    if (troop.AvailableMoveActions > troop.MaxStoredActions)
                        troop.AvailableMoveActions = troop.MaxStoredActions;
                    troop.MoveTimer = troop.MoveTimerBase;
                }
                troop.AttackTimer -= elapsedTime;
                if (troop.AttackTimer < 0.0)
                {
                    ++troop.AvailableAttackActions;
                    if (troop.AvailableAttackActions > troop.MaxStoredActions)
                        troop.AvailableAttackActions = troop.MaxStoredActions;
                    troop.AttackTimer = troop.AttackTimerBase;
                }
            }
            foreach (Troop troop in list)
                TroopsHere.Remove(troop);
        }

        private void MakeCombatDecisions()
        {
            bool enemyTroopsFound = false;
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                if (planetGridSquare.TroopsHere.Count > 0 && planetGridSquare.TroopsHere[0].GetOwner() != Owner || planetGridSquare.building != null && !string.IsNullOrEmpty(planetGridSquare.building.EventTriggerUID))
                {
                    enemyTroopsFound = true;
                    break;
                }
            }
            if (!enemyTroopsFound)
                return;
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            for (int index = 0; index < TilesList.Count; ++index)
            {
                PlanetGridSquare pgs = TilesList[index];
                bool hasAttacked = false;
                if (pgs.TroopsHere.Count > 0)
                {
                    if (pgs.TroopsHere[0].AvailableAttackActions > 0)
                    {
                        if (pgs.TroopsHere[0].GetOwner() != Empire.Universe.PlayerEmpire || !Empire.Universe.LookingAtPlanet || (!(Empire.Universe.workersPanel is CombatScreen) || (Empire.Universe.workersPanel as CombatScreen).p != this) || GlobalStats.AutoCombat)
                        {
                            {
                                foreach (PlanetGridSquare planetGridSquare in TilesList)
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
                            foreach (PlanetGridSquare planetGridSquare in ((IEnumerable<PlanetGridSquare>)TilesList).OrderBy<PlanetGridSquare, int>((Func<PlanetGridSquare, int>)(tile => Math.Abs(tile.x - pgs.x) + Math.Abs(tile.y - pgs.y))))
                            {
                                if (!pgs.TroopsHere.Any())
                                    break;
                                if (planetGridSquare != pgs )
                                {                                    
                                    if (planetGridSquare.TroopsHere.Any())
                                    {
                                        if (planetGridSquare.TroopsHere[0].GetOwner() != pgs.TroopsHere[0].GetOwner())
                                        {
                                            if (planetGridSquare.x > pgs.x)
                                            {
                                                if (planetGridSquare.y > pgs.y)
                                                {
                                                    if (TryTroopMove(1, 1, pgs))
                                                        break;
                                                }
                                                if (planetGridSquare.y < pgs.y)
                                                {
                                                    if (TryTroopMove(1, -1, pgs))
                                                        break;
                                                }
                                                if (!TryTroopMove(1, 0, pgs))
                                                {
                                                    if (!TryTroopMove(1, -1, pgs))
                                                    {
                                                        if (TryTroopMove(1, 1, pgs))
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
                                                    if (TryTroopMove(-1, 1, pgs))
                                                        break;
                                                }
                                                if (planetGridSquare.y < pgs.y)
                                                {
                                                    if (TryTroopMove(-1, -1, pgs))
                                                        break;
                                                }
                                                if (!TryTroopMove(-1, 0, pgs))
                                                {
                                                    if (!TryTroopMove(-1, -1, pgs))
                                                    {
                                                        if (TryTroopMove(-1, 1, pgs))
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
                                                    if (TryTroopMove(0, 1, pgs))
                                                        break;
                                                }
                                                if (planetGridSquare.y < pgs.y)
                                                {
                                                    if (TryTroopMove(0, -1, pgs))
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    else if (planetGridSquare.building != null && (planetGridSquare.building.CombatStrength > 0 || !string.IsNullOrEmpty(planetGridSquare.building.EventTriggerUID)) && (Owner != pgs.TroopsHere[0].GetOwner() || !string.IsNullOrEmpty(planetGridSquare.building.EventTriggerUID)))
                                    {
                                        if (planetGridSquare.x > pgs.x)
                                        {
                                            if (planetGridSquare.y > pgs.y)
                                            {
                                                if (TryTroopMove(1, 1, pgs))
                                                    break;
                                            }
                                            if (planetGridSquare.y < pgs.y)
                                            {
                                                if (TryTroopMove(1, -1, pgs))
                                                    break;
                                            }
                                            if (!TryTroopMove(1, 0, pgs))
                                            {
                                                if (!TryTroopMove(1, -1, pgs))
                                                {
                                                    if (TryTroopMove(1, 1, pgs))
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
                                                if (TryTroopMove(-1, 1, pgs))
                                                    break;
                                            }
                                            if (planetGridSquare.y < pgs.y)
                                            {
                                                if (TryTroopMove(-1, -1, pgs))
                                                    break;
                                            }
                                            if (!TryTroopMove(-1, 0, pgs))
                                            {
                                                if (!TryTroopMove(-1, -1, pgs))
                                                {
                                                    if (TryTroopMove(-1, 1, pgs))
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
                                                if (!TryTroopMove(0, 1, pgs))
                                                {
                                                    if (!TryTroopMove(1, 1, pgs))
                                                    {
                                                        if (TryTroopMove(-1, 1, pgs))
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
                                                if (!TryTroopMove(0, -1, pgs))
                                                {
                                                    if (!TryTroopMove(1, -1, pgs))
                                                    {
                                                        if (TryTroopMove(-1, -1, pgs))
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
                    
                else if (pgs.building != null && pgs.building.CombatStrength > 0 && (Owner != Empire.Universe.PlayerEmpire || !Empire.Universe.LookingAtPlanet || (!(Empire.Universe.workersPanel is CombatScreen) || (Empire.Universe.workersPanel as CombatScreen).p != this) || GlobalStats.AutoCombat) && pgs.building.AvailableAttackActions > 0)
                {
                    for (int i = 0; i < TilesList.Count; i++)
                    {
                        PlanetGridSquare planetGridSquare = TilesList[i];
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
            foreach (PlanetGridSquare eventLocation in TilesList)
            {
                if (eventLocation.x != start.x + changex || eventLocation.y != start.y + changey)
                    continue;

                Troop troop = null;
                using (eventLocation.TroopsHere.AcquireWriteLock())
                {
                    if (start.TroopsHere.Count > 0)
                    {
                        troop = start.TroopsHere[0];
                    }

                    if (eventLocation.building != null && eventLocation.building.CombatStrength > 0 || eventLocation.TroopsHere.Count > 0)
                        return false;
                    if (troop != null)
                    {
                        if (changex > 0)
                            troop.facingRight = true;
                        else if (changex < 0)
                            troop.facingRight = false;
                        troop.SetFromRect(start.TroopClickRect);
                        troop.MovingTimer = 0.75f;
                        --troop.AvailableMoveActions;
                        troop.MoveTimer = troop.MoveTimerBase;
                        eventLocation.TroopsHere.Add(troop);
                        start.TroopsHere.Clear();
                    }
                    if (string.IsNullOrEmpty(eventLocation.building?.EventTriggerUID) || (eventLocation.TroopsHere.Count <= 0 || eventLocation.TroopsHere[0].GetOwner().isFaction))
                        return true;
                }

                ResourceManager.EventsDict[eventLocation.building.EventTriggerUID].TriggerPlanetEvent(this, eventLocation.TroopsHere[0].GetOwner(), eventLocation, Empire.Universe);
            }
            return false;
        }

        public void Update(float elapsedTime)
        {
            DecisionTimer -= elapsedTime;
            CombatTimer -= elapsedTime;
            RecentCombat = CombatTimer > 0.0f;
            Array<Guid> list = new Array<Guid>();
            foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
            {
                if (!keyValuePair.Value.Active 
                    || keyValuePair.Value.Size == 0
                    //|| keyValuePair.Value.loyalty != this.Owner
                    )
                    list.Add(keyValuePair.Key);
            }
            foreach (Guid key in list)
                Shipyards.Remove(key);
            if (!Empire.Universe.Paused)
            {
                
                if (TroopsHere.Count > 0)
                {
                    //try
                    {
                        DoCombats(elapsedTime);
                        if (DecisionTimer <= 0)
                        {
                            MakeCombatDecisions();
                            DecisionTimer = 0.5f;
                        }
                    }
                    //catch
                    {
                    }
                }
                if (TroopsHere.Count != 0 || BuildingList.Count != 0)
                    DoTroopTimers(elapsedTime);
            }
            for (int index1 = 0; index1 < BuildingList.Count; ++index1)
            {
                //try
                {
                    Building building = BuildingList[index1];
                    if (building.isWeapon)
                    {
                        building.WeaponTimer -= elapsedTime;
                        if (building.WeaponTimer < 0 && ParentSystem.ShipList.Count > 0)
                        {
                            if (Owner != null)
                            {
                                Ship target = null;
                                Ship troop = null;
                                float currentD = 0;
                                float previousD = building.theWeapon.Range + 1000f;
                                //float currentT = 0;
                                float previousT = building.theWeapon.Range + 1000f;
                                //this.system.ShipList.thisLock.EnterReadLock();
                                for (int index2 = 0; index2 < ParentSystem.ShipList.Count; ++index2)
                                {
                                    Ship ship = ParentSystem.ShipList[index2];
                                    if (ship.loyalty == Owner || (!ship.loyalty.isFaction && Owner.GetRelations(ship.loyalty).Treaty_NAPact) )
                                        continue;
                                    currentD = Vector2.Distance(Center, ship.Center);                                   
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

                                if (troop != null)
                                    target = troop;
                                if(target != null)
                                {
                                    building.theWeapon.Center = Center;
                                    building.theWeapon.FireFromPlanet(this, target);
                                    building.WeaponTimer = building.theWeapon.fireDelay;
                                    break;
                                }


                            }
                        }
                    }
                }
            }
            for (int index = 0; index < Projectiles.Count; ++index)
            {
                Projectile projectile = Projectiles[index];
                if (projectile.Active)
                {
                    if (elapsedTime > 0)
                        projectile.Update(elapsedTime);
                }
                else
                    Projectiles.QueuePendingRemoval(projectile);
            }
            Projectiles.ApplyPendingRemovals();
            UpdatePosition(elapsedTime);
        }

   
        //added by gremlin affectnearbyships
        private void AffectNearbyShips()
        {
            float repairPool = developmentLevel * RepairPerTurn * 20;
            if(HasShipyard)
            {
                foreach(Ship ship in Shipyards.Values)
                    repairPool += ship.RepairRate;                    
            }
            for (int i = 0; i < ParentSystem.ShipList.Count; i++)
            {
                Ship ship = ParentSystem.ShipList[i];
                if(ship != null && ship.loyalty.isFaction)
                {
                    ship.Ordinance = ship.OrdinanceMax;
                    if (ship.HasTroopBay )
                    {
                        if (Population > 0)
                        {
                            if (ship.TroopCapacity > ship.TroopList.Count)
                            {
                                ship.TroopList.Add(ResourceManager.CreateTroop("Wyvern", ship.loyalty));
                            }
                            if (Owner != null && Population > 0)
                            {
                                Population *= .5f;
                                Population -= 1000;
                                ProductionHere *= .5f;
                                FoodHere *= .5f;
                            }
                            if (Population < 0)
                                Population = 0;
                        }
                        else if (ParentSystem.combatTimer < -30 && ship.TroopCapacity > ship.TroopList.Count)
                        {
                            ship.TroopList.Add(ResourceManager.CreateTroop("Wyvern", ship.loyalty));
                            ParentSystem.combatTimer = 0;
                        }
                    }
                }
                if (ship != null && ship.loyalty == Owner && HasShipyard && ship.Position.InRadius(Center, 5000f))
                {
                    ship.PowerCurrent = ship.PowerStoreMax;
                    ship.Ordinance = ship.OrdinanceMax;
                    if (GlobalStats.HardcoreRuleset)
                    {
                        //while (ship.CargoSpaceFree > 0f)
                        //{
                        //    var resource = ResourcesDict.FirstOrDefault(kv => kv.Value > 0f);
                        //    if (resource.Value <= 0f) break;
                        //}
                        //foreach (KeyValuePair<string, float> maxGood in ship.GetMaxGoods())
                        //{
                        //    if (ship.GetCargo(maxGood.Key) >= maxGood.Value)
                        //        continue;
                        //    while (ResourcesDict[maxGood.Key] > 0f && ship.GetCargo(maxGood.Key) < maxGood.Value)
                        //    {
                        //        float cargoSpace = maxGood.Value - ship.GetCargo(maxGood.Key);
                        //        if (cargoSpace < 1f)
                        //        {
                        //            ResourcesDict[maxGood.Key] -= cargoSpace;
                        //            ship.AddCargo(maxGood.Key, cargoSpace);
                        //        }
                        //        else
                        //        {
                        //            ResourcesDict[maxGood.Key] -= 1f;
                        //            ship.AddCargo(maxGood.Key, 1f);
                        //        }
                        //    }
                        //}
                    }
                    //Modified by McShooterz: Repair based on repair pool, if no combat in system                 
                    if (!ship.InCombat && repairPool > 0 && (ship.Health < ship.HealthMax || ship.shield_percent <90))
                    {
                        //bool repairing = false;
                        ship.RepairShipModules(ref repairPool);
                    }
                    else if(ship.AI.State == AIState.Resupply)
                    {
                
                        ship.AI.OrderQueue.Clear();
                    
                        ship.AI.Target = null;
                        ship.AI.PotentialTargets.Clear();
                        ship.AI.HasPriorityOrder = false;
                        ship.AI.State = AIState.AwaitingOrders;

                    }
                    //auto load troop:
                    using (TroopsHere.AcquireWriteLock())
                    {
                        if ((ParentSystem.combatTimer > 0 && ship.InCombat) || !TroopsHere.Any() ||
                            TroopsHere.Any(troop => troop.GetOwner() != Owner))
                            continue;
                        foreach (var pgs in TilesList)
                        {
                            if (ship.TroopCapacity ==0 || ship.TroopList.Count >= ship.TroopCapacity) 
                                break;

                            using (pgs.TroopsHere.AcquireWriteLock())
                                if (pgs.TroopsHere.Count > 0 && pgs.TroopsHere[0].GetOwner() == Owner)
                                {                                
                                    Troop troop = pgs.TroopsHere[0];
                                    ship.TroopList.Add(troop);
                                    pgs.TroopsHere.Clear();
                                    TroopsHere.Remove(troop);
                                }
                        }
                    }
                }
            }
        }

        public void TerraformExternal(float amount)
        {
            Fertility += amount;
            if (Fertility <= 0.0)
            {
                Fertility = 0.0f;
                planetType = 7;
                Terraform();
            }
            else if (Type == "Barren" && Fertility > 0.01)
            {
                planetType = 14;
                Terraform();
            }
            else if (Type == "Desert" && Fertility > 0.35)
            {
                planetType = 18;
                Terraform();
            }
            else if (Type == "Ice" && Fertility > 0.35)
            {
                planetType = 19;
                Terraform();
            }
            else if (Type == "Swamp" && Fertility > 0.75)
            {
                planetType = 21;
                Terraform();
            }
            else if (Type == "Steppe" && Fertility > 0.6)
            {
                planetType = 11;
                Terraform();
            }
            else
            {
                if (!(Type == "Tundra") || Fertility <= 0.95)
                    return;
                planetType = 22;
                Terraform();
            }
        }

        public void UpdateOwnedPlanet()
        {
            ++TurnsSinceTurnover;
            --Crippled_Turns;
            if (Crippled_Turns < 0)
                Crippled_Turns = 0;
            ConstructionQueue.ApplyPendingRemovals();
            UpdateDevelopmentStatus();
            Description = DevelopmentStatus;
            AffectNearbyShips();
            TerraformPoints += TerraformToAdd;
            if (TerraformPoints > 0.0f && Fertility < 1.0)
            {
                Fertility += TerraformToAdd;
                if (Type == "Barren" && Fertility > 0.01)
                {
                    planetType = 14;
                    Terraform();
                }
                else if (Type == "Desert" && Fertility > 0.35)
                {
                    planetType = 18;
                    Terraform();
                }
                else if (Type == "Ice" && Fertility > 0.35)
                {
                    planetType = 19;
                    Terraform();
                }
                else if (Type == "Swamp" && Fertility > 0.75)
                {
                    planetType = 21;
                    Terraform();
                }
                else if (Type == "Steppe" && Fertility > 0.6)
                {
                    planetType = 11;
                    Terraform();
                }
                else if (Type == "Tundra" && Fertility > 0.95)
                {
                    planetType = 22;
                    Terraform();
                }
                if (Fertility > 1.0)
                    Fertility = 1f;
            }
            if (GovernorOn)
                DoGoverning();
            UpdateIncomes(false);

            // notification about empty queue
            if (GlobalStats.ExtraNotifications && Owner != null && Owner.isPlayer)
            {
                if (ConstructionQueue.Count == 0 && !queueEmptySent)
                {
                    if (colonyType == ColonyType.Colony || colonyType == ColonyType.Core || colonyType == ColonyType.Industrial || !GovernorOn)
                    {
                        queueEmptySent = true;
                        Empire.Universe.NotificationManager.AddEmptyQueueNotification(this);
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
            if (ShieldStrengthCurrent < ShieldStrengthMax)
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
            CalculateIncomingTrade();
            if (FoodHere > MAX_STORAGE)
                FoodHere = MAX_STORAGE;
            if (ProductionHere > MAX_STORAGE)
                ProductionHere = MAX_STORAGE;
        }

        public float IncomingFood = 0;
        public float IncomingProduction = 0;
        public float IncomingColonists = 0;



        private bool AddToIncomingTrade(ref float type, float amount)
        {
            if (amount < 1) return false;
            type += amount;
            return true;
        }
        private void CalculateIncomingTrade()
        {
            if (Owner == null || Owner.isFaction) return;
            IncomingProduction = 0;
            IncomingFood = 0;
            TradeIncomingColonists = 0;
            using (Owner.GetShips().AcquireReadLock())
            {
                foreach (var ship in Owner.GetShips())
                {
                    if (ship.DesignRole != ShipData.RoleName.freighter) continue;
                    if (ship.AI.end != this) continue;
                    if (ship.AI.State != AIState.SystemTrader && ship.AI.State != AIState.PassengerTransport) continue;

                    if (AddToIncomingTrade(ref IncomingFood, ship.GetFood())) return;
                    if (AddToIncomingTrade(ref IncomingProduction, ship.GetProduction())) return;
                    if (AddToIncomingTrade(ref IncomingColonists, ship.GetColonists())) return;

                    if (AddToIncomingTrade(ref IncomingFood, ship.CargoSpaceMax * (ship.TransportingFood ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingProduction, ship.CargoSpaceMax * (ship.TransportingProduction ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingColonists, ship.CargoSpaceMax)) return;
                }
            }
        }

        private float CalculateCyberneticPercentForSurplus(float desiredSurplus)
        {
 

            float NoDivByZero = .0000001f;
            float Surplus = (float)((consumption + desiredSurplus - PlusFlatProductionPerTurn) 
                / ((Population / 1000.0) * (MineralRichness + PlusProductionPerColonist)) * (1 - Owner.data.TaxRate)+NoDivByZero);
            if (Surplus < 1.0f)
            {
                if (Surplus < 0)
                    return 0.0f;
                return Surplus;
            }
            else
            {             
                return 1.0f;
            }
        }

        private float CalculateFarmerPercentForSurplus(float desiredSurplus)
        {
            float Surplus = 0.0f;
            float NoDivByZero = .0000001f;
            if(Owner.data.Traits.Cybernetic >0)
            {

                Surplus = Surplus = (float)((consumption + desiredSurplus - PlusFlatProductionPerTurn) / ((Population / 1000.0) 
                    * (MineralRichness + PlusProductionPerColonist)) * (1 - Owner.data.TaxRate) + NoDivByZero);

                if (Surplus < .75f)
                {
                    if (Surplus < 0)
                        return 0.0f;
                    return Surplus;
                }
                else
                {
                    return .75f;
                }
            }
            
            if (Fertility == 0.0)
                return 0.0f;
            // replacing while loop with singal fromula, should save some clock cycles

           
            Surplus = (float)((consumption + desiredSurplus - FlatFoodAdded) / ((Population / 1000.0) 
                * (Fertility + PlusFoodPerColonist) * (1 + FoodPercentAdded) +NoDivByZero));
            if (Surplus < .75f)
            {
                if (Surplus < 0)
                    return 0.0f;
                return Surplus;
            }
            else
            {
                //if you cant reach the desired surplus, produce as much as you can
                return .75f;
            }
        }

        private bool DetermineIfSelfSufficient()
        {
             float NoDivByZero = .0000001f;
            return (float)((consumption - FlatFoodAdded) / ((Population / 1000.0) * (Fertility + PlusFoodPerColonist) * (1 + FoodPercentAdded) +NoDivByZero)) < 1;
        }

        public float GetDefendingTroopStrength()
        {
            float num = 0;
            for (int index = 0; index < TroopsHere.Count; index++)
            {
                Troop troop = TroopsHere[index];
                if (troop.GetOwner() == Owner)
                    num += troop.Strength;
            }
            return num;
        }
        public int CountEmpireTroops(Empire us)
        {
            int num = 0;
            for (int index = 0; index < TroopsHere.Count; index++)
            {
                Troop troop = TroopsHere[index];
                if (troop.GetOwner() == us)
                    num++;
            }
            return num;
        }
        public int GetDefendingTroopCount()
        {            
            return CountEmpireTroops(Owner);
        }
        public bool AnyOfOurTroops(Empire us)
        {
            for (int index = 0; index < TroopsHere.Count; index++)
            {
                Troop troop = TroopsHere[index];
                if (troop.GetOwner() == us)
                    return true;
            }
            return false;
        }


        public Array<Building> GetBuildingsWeCanBuildHere()
        {
            if (Owner == null)
                return new Array<Building>();
            BuildingsCanBuild.Clear();
            bool flag1 = true;
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Capital City" || building.Name == "Outpost")
                {
                    flag1 = false;
                    break;
                }
            }
            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (keyValuePair.Value)
                {
                    Building building1 = ResourceManager.BuildingsDict[keyValuePair.Key];
                    bool flag2 = true;
                    if(Owner.data.Traits.Cybernetic >0)
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
                        foreach (Planet planet in Owner.GetPlanets())
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
                                foreach (QueueItem queueItem in planet.ConstructionQueue)
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
                        foreach (Building building2 in BuildingList)
                        {
                            if (building2.Name == building1.Name && building1.Name != "Biospheres" && building2.Unique)
                            {
                                flag2 = false;
                                break;
                            }
                        }
                        for (int index = 0; index < ConstructionQueue.Count; ++index)
                        {
                            QueueItem queueItem = ConstructionQueue[index];
                            if (queueItem.isBuilding && queueItem.Building.Name == building1.Name && (building1.Name != "Biospheres" && queueItem.Building.Unique))
                            {
                                flag2 = false;
                                break;
                            }
                        }
                        if(building1.Name == "Biosphers")
                        {
                            foreach(PlanetGridSquare tile in TilesList)
                            {
                                if (!tile.Habitable)
                                    break;
                                flag2 = false;

                            }
                        }
                    }
                    if (flag2)
                        BuildingsCanBuild.Add(building1);
                }
            }
            return BuildingsCanBuild;
        }
        public void AddBuildingToCQ(Building b)
        {
            AddBuildingToCQ(b, false);
        }
        public void AddBuildingToCQ(Building b, bool PlayerAdded)
        {
            int count = ConstructionQueue.Count;
            QueueItem qi = new QueueItem();
            qi.IsPlayerAdded = PlayerAdded;
            qi.isBuilding = true;
            qi.Building = b;
            qi.Cost = b.Cost;
            qi.productionTowards = 0.0f;
            qi.NotifyOnEmpty = false;
            ResourceManager.BuildingsDict.TryGetValue("Terraformer",out Building terraformer);
            
            if (terraformer == null)
            {
                foreach(KeyValuePair<string,bool> bdict in Owner.GetBDict())
                {
                    if (!bdict.Value)
                        continue;
                    Building check = ResourceManager.GetBuildingTemplate(bdict.Key);
                    
                    if (check.PlusTerraformPoints <=0)
                        continue;
                    terraformer = check;
                }
            }
            if (AssignBuildingToTile(b, qi))
                ConstructionQueue.Add(qi);

            else if (Owner.data.Traits.Cybernetic <=0 && Owner.GetBDict()[terraformer.Name] && Fertility < 1.0 
                && WeCanAffordThis(terraformer, colonyType))
            {
                bool flag = true;
                foreach (QueueItem queueItem in ConstructionQueue)
                {
                    if (queueItem.isBuilding && queueItem.Building.Name == terraformer.Name)
                        flag = false;
                }
                foreach (Building building in BuildingList)
                {
                    if (building.Name == terraformer.Name)
                        flag = false;
                }
                if (!flag)
                    return;
                AddBuildingToCQ(ResourceManager.CreateBuilding(terraformer.Name),false);
            }
            else
            {
                if (!Owner.GetBDict()["Biospheres"])
                    return;
                TryBiosphereBuild(ResourceManager.CreateBuilding("Biospheres"), qi);
            }
        }

        public bool BuildingInQueue(string UID)
        {
            for (int index = 0; index < ConstructionQueue.Count; ++index)
            {
                if (ConstructionQueue[index].isBuilding && ConstructionQueue[index].Building.Name == UID)
                    return true;
            }
            //foreach (var pgs in TilesList)
            //{
            //    if (pgs.QItem?.isBuilding  == true && pgs.QItem.Building.Name == UID)
            //        return true;
            //}
            return false;
        }

        public bool BuildingExists(Building exactInstance) => BuildingList.Contains(exactInstance);

        public bool BuildingExists(string buildingName)
        {
            for (int i = 0; i < BuildingList.Count; ++i)
                if (BuildingList[i].Name == buildingName)
                    return true;
            return BuildingInQueue(buildingName);
            
        }

        public bool WeCanAffordThis(Building building, Planet.ColonyType governor)
        {
            if (governor == ColonyType.TradeHub)
                return true;
            if (building == null)
                return false;
            if (building.IsPlayerAdded)
                return true;
            Empire empire = Owner;
            float buildingMaintenance = empire.GetTotalBuildingMaintenance();
            float grossTaxes = empire.GrossTaxes;
            //bool playeradded = ;
          
            bool itsHere = BuildingList.Contains(building);
            
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding)
                {
                    buildingMaintenance += Owner.data.Traits.MaintMod * queueItem.Building.Maintenance;
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
            buildingMaintenance += building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod;
            
            bool LowPri = buildingMaintenance / grossTaxes < .25f;
            bool MedPri = buildingMaintenance / grossTaxes < .60f;
            bool HighPri = buildingMaintenance / grossTaxes < .80f;
            float income = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);           
            float maintCost = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - building.Maintenance- (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);
            bool makingMoney = maintCost > 0;
      
            int defensiveBuildings = BuildingList.Where(combat => combat.SoftAttack > 0 || combat.PlanetaryShieldStrengthAdded >0 ||combat.theWeapon !=null ).Count();           
           int possibleoffensiveBuilding = BuildingsCanBuild.Where(b => b.PlanetaryShieldStrengthAdded > 0 || b.SoftAttack > 0 || b.theWeapon != null).Count();
           bool isdefensive = building.SoftAttack > 0 || building.PlanetaryShieldStrengthAdded > 0 || building.isWeapon ;
           float defenseratio =0;
            if(defensiveBuildings+possibleoffensiveBuilding >0)
                defenseratio = (defensiveBuildings + 1) / (float)(defensiveBuildings + possibleoffensiveBuilding + 1);
            SystemCommander SC;
            bool needDefense =false;
            
            if (Owner.data.TaxRate > .5f)
                makingMoney = false;
            //dont scrap buildings if we can use treasury to pay for it. 
            if (building.AllowInfantry && !BuildingList.Contains(building) && (AllowInfantry || governor == ColonyType.Military))
                return false;

            //determine defensive needs.
            if (Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out SC))
            {
                if (makingMoney)
                    needDefense = SC.RankImportance >= defenseratio *10; ;// / (defensiveBuildings + offensiveBuildings+1)) >defensiveNeeds;
            }
            
            if (!string.IsNullOrEmpty(building.ExcludesPlanetType) && building.ExcludesPlanetType == Type)
                return false;
            

            if (itsHere && building.Unique && (makingMoney || building.Maintenance < Owner.Money * .001))
                return true;

            if (building.PlusTaxPercentage * GrossMoneyPT >= building.Maintenance 
                || building.CreditsProduced(this) >= building.Maintenance 

                
                ) 
                return true;
            if (building.Name == "Outpost" || building.WinsGame  )
                return true;
            //dont build +food if you dont need to

            if (Owner.data.Traits.Cybernetic <= 0 && building.PlusFlatFoodAmount > 0)// && this.Fertility == 0)
            {

                if (NetFoodPerTurn > 0 && FarmerPercentage < .3 || BuildingExists(building.Name))

                    return false;
                else
                    return true;
               
            }
            if (Owner.data.Traits.Cybernetic < 1 && income > building.Maintenance ) 
            {
                float food = building.FoodProduced(this);
                if (food * FarmerPercentage > 1)
                {
                    return true;
                }
                else
                {
                    
                }
            }
            if(Owner.data.Traits.Cybernetic >0)
            {
                if(NetProductionPerTurn - consumption <0)
                {
                    if(building.PlusFlatProductionAmount >0 && (WorkerPercentage > .5 || income >building.Maintenance*2))
                    {
                        return true;
                    }
                    if (building.PlusProdPerColonist > 0 && building.PlusProdPerColonist * (Population / 1000) > building.Maintenance *(2- WorkerPercentage))
                    {
                        if (income > ShipBuildingModifier * 2)
                            return true;

                    }
                    if (building.PlusProdPerRichness * MineralRichness > building.Maintenance )
                        return true;
                }
            }
            if(building.PlusTerraformPoints >0)
            {
                if (!makingMoney || Owner.data.Traits.Cybernetic>0|| BuildingList.Contains(building) || BuildingInQueue(building.Name))
                    return false;
                
            }
            if(!makingMoney || developmentLevel < 3)
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
                        if (building.AllowShipBuilding && GetMaxProductionPotential()>20 )
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <=0)
                            return false;
                        if (HighPri)
                        {
                            if (building.PlusFlatFoodAmount > 0
                                || (building.PlusFoodPerColonist > 0 && Population > 500f)
                                
                                //|| this.developmentLevel > 4
                                || ((building.MaxPopIncrease > 0
                                || building.PlusFlatPopulation > 0 || building.PlusTerraformPoints > 0) && Population > MaxPopulation * .5f)
                                || building.PlusFlatFoodAmount > 0
                                || building.PlusFlatProductionAmount > 0
                                || building.StorageAdded > 0 
                               // || (this.Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount>0))
                                || (needDefense && isdefensive && developmentLevel > 3)
                                )
                                return true;
                                //iftrue = true;
                            
                        }
                        if (!iftrue && MedPri && developmentLevel > 2 && makingMoney)
                        {
                            if (
                                building.Name == "Biospheres"||
                                ( building.PlusTerraformPoints > 0 && Fertility < 3)
                                || building.MaxPopIncrease > 0 
                                || building.PlusFlatPopulation > 0
                                || developmentLevel > 3
                                  || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                || (needDefense && isdefensive )

                                )
                                return true;
                        }
                        if (LowPri && developmentLevel > 4 && makingMoney)
                        {
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Core:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {

                            if (building.StorageAdded > 0
                                || (Owner.data.Traits.Cybernetic <=0 && (building.PlusTerraformPoints > 0 && Fertility < 1) && MaxPopulation > 2000)
                                || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)                             
                                || (Owner.data.Traits.Cybernetic <=0 && building.PlusFlatFoodAmount > 0)
                                || (Owner.data.Traits.Cybernetic <=0 && building.PlusFoodPerColonist > 0)                                
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness >0
                                || building.PlusProdPerColonist >0
                                || building.PlusFlatResearchAmount>0
                                || (building.PlusResearchPerColonist>0 && Population / 1000 > 1)
                                //|| building.Name == "Biospheres"                                
                                
                                || (needDefense && isdefensive && developmentLevel > 3)                                
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                return true;
                        }
                        if (MedPri && developmentLevel > 3 &&makingMoney )
                        {
                            if (developmentLevel > 2 && needDefense && (building.theWeapon != null || building.Strength > 0))
                                return true;
                            iftrue = true;
                        }
                        if (!iftrue && LowPri && developmentLevel > 4 && makingMoney && income > building.Maintenance)
                        {
                            
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion

                case ColonyType.Industrial:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (HighPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0
                                || (Owner.data.Traits  .Cybernetic <=0 && Fertility < 1f && building.PlusFlatFoodAmount > 0)                             
                                || building.StorageAdded > 0
                                || (needDefense && isdefensive && developmentLevel > 3)
                                )
                                return true;
                        }
                        if (MedPri && developmentLevel > 2 && makingMoney)
                        {
                            if (building.PlusResearchPerColonist * Population / 1000 >building.Maintenance
                            || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)
                            || (Owner.data.Traits.Cybernetic <= 0 && building.PlusTerraformPoints > 0 && Fertility < 1 && Population == MaxPopulation && MaxPopulation > 2000 && income>building.Maintenance)
                               || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0)
                                ||building.PlusFlatResearchAmount >0
                                || (building.PlusResearchPerColonist >0 && MaxPopulation > 999)
                                )
                               
                            {
                                iftrue = true;
                            }

                        }
                        if (!iftrue && LowPri && developmentLevel > 3 && makingMoney && income >building.Maintenance)
                        {
                            if (needDefense && isdefensive && developmentLevel > 2)
                                return true;
                            
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Military:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {
                            if (building.isWeapon
                                || building.IsSensor
                                || building.Defense > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (MineralRichness < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlanetaryShieldStrengthAdded > 0
                                || (building.AllowShipBuilding  && GrossProductionPerTurn > 1)
                                || (building.ShipRepair > 0&& GrossProductionPerTurn > 1)
                                || building.Strength > 0
                                || (building.AllowInfantry && GrossProductionPerTurn > 1)
                                || needDefense &&(building.theWeapon !=null || building.Strength >0)
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
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
                        if (!iftrue && LowPri && developmentLevel > 4)
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
                        if (building.AllowShipBuilding && GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;

                        if (HighPri)
                        {
                            if (building.PlusFlatResearchAmount > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlusFlatProductionAmount >0
                                || building.PlusResearchPerColonist > 0
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusFlatProductionAmount > 0 || building.PlusProdPerColonist > 0 ))
                                || (needDefense && isdefensive && developmentLevel > 3)
                                )
                                return true;

                        }
                        if ( MedPri && developmentLevel > 3 && makingMoney)
                        {
                            if (((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population > MaxPopulation * .5f)
                            || Owner.data.Traits.Cybernetic <=0 &&( (building.PlusTerraformPoints > 0 && Fertility < 1 && Population > MaxPopulation * .5f && MaxPopulation > 2000)
                                || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0))
                                )
                                return true;
                        }
                        if ( LowPri && developmentLevel > 4 && makingMoney)
                        {
                            if (needDefense && isdefensive && developmentLevel > 2)
                                
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
            int pc = Owner.GetPlanets().Count;




            bool exportPSFlag = true;
            bool exportFSFlag = true;
            float exportPTrack = Owner.exportPTrack;
            float exportFTrack = Owner.exportFTrack;

            if (pc == 1)
            {
                FSexport = false;
                PSexport = false;
            }
            exportFSFlag = exportFTrack / pc * 2 >= ExportFSWeight;
            exportPSFlag = exportPTrack / pc * 2 >= ExportPSWeight;

            if (!exportFSFlag || Owner.averagePLanetStorage >= MAX_STORAGE)
                FSexport = true;

            if (!exportPSFlag || Owner.averagePLanetStorage >= MAX_STORAGE)
                PSexport = true;
            float PRatio = ProductionHere / MAX_STORAGE;
            float FRatio = FoodHere / MAX_STORAGE;

            int queueCount = ConstructionQueue.Count;
            switch (colonyType)
            {

                case ColonyType.Colony:
                case ColonyType.Industrial:
                    if (Population >= 1000 && MaxPopulation >= Population)
                    {
                        if (PRatio < .9 && queueCount > 0) 
                            ps = GoodState.IMPORT;
                        else if (queueCount == 0)
                        {
                            ps = GoodState.EXPORT;
                        }
                        else
                            ps = GoodState.STORE;

                    }
                    else if (queueCount > 0 || Owner.data.Traits.Cybernetic > 0)
                    {
                        if (PRatio < .5f)
                            ps = GoodState.IMPORT;
                        else if (!PSexport && PRatio > .5)
                            ps = GoodState.EXPORT;
                        else
                            ps = GoodState.STORE;
                    }
                    else
                    {
                        if (PRatio > .5f && !PSexport)
                            ps = GoodState.EXPORT;
                        else if (PRatio > .5f && PSexport)
                            ps = GoodState.STORE;
                        else ps = GoodState.EXPORT;

                    }

                    if (NetFoodPerTurn < 0)
                        fs = Planet.GoodState.IMPORT;
                    else if (FRatio > .75f)
                        fs = Planet.GoodState.STORE;
                    else
                        fs = Planet.GoodState.IMPORT;
                    break;


                case ColonyType.Agricultural:
                    if (PRatio > .75 && !PSexport)
                        ps = Planet.GoodState.EXPORT;
                    else if (PRatio < .5 && PSexport)
                        ps = Planet.GoodState.IMPORT;
                    else
                        ps = GoodState.STORE;


                    if (NetFoodPerTurn > 0)
                        fs = Planet.GoodState.EXPORT;
                    else if (NetFoodPerTurn < 0)
                        fs = Planet.GoodState.IMPORT;
                    else if (FRatio > .75f)
                        fs = Planet.GoodState.STORE;
                    else
                        fs = Planet.GoodState.IMPORT;

                    break;

                case ColonyType.Research:

                    {
                        if (PRatio > .75f && !PSexport)
                            ps = Planet.GoodState.EXPORT;
                        else if (PRatio < .5f) //&& PSexport
                            ps = Planet.GoodState.IMPORT;
                        else
                            ps = GoodState.STORE;

                        if (NetFoodPerTurn < 0)
                            fs = Planet.GoodState.IMPORT;
                        else if (NetFoodPerTurn < 0)
                            fs = Planet.GoodState.IMPORT;
                        else
                        if (FRatio > .75f && !FSexport)
                            fs = Planet.GoodState.EXPORT;
                        else if (FRatio < .75) //FSexport &&
                            fs = Planet.GoodState.IMPORT;
                        else
                            fs = GoodState.STORE;

                        break;
                    }

                case ColonyType.Core:
                    if (MaxPopulation > Population * .75f && Population > developmentLevel * 1000)
                    {

                        if (PRatio > .33f)
                            ps = GoodState.EXPORT;
                        else if (PRatio < .33)
                            ps = GoodState.STORE;
                        else
                            ps = GoodState.IMPORT;
                    }
                    else
                    {
                        if (PRatio > .75 && !FSexport)
                            ps = GoodState.EXPORT;
                        else if (PRatio < .5) //&& FSexport
                            ps = GoodState.IMPORT;
                        else ps = GoodState.STORE;
                    }

                    if (NetFoodPerTurn < 0)
                        fs = Planet.GoodState.IMPORT;
                    else if (FRatio > .25)
                        fs = GoodState.EXPORT;
                    else if (NetFoodPerTurn > developmentLevel * .5)
                        fs = GoodState.STORE;
                    else
                        fs = GoodState.IMPORT;


                    break;
                case ColonyType.Military:
                case ColonyType.TradeHub:
                    if (fs != GoodState.STORE)
                        if (FRatio > .50)
                            fs = GoodState.EXPORT;
                        else
                            fs = GoodState.IMPORT;
                    if (ps != GoodState.STORE)
                        if (PRatio > .50)
                            ps = GoodState.EXPORT;
                        else
                            ps = GoodState.IMPORT;

                    break;

                default:
                    break;
            }
            if (!PSexport)
                this.PSexport = true;
            else
            {
                this.PSexport = false;
            }


        }
        public void DoGoverning()
        {

            float income = GrossMoneyPT - TotalMaintenanceCostsPerTurn;
            if (colonyType == Planet.ColonyType.Colony)
                return;
            GetBuildingsWeCanBuildHere();
            Building cheapestFlatfood =
                BuildingsCanBuild.Where(flatfood => flatfood.PlusFlatFoodAmount > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
 
            Building cheapestFlatprod = BuildingsCanBuild.Where(flat => flat.PlusFlatProductionAmount > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
            Building cheapestFlatResearch = BuildingsCanBuild.Where(flat => flat.PlusFlatResearchAmount > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
            if (Owner.data.Traits.Cybernetic > 0)
            {
                cheapestFlatfood = cheapestFlatprod;// this.BuildingsCanBuild.Where(flat => flat.PlusProdPerColonist > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
            }
            Building pro = cheapestFlatprod;
            Building food = cheapestFlatfood;
            Building res = cheapestFlatResearch;
            bool noMoreBiospheres = true;
            foreach(PlanetGridSquare pgs in TilesList)
            {
                if(pgs.Habitable)
                    continue;
                noMoreBiospheres = false;
                break;
            }
            int buildingsinQueue = ConstructionQueue.Where(isbuilding => isbuilding.isBuilding).Count();
            bool needsBiospheres = ConstructionQueue.Where(isbuilding => isbuilding.isBuilding && isbuilding.Building.Name == "Biospheres").Count() != buildingsinQueue;
            bool StuffInQueueToBuild = ConstructionQueue.Count >5;// .Where(building => building.isBuilding || (building.Cost - building.productionTowards > this.ProductionHere)).Count() > 0;
            bool ForgetReseachAndBuild =
     string.IsNullOrEmpty(Owner.ResearchTopic) || StuffInQueueToBuild || (developmentLevel < 3 && (ProductionHere + 1) / (MAX_STORAGE + 1) < .5f);
            if (colonyType == ColonyType.Research && string.IsNullOrEmpty(Owner.ResearchTopic))
            {
                colonyType = ColonyType.Industrial;
            }
            if ( !true && Owner.data.Traits.Cybernetic < 0) //no longer needed
            #region cybernetic
            {

                FarmerPercentage = 0.0f;

                float surplus = GrossProductionPerTurn - consumption;
                surplus = surplus * (1 - (ProductionHere + 1) / (MAX_STORAGE + 1));
                WorkerPercentage = CalculateCyberneticPercentForSurplus(surplus);
                if (WorkerPercentage > 1.0)
                    WorkerPercentage = 1f;
                ResearcherPercentage = 1f - WorkerPercentage;
                if (ResearcherPercentage < 0f)
                    ResearcherPercentage = 0f;
                //if (this.ProductionHere > this.MAX_STORAGE * 0.25f && (double)this.GetNetProductionPerTurn() > 1.0)// &&
                //    this.ps = Planet.GoodState.EXPORT;
                //else
                //    this.ps = Planet.GoodState.IMPORT;
                float buildingCount = 0.0f;
                foreach (QueueItem queueItem in ConstructionQueue)
                {
                    if (queueItem.isBuilding)
                        ++buildingCount;
                    if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                        ++buildingCount;
                }
                bool flag1 = true;
                foreach (Building building in BuildingList)
                {
                    if (building.Name == "Outpost" || building.Name == "Capital City")
                        flag1 = false;
                }
                if (flag1)
                {
                    bool flag2 = false;
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                        {
                            flag2 = true;
                            break;
                        }
                    }
                    if (!flag2)
                        AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"),false);
                }
                bool flag3 = false;
                foreach (Building building1 in BuildingsCanBuild)
                {
                    if (building1.PlusFlatProductionAmount > 0.0
                        || building1.PlusProdPerColonist > 0.0
                        || (building1.Name == "Space Port" || building1.PlusProdPerRichness > 0.0) || building1.Name == "Outpost")
                    {
                        int num2 = 0;
                        foreach (Building building2 in BuildingList)
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
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isBuilding
                            && (queueItem.Building.PlusFlatProductionAmount > 0.0
                            || queueItem.Building.PlusProdPerColonist > 0.0
                            || queueItem.Building.PlusProdPerRichness > 0.0))
                        {
                            flag4 = false;
                            break;
                        }
                    }
                }
                if (Owner != EmpireManager.Player
                    && !Shipyards.Any(ship => ship.Value.GetShipData().IsShipyard)
                    && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard) && GrossMoneyPT > 5.0
                    && NetProductionPerTurn > 6.0)
                {
                    bool hasShipyard = false;
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isShip && queueItem.sData.IsShipyard)
                        {
                            hasShipyard = true;
                            break;
                        }
                    }
                    if (!hasShipyard)
                        ConstructionQueue.Add(new QueueItem()
                        {
                            isShip = true,
                            sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetShipData(),
                            Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner)
                        });
                }
                if (buildingCount < 2.0 && flag4)
                {
                    GetBuildingsWeCanBuildHere();
                    Building b = null;
                    float num2 = 99999f;
                    foreach (Building building in BuildingsCanBuild)
                    {
                        if ((building.PlusTerraformPoints <= 0.0) //(building.Name == "Terraformer") 
                            && building.PlusFlatFoodAmount <= 0.0f
                            && (building.PlusFoodPerColonist <= 0.0f && !(building.Name == "Biospheres"))
                            && (building.PlusFlatPopulation <= 0.0f || Population / MaxPopulation <= 0.25f))
                        {
                            if (building.PlusFlatProductionAmount > 0.0f
                                || building.PlusProdPerColonist > 0.0f
                                || building.PlusTaxPercentage > 0.0f
                                || building.PlusProdPerRichness > 0.0f
                                || building.CreditsPerColonist > 0.0f
                                || (building.Name == "Space Port" || building.Name == "Outpost"))
                            {
                                if ((building.Cost + 1) / (GetNetProductionPerTurn() + 1) < 150 || ProductionHere > building.Cost * .5) //(building.Name == "Space Port") &&
                                {
                                    float num3 = building.Cost;
                                    b = building;
                                    break;
                                }
                            }
                            else if (building.Cost < num2 && (!(building.Name == "Space Port") || BuildingList.Count >= 2) || ((building.Cost + 1) / (GetNetProductionPerTurn() + 1) < 150 || ProductionHere > building.Cost * .5))
                            {
                                num2 = building.Cost;
                                b = building;
                            }
                        }
                    }
                    if (b != null && (GrossMoneyPT - TotalMaintenanceCostsPerTurn > 0.0f || (b.CreditsPerColonist > 0 || PlusTaxPercentage > 0)))//((double)this.Owner.EstimateIncomeAtTaxRate(0.4f) - (double)b.Maintenance > 0.0 ))
                    {
                        bool flag2 = true;
                        if (b.BuildOnlyOnce)
                        {
                            for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                            {
                                if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                {
                                    flag2 = false;
                                    break;
                                }
                            }
                        }
                        if (flag2)
                            AddBuildingToCQ(b,false);
                    }
                    else if (buildingCount < 2.0 && Owner.GetBDict()["Biospheres"] && MineralRichness >= .5f)
                    {
                        if (Owner == Empire.Universe.PlayerEmpire)
                        {
                            if (Population / (MaxPopulation + MaxPopBonus) > 0.949999f && (Owner.EstimateIncomeAtTaxRate(Owner.data.TaxRate) -  ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || Owner.Money > Owner.GrossTaxes * 3))
                                TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                        }
                        else if (Population / (MaxPopulation + MaxPopBonus) > 0.949999988079071 && (Owner.EstimateIncomeAtTaxRate(0.5f) -  ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || Owner.Money > Owner.GrossTaxes * 3))
                            TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                    }
                }
                for (int index = 0; index < ConstructionQueue.Count; ++index)
                {
                    QueueItem queueItem1 = ConstructionQueue[index];
                    if (index == 0 && queueItem1.isBuilding && ProductionHere > MAX_STORAGE * .5)
                    {
                        if (queueItem1.Building.Name == "Outpost"
                            ||  queueItem1.Building.PlusFlatProductionAmount > 0.0f
                            ||  queueItem1.Building.PlusProdPerRichness > 0.0f
                            ||  queueItem1.Building.PlusProdPerColonist > 0.0f
                            //|| (double)queueItem1.Building.PlusTaxPercentage > 0.0
                            //|| (double)queueItem1.Building.CreditsPerColonist > 0.0
                            )
                        {
                            ApplyAllStoredProduction(0);
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
                        ConstructionQueue.Remove(queueItem1);
                        ConstructionQueue.Insert(0, queueItem1);
                    }
                }
            }
            #endregion
            else
            {

                switch (colonyType)
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
                               
                                float surplus = (NetFoodPerTurn * (string.IsNullOrEmpty(Owner.ResearchTopic) ? 1 : .5f)) * (1 - (FoodHere + 1) / (MAX_STORAGE + 1));
                                if(Owner.data.Traits.Cybernetic >0)
                                {
                                    surplus = GrossProductionPerTurn - consumption;
                                    surplus = surplus * ((string.IsNullOrEmpty(Owner.ResearchTopic) ? 1 : .5f)) * (1 - (ProductionHere + 1) / (MAX_STORAGE + 1));
                                        //(1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                                }
                                FarmerPercentage = CalculateFarmerPercentForSurplus(surplus);
                                if ( FarmerPercentage == 1 && StuffInQueueToBuild)
                                    FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                                if (FarmerPercentage == 1 && StuffInQueueToBuild)
                                    FarmerPercentage = .9f;
                                WorkerPercentage =
                                (1f - FarmerPercentage) *
                                (ForgetReseachAndBuild ? 1 : (1 - (ProductionHere + 1) / (MAX_STORAGE + 1)));
   
                                float Remainder = 1f - FarmerPercentage;
                                //Research is happening
                                WorkerPercentage = (Remainder * (string.IsNullOrEmpty(Owner.ResearchTopic) ? 1 : (1 - (ProductionHere) / (MAX_STORAGE))));
                                if (ProductionHere / MAX_STORAGE > .9 && !StuffInQueueToBuild)
                                    WorkerPercentage = 0;
                                ResearcherPercentage = Remainder - WorkerPercentage;
                                if (Owner.data.Traits.Cybernetic > 0)
                                {
                                    WorkerPercentage += FarmerPercentage;
                                    FarmerPercentage = 0;
                                }
                            }
                            #endregion
                            SetExportState(colonyType);
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
                            if (Owner != Empire.Universe.PlayerEmpire
                                && !Shipyards.Any(ship => ship.Value.GetShipData().IsShipyard)
                                && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard)

                                )
                            // && (double)this.Owner.MoneyLastTurn > 5.0 && (double)this.NetProductionPerTurn > 4.0)
                            {
                                bool hasShipyard = false;
                                foreach (QueueItem queueItem in ConstructionQueue)
                                {
                                    if (queueItem.isShip && queueItem.sData.IsShipyard)
                                    {
                                        hasShipyard = true;
                                        break;
                                    }
                                }
                                if (!hasShipyard && developmentLevel > 2)
                                    ConstructionQueue.Add(new QueueItem()
                                    {
                                        isShip = true,
                                        sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetShipData(),
                                        Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) * UniverseScreen.GamePaceStatic
                                    });
                            }
                            #endregion
                            byte num5 = 0;
                            bool flag5 = false;
                            foreach (QueueItem queueItem in ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name != "Biospheres")
                                    ++num5;
                                if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                    ++num5;
                                if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                    flag5 = true;
                            }
                            bool flag6 = true;
                            foreach (Building building in BuildingList)
                            {
                                if (building.Name == "Outpost" || building.Name == "Capital City")
                                    flag6 = false;
                                if (building.Name == "Terraformer")
                                    flag5 = true;
                            }
                            if (flag6)
                            {
                                bool flag1 = false;
                                foreach (QueueItem queueItem in ConstructionQueue)
                                {
                                    if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                    {
                                        flag1 = true;
                                        break;
                                    }
                                }
                                if (!flag1)
                                    AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"),false);
                            }
                            if (num5 < 2)
                            {
                                GetBuildingsWeCanBuildHere();

                                foreach (PlanetGridSquare PGS in TilesList)
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
                                    AddBuildingToCQ(buildthis,false);
                                }

                            }
                            if (num5 < 2)
                            {
                                float coreCost = 99999f;
                                GetBuildingsWeCanBuildHere();
                                Building b = null;
                                foreach (Building building in BuildingsCanBuild)
                                {
                                    if (!WeCanAffordThis(building, colonyType))
                                        continue;
                                    //if you dont want it to be built put it here.
                                    //this first if is the low pri build spot. 
                                    //the second if will override items that make it through this if. 
                                    if (cheapestFlatfood == null && cheapestFlatprod == null &&
                                        //(building.PlusFlatPopulation <= 0.0f || this.Population <= 1000.0)
                                        //&& 
                                        ( (building.MinusFertilityOnBuild <= 0.0f || Owner.data.Traits.Cybernetic > 0) && !(building.Name == "Biospheres") )
                                        //&& (!(building.Name == "Terraformer") || !flag5 && this.Fertility < 1.0)
                                        && ( building.PlusTerraformPoints < 0 || !flag5 && (Fertility < 1.0 && Owner.data.Traits.Cybernetic <= 0 ))

                                        //&& (building.PlusFlatPopulation <= 0.0
                                        //|| (this.Population / this.MaxPopulation <= 0.25 && this.developmentLevel >2 && !noMoreBiospheres))
                                        //||(this.Owner.data.Traits.Cybernetic >0 && building.PlusProdPerRichness >0)
                                        )
                                    {

                                        b = building;
                                        coreCost = b.Cost;
                                        break;
                                    }
                                    else if (building.Cost < coreCost && ((building.Name != "Biospheres" && building.PlusTerraformPoints <=0 ) || Population / MaxPopulation <= 0.25 && developmentLevel > 2 && !noMoreBiospheres))
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
                                        for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                        {
                                            if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                            {
                                                flag1 = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (flag1)
                                        AddBuildingToCQ(b,false);
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
                                        for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                        {
                                            if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                            {
                                                flag1 = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (flag1)
                                        AddBuildingToCQ(b);
                                }
                                else if (Owner.GetBDict()["Biospheres"] && MineralRichness >= 1.0f && ((Owner.data.Traits.Cybernetic > 0 && GrossProductionPerTurn > consumption) || Owner.data.Traits.Cybernetic <=0 && Fertility >= 1.0))
                                {
                                    if (Owner == Empire.Universe.PlayerEmpire)
                                    {
                                        if (Population / (MaxPopulation + MaxPopBonus) > 0.94999f && (Owner.EstimateIncomeAtTaxRate(Owner.data.TaxRate) - ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || Owner.Money > Owner.GrossTaxes * 3))
                                            TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                                    }
                                    else if (Population / (MaxPopulation + MaxPopBonus) > 0.94999f && (Owner.EstimateIncomeAtTaxRate(0.5f) -  ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f || Owner.Money > Owner.GrossTaxes * 3))
                                        TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                                }
                            }

                            for (int index = 0; index < ConstructionQueue.Count; ++index)
                            {
                                QueueItem queueItem1 = ConstructionQueue[index];
                                if (index == 0 && queueItem1.isBuilding)
                                {
                                    if (queueItem1.Building.Name == "Outpost" ) //|| (double)queueItem1.Building.PlusFlatProductionAmount > 0.0 || (double)queueItem1.Building.PlusProdPerRichness > 0.0 || (double)queueItem1.Building.PlusProdPerColonist > 0.0)
                                    {
                                        ApplyAllStoredProduction(0);
                                    }
                                    break;
                                }
                                else if (queueItem1.isBuilding && 
                                    (queueItem1.Building.PlusFlatProductionAmount > 0.0f || 
                                    queueItem1.Building.PlusProdPerColonist > 0.0f || 
                                    queueItem1.Building.Name == "Outpost"))
                                {
                                    ConstructionQueue.Remove(queueItem1);
                                    ConstructionQueue.Insert(0, queueItem1);
                                }
                            }


                            break;
                        }
                        #endregion
                    case Planet.ColonyType.Industrial:
                        #region Industrial
                        //this.fs = Planet.GoodState.IMPORT;

                        FarmerPercentage = 0.0f;
                        WorkerPercentage = 1f;
                        ResearcherPercentage = 0.0f;

                        //? true : .75f;
                        //this.ps = (double)this.ProductionHere >= 20.0 ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                        float IndySurplus = (NetFoodPerTurn) *//(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? .5f : .25f)) * 
                            (1 - (FoodHere + 1) / (MAX_STORAGE + 1));
                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            IndySurplus = GrossProductionPerTurn - consumption;
                            IndySurplus = IndySurplus * (1 - (FoodHere + 1) / (MAX_STORAGE + 1));
                            //(1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                        }
                        //if ((double)this.FoodHere <= (double)this.consumption)
                        {

                            FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);
                            FarmerPercentage *= (FoodHere / MAX_STORAGE) > .25 ? .5f : 1;
                            if ( FarmerPercentage == 1 && StuffInQueueToBuild)
                                FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                            WorkerPercentage =
                                (1f - FarmerPercentage)   //(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? 1f :
                                * (ForgetReseachAndBuild ? 1 :
                             (1 - (ProductionHere + 1) / (MAX_STORAGE + 1)));
                            if (ProductionHere / MAX_STORAGE > .75 && !StuffInQueueToBuild)
                                WorkerPercentage = 0;

                            ResearcherPercentage = 1 - FarmerPercentage - WorkerPercentage;// 0.0f;
                            if (Owner.data.Traits.Cybernetic > 0)
                            {
                                WorkerPercentage += FarmerPercentage;
                                FarmerPercentage = 0;
                            }
                        }
                        SetExportState(colonyType);
                        

                        float num6 = 0.0f;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
                                ++num6;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++num6;
                        }
                        bool flag7 = true;
                        foreach (Building building in BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                flag7 = false;
                        }
                        if (flag7)
                        {
                            bool flag1 = false;
                            foreach (QueueItem queueItem in ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    flag1 = true;
                                    break;
                                }
                            }
                            if (!flag1)
                                AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                        }

                        bool flag8 = false;
                        GetBuildingsWeCanBuildHere();
                        if (num6 < 2)
                        {

                            GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in TilesList)
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
                                AddBuildingToCQ(buildthis);
                            }

                        }
                        {


                            double num1 = 0;
                            foreach (Building building1 in BuildingsCanBuild)
                            {
                                
                                    if ( building1.PlusFlatProductionAmount > 0.0
                                        ||  building1.PlusProdPerColonist > 0.0
                                        ||  building1.PlusProdPerRichness > 0.0
                                        )
                                {

                                    foreach (Building building2 in BuildingList)
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
                            using (ConstructionQueue.AcquireReadLock())
                            foreach (QueueItem queueItem in ConstructionQueue)
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
                        }
                        if (flag9 &&  num6 < 2f)
                        {
                            float indycost = 99999f;
                            Building b = null;
                            foreach (Building building in BuildingsCanBuild)//.OrderBy(cost=> cost.Cost))
                            {
                                if (!WeCanAffordThis(building, colonyType))
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
                                    for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                    {
                                        if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                        {
                                            flag1 = false;
                                            break;
                                        }
                                    }
                                }
                                if (flag1)
                                {
                                    AddBuildingToCQ(b);

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
                        FarmerPercentage = 0.0f;
                        WorkerPercentage = 0.0f;
                        ResearcherPercentage = 1f;
                        //StuffInQueueToBuild =
                        //    this.ConstructionQueue.Where(building => building.isBuilding
                        //        && (building.Building.PlusFlatFoodAmount > 0
                        //        || building.Building.PlusFlatProductionAmount > 0
                        //        || building.Building.PlusFlatResearchAmount > 0
                        //        || building.Building.PlusResearchPerColonist > 0
                        //        //|| building.Building.Name == "Biospheres"
                        //        )).Count() > 0;  //(building.Cost > this.NetProductionPerTurn * 10)).Count() > 0;
                        ForgetReseachAndBuild =
                            string.IsNullOrEmpty(Owner.ResearchTopic); //|| StuffInQueueToBuild; //? 1 : .5f;
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

                        IndySurplus = (NetFoodPerTurn) * ((MAX_STORAGE - FoodHere * 2f ) / MAX_STORAGE);
                        //(1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                        FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);

                        WorkerPercentage = (1f - FarmerPercentage);

                        if (StuffInQueueToBuild)
                            WorkerPercentage *= ((MAX_STORAGE - ProductionHere) / MAX_STORAGE) / developmentLevel;
                        else
                            WorkerPercentage = 0;

                        ResearcherPercentage = 1f - FarmerPercentage - WorkerPercentage;

                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            WorkerPercentage += FarmerPercentage;
                            FarmerPercentage = 0;
                        }


                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            WorkerPercentage += FarmerPercentage;
                            FarmerPercentage = 0;
                        }
                        SetExportState(colonyType);
                        //    if ((double)this.FoodHere <= (double)this.consumption)
                        //{
                        //    this.FarmerPercentage = this.CalculateFarmerPercentForSurplus(0.0f);
                        //    this.ResearcherPercentage = 1f - this.FarmerPercentage;
                        //}
                        float num8 = 0.0f;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding )
                                ++num8;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++num8;
                        }
                        bool flag10 = true;
                        foreach (Building building in BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                flag10 = false;
                        }
                        if (flag10)
                        {
                            bool flag1 = false;
                            foreach (QueueItem queueItem in ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    flag1 = true;
                                    break;
                                }
                            }
                            if (!flag1)
                                AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                        }
                        if (num8 < 2.0)
                        {
                            GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in TilesList)
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

                            if (buildthis != null && WeCanAffordThis(buildthis, colonyType))
                            {
                                num8++;
                                AddBuildingToCQ(buildthis);
                            }
                        }
                        if (num8 < 2.0)
                        {
                            GetBuildingsWeCanBuildHere();
                            Building b = null;
                            float num1 = 99999f;
                            foreach (Building building in BuildingsCanBuild)
                            {
                                if (!WeCanAffordThis(building, colonyType))
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
                                        for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                        {
                                            if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                            {
                                                flag1 = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (flag1)
                                    {
                                        AddBuildingToCQ(b);
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
                        FarmerPercentage = 1f;
                        WorkerPercentage = 0.0f;
                        ResearcherPercentage = 0.0f;
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

                        SetExportState(colonyType);
                        StuffInQueueToBuild = ConstructionQueue.Where(building => building.isBuilding || (building.Cost > NetProductionPerTurn * 10)).Count() > 0;
                        ForgetReseachAndBuild =
                            string.IsNullOrEmpty(Owner.ResearchTopic) ; //? 1 : .5f;
                        IndySurplus = (NetFoodPerTurn) * ((MAX_STORAGE - FoodHere) / MAX_STORAGE);
                        //(1 - (this.FoodHere + 1) / (this.MAX_STORAGE + 1));
                        FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);

                        WorkerPercentage = (1f - FarmerPercentage);

                        if (StuffInQueueToBuild)
                            WorkerPercentage *= ((MAX_STORAGE - ProductionHere) / MAX_STORAGE);
                        else
                            WorkerPercentage = 0;

                        ResearcherPercentage = 1f - FarmerPercentage - WorkerPercentage;

                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            WorkerPercentage += FarmerPercentage;
                            FarmerPercentage = 0;
                        }

                        float num9 = 0.0f;
                        //bool flag11 = false;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
                                ++num9;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++num9;
                            //if (queueItem.isBuilding && queueItem.Building.Name == "Terraformer")
                            //    flag11 = true;
                        }
                        bool flag12 = true;
                        foreach (Building building in BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                flag12 = false;
                            //if (building.Name == "Terraformer" && this.Fertility >= 1.0)
                            //    flag11 = true;
                        }
                        if (flag12)
                        {
                            bool flag1 = false;
                            foreach (QueueItem queueItem in ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    flag1 = true;
                                    break;
                                }
                            }
                            if (!flag1)
                                AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                        }
                        if ( num9 < 2 )
                        {
                            GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in TilesList)
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
                                AddBuildingToCQ(buildthis);
                            }
                        }

                        if ( num9 < 2.0f)
                        {
                            GetBuildingsWeCanBuildHere();
                            Building b = null;
                            float num1 = 99999f;
                            foreach (Building building in BuildingsCanBuild.OrderBy(cost => cost.Cost))
                            {
                                if (!WeCanAffordThis(building, colonyType))
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
                                    for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                    {
                                        if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                        {
                                            flag1 = false;
                                            break;
                                        }
                                    }
                                }
                                if (flag1)
                                    AddBuildingToCQ(b);
                            }
                        }
                        break;
                        #endregion
                    case Planet.ColonyType.Military:
                        #region Military                        
                        FarmerPercentage = 0.0f;
                        WorkerPercentage = 1f;
                        ResearcherPercentage = 0.0f;
                        if (FoodHere <= consumption)
                        {
                            FarmerPercentage = CalculateFarmerPercentForSurplus(0.01f);
                            WorkerPercentage = 1f - FarmerPercentage;
                        
                        }

                        WorkerPercentage = (1f - FarmerPercentage);

                        if (StuffInQueueToBuild)
                            WorkerPercentage *= ((MAX_STORAGE - ProductionHere) / MAX_STORAGE);
                        else
                            WorkerPercentage = 0;

                        ResearcherPercentage = 1f - FarmerPercentage - WorkerPercentage;

                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            WorkerPercentage += FarmerPercentage;
                            FarmerPercentage = 0;
                        }


                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            WorkerPercentage += FarmerPercentage;
                            FarmerPercentage = 0;
                        }
                        if(!Owner.isPlayer && fs == GoodState.STORE)
                        {
                            fs = GoodState.IMPORT;
                            ps = GoodState.IMPORT;
                               
                        }
                        SetExportState(colonyType);
                        //this.ps = (double)this.ProductionHere >= 20.0 ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                        float buildingCount = 0.0f;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding)
                                ++buildingCount;
                            if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                                ++buildingCount;
                        }
                        bool missingOutpost = true;
                        foreach (Building building in BuildingList)
                        {
                            if (building.Name == "Outpost" || building.Name == "Capital City")
                                missingOutpost = false;
                        }
                        if (missingOutpost)
                        {
                            bool hasOutpost = false;
                            foreach (QueueItem queueItem in ConstructionQueue)
                            {
                                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                                {
                                    hasOutpost = true;
                                    break;
                                }
                            }
                            if (!hasOutpost)
                                AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                        }
                        if (Owner != EmpireManager.Player
                            && !Shipyards.Any(ship => ship.Value.GetShipData().IsShipyard)
                            && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard) && GrossMoneyPT > 3.0)
                        {
                            bool hasShipyard = false;
                            foreach (QueueItem queueItem in ConstructionQueue)
                            {
                                if (queueItem.isShip && queueItem.sData.IsShipyard)
                                {
                                    hasShipyard = true;
                                    break;
                                }
                            }
                            if (!hasShipyard)
                                ConstructionQueue.Add(new QueueItem()
                                {
                                    isShip = true,
                                    sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetShipData(),
                                    Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner)
                                });
                        }
                        if ( buildingCount < 2.0f)
                        {
                            GetBuildingsWeCanBuildHere();

                            foreach (PlanetGridSquare PGS in TilesList)
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
                                AddBuildingToCQ(buildthis);
                            }




                            GetBuildingsWeCanBuildHere();
                            Building b = null;
                            float num1 = 99999f;
                            foreach (Building building in BuildingsCanBuild.OrderBy(cost => cost.Cost))
                            {
                                if (!WeCanAffordThis(building, colonyType))
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
                                    for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                    {
                                        if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                        {
                                            flag1 = false;
                                            break;
                                        }
                                    }
                                }
                                if (flag1)
                                    AddBuildingToCQ(b);
                            }
                        }
                        break;
                        #endregion

                    case Planet.ColonyType.TradeHub:
                        #region TradeHub
                        {

                            //this.fs = Planet.GoodState.IMPORT;

                            FarmerPercentage = 0.0f;
                            WorkerPercentage = 1f;
                            ResearcherPercentage = 0.0f;

                            //? true : .75f;
                            ps = ProductionHere >= 20  ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                            float IndySurplus2 = (NetFoodPerTurn) *//(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? .5f : .25f)) * 
                                (1 - (FoodHere + 1) / (MAX_STORAGE + 1));
                            if (Owner.data.Traits.Cybernetic > 0)
                            {
                                IndySurplus = GrossProductionPerTurn - consumption;
                                IndySurplus = IndySurplus * (1 - (FoodHere + 1) / (MAX_STORAGE + 1));
                                //(1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                            }
                            //if ((double)this.FoodHere <= (double)this.consumption)
                            {

                                FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus2);
                                if (FarmerPercentage == 1 && StuffInQueueToBuild)
                                    FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                                WorkerPercentage =
                                    (1f - FarmerPercentage)   //(string.IsNullOrEmpty(this.Owner.ResearchTopic) ? 1f :
                                    * (ForgetReseachAndBuild ? 1 :
                                 (1 - (ProductionHere + 1) / (MAX_STORAGE + 1)));
                                if (ProductionHere / MAX_STORAGE > .75 && !StuffInQueueToBuild)
                                    WorkerPercentage = 0;
                                ResearcherPercentage = 1 - FarmerPercentage - WorkerPercentage;// 0.0f;
                                if (Owner.data.Traits.Cybernetic > 0)
                                {
                                    WorkerPercentage += FarmerPercentage;
                                    FarmerPercentage = 0;
                                }
                                SetExportState(colonyType);

                            }
                            break;
                        }
                        #endregion
                }
            }

            if (ConstructionQueue.Count < 5 && !ParentSystem.CombatInSystem && developmentLevel > 2 && colonyType != ColonyType.Research) //  this.ProductionHere > this.MAX_STORAGE * .75f)
            #region Troops and platforms
            {
                //Added by McShooterz: Colony build troops
                #region MyRegion
                if (Owner.isPlayer && colonyType == ColonyType.Military)
                {
                    bool addTroop = false;
                    foreach (PlanetGridSquare planetGridSquare in TilesList)
                    {
                        if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops)
                        {
                            addTroop = true;
                            break;
                        }
                    }
                    if (addTroop && AllowInfantry)
                    {
                        foreach (string troopType in ResourceManager.TroopTypes)
                        {
                            if (!Owner.WeCanBuildTroop(troopType))
                                continue;
                            QueueItem qi = new QueueItem();
                            qi.isTroop = true;
                            qi.troopType = troopType;
                            qi.Cost = ResourceManager.GetTroopCost(troopType);
                            qi.productionTowards = 0f;
                            qi.NotifyOnEmpty = false;
                            ConstructionQueue.Add(qi);
                            break;
                        }
                    }


                }
                #endregion
                //Added by McShooterz: build defense platforms

                if (HasShipyard && !ParentSystem.CombatInSystem 
                     && (!Owner.isPlayer || colonyType == ColonyType.Military))
                {

                    SystemCommander SCom;
                    if (Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out SCom))
                    {
                        float DefBudget;
                        DefBudget = Owner.data.DefenseBudget * SCom.PercentageOfValue;

                        float maxProd = GetMaxProductionPotential();
                        //bool buildStation =false;
                        float platformUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.platform].Upkeep;
                        float stationUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.station].Upkeep;
                        string station = Owner.GetGSAI().GetStarBase();
                        //if (DefBudget >= 1 && !string.IsNullOrEmpty(station))
                        //    buildStation = true;
                        int PlatformCount = 0;
                        int stationCount = 0;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (!queueItem.isShip)
                                continue;
                            if (queueItem.sData.Role == ShipData.RoleName.platform)
                            {
                                if (DefBudget - platformUpkeep < -platformUpkeep * .5) //|| (buildStation && DefBudget > stationUpkeep))
                                {
                                    ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                DefBudget -= platformUpkeep;
                                PlatformCount++;
                            }
                            if (queueItem.sData.Role == ShipData.RoleName.station)
                            {
                                if (DefBudget - stationUpkeep < -stationUpkeep)
                                {
                                    ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                DefBudget -= stationUpkeep;
                                stationCount++;
                            }
                        }
                        foreach (Ship platform in Shipyards.Values)
                        {
                            if (platform.BaseStrength <= 0)
                                continue;
                            if (platform.AI.State == AIState.Scrap)
                                continue;
                            if (platform.shipData.Role == ShipData.RoleName.station)
                            {
                                stationUpkeep = platform.GetMaintCost();
                                if (DefBudget - stationUpkeep < -stationUpkeep)
                                {

                                    platform.AI.OrderScrapShip();
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
                                    platform.AI.OrderScrapShip();

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
                                if (ship.GetCost(Owner) / GrossProductionPerTurn < 10)
                                    ConstructionQueue.Add(new QueueItem()
                                   {
                                       isShip = true,
                                       sData = ship.GetShipData(),
                                       Cost = ship.GetCost(Owner)
                                   });
                            }
                            DefBudget -= stationUpkeep;
                        }
                        if (DefBudget > platformUpkeep && maxProd > 1.0
                            && PlatformCount < SCom.RankImportance //(int)(SCom.PercentageOfValue * this.developmentLevel)
                            && PlatformCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                        {
                            string platform = Owner.GetGSAI().GetDefenceSatellite();
                            if (!string.IsNullOrEmpty(platform))
                            {
                                Ship ship = ResourceManager.ShipsDict[platform];
                                ConstructionQueue.Add(new QueueItem()
                                {
                                    isShip = true,
                                    sData = ship.GetShipData(),
                                    Cost = ship.GetCost(Owner)
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
                //Array<Building> list = new Array<Building>();
                //foreach (Building building in this.BuildingList)
                //{
                //    if ((double)building.PlusFlatPopulation > 0.0 && (double)building.Maintenance > 0.0 && building.Name != "Biospheres")
                //    //
                //        list.Add(building);
                //}
                //foreach (Building b in list)
                //    this.ScrapBuilding(b);

                Array<Building> list1 = new Array<Building>();
                if (Fertility >= 1 )
                {

                    foreach (Building building in BuildingList)
                    {
                        if (building.PlusTerraformPoints > 0.0f &&  building.Maintenance > 0 )
                            list1.Add(building);
                    }
                }

                //finances
                //if (this.Owner.Money*.5f < this.Owner.GrossTaxes*(1-this.Owner.data.TaxRate) && this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn < 0)
                {
                    using (ConstructionQueue.AcquireReadLock())
                    foreach (PlanetGridSquare PGS in TilesList)
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
                        if (PGS.building != null && !qitemTest && PGS.building.Scrappable && !WeCanAffordThis(PGS.building, colonyType)) // queueItem.isBuilding && !WeCanAffordThis(queueItem.Building, this.colonyType))
                        {
                                ScrapBuilding(PGS.building);

                        }
                        if (qitemTest && !WeCanAffordThis(PGS.QItem.Building, colonyType))
                        {
                                ProductionHere += PGS.QItem.productionTowards;
                                ConstructionQueue.QueuePendingRemoval(PGS.QItem);
                            PGS.QItem = null;

                        }
                    }
                    //foreach (QueueItem queueItem in (Array<QueueItem>)this.ConstructionQueue)
                    //{
                    //    if(queueItem.Building == cheapestFlatfood || queueItem.Building == cheapestFlatprod || queueItem.Building == cheapestFlatResearch)
                    //        continue;
                    //    if (queueItem.isBuilding &&  !WeCanAffordThis(queueItem.Building, this.colonyType))
                    //    {
                    //        this.ProductionHere += queueItem.productionTowards;
                    //        this.ConstructionQueue.QueuePendingRemoval(queueItem);
                    //    }
                    //}
                    ConstructionQueue.ApplyPendingRemovals();
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

            Building building1 = null;
            foreach (Building building2 in BuildingList)
            {
                if (b == building2)
                    building1 = building2;
            }
            BuildingList.Remove(building1);
            ProductionHere += ResourceManager.BuildingsDict[b.Name].Cost / 2f;
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                if (planetGridSquare.building != null && planetGridSquare.building == building1)
                    planetGridSquare.building = null;
            }
        }


        public bool ApplyStoredProduction(int Index)
        {
            
            if (Crippled_Turns > 0 || RecentCombat || (ConstructionQueue.Count <= 0 || Owner == null ))//|| this.Owner.Money <=0))
                return false;
            if (Owner != null && !Owner.isPlayer && Owner.data.Traits.Cybernetic > 0)
                return false;
            
            QueueItem item = ConstructionQueue[Index];
            float amountToRush = GetMaxProductionPotential();

            amountToRush = Empire.Universe.Debug ? float.MaxValue : amountToRush < 5 ? 5 : amountToRush;
            float amount = amountToRush < ProductionHere ? amountToRush : Empire.Universe.Debug ? float.MaxValue : ProductionHere;
            if (amount < 1)
            {
                return false;
            }
            ProductionHere -= amount;
            ApplyProductiontoQueue(amount, Index);
                
                return true;
       }

        public void ApplyAllStoredProduction(int Index)
        {
            if (Crippled_Turns > 0 || RecentCombat || (ConstructionQueue.Count <= 0 || Owner == null )) //|| this.Owner.Money <= 0))
                return;
           
            float amount = Empire.Universe.Debug ? float.MaxValue : ProductionHere ;
            ProductionHere = 0f;
            ApplyProductiontoQueue(amount, Index);
            
        }

        private void ApplyProductionTowardsConstruction()
        {
            if (Crippled_Turns > 0 || RecentCombat)
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
            
            float maxp = GetMaxProductionPotential() *(1- FarmerPercentage); //this.NetProductionPerTurn; //
            if (maxp < 5)
                maxp = 5;
            float StorageRatio = 0;
            float take10Turns = 0;
                StorageRatio= ProductionHere / MAX_STORAGE;
            take10Turns = maxp * StorageRatio;

               
                if (!PSexport)
                    take10Turns *= (StorageRatio < .75f ? ps == GoodState.EXPORT ? .5f : ps == GoodState.STORE ? .25f : 1 : 1);                    
            if (!GovernorOn || colonyType == ColonyType.Colony)
                {
                    take10Turns = NetProductionPerTurn; ;
                }
                float normalAmount =  take10Turns;

            normalAmount = normalAmount < 0 ? 0 : normalAmount;
            normalAmount = normalAmount >
                ProductionHere ? ProductionHere : normalAmount;
            ProductionHere -= normalAmount;

            ApplyProductiontoQueue(normalAmount,0);
            ProductionHere += NetProductionPerTurn > 0.0f ? NetProductionPerTurn : 0.0f;

            //fbedard: apply all remaining production on Planet with no governor
            if (ps != GoodState.EXPORT && colonyType == Planet.ColonyType.Colony && Owner.isPlayer )
            {
                normalAmount =  ProductionHere;
                ProductionHere = 0f;
                ApplyProductiontoQueue(normalAmount, 0);                
            }            
        }

        public void ApplyProductiontoQueue(float howMuch, int whichItem)
        {
            if (Crippled_Turns > 0 || RecentCombat || howMuch <= 0.0)
            {
                if (howMuch > 0 && Crippled_Turns <= 0)
                ProductionHere += howMuch;
                return;
            }
            float cost = 0;
            if (ConstructionQueue.Count > 0 && ConstructionQueue.Count > whichItem)
            {                
                QueueItem item = ConstructionQueue[whichItem];
                cost = item.Cost;
                if (item.isShip)
                    //howMuch += howMuch * this.ShipBuildingModifier;
                    cost *= ShipBuildingModifier;
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
                    ProductionHere += howMuch;
                }
                ConstructionQueue[whichItem] = item;
            }
            else ProductionHere += howMuch;

            for (int index1 = 0; index1 < ConstructionQueue.Count; ++index1)
            {
                QueueItem queueItem = ConstructionQueue[index1];

               //Added by gremlin remove exess troops from queue 
                if (queueItem.isTroop)
                {

                    int space = 0;
                    foreach (PlanetGridSquare tilesList in TilesList)
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
                            ConstructionQueue.Remove(queueItem);
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

                if (queueItem.isBuilding && queueItem.productionTowards >= queueItem.Cost)
                {
                    bool dupBuildingWorkaround = false;
                    if (queueItem.Building.Name != "Biospheres")
                        foreach (Building dup in BuildingList)
                        {
                            if (dup.Name == queueItem.Building.Name)
                            {
                                ProductionHere += queueItem.productionTowards;
                                ConstructionQueue.QueuePendingRemoval(queueItem);
                                dupBuildingWorkaround = true;
                            }
                        }
                    if (!dupBuildingWorkaround)
                    {
                        Building building = ResourceManager.CreateBuilding(queueItem.Building.Name);
                        if (queueItem.IsPlayerAdded)
                            building.IsPlayerAdded = queueItem.IsPlayerAdded;
                        BuildingList.Add(building);
                        Fertility -= ResourceManager.CreateBuilding(queueItem.Building.Name).MinusFertilityOnBuild;
                        if (Fertility < 0.0)
                            Fertility = 0.0f;
                        if (queueItem.pgs != null)
                        {
                            if (queueItem.Building != null && queueItem.Building.Name == "Biospheres")
                            {
                                queueItem.pgs.Habitable = true;
                                queueItem.pgs.Biosphere = true;
                                queueItem.pgs.building = null;
                                queueItem.pgs.QItem = null;
                            }
                            else
                            {
                                queueItem.pgs.building = building;
                                queueItem.pgs.QItem = null;
                            }
                        }
                        if (queueItem.Building.Name == "Space Port")
                        {
                            Station.planet = this;
                            Station.ParentSystem = ParentSystem;
                            Station.LoadContent(Empire.Universe.ScreenManager);
                            HasShipyard = true;
                        }
                        if (queueItem.Building.AllowShipBuilding)
                            HasShipyard = true;
                        if (building.EventOnBuild != null && Owner != null && Owner == Empire.Universe.PlayerEmpire)
                            Empire.Universe.ScreenManager.AddScreen(new EventPopup(Empire.Universe, Empire.Universe.PlayerEmpire, ResourceManager.EventsDict[building.EventOnBuild], ResourceManager.EventsDict[building.EventOnBuild].PotentialOutcomes[0], true));
                        ConstructionQueue.QueuePendingRemoval(queueItem);
                    }
                }
                else if (queueItem.isShip && !ResourceManager.ShipsDict.ContainsKey(queueItem.sData.Name))
                {
                    ConstructionQueue.QueuePendingRemoval(queueItem);
                    ProductionHere += queueItem.productionTowards;
                    if (ProductionHere > MAX_STORAGE)
                        ProductionHere = MAX_STORAGE;
                }
                else if (queueItem.isShip && queueItem.productionTowards >= queueItem.Cost)
                {
                    Ship shipAt;
                    if (queueItem.isRefit)
                        shipAt = Ship.CreateShipAt(queueItem.sData.Name, Owner, this, true, !string.IsNullOrEmpty(queueItem.RefitName) ? queueItem.RefitName : queueItem.sData.Name, queueItem.sData.Level);
                    else
                        shipAt = Ship.CreateShipAt(queueItem.sData.Name, Owner, this, true);
                    ConstructionQueue.QueuePendingRemoval(queueItem);

                    //foreach (string current in shipAt.GetMaxGoods().Keys)
                    //{
                    //    if (ResourcesDict[current] > 0.0f &&  shipAt.GetCargo(current) < shipAt.GetMaxCargo(current))
                    //    {
                    //        ResourcesDict[current] -= 1f;
                    //        shipAt.AddCargo(current, 1);
                    //    }
                    //    else break;
                    //}

                    if (queueItem.sData.Role == ShipData.RoleName.station || queueItem.sData.Role == ShipData.RoleName.platform)
                    {
                        int num = Shipyards.Count / 9;
                        shipAt.Position = Center + MathExt.PointOnCircle(Shipyards.Count * 40, 2000 + 2000 * num * scale);
                        shipAt.Center = shipAt.Position;
                        shipAt.TetherToPlanet(this);
                        Shipyards.Add(shipAt.guid, shipAt);
                    }
                    if (queueItem.Goal != null)
                    {
                        if (queueItem.Goal.GoalName == "BuildConstructionShip")
                        {
                            shipAt.AI.OrderDeepSpaceBuild(queueItem.Goal);
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
                            if (Owner != Empire.Universe.PlayerEmpire)
                                Owner.ForcePoolAdd(shipAt);
                            queueItem.Goal.ReportShipComplete(shipAt);
                        }
                    }
                    else if ((queueItem.sData.Role != ShipData.RoleName.station || queueItem.sData.Role == ShipData.RoleName.platform)
                        && Owner != Empire.Universe.PlayerEmpire)
                        Owner.ForcePoolAdd(shipAt);
                }
                else if (queueItem.isTroop && queueItem.productionTowards >= queueItem.Cost)
                {
                    if (AssignTroopToTile(ResourceManager.CreateTroop(queueItem.troopType, Owner)))
                    {
                        if (queueItem.Goal != null)
                            ++queueItem.Goal.Step;
                        ConstructionQueue.QueuePendingRemoval(queueItem);
                    }
                }
            }
            ConstructionQueue.ApplyPendingRemovals();
        }

        public int EstimatedTurnsTillComplete(QueueItem qItem)
        {
            int num = (int)Math.Ceiling((double)(int)((qItem.Cost - qItem.productionTowards) / NetProductionPerTurn));
            if (NetProductionPerTurn > 0.0)
                return num;
            else
                return 999;
        }

        public float GetMaxProductionPotential()
        {
            float num1 = 0.0f;
            float num2 = MineralRichness * Population / 1000;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.PlusProdPerRichness > 0.0)
                    num1 += building.PlusProdPerRichness * MineralRichness;
                num1 += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0.0)
                    num2 += building.PlusProdPerColonist;
            }
            float num3 = num2 + num1 * Population / 1000;
            float num4 = num3;
            if (Owner.data.Traits.Cybernetic > 0)
                return num4 + Owner.data.Traits.ProductionMod * num4 - consumption;
            return num4 + Owner.data.Traits.ProductionMod * num4;
        }
        public float GetMaxResearchPotential =>        
            (Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn
            * (1+ ResearchPercentAdded
            + Owner.data.Traits.ResearchMod* NetResearchPerTurn);

        public void InitializeSliders(Empire o)
        {
            if (o.data.Traits.Cybernetic == 1 || Type == "Barren")
            {
                FarmerPercentage = 0.0f;
                WorkerPercentage = 0.5f;
                ResearcherPercentage = 0.5f;
            }
            else
            {
                FarmerPercentage = 0.55f;
                ResearcherPercentage = 0.2f;
                WorkerPercentage = 0.25f;
            }
        }

        public bool CanBuildInfantry()
        {
            for (int i = 0; i < BuildingList.Count; i++)
            {
                if (BuildingList[i].AllowInfantry)
                    return true;
            }
            return false;
        }

        public void UpdateIncomes(bool LoadUniverse)
        {
            if (Owner == null)
                return;
            PlusFlatPopulationPerTurn = 0f;
            ShieldStrengthMax = 0f;
            TotalMaintenanceCostsPerTurn = 0f;
            StorageAdded = 0;
            AllowInfantry = false;
            TotalDefensiveStrength = 0;
            GrossFood = 0f;
            PlusResearchPerColonist = 0f;
            PlusFlatResearchPerTurn = 0f;
            PlusFlatProductionPerTurn = 0f;
            PlusProductionPerColonist = 0f;
            FlatFoodAdded = 0f;
            PlusFoodPerColonist = 0f;

            PlusFlatPopulationPerTurn = 0f;
            ShipBuildingModifier = 0f;
            CommoditiesPresent.Clear();
            float shipbuildingmodifier = 1f;
            Array<Guid> list = new Array<Guid>();
            float shipyards =1;
            
            if (!LoadUniverse)
            foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
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
            ShipBuildingModifier = shipbuildingmodifier;
            foreach (Guid key in list)
            {

                Shipyards.Remove(key);
            }
            PlusCreditsPerColonist = 0f;
            MaxPopBonus = 0f;
            PlusTaxPercentage = 0f;
            TerraformToAdd = 0f;
            bool shipyard =false;            
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.WinsGame)
                    HasWinBuilding = true;
                //if (building.NameTranslationIndex == 458)
                if (building.AllowShipBuilding || building.Name == "Space Port" )
                    shipyard= true;
                
                if (building.PlusFlatPopulation > 0)
                    PlusFlatPopulationPerTurn += building.PlusFlatPopulation;
                ShieldStrengthMax += building.PlanetaryShieldStrengthAdded;
                PlusCreditsPerColonist += building.CreditsPerColonist;
                if (building.PlusTerraformPoints > 0)
                    TerraformToAdd += building.PlusTerraformPoints;
                if (building.Strength > 0)
                    TotalDefensiveStrength += building.CombatStrength;
                PlusTaxPercentage += building.PlusTaxPercentage;
                if (building.IsCommodity)
                    CommoditiesPresent.Add(building.Name);
                if (building.AllowInfantry)
                    AllowInfantry = true;
                if (building.StorageAdded > 0)
                    StorageAdded += building.StorageAdded;
                if (building.PlusFoodPerColonist > 0)
                    PlusFoodPerColonist += building.PlusFoodPerColonist;
                if (building.PlusResearchPerColonist > 0)
                    PlusResearchPerColonist += building.PlusResearchPerColonist;
                if (building.PlusFlatResearchAmount > 0)
                    PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                if (building.PlusProdPerRichness > 0)
                    PlusFlatProductionPerTurn += building.PlusProdPerRichness * MineralRichness;
                PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0)
                    PlusProductionPerColonist += building.PlusProdPerColonist;
                if (building.MaxPopIncrease > 0)
                    MaxPopBonus += building.MaxPopIncrease;
                if (building.Maintenance > 0)
                    TotalMaintenanceCostsPerTurn += building.Maintenance;
                FlatFoodAdded += building.PlusFlatFoodAmount;
                RepairPerTurn += building.ShipRepair;
                //Repair if no combat
                if(!RecentCombat)
                {
                    building.CombatStrength = Ship_Game.ResourceManager.BuildingsDict[building.Name].CombatStrength;
                    building.Strength = Ship_Game.ResourceManager.BuildingsDict[building.Name].Strength;
                }
            }
            //Added by Gretman -- This will keep a planet from still having sheilds even after the shield building has been scrapped.
            if (ShieldStrengthCurrent > ShieldStrengthMax) ShieldStrengthCurrent = ShieldStrengthMax;

            if (shipyard && (colonyType != ColonyType.Research || Owner.isPlayer))
                HasShipyard = true;
            else
                HasShipyard = false;
            //Research
            NetResearchPerTurn =  (ResearcherPercentage * Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn + ResearchPercentAdded * NetResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn + Owner.data.Traits.ResearchMod * NetResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn - Owner.data.TaxRate * NetResearchPerTurn;
            //Food
            NetFoodPerTurn =  (FarmerPercentage * Population / 1000 * (Fertility + PlusFoodPerColonist)) + FlatFoodAdded;
            NetFoodPerTurn = NetFoodPerTurn + FoodPercentAdded * NetFoodPerTurn;
            GrossFood = NetFoodPerTurn;
            //Production
            NetProductionPerTurn =  (WorkerPercentage * Population / 1000f * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            NetProductionPerTurn = NetProductionPerTurn + Owner.data.Traits.ProductionMod * NetProductionPerTurn;
            if (Owner.data.Traits.Cybernetic > 0)
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * (NetProductionPerTurn - consumption) ;
            else
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * NetProductionPerTurn;

            GrossProductionPerTurn =  (Population / 1000  * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            GrossProductionPerTurn = GrossProductionPerTurn + Owner.data.Traits.ProductionMod * GrossProductionPerTurn;


            if (Station != null && !LoadUniverse)
            {
                if (!HasShipyard)
                    Station.SetVisibility(false, Empire.Universe.ScreenManager, this);
                else
                    Station.SetVisibility(true, Empire.Universe.ScreenManager, this);
            }

            consumption =  (Population / 1000 + Owner.data.Traits.ConsumptionModifier * Population / 1000);
            if(Owner.data.Traits.Cybernetic >0)
            {
                if(Population > 0.1 && NetProductionPerTurn <= 0)
                {

                }
            }
            //Money
            GrossMoneyPT = Population / 1000f;
            GrossMoneyPT += PlusTaxPercentage * GrossMoneyPT;
            //this.GrossMoneyPT += this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            //this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
            MAX_STORAGE = StorageAdded;
            if (MAX_STORAGE < 10)
                MAX_STORAGE = 10f;
        }

        private void HarvestResources()
        {
            unfed = 0.0f;
            if (Owner.data.Traits.Cybernetic > 0 )
            {
                FoodHere = 0.0f;
                NetProductionPerTurn -= consumption;

                 if(NetProductionPerTurn < 0f)
                    ProductionHere += NetProductionPerTurn;
                
              if (ProductionHere > MAX_STORAGE)
                {
                    unfed = 0.0f;
                    ProductionHere = MAX_STORAGE;
                }
                else if (ProductionHere < 0 )
                {

                    unfed = ProductionHere;
                    ProductionHere = 0.0f;
                }
            }
            else
            {
                NetFoodPerTurn -= consumption;
                FoodHere += NetFoodPerTurn;
                if (FoodHere > MAX_STORAGE)
                {
                    unfed = 0.0f;
                    FoodHere = MAX_STORAGE;
                }
                else if (FoodHere < 0 )
                {
                    unfed = FoodHere;
                    FoodHere = 0.0f;
                }
            }
            foreach (Building building1 in BuildingList)
            {
                if (building1.ResourceCreated != null)
                {
                    if (building1.ResourceConsumed != null)
                    {
                        if (ResourcesDict[building1.ResourceConsumed] >=  building1.ConsumptionPerTurn)
                        {
                            Map<string, float> dictionary1;
                            string index1;
                            (dictionary1 = ResourcesDict)[index1 = building1.ResourceConsumed] = dictionary1[index1] - building1.ConsumptionPerTurn;
                            Map<string, float> dictionary2;
                            string index2;
                            (dictionary2 = ResourcesDict)[index2 = building1.ResourceCreated] = dictionary2[index2] + building1.OutputPerTurn;
                        }
                    }
                    else if (building1.CommodityRequired != null)
                    {
                        if (CommoditiesPresent.Contains(building1.CommodityRequired))
                        {
                            foreach (Building building2 in BuildingList)
                            {
                                if (building2.IsCommodity && building2.Name == building1.CommodityRequired)
                                {
                                    Map<string, float> dictionary;
                                    string index;
                                    (dictionary = ResourcesDict)[index = building1.ResourceCreated] = dictionary[index] + building1.OutputPerTurn;
                                }
                            }
                        }
                    }
                    else
                    {
                        Map<string, float> dictionary;
                        string index;
                        (dictionary = ResourcesDict)[index = building1.ResourceCreated] = dictionary[index] + building1.OutputPerTurn;
                    }
                }
            }
        }

        public int GetGoodAmount(string good)
        {
            return (int)ResourcesDict[good];
        }

        private void GrowPopulation()
        {
            if (Owner == null)
                return;
            
            float num1 = Owner.data.BaseReproductiveRate * Population;
            if ( num1 > Owner.data.Traits.PopGrowthMax * 1000  && Owner.data.Traits.PopGrowthMax != 0 )
                num1 = Owner.data.Traits.PopGrowthMax * 1000f;
            if ( num1 < Owner.data.Traits.PopGrowthMin * 1000 )
                num1 = Owner.data.Traits.PopGrowthMin * 1000f;
            float num2 = num1 + PlusFlatPopulationPerTurn;
            float num3 = num2 + Owner.data.Traits.ReproductionMod * num2;
            if ( Math.Abs(unfed) <= 0 )
            {

                Population += num3;
                if (Population +  num3 > MaxPopulation + MaxPopBonus)
                    Population = MaxPopulation + MaxPopBonus;
            }
            else
                Population += unfed * 10f;
            if (Population >= 100.0)
                return;
            Population = 100f;
        }

        public float CalculateGrowth(float EstimatedFoodGain)
        {
            if (Owner != null)
            {
                float num1 = Owner.data.BaseReproductiveRate * Population;
                if ( num1 > Owner.data.Traits.PopGrowthMax)
                    num1 = Owner.data.Traits.PopGrowthMax;
                if ( num1 < Owner.data.Traits.PopGrowthMin)
                    num1 = Owner.data.Traits.PopGrowthMin;
                float num2 = num1 + PlusFlatPopulationPerTurn;
                float num3 = num2 + Owner.data.Traits.ReproductionMod * num2;
                if (Owner.data.Traits.Cybernetic > 0)
                {
                    if (ProductionHere + NetProductionPerTurn - consumption <= 0.1)
                        return -(Math.Abs(ProductionHere + NetProductionPerTurn - consumption) / 10f);
                    if (Population < MaxPopulation + MaxPopBonus && Population +  num3 < MaxPopulation + MaxPopBonus)
                        return Owner.data.BaseReproductiveRate * Population;
                }
                else
                {
                    if (FoodHere + NetFoodPerTurn - consumption <= 0f)
                        return -(Math.Abs(FoodHere + NetFoodPerTurn - consumption) / 10f);
                    if (Population < MaxPopulation + MaxPopBonus && Population +  num3 < MaxPopulation + MaxPopBonus)
                        return Owner.data.BaseReproductiveRate * Population;
                }
            }
            return 0.0f;
        }

        private void CreatePlanetSceneObject(GameScreen screen)
        {
            if (SO != null)
                screen?.RemoveObject(SO);

            SO = ResourceManager.GetPlanetarySceneMesh("Model/SpaceObjects/planet_" + planetType);
            SO.World = Matrix.CreateScale(scale * 3)
                     * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            RingWorld = Matrix.CreateRotationX(ringTilt.ToRadians())
                      * Matrix.CreateScale(5f)
                      * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            screen?.AddObject(SO);
        }

        public void InitializePlanetMesh(GameScreen screen)
        {
            Shield = ShieldManager.AddPlanetaryShield(Center);
            UpdateDescription();
            CreatePlanetSceneObject(screen);

            GravityWellRadius = (float)(GlobalStats.GravityWellRange * (1 + ((Math.Log(scale)) / 1.5)));
        }

        public void AddGood(string goodId, int Amount)
        {
            if (ResourcesDict.ContainsKey(goodId))
                ResourcesDict[goodId] += Amount;
            else
                ResourcesDict.Add(goodId, Amount);
        }

        private void UpdatePosition(float elapsedTime)
        {
            Zrotate += ZrotateAmount * elapsedTime;
            if (!Empire.Universe.Paused)
            {
                OrbitalAngle += (float)Math.Asin(15.0 / OrbitalRadius);
                if (OrbitalAngle >= 360.0f)
                    OrbitalAngle -= 360f;
            }
            PosUpdateTimer -= elapsedTime;
            if (PosUpdateTimer <= 0.0f || ParentSystem.isVisible)
            {
                PosUpdateTimer = 5f;
                Center = ParentSystem.Position.PointOnCircle(OrbitalAngle, OrbitalRadius);
            }
            if (ParentSystem.isVisible)
            {
                BoundingSphere boundingSphere = new BoundingSphere(new Vector3(Center, 0.0f), 300000f);
                //System.Threading.Tasks.Parallel.Invoke(() =>
                {
                    SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(scale) * Matrix.CreateRotationZ(-Zrotate) * Matrix.CreateRotationX(-45f.ToRadians()) * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                }//,
                //() =>
                {
                    cloudMatrix = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(scale) * Matrix.CreateRotationZ((float)(-Zrotate / 1.5)) * Matrix.CreateRotationX(-45f.ToRadians()) * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                }//,
                //() =>
                {
                    RingWorld = Matrix.Identity * Matrix.CreateRotationX(ringTilt.ToRadians()) * Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                }
                //);




                SO.Visibility = ObjectVisibility.Rendered;
            }
            else
                SO.Visibility = ObjectVisibility.None;
        }



        private void UpdateDescription()
        {
            if (SpecialDescription != null)
            {
                Description = SpecialDescription;
            }
            else
            {
                Description = "";
                Planet planet1 = this;
                string str1 = planet1.Description + Name + " " + planetComposition + ". ";
                planet1.Description = str1;
                if (Fertility > 2 )
                {
                    if (planetType == 21)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1729);
                        planet2.Description = str2;
                    }
                    else if (planetType == 13 || planetType == 22)
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
                else if (Fertility > 1 )
                {
                    if (planetType == 19)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1732);
                        planet2.Description = str2;
                    }
                    else if (planetType == 21)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1733);
                        planet2.Description = str2;
                    }
                    else if (planetType == 13 || planetType == 22)
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
                else if (Fertility > 0.6f)
                {
                    if (planetType == 14)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1736);
                        planet2.Description = str2;
                    }
                    else if (planetType == 21)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1737);
                        planet2.Description = str2;
                    }
                    else if (planetType == 17)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1738);
                        planet2.Description = str2;
                    }
                    else if (planetType == 19)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1739);
                        planet2.Description = str2;
                    }
                    else if (planetType == 18)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1740);
                        planet2.Description = str2;
                    }
                    else if (planetType == 11)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1741);
                        planet2.Description = str2;
                    }
                    else if (planetType == 13 || planetType == 22)
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
                    double num = RandomMath.RandomBetween(1f, 2f);
                    if (planetType == 9 || planetType == 23)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1744);
                        planet2.Description = str2;
                    }
                    else if (planetType == 20 || planetType == 15)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1745);
                        planet2.Description = str2;
                    }
                    else if (planetType == 17)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1746);
                        planet2.Description = str2;
                    }
                    else if (planetType == 18)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1747);
                        planet2.Description = str2;
                    }
                    else if (planetType == 11)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1748);
                        planet2.Description = str2;
                    }
                    else if (planetType == 14)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1749);
                        planet2.Description = str2;
                    }
                    else if (planetType == 2 || planetType == 6 || planetType == 10)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1750);
                        planet2.Description = str2;
                    }
                    else if (planetType == 3 || planetType == 4 || planetType == 16)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1751);
                        planet2.Description = str2;
                    }
                    else if (planetType == 1)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1752);
                        planet2.Description = str2;
                    }
                    else if (habitable)
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
                if (Fertility < 0.6f && MineralRichness >= 2 && habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1754);
                    planet2.Description = str2;
                    if (MineralRichness > 3 )
                    {
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1755);
                        planet3.Description = str3;
                    }
                    else if (MineralRichness >= 2 )
                    {
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1756);
                        planet3.Description = str3;
                    }
                    else
                    {
                        if (MineralRichness < 1 )
                            return;
                        Planet planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1757);
                        planet3.Description = str3;
                    }
                }
                else if (MineralRichness > 3  && habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1758);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 2  && habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1759);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 1 && habitable)
                {
                    Planet planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1760);
                    planet2.Description = str2;
                }
                else
                {
                    if (MineralRichness >= 1 || !habitable)
                        return;
                    if (planetType == 14)
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1761);
                        planet2.Description = str2;
                    }
                    else
                    {
                        Planet planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1762);
                        planet2.Description = str2;
                    }
                }
            }
        }

        private void UpdateDevelopmentStatus()
        {
            Density = Population / 1000f;
            float num = MaxPopulation / 1000f;
            if (Density <= 0.5f)
            {
                developmentLevel = 1;
                DevelopmentStatus = Localizer.Token(1763);
                if ( num >= 2  && Type != "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1764);
                    planet.DevelopmentStatus = str;
                }
                else if ( num >= 2f && Type == "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1765);
                    planet.DevelopmentStatus = str;
                }
                else if (num < 0 && Type != "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1766);
                    planet.DevelopmentStatus = str;
                }
                else if ( num < 0.5f && Type == "Barren")
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1767);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 0.5f && Density <= 2 )
            {
                developmentLevel = 2;
                DevelopmentStatus = Localizer.Token(1768);
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
            else if (Density > 2.0 && Density <= 5.0)
            {
                developmentLevel = 3;
                DevelopmentStatus = Localizer.Token(1771);
                if (num >= 5.0)
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1772);
                    planet.DevelopmentStatus = str;
                }
                else if (num < 5.0)
                {
                    Planet planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1773);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 5.0 && Density <= 10.0)
            {
                developmentLevel = 4;
                DevelopmentStatus = Localizer.Token(1774);
            }
            else if (Density > 10.0)
            {
                developmentLevel = 5;
                DevelopmentStatus = Localizer.Token(1775);
            }
            if (NetProductionPerTurn >= 10.0 && HasShipyard)
            {
                Planet planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1776);
                planet.DevelopmentStatus = str;
            }
            else if (Fertility >= 2.0 && NetFoodPerTurn > (double)MaxPopulation)
            {
                Planet planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1777);
                planet.DevelopmentStatus = str;
            }
            else if (NetResearchPerTurn > 5.0)
            {
                Planet planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1778);
                planet.DevelopmentStatus = str;
            }
            if (!AllowInfantry || TroopsHere.Count <= 6)
                return;
            Planet planet1 = this;
            string str1 = planet1.DevelopmentStatus + Localizer.Token(1779);
            planet1.DevelopmentStatus = str1;
        }

        //added by gremlin: get a planets ground combat strength
        public float GetGroundStrength(Empire empire)
        {
            float num = 0;
            if (Owner == empire)
                num += BuildingList.Sum(offense => offense.CombatStrength);
            using (TroopsHere.AcquireReadLock())
                num += TroopsHere.Where(empiresTroops => empiresTroops.GetOwner() == empire).Sum(strength => strength.Strength);
            return num;


        }
        public int GetPotentialGroundTroops()
        {
            int num = 0;
            
            foreach(PlanetGridSquare PGS in TilesList)
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
            TroopsHere.ForEach(trooper =>
            {

                if (trooper.GetOwner() != AllButThisEmpire)
                {
                    EnemyTroopStrength += trooper.Strength;
                }
            });
            for(int i =0; i< BuildingList.Count;i++)
            {
                Building b;
                try
                {
                    b = BuildingList[i];
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
            using (TroopsHere.AcquireReadLock())
            foreach (Troop trooper in TroopsHere)
            {
                if (!empire.TryGetRelations(trooper.GetOwner(), out Relationship trouble) || trouble.AtWar)
                {
                    enemies=true;
                    break;
                }

            }
            return enemies;
        }
        public bool EventsOnBuildings()
        {
            bool events = false;            
            foreach (Building building in BuildingList)
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
            int spotCount = TilesList.Where(spots => spots.building == null).Sum(spots => spots.number_allowed_troops);            
            int troops = TroopsHere.Where(owner=> owner.GetOwner() == Owner).Count();
            return spotCount - troops;


        }
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake)
        {
            var troops = new Array<Troop>();
            foreach (Troop troop in TroopsHere)
            {
                if (troop.GetOwner() != empire) continue;

                if (maxToTake-- < 0)
                    troops.Add(troop);
            }
            return troops;
        }
        //Added by McShooterz: heal builds and troops every turn
        public void HealTroops()
        {
            if (RecentCombat)
                return;
            using (TroopsHere.AcquireReadLock())
                foreach (Troop troop in TroopsHere)
                    troop.Strength = troop.GetStrengthMax();
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
            ActiveCombats?.Dispose(ref ActiveCombats);
            OrbitalDropList?.Dispose(ref OrbitalDropList);
            ConstructionQueue?.Dispose(ref ConstructionQueue);
            BasedShips?.Dispose(ref BasedShips);
            Projectiles?.Dispose(ref Projectiles);
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
