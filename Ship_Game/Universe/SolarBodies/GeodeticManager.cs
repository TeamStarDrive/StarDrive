using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;
using System;

namespace Ship_Game.Universe.SolarBodies
{
    public class GeodeticManager
    {
        private readonly Planet SolarSystemBody;
        private Array<PlanetGridSquare> TilesList => SolarSystemBody.TilesList;
        private Empire Owner => SolarSystemBody.Owner;
        private BatchRemovalCollection<Troop> TroopsHere => SolarSystemBody.TroopsHere;
        private Array<Building> BuildingList => SolarSystemBody.BuildingList;        
        private SolarSystem ParentSystem => SolarSystemBody.ParentSystem;
        private int TurnsSinceTurnover => SolarSystemBody.TurnsSinceTurnover;
        private float ShieldStrengthCurrent => SolarSystemBody.ShieldStrengthCurrent;
        //private readonly TroopManager TroopManager;
        private float Population => SolarSystemBody.Population;
        private Shield Shield => SolarSystemBody.Shield;
        private Vector2 Center => SolarSystemBody.Center;
        private SceneObject SO => SolarSystemBody.SO;
        private bool HasSpacePort => SolarSystemBody.HasShipyard;
        private int DevelopmentLevel => SolarSystemBody.DevelopmentLevel;
        private Map<Guid,Ship> Stations => SolarSystemBody.Shipyards;
        private float RepairPerTurn => SolarSystemBody.RepairPerTurn;        
        
        public GeodeticManager (Planet planet)
        {
            SolarSystemBody = planet;
        }
        private int CountShipYards()
        {
            int shipYardCount =0;
            foreach (var shipYard in Stations)
            {
                if (!shipYard.Value.shipData.IsShipyard) continue;
                shipYardCount++;
            }
            return shipYardCount;
        }
        public void Update(float elaspedTime)
        {
            
        }

        private void PlayPlanetSfx(string name, Vector3 position) => SolarSystemBody.PlayPlanetSfx(name, position);

        private void ApplyBombEnvEffects(float amount) // added by Fat Bastard
        {
            SolarSystemBody.Population -= 1000f * amount;
            SolarSystemBody.ChangeFertility(-(amount / 2));
            if (SolarSystemBody.Fertility > 0)
                return;

            float envDestructionRoll = RandomMath.RandomBetween(0f, 100f);
            if (envDestructionRoll > amount * 250)
                return;

            SolarSystemBody.ChangeMaxFertility(-0.02f);
            SolarSystemBody.DegradePlanetType();
        }

        public void DropBomb(Bomb bomb)
        {
            if (bomb.Owner == Owner)
                return;

            if (Owner != null && !Owner.GetRelations(bomb.Owner).AtWar && TurnsSinceTurnover > 10 && Empire.Universe.PlayerEmpire == bomb.Owner)
<<<<<<< local
            {
                Owner.GetEmpireAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
            }
=======
                Owner.GetGSAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);

>>>>>>> other
            SolarSystemBody.SetInGroundCombat();
            if (ShieldStrengthCurrent <= 0f)
            {
                float popKilled    = ResourceManager.WeaponsDict[bomb.WeaponName].BombPopulationKillPerHit;
                float ran          = RandomMath.RandomBetween(0f, 100f);
                bool hit           = !(ran < 75f);

                ApplyBombEnvEffects(popKilled);
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
                var od = new OrbitalDrop();
                var potentialHits = new Array<PlanetGridSquare>();
                if (hit)
                {                    
                    foreach (PlanetGridSquare pgs in TilesList)
                    {
                        if (pgs.building == null && pgs.TroopsHere.Count <= 0)                        
                            continue;
                        
                        potentialHits.Add(pgs);                        
                    }
                    if (potentialHits.Count <= 0)
                    {
                        hit = false;
                        if (BuildingList.Count > 0)
                            BuildingList.Clear();
                    }
                    else
                    {
                        int ranhit = (int)RandomMath.RandomBetween(0f, potentialHits.Count + 1f);
                        if (ranhit > potentialHits.Count - 1)
                            ranhit = potentialHits.Count - 1;

                        od.Target = potentialHits[ranhit];
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
                            continue;

                        od.Target = pgs;
                        break;
                    }
                }
                int troopDamageMin    = ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Min;
                int troopDamageMax    = ResourceManager.WeaponsDict[bomb.WeaponName].BombTroopDamage_Max;
                int buildingDamageMin = ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMin;
                int buildingDamageMax = ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMax;
                if (od.Target.TroopsHere.Count > 0)
                {
                    Troop item = od.Target.TroopsHere[0];
                    item.Strength = item.Strength - (int)RandomMath.RandomBetween(troopDamageMin, troopDamageMax);
                    if (od.Target.TroopsHere[0].Strength <= 0)
                    {
                        TroopsHere.Remove(od.Target.TroopsHere[0]);
                        od.Target.TroopsHere.Clear();
                    }
                }
                else if (od.Target.building != null)
                {
                    Building target = od.Target.building;
                    target.Strength = target.Strength - (int)RandomMath.RandomBetween(buildingDamageMin, buildingDamageMax);
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
                                SolarSystemBody.Population -= od.Target.building.MaxPopIncrease;
                                BuildingList.Remove(od.Target.building);
                                od.Target.building = null;
                            }
                        }
                    }
                }
                if (Empire.Universe.workersPanel is CombatScreen && Empire.Universe.LookingAtPlanet && (Empire.Universe.workersPanel as CombatScreen).p == SolarSystemBody)
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
                    SolarSystemBody.Population = 0f;
                    if (Owner != null)
                    {
                        Owner.RemovePlanet(SolarSystemBody);
                        if (SolarSystemBody.IsExploredBy(Empire.Universe.PlayerEmpire))
                        {
                            Empire.Universe.NotificationManager.AddPlanetDiedNotification(SolarSystemBody, Empire.Universe.PlayerEmpire);
                        }
                        bool removeowner = true;
                        if (Owner != null)
                        {
                            foreach (Planet other in ParentSystem.PlanetList)
                            {
                                if (other.Owner != Owner || other == SolarSystemBody)
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
                        SolarSystemBody.ConstructionQueue.Clear();
                        SolarSystemBody.Owner = null;
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
                                    Game1.Instance.SetSteamAchievment("Owlwoks_Freed");                                   
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
                if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView &&
                    Empire.Universe.Frustum.Contains(SolarSystemBody.Center, SolarSystemBody.OrbitalRadius * 2))
                {
                    Shield.HitShield(SolarSystemBody, bomb, Center, SO.WorldBoundingSphere.Radius + 100f);
                }

                SolarSystemBody.ShieldStrengthCurrent -= ResourceManager.WeaponsDict[bomb.WeaponName].BombHardDamageMax;
                if (SolarSystemBody.ShieldStrengthCurrent < 0f)
                    SolarSystemBody.ShieldStrengthCurrent = 0f;
                
            }
        }

        public void AffectNearbyShips() // Refactored by Fat Bastard - 23, July 2018
        {
            float repairPool = CalcRepairPool();
            // FB: I added here a minimum threshold of 5 troops to stay as garrison so the LoadTroops wont clean the colony
            // But this should be made at a button for the player to decide how many troops he wants to leave as a garrison in ship colony screen
            int garrisonSize = SolarSystemBody.Owner.isPlayer ? 5 : 0;

            for (int i = 0; i < ParentSystem.ShipList.Count; i++)
            {
                Ship ship         = ParentSystem.ShipList[i];
                bool loyaltyMatch = ship.loyalty == Owner;

                if (ship.loyalty.isFaction)
                    AddTroopsForFactions(ship);

                if (loyaltyMatch && ship.Position.InRadius(Center, 5000f))
                {
                    LoadTroops(ship, garrisonSize);
                    SupplyShip(ship);
                    RepairShip(ship, repairPool);
                }
            }
        }

        private void SupplyShip(Ship ship)
        {
            if (ship.shipData.Role == ShipData.RoleName.platform) // platforms always get max ordnance to retain platforms Vanilla functionality
            {
                ship.ChangeOrdnance(ship.OrdinanceMax);
                ship.AddPower(ship.PowerStoreMax);
            }
            else
            {
                float supply = DevelopmentLevel;
                supply      *= HasSpacePort ? 5f : 2f;
                supply      *= ship.InCombat ? 0.1f : 10f;
                supply       = Math.Max(.1f, supply);
                ship.AddPower(supply);
                ship.ChangeOrdnance(supply);
            }
        }

        private float CalcRepairPool()
        {
            float repairPool = DevelopmentLevel * RepairPerTurn * 10 * (2 - SolarSystemBody.ShipBuildingModifier);
            foreach (Ship station in Stations.Values)
            {
                repairPool += station.RepairRate;
            }
            return repairPool;
        }

        private void RepairShip(Ship ship, float repairPool)
        {
            ship.AI.TerminateResupplyIfDone();
            //Modified by McShooterz: Repair based on repair pool, if no combat in system
            if (!HasSpacePort || ship.Health.AlmostEqual(ship.HealthMax))
                return;

            if (ship.InCombat)
                repairPool /= 10; // allow minimal repair for ships near space port even in combat, per turn (good for ships which lost command modules)

            int repairLevel = SolarSystemBody.DevelopmentLevel + CountShipYards();
            ship.ApplyAllRepair(repairPool, repairLevel, repairShields: true);
        }

        private void LoadTroops(Ship ship, int garrisonSize)
        {
            if (TroopsHere.Count <= garrisonSize || ship.TroopCapacity == 0 || ship.TroopCapacity <= ship.TroopList.Count || ship.InCombat)
                return;

            int troopCount = ship.Carrier.NumTroopsInShipAndInSpace;
            using (TroopsHere.AcquireWriteLock())
            {
                if ((ParentSystem.combatTimer > 0 && ship.InCombat) || TroopsHere.IsEmpty
                    || TroopsHere.Any(troop => troop.GetOwner() != Owner))
                    return;
                foreach (var pgs in TilesList)
                {
                    if (troopCount >= ship.TroopCapacity || TroopsHere.Count <= garrisonSize)
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

        private void AddTroopsForFactions(Ship ship)
        {
            if (ParentSystem.combatTimer < -30 && ship.TroopCapacity > ship.TroopList.Count)
            {
                ship.TroopList.Add(ResourceManager.CreateTroop("Wyvern", ship.loyalty));
                ParentSystem.combatTimer = 0;
            }
            /* FB: this code is unclear, why is it being run for all planets with pop > 0 ?
            if (ship.Carrier.HasTroopBays)
            {
                if (Population > 0)
                {
                    if (ship.TroopCapacity > ship.TroopList.Count)
                    {
                        ship.TroopList.Add(ResourceManager.CreateTroop("Wyvern", ship.loyalty));
                    }
                    if (Owner != null && Population > 0)
                    {
                        SolarSystemBody.Population *= .5f;
                        SolarSystemBody.Population -= 1000;
                        SolarSystemBody.ProductionHere *= .5f;
                        SolarSystemBody.FoodHere *= .5f;
                    }
                    if (Population < 0)
                        SolarSystemBody.Population = 0;
                }
                else if (ParentSystem.combatTimer < -30 && ship.TroopCapacity > ship.TroopList.Count)
                {
                    ship.TroopList.Add(ResourceManager.CreateTroop("Wyvern", ship.loyalty));
                    ParentSystem.combatTimer = 0;
                }
            }
            */
        }
    }
}