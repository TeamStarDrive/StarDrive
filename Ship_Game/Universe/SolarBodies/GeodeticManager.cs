using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Universe.SolarBodies // Fat Bastard - Refactored March 21, 2019
{
    public class GeodeticManager
    {
        readonly Planet P;
        float SystemCombatTimer;
        float ChanceToLaunchTroopsVsBombers;
        public float RepairRatePerSecond { get; private set; }

        float Population  => P.Population;
        Empire Owner      => P.Owner;
        Shield Shield     => P.Shield;
        Vector2 Position  => P.Position;
        bool HasSpacePort => P.HasSpacePort;
        int Level         => P.Level;
        int NumShipYards  => P.OrbitalStations.Count(s => s.ShipData.IsShipyard);
        SolarSystem ParentSystem     => P.ParentSystem;
        int TurnsSinceTurnover       => P.TurnsSinceTurnover;
        float ShieldStrengthCurrent  => P.ShieldStrengthCurrent;
        float ShieldStrengthPercent  => P.ShieldStrengthMax > 0.01f ? P.ShieldStrengthCurrent / P.ShieldStrengthMax : 0;
        Array<PlanetGridSquare> TilesList => P.TilesList;

        public GeodeticManager(Planet planet)
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
            {
                DamageColonyShields(bomb);
            }
            else
            {
                var orbitalDrop = new OrbitalDrop
                {
                    TargetTile = SelectTargetTile(bomb),
                    Surface = P
                };

                orbitalDrop.DamageColonySurface(bomb);
                bomb.PlayCombatScreenEffects(P, orbitalDrop);
                if (Population <= 0f)
                {
                    P.WipeOutColony(bomb.Owner);
                    return;
                }

                bomb.ResolveSpecialBombActions(P); // This is for "Free Owlwoks" bomb
            }

            TryLaunchTroopsVsBombers(bomb.Owner);
        }

        void TryLaunchTroopsVsBombers(Empire enemy)
        {
            if (Owner == null || Owner.isPlayer || !enemy.isPlayer)
                return;

            if (ShieldStrengthPercent > 0 && ShieldStrengthPercent < 0.25f)
            {
                // Start increasing the chance to launch assault vs bombers
                float assaultBombersChance = 100 - (ShieldStrengthPercent*100 * 4f);
                if (RandomMath.RollDice(assaultBombersChance))
                    Owner.TryCreateAssaultBombersGoal(enemy, P);
            }
            else if (P.ShieldStrengthMax <= 0)
            {
                if (RandomMath.RollDice(GetTroopLaunchChance()))
                    Owner.TryCreateAssaultBombersGoal(enemy, P);
            }

            // Local Method
            float GetTroopLaunchChance()
            {
                if (ChanceToLaunchTroopsVsBombers > 0)
                    return ChanceToLaunchTroopsVsBombers;

                // Recalculate chance since it is reset every turn
                var enemyBombers = P.ParentSystem.ShipList.Filter(s => s.Loyalty == enemy && s.HasBombs
                                                                    && s.Position.Distance(P.Position) < P.GravityWellRadius);
                if (enemyBombers.Length == 0)
                    return 0;

                int totalBombs = enemyBombers.Sum(s => s.BombBays.Count);
                return totalBombs * 2;
            }
        }

        private PlanetGridSquare SelectTargetTile(Bomb bomb)
        {
            float baseHitChance = ((85 + bomb.ShipLevel) * bomb.ShipHealthPercent).Clamped(10, 100);
            if (!RandomMath.RollDice(baseHitChance))
                return TilesList.RandItem();

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
                              && bomb.Owner.isPlayer)
            {
                Owner.AI.DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
            }
        }

        private void DamageColonyShields(Bomb bomb)
        {
            if (P.Universe.IsSystemViewOrCloser
                && P.Universe.Screen.IsInFrustum(P.Position, P.OrbitalRadius * 2))
            {
                Shield.HitShield(P, bomb, Position, P.Radius + 100f);
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
               Owner.AI.AddPlanetaryRearmGoal(ourShipsNeedRearm[i], P);
        }

        public void AffectNearbyShips(FixedSimTime elapsedTurnTime) // Refactored by Fat Bastard - 23, July 2018
        {
            RepairRatePerSecond = GetPlanetRepairRatePerSecond();

            ChanceToLaunchTroopsVsBombers = 0; // Reset
            AssignPlanetarySupply();
            bool spaceCombat = P.SpaceCombatNearPlanet;

            for (int i = 0; i < ParentSystem.ShipList.Count; i++)
            {
                Ship ship = ParentSystem.ShipList[i];
                if (ship == null)
                    continue;

                bool loyaltyMatch = ship.Loyalty == Owner || ship.Loyalty.IsAlliedWith(Owner);
                if (ship.Loyalty.IsFaction)
                    AddTroopsForFactions(ship);

                if (loyaltyMatch && (ship.Position.InRadius(Position, 7000f) ||
                                     ship.IsOrbiting(P) || ship.GetTether() == P))
                {
                    SupplyShip(ship, spaceCombat);
                    ship.AI.TerminateResupplyIfDone(SupplyType.All, terminateIfEnemiesNear: true);

                    if (!spaceCombat && ship.Loyalty == Owner) // dont do this for allies
                    {
                        LoadTroops(ship, P.NumTroopsCanLaunch);
                        DisengageTroopsFromCapturedShips(ship);
                    }
                }
            }
        }

        private void SupplyShip(Ship ship, bool spaceCombat)
        {
            if (ship.ShipData.Role == RoleName.platform) // platforms always get max ordnance to retain platforms Vanilla functionality
            {
                ship.ChangeOrdnance(ship.OrdinanceMax);
                ship.AddPower(ship.PowerStoreMax);
            }
            else
            {
                float supply = Level;
                supply *= HasSpacePort ? 5f : 2f;
                supply *= spaceCombat ? 0.1f : 10f;
                supply = Math.Max(.1f, supply);
                ship.AddPower(supply*10);
                ship.ChangeOrdnance(supply);
            }

            ship.HealTroops(Level.LowerBound(1));
        }

        /// <summary>
        /// Maximum amount of Repair that this planet can give each ship which is in Orbit
        /// </summary>
        public float GetPlanetRepairRatePerSecond()
        {
            float baseRepairRate = P.RepairMultiplier * GlobalStats.Defaults.BaseShipyardRepair;
            float levelBasedBonus = 1f + P.Level * GlobalStats.Defaults.BonusRepairPerColonyLevel;
            float inCombatMod = P.SpaceCombatNearPlanet ? GlobalStats.Defaults.InCombatRepairModifier : 1f;
            float buildRate = 1f / P.ShipCostModifier; // build rate is inverse to the cost modifier
            float repairPool = baseRepairRate * levelBasedBonus * inCombatMod * buildRate;
            return repairPool;
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

                if (pgs.LockOnOurTroop(ship.Loyalty, out Troop troop))
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
            if (ship.TroopCount == 0 || ship.IsSingleTroopShip || ship.IsDefaultAssaultShuttle)
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
                if (ResourceManager.TryCreateTroop("Wyvern", ship.Loyalty, out Troop troop))
                {
                    troop.LandOnShip(ship);
                }
            }
        }
    }
}