using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;
using System;

namespace Ship_Game.Universe.SolarBodies // Fat Bastard - Refactored March 21, 2019
{
    public class GeodeticManager
    {
        private float SystemCombatTimer;
        private readonly Planet P;
        private float Population  => P.Population;
        private Empire Owner      => P.Owner;
        private Shield Shield     => P.Shield;
        private Vector2 Center    => P.Center;
        private SceneObject SO    => P.SO;
        private bool HasSpacePort => P.HasSpacePort;
        private int Level         => P.Level;
        private Map<Guid,Ship> Stations      => P.OrbitalStations;
        private float RepairPerTurn          => P.RepairPerTurn;
        private SolarSystem ParentSystem     => P.ParentSystem;
        private int TurnsSinceTurnover       => P.TurnsSinceTurnover;
        private float ShieldStrengthCurrent  => P.ShieldStrengthCurrent;
        private Array<PlanetGridSquare> TilesList        => P.TilesList;
        private BatchRemovalCollection<Troop> TroopsHere => P.TroopsHere;

        public GeodeticManager (Planet planet)
        {
            P = planet;
        }

        private int NumShipYards
        {
            get
            {
                int shipYardCount = 0;
                foreach (var shipYard in Stations)
                {
                    if (!shipYard.Value.shipData.IsShipyard)
                        continue;

                    shipYardCount++;
                }

                return shipYardCount;
            }
        }

        public void Update(float elapsedTime)
        {
            if (P.ParentSystem.HostileForcesPresent(Owner))
                SystemCombatTimer += elapsedTime;
            else
                SystemCombatTimer = 0f;
        }

        public void DropBomb(Bomb bomb)
        {
            if (bomb.Owner == Owner)
                return; // No friendly_fire

            DeclareWarOnBombingEmpire(bomb);
            P.SetInGroundCombat();
            if (ShieldStrengthCurrent > 0f)
                DamageColonyShields(bomb);
            else
            {
                P.ApplyBombEnvEffects(bomb.PopKilled);
                bomb.SurfaceImpactEffects();
                var orbitalDrop = new OrbitalDrop
                {
                    Target = RandomMath.RandItem(TilesList),
                    Surface = P
                };
                orbitalDrop.DamageColonySurface(bomb);
                bomb.PlayCombatScreenEffects(P, orbitalDrop);
                if (Population <= 0f)
                {
                    P.WipeOutColony();
                    return;
                }

                bomb.ResolveSpecialBombActions(P);
            }
        }

        private void DeclareWarOnBombingEmpire(Bomb bomb)
        {
            if (Owner != null && !Owner.GetRelations(bomb.Owner).AtWar
                              && TurnsSinceTurnover > 10
                              && Empire.Universe.PlayerEmpire == bomb.Owner)
                Owner.GetEmpireAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
        }

        private void DamageColonyShields(Bomb bomb)
        {
            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView 
                && Empire.Universe.Frustum.Contains(P.Center, P.OrbitalRadius * 2))
                    Shield.HitShield(P, bomb, Center, SO.WorldBoundingSphere.Radius + 100f);

            P.ShieldStrengthCurrent = Math.Max(P.ShieldStrengthCurrent - bomb.HardDamageMax, 0);
        }

        public void AffectNearbyShips() // Refactored by Fat Bastard - 23, July 2018
        {
            float repairPool = CalcRepairPool();
            // FB: I added here a minimum threshold of 5 troops to stay as garrison so the LoadTroops wont clean the colony
            // But this should be made at a button for the player to decide how many troops he wants to leave as a garrison
            // in ship colony screen (Issue #1626)
            int garrisonSize = P.Owner.isPlayer ? 5 : 0;

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
                float supply = Level;
                supply      *= HasSpacePort ? 5f : 2f;
                supply      *= ship.InCombat ? 0.1f : 10f;
                supply       = Math.Max(.1f, supply);
                ship.AddPower(supply);
                ship.ChangeOrdnance(supply);
            }
        }

        private float CalcRepairPool()
        {
            float repairPool = Level * RepairPerTurn * 10 * (2 - P.ShipBuildingModifier);
            foreach (Ship station in Stations.Values)
                repairPool += station.RepairRate;

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

            int repairLevel = Level + NumShipYards;
            ship.ApplyAllRepair(repairPool, repairLevel, repairShields: true);
        }

        private void LoadTroops(Ship ship, int garrisonSize)
        {
            if (TroopsHere.Count <= garrisonSize || ship.TroopCapacity == 0 || ship.TroopCapacity <= ship.TroopList.Count || ship.InCombat)
                return;

            int troopCount = ship.Carrier.NumTroopsInShipAndInSpace;
            using (TroopsHere.AcquireWriteLock())
            {
                if ((ship.InCombat && ParentSystem.HostileForcesPresent(ship.loyalty)) 
                    || TroopsHere.IsEmpty
                    || TroopsHere.Any(troop => troop.Loyalty != Owner))
                    return;

                foreach (PlanetGridSquare pgs in TilesList)
                {
                    if (troopCount >= ship.TroopCapacity || TroopsHere.Count <= garrisonSize)
                        break;

                    using (pgs.TroopsHere.AcquireWriteLock())
                    {
                        if (pgs.TroopsAreOnTile && pgs.SingleTroop.Loyalty == Owner)
                        {
                            Troop troop = pgs.SingleTroop;
                            ship.TroopList.Add(troop);
                            pgs.TroopsHere.Clear();
                            TroopsHere.Remove(troop);
                        }
                    }
                }
            }
        }

        private void AddTroopsForFactions(Ship ship)
        {
            // @todo FB - need to separate this to a method which will return a troop based on faction
            if ((SystemCombatTimer % 30).AlmostZero()  && ship.TroopCapacity > ship.TroopList.Count)
                ship.TroopList.Add(ResourceManager.CreateTroop("Wyvern", ship.loyalty));
        }
    }
}