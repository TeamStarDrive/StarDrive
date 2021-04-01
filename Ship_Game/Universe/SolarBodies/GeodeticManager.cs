using Microsoft.Xna.Framework;
using Ship_Game.Sensors;
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
        private int NumShipYards  => P.OrbitalStations.Count(s => s.shipData.IsShipyard);
        private float RepairPerTurn          => P.RepairPerTurn;
        private SolarSystem ParentSystem     => P.ParentSystem;
        private int TurnsSinceTurnover       => P.TurnsSinceTurnover;
        private float ShieldStrengthCurrent  => P.ShieldStrengthCurrent;
        private Array<PlanetGridSquare> TilesList => P.TilesList;

        public GeodeticManager (Planet planet)
        {
            P = planet;
            //AllNearShips = new BasicSensors(this,P.Owner);
        }

        public void Update(FixedSimTime timeStep)
        {
            if (P.ParentSystem.DangerousForcesPresent(Owner))
                SystemCombatTimer += timeStep.FixedTime;
            else
                SystemCombatTimer = 0f;
        }

        public void DropBomb(Bomb bomb)
        {
            if (bomb.Owner == Owner)
                return; // No friendly_fire

            DeclareWarOnBombingEmpire(bomb);
            P.SetInGroundCombat(Owner);

            if (ShieldStrengthCurrent > 0f)
                DamageColonyShields(bomb);
            else
            {
                var orbitalDrop = new OrbitalDrop
                {
                    TargetTile = SelectTargetTile(bomb),
                    Surface = P
                };

                Owner?.CreateAssaultBombersGoal(bomb.Owner, P);
                orbitalDrop.DamageColonySurface(bomb);
                bomb.PlayCombatScreenEffects(P, orbitalDrop);
                if (Population <= 0f)
                {
                    P.WipeOutColony(bomb.Owner);
                    return;
                }

                bomb.ResolveSpecialBombActions(P); // This is for "Free Owlwoks" bomb
            }
        }

        private PlanetGridSquare SelectTargetTile(Bomb bomb)
        {
            // check for buildings as well, if bombing enemy planet
            var priorityTargets = bomb.Owner == P.Owner ? TilesList.Filter(t => t.EnemyTroopsHere(bomb.Owner))
                                                        : TilesList.Filter(t => t.CombatBuildingOnTile || t.EnemyTroopsHere(bomb.Owner)); 

            // If there are priority targets, choose one of them.
            return priorityTargets.Length > 0 ? priorityTargets.RandItem() 
                                              : TilesList.RandItem();
        }

        private void DeclareWarOnBombingEmpire(Bomb bomb)
        {
            if (Owner != null && !Owner.IsAtWarWith(bomb.Owner)
                              && TurnsSinceTurnover > 10
                              && Empire.Universe.PlayerEmpire == bomb.Owner)
            {
                Owner.GetEmpireAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
            }
        }

        private void DamageColonyShields(Bomb bomb)
        {
            if (Empire.Universe.IsSystemViewOrCloser
                && Empire.Universe.Frustum.Contains(P.Center, P.OrbitalRadius * 2))
            {
                Shield.HitShield(P, bomb, Center, SO.WorldBoundingSphere.Radius + 100f);
            }

            P.ShieldStrengthCurrent = Math.Max(P.ShieldStrengthCurrent - bomb.HardDamageMax, 0);
        }

        void AssignPlanetarySupply()
        {
            int remainingSupplyShuttles = P.NumSupplyShuttlesCanLaunch();
            if (remainingSupplyShuttles <= 0)
                return; // Maximum supply ships launched

            if (!P.TryGetShipsNeedRearm(out Ship[] ourShipsNeedRearm, Owner))
                return;

            for (int i = 0; i < ourShipsNeedRearm.Length && remainingSupplyShuttles-- > 0; i++)
               Owner.GetEmpireAI().AddPlanetaryRearmGoal(ourShipsNeedRearm[i], P);
        }

        public void AffectNearbyShips() // Refactored by Fat Bastard - 23, July 2018
        {
            AssignPlanetarySupply();
            float repairPool = CalcRepairPool();
            bool spaceCombat = P.SpaceCombatNearPlanet;
            for (int i = 0; i < ParentSystem.ShipList.Count; i++)
            {
                Ship ship = ParentSystem.ShipList[i];
                if (ship == null)
                    continue;

                bool loyaltyMatch = ship.loyalty == Owner || ship.loyalty.IsAlliedWith(Owner);
                if (ship.loyalty.isFaction)
                    AddTroopsForFactions(ship);

                if (loyaltyMatch)
                {
                    if (ship.Position.InRadius(Center, 5000f) || ship.GetTether() == P)
                    {
                        SupplyShip(ship);
                        RepairShip(ship, repairPool);
                        if (!spaceCombat && ship.loyalty == Owner) // dont do this for allies
                        {
                            LoadTroops(ship, P.NumTroopsCanLaunch);
                            DisengageTroopsFromCapturedShips(ship);
                        }
                    }
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
                ship.AddPower(supply*10);
                ship.ChangeOrdnance(supply);
            }

            ship.HealTroops(healOne: true);
        }

        private float CalcRepairPool()
        {
            float outOfCombatBonus = P.SpaceCombatNearPlanet ? 0.1f : 10;
            float repairPool       = RepairPerTurn * outOfCombatBonus / P.ShipBuildingModifier;

            return repairPool;
        }

        private void RepairShip(Ship ship, float repairPool)
        {
            ship.AI.TerminateResupplyIfDone();
            //Modified by McShooterz: Repair based on repair pool, if no combat in system
            if (!HasSpacePort || ship.Health.AlmostEqual(ship.HealthMax))
                return;

            int repairLevel = Level + NumShipYards;
            ship.ApplyAllRepair(repairPool, repairLevel, repairShields: true);
            ship.CauseEmpDamage(-repairPool * 10); // Remove EMP
        }

        private void LoadTroops(Ship ship, int garrisonSize)
        {
            if (ship.TroopCapacity == 0 || ship.TroopCapacity <= ship.TroopCount)
                return;

            int troopCount = ship.Carrier.NumTroopsInShipAndInSpace;
            foreach (PlanetGridSquare pgs in TilesList)
            {
                if (troopCount + ship.NumTroopsRebasingHere >= ship.TroopCapacity || garrisonSize == 0)
                    break;

                if (pgs.LockOnOurTroop(ship.loyalty, out Troop troop))
                {
                    Ship troopShip = troop.Launch();
                    if (troopShip != null)
                    {
                        garrisonSize--;
                        troopShip.AI.OrderRebaseToShip(ship);
                    }
                }
            }
        }

        void DisengageTroopsFromCapturedShips(Ship ship)
        {
            if (ship.TroopCount == 0)
                return;

            // If we left garrisoned troops on a captured ship
            // remove them now as they are replaced with regular ship crew
            int troopsToTRemove = (ship.GetOurTroops().Count - ship.TroopCapacity).LowerBound(0);
            if (troopsToTRemove > 0)
                ship.DisengageExcessTroops(ship.GetOurTroops().Count - ship.TroopCapacity);
        }

        private void AddTroopsForFactions(Ship ship)
        {
            // @todo FB - need to separate this to a method which will return a troop based on faction
            if ((SystemCombatTimer % 30).AlmostZero()  && ship.TroopCapacity > ship.TroopCount)
            {
                Troop troop = ResourceManager.CreateTroop("Wyvern", ship.loyalty);
                troop.LandOnShip(ship);
            }
        }
    }
}