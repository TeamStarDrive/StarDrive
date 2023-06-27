using Ship_Game.Gameplay;
using System;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.GameScreens.DiplomacyScreen;
using Point = SDGraphics.Point;
using Ship_Game.Data.Serialization;
using System.Collections.Generic;
using Ship_Game.Utils;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    [StarDataType]
    public class TroopManager // Refactored by Fat Bastard. Feb 6, 2019
    {
        [StarData] readonly Planet Ground;
        [StarData] readonly Array<Troop> TroopsHere = new();

        // optimized bit-set for which faction troops are present here
        [StarData] SmallBitSet FactionTroopsPresentHere;

        Empire Owner => Ground.Owner;
        public int Count => TroopsHere.Count;

        public bool RecentCombat => InCombatTimer > 0f && ForeignTroopHere(Owner);
        bool NoTroopsOnPlanet  => TroopsHere.IsEmpty;

        Array<PlanetGridSquare> TilesList => Ground.TilesList;
        SolarSystem ParentSystem => Ground.System;

        // TODO: refactor these getters
        public bool WeAreInvadingHere(Empire us) => Ground.Owner != us && WeHaveTroopsHere(us);
        public bool MightBeAWarZone(Empire us) => RecentCombat || Ground.SpaceCombatNearPlanet || ForeignTroopHere(us);
        public float OwnerTroopStrength => TroopsHere.Sum(troop => troop.Loyalty == Owner ? troop.Strength : 0);

        public int NumDefendingTroopCount => NumTroopsHere(Owner);
        public int NumTroopsCanLaunchFor(Empire empire) => TroopsHere.Count(t => t.Loyalty == empire && t.CanLaunch);
        public int NumTroopsCanMoveFor(Empire empire) => GetTroopsOf(empire).Count(t => t.CanMove);

        float DecisionTimer;
        float InCombatTimer;
        int NumInvadersLast;
        bool Init = true; // Force initial timers update in case of combat when the class is created

        public void SetInCombat(float timer = 1)
        {
            InCombatTimer = timer;
        }

        [StarDataConstructor] TroopManager() {}

        public TroopManager(Planet planet)
        {
            Ground = planet;
        }

        #region Troop List Add/Remove

        public bool Contains(Troop troop) // troop exists here?
        {
            return TroopsHere.ContainsRef(troop);
        }

        public void AddTroop(PlanetGridSquare tile, Troop troop)
        {
            lock (TroopsHere)
            {
                if (TroopsHere.AddUniqueRef(troop))
                {
                    FactionTroopsPresentHere.Set(troop.Loyalty.Id);
                    tile.AddTroop(troop);
                }
            }
        }

        public bool TryRemoveTroop(PlanetGridSquare tile, Troop troop)
        {
            lock (TroopsHere)
            {
                // not using RemoveSwapLast since the order of troop is important for allied invasion
                if (TroopsHere.Remove(troop))
                {
                    if (tile == null || !tile.TroopsHere.Remove(troop))
                        Ground.SearchAndRemoveTroopFromAllTiles(troop);

                    UpdateFactionTroopsPresentHere(troop.Loyalty);
                    return true;
                }

                Log.Error($"Could not remove troop {troop} on planet {Ground.Name}");
                return false;
            }
        }

        public void UpdateFactionTroopsPresentHere(Empire empire)
        {
            bool anyLeftHere = WeHaveTroopsHereUncached(empire);
            FactionTroopsPresentHere.SetValue(empire.Id, anyLeftHere);
        }

        #endregion

        public void Update(FixedSimTime timeStep)
        {
            if (Ground.Universe.Paused) 
                return;

            if (Init)
                ChangeLoyaltyAbsorbed();

            bool startCombatTimer = false;
            if (InCombatTimer > 0 || Init)
            {
                InCombatTimer -= timeStep.FixedTime;
                DecisionTimer -= timeStep.FixedTime;
                ResolvePlanetaryBattle(timeStep, ref startCombatTimer);
                if (DecisionTimer <= 0)
                {
                    MakeCombatDecisions();
                    DecisionTimer = 0.5f;
                    if (Ground.EventsOnTiles() && TroopsHere.Count > 0)
                        startCombatTimer = true;
                }

                DoBuildingTimers(timeStep, ref startCombatTimer);
                DoTroopTimers(timeStep, ref startCombatTimer);
                Init = false;
            }

            if (startCombatTimer) // continue the setting the timer until nothing needs update
                SetInCombat();
        }

        private void MakeCombatDecisions()
        {
            bool foreignTroopHere = ForeignTroopHere(Owner); 
            if (!foreignTroopHere && !Ground.EventsOnTiles())
                return;

            for (int i = 0; i < TilesList.Count; ++i)
            {
                PlanetGridSquare tile = TilesList[i];
                if (tile.TroopsAreOnTile)
                    PerformTroopsGroundActions(tile);

                else if (tile.BuildingOnTile)
                    PerformGroundActions(tile.Building, tile);
            }
        }

        // This is a workaround to solve troop loyalties not changed from some reason after federation on some planets.
        // It runs only after a save is loaded
        // To reproduce - need a before federation save
        void ChangeLoyaltyAbsorbed()
        {
            foreach (Troop t in TroopsHere)
            {
                if (t.Loyalty.data.AbsorbedBy.NotEmpty())
                    t.ChangeLoyalty(Ground.Universe.GetEmpireByName(t.Loyalty.data.AbsorbedBy));
            }
        }

        private void PerformTroopsGroundActions(PlanetGridSquare tile)
        {
            for (int i = 0; i < tile.TroopsHere.Count; ++i)
            {
                Troop troop = tile.TroopsHere[i];
                PerformGroundActions(troop, tile);
            }
        }

        private void PerformGroundActions(Building b, PlanetGridSquare ourTile)
        {
            if (!b.CanAttack || Ground.Owner == null)
                return;

            // scan for enemies, right now all buildings have range 1
            if (!SpotClosestHostile(ourTile, 1, Owner, out PlanetGridSquare nearestTargetTile))
                return; // no targets on planet

            // lock on enemy troop target
            if (nearestTargetTile.LockOnEnemyTroop(Owner, out Troop enemy))
            {
                // start combat
                b.UpdateAttackActions(-1);
                CombatScreen.StartCombat(b, enemy, nearestTargetTile, Ground);
            }
        }

        private void PerformGroundActions(Troop t, PlanetGridSquare ourTile)
        {
            if (!t.CanMove && !t.CanAttack)
                return;

            if (!SpotClosestHostile(ourTile, 10, t.Loyalty, out PlanetGridSquare nearestTargetTile))
                return; // No targets on planet

            // find target to attack
            if (t.CanAttack && t.AcquireTarget(ourTile, Ground, out PlanetGridSquare targetTile))
            {
                // start combat
                t.UpdateAttackActions(-1);

                t.FaceEnemy(targetTile, ourTile);
                if (targetTile.CombatBuildingOnTile)
                {
                    t.UpdateMoveActions(-1);
                    CombatScreen.StartCombat(t, targetTile.Building, targetTile, Ground);
                }
                else if (targetTile.LockOnEnemyTroop(t.Loyalty, out Troop enemy))
                {
                    if (enemy.Strength.LessOrEqual(0))
                    {
                        // Workaround for negative troop health
                        Log.Warning($"{enemy.Name} health is less or 0, on planet {Ground.Name}");
                        enemy.DamageTroop(1, Ground, targetTile, out _);
                    }
                    CombatScreen.StartCombat(t, enemy, targetTile, Ground);
                    if (t.ActualRange == 1)
                        MoveTowardsTarget(t, ourTile, targetTile); // enter the same tile 
                    else
                        t.UpdateMoveActions(-1);
                }
            }
            else // Move to target
            {
                MoveTowardsTarget(t, ourTile, nearestTargetTile);

                // resolve possible events 
                ResolveEvents(ourTile, nearestTargetTile, t.Loyalty);
            }
        }

        private void ResolveEvents(PlanetGridSquare troopTile, PlanetGridSquare possibleEventTile, Empire empire)
        {
            if (troopTile == possibleEventTile)
                possibleEventTile.CheckAndTriggerEvent(Ground, empire);
        }

        public void MoveTowardsTarget(Troop t, PlanetGridSquare ourTile, PlanetGridSquare targetTile)
        {
            if (!t.CanMove || ourTile == targetTile)
                return;

            TileDirection direction = ourTile.GetDirectionTo(targetTile);
            PlanetGridSquare moveToTile = PickTileToMoveTo(direction, ourTile, t.Loyalty);
            if (moveToTile == null)
                return; // no free tile

            // move to selected direction
            t.SetFromRect(t.ClickRect);
            t.MovingTimer = 0.75f;
            t.UpdateMoveActions(-1);
            t.ResetMoveTimer();
            moveToTile.AddTroop(t);
            ourTile.TroopsHere.Remove(t);
        }
        
        // try 3 directions to move into, based on general direction to the target
        private PlanetGridSquare PickTileToMoveTo(TileDirection direction, PlanetGridSquare ourTile, Empire us)
        {
            PlanetGridSquare bestFreeTile = FreeTile(direction, ourTile, us);
            if (bestFreeTile != null)
                return bestFreeTile;

            // try alternate tiles
            switch (direction)
            {
                case TileDirection.North:     return FreeTile(TileDirection.NorthEast, ourTile, us) ?? 
                                                     FreeTile(TileDirection.NorthWest, ourTile, us);
                case TileDirection.South:     return FreeTile(TileDirection.SouthEast, ourTile, us) ??
                                                     FreeTile(TileDirection.SouthWest, ourTile, us);
                case TileDirection.East:      return FreeTile(TileDirection.NorthEast, ourTile, us) ??
                                                     FreeTile(TileDirection.SouthEast, ourTile, us);
                case TileDirection.West:      return FreeTile(TileDirection.NorthWest, ourTile, us) ??
                                                     FreeTile(TileDirection.SouthWest, ourTile, us);
                case TileDirection.NorthEast: return FreeTile(TileDirection.North, ourTile, us) ??
                                                     FreeTile(TileDirection.East, ourTile, us);
                case TileDirection.NorthWest: return FreeTile(TileDirection.North, ourTile, us) ??
                                                     FreeTile(TileDirection.West, ourTile, us);
                case TileDirection.SouthEast: return FreeTile(TileDirection.South, ourTile, us) ??
                                                     FreeTile(TileDirection.East, ourTile, us);
                case TileDirection.SouthWest: return FreeTile(TileDirection.South, ourTile, us) ??
                                                     FreeTile(TileDirection.West, ourTile, us);
                default: return null;
            }
        }

        private PlanetGridSquare FreeTile(TileDirection direction, PlanetGridSquare ourTile, Empire us)
        {
            Point newCords        = ourTile.ConvertDirectionToCoordinates(direction);
            PlanetGridSquare tile = Ground.GetTileByCoordinates(newCords.X, newCords.Y);

            if (tile != null && tile.IsTileFree(us) && tile != ourTile)
                return tile;
            return null;
        }

        bool SpotClosestHostile(PlanetGridSquare spotterTile, int range, Empire spotterOwner, out PlanetGridSquare targetTile)
        {
            targetTile = null;
            if (spotterTile.LockOnEnemyTroop(spotterOwner, out _))
            {
                targetTile = spotterTile; // We have enemy on our tile
            }
            else
            {
                var tiles = TilesList.Filter(t => t.InRangeOf(spotterTile, range));
                foreach (PlanetGridSquare scannedTile in tiles.OrderBy(tile =>
                    Math.Abs(tile.X - spotterTile.X) + Math.Abs(tile.Y - spotterTile.Y)))
                {
                    bool hostilesOnTile = spotterTile.CombatBuildingOnTile
                                          ? scannedTile.HostilesTargetsOnTileToBuilding(spotterOwner, Owner, Ground.SpaceCombatNearPlanet)
                                          : scannedTile.HostilesTargetsOnTile(spotterOwner, Owner, Ground.SpaceCombatNearPlanet);

                    if (hostilesOnTile)
                    {
                        targetTile = scannedTile;
                        break;
                    }
                }
            }

            return targetTile != null;
        }

        private void DoBuildingTimers(FixedSimTime timeStep, ref bool startCombatTimer)
        {
            if (Ground.NumBuildings == 0)
                return;

            bool combatBuildingHere = false;
            foreach (Building b in Ground.Buildings)
            {
                if (!b.IsAttackable)
                    continue;

                combatBuildingHere = true;
                b.UpdateAttackTimer(-timeStep.FixedTime);
                if (b.AttackTimer < 0)
                {
                    b.UpdateAttackActions(1);
                    b.ResetAttackTimer();
                }

                if (!b.CanAttack)
                    startCombatTimer = true;
            }

            if (ForeignTroopHere(Owner) && combatBuildingHere && Owner != null)
                startCombatTimer = true;
        }

        private void DoTroopTimers(FixedSimTime timeStep, ref bool startCombatTimer)
        {
            if (NoTroopsOnPlanet)
                return;

            for (int x = TroopsHere.Count - 1; x >= 0; x--)
            {
                Troop troop = TroopsHere[x];
                if (troop == null)
                    continue;

                if (troop.Strength <= 0f)
                {
                    TryRemoveTroop(null, troop);
                    Log.Warning($"Removed 0 strength troop: {troop}");
                    continue;
                }

                troop.UpdateLaunchTimer(-timeStep.FixedTime);
                troop.UpdateMoveTimer(-timeStep.FixedTime);
                troop.MovingTimer -= timeStep.FixedTime;
                if (troop.MoveTimer < 0f)
                {
                    troop.UpdateMoveActions(troop.MaxStoredActions);
                    troop.ResetMoveTimer();
                }

                troop.UpdateAttackTimer(-timeStep.FixedTime);
                if (troop.AttackTimer < 0f)
                {
                    troop.UpdateAttackActions(troop.MaxStoredActions);
                    troop.ResetAttackTimer();
                }

                if (!troop.CanAttack || !troop.CanMove || Owner != null && troop.Loyalty != Owner)
                    startCombatTimer = true;
            }
        }

        private void ResolveTacticalCombats(FixedSimTime timeStep, bool isViewing = false)
        {
            var combats = Ground.ActiveCombats;
            for (int i = combats.Count - 1; i >= 0; i--)
            {
                Combat combat = combats[i];
                if (combat.Done)
                {
                    combats.Remove(combat);
                    break;
                }

                combat.Timer -= timeStep.FixedTime;
                if (combat.Timer < 3.0 && combat.Phase == 1)
                    combat.ResolveDamage(isViewing);
                else if (combat.Phase == 2)
                    combats.Remove(combat);
            }
        }

        private void ResolveDiplomacy(int invadingForces, Array<Empire> invadingEmpires)
        {
            if (Owner == null || invadingForces <= NumInvadersLast || invadingEmpires.Count == 0)
                return; // FB - nothing to change if no new troops invade

            Empire player = Ground.Universe.Player;
            if (invadingEmpires.Any(e => e.isPlayer) && !Owner.IsFaction && !player.IsAtWarWith(Owner))
            {
                if (player.IsNAPactWith(Owner))
                {
                    DiplomacyScreen.Show(Owner, "Invaded NA Pact", ParentSystem);
                    Owner.AI.DeclareWarOn(player, WarType.ImperialistWar);
                    Owner.GetRelations(player).Trust -= 50f;
                    Owner.GetRelations(player).AddAngerDiplomaticConflict(50);
                }
                else
                {
                    DiplomacyScreen.Show(Owner, "Invaded Start War", ParentSystem);
                    Owner.AI.DeclareWarOn(player, WarType.ImperialistWar);
                    Owner.GetRelations(player).Trust -= 25f;
                    Owner.GetRelations(player).AddAngerDiplomaticConflict(25);
                }
            }
        }

        private void ResolvePlanetaryBattle(FixedSimTime timeStep, ref bool startCombatTimer)
        {
            if (Ground.Universe.Screen.LookingAtPlanet 
                && Ground.Universe.Screen.workersPanel is CombatScreen screen 
                && screen.P == Ground)
            {
                ResolveTacticalCombats(timeStep, isViewing: true);
            }
            else
            {
                ResolveTacticalCombats(timeStep);
            }

            bool combatBeingResolved = false;
            if (Ground.ActiveCombats.Count > 0)
            {
                startCombatTimer    = true;
                combatBeingResolved = true;
            }

            var forces = new Forces(Owner, Ground);
            ResolveDiplomacy(forces.InvadingForces, forces.InvadingEmpires);
            NumInvadersLast = forces.InvadingForces;

            if (forces.InvadingForces > 0 && forces.DefendingForces == 0 && Ground.Owner != null) // Invaders have won
                DetermineNewOwnerAndChangeOwnership(forces.InvadingEmpires);

            if (combatBeingResolved && forces.DefendingForces > 0 && forces.InvadingForces == 0) // Defenders have won
            {
                Ground.LaunchNonOwnerTroops();
                Ground.AbortLandingPlayerFleets();
                IncreaseTrustAlliesWon(Owner, forces.DefendingEmpires);
            }
        }

        void DetermineNewOwnerAndChangeOwnership(Array<Empire> empires)
        {
            Empire newOwner = null;
            switch (empires.Count)
            {
                case 0: return;
                case 1: newOwner = empires[0]; break;
                default: if (TroopsHere.Count > 1) newOwner = TroopsHere[0].Loyalty; break;
            }

            if (Owner != null)
            {
                if (FactionTroopsPresentHere.IsSet(Owner.Id))
                {
                    // work around since sometimes this is not unset and  it looks like the planet is under invasion forever
                    FactionTroopsPresentHere.Unset(Owner.Id);
                    Log.Error($"{Ground.Name} - {Owner.Name} is still set in FactionTroopsPresentHere");
                }

                Ground.ChangeOwnerByInvasion(newOwner, Ground.Level);
            }
            IncreaseTrustAlliesWon(newOwner, empires);
        }

        void IncreaseTrustAlliesWon(Empire newOwner, Array<Empire> empires)
        {
            if (newOwner == null || newOwner.isPlayer || newOwner.IsFaction)
                return;

            for (int i = 0; i < empires.Count; i++)
            {
                Empire e = empires[i];
                if (e != newOwner && !e.IsFaction)
                {
                    Relationship rel = newOwner.GetRelations(e);
                    rel.Trust += newOwner.data.DiplomaticPersonality.Territorialism / 5f;
                }
            }
        }

        public float GroundStrength(Empire empire)
        {
            float strength = 0;
            if (Owner == empire)
                strength += Ground.SumBuildings(BuildingCombatStrength);

            strength += TroopsHere.Sum(t => t.Loyalty == empire ? t.ActualStrengthMax : 0);
            return strength;
        }

        public int GetPotentialGroundTroops()
        {
            int num = 0;

            foreach (PlanetGridSquare pgs in TilesList)
            {
                num += pgs.MaxAllowedTroops;
            }
            return num;
        }

        public float GroundStrengthOther(Empire allButThisEmpire)
        {
            float enemyTroopStrength = TroopsHere.Sum(t => t.Loyalty != allButThisEmpire ? t.ActualStrengthMax : 0);

            if (Owner == allButThisEmpire)
                return enemyTroopStrength; // The planet is ours, so no need to check from buildings

            foreach (Building b in Ground.Buildings)
            {
                enemyTroopStrength += BuildingCombatStrength(b);
            }
            return enemyTroopStrength;            
        }

        float BuildingCombatStrength(Building b)
        {
            float strength = 0;
            if (b?.IsAttackable == true)
            {
                strength += b.CombatStrength;
                strength += b.InvadeInjurePoints * Ground.TileArea/2f;
            }
            return strength;
        }

        public int NumTroopsHere(Empire empire)
        {
            int count = 0;
            for (int i = 0; i < TroopsHere.Count; ++i) // using loop for perf
                if (TroopsHere[i].Loyalty == empire) ++count;
            return count;
        }

        public bool WeHaveTroopsHereUncached(Empire empire)
        {
            for (int i = 0; i < TroopsHere.Count; ++i) // using loop for perf
                if (TroopsHere[i].Loyalty == empire) return true;
            return false;
        }

        public bool WeHaveTroopsHere(Empire empire)
        {
            if (empire == null)
                return false;
            return FactionTroopsPresentHere.IsSet(empire.Id);
        }

        public bool ForeignTroopHere(Empire us)
        {
            if (us == null)
                return FactionTroopsPresentHere.IsAnyBitsSet;
            return FactionTroopsPresentHere.IsAnyBitsSetExcept(us.Id);
        }

        public bool TroopsHereAreEnemies(Empire empire)
        {
            foreach (Troop t in TroopsHere)
                if (t.Loyalty != empire && empire.IsAtWarWith(t.Loyalty))
                    return true;
            return false;
        }

        public int NumFreeTiles(Empire empire)
        {
            return TilesList.Count(t => t.IsTileFree(empire));
        }

        public int GetEnemyAssets(Empire empire)
        {
            int numTroops = TroopsHere.Count(t => t.Loyalty != empire);
            int numCombatBuildings = Owner != empire ? Ground.CountBuildings(b => b.IsAttackable) : 0;

            return numTroops + numCombatBuildings;
        }

        // tries to take up to N Launchable troops
        public IEnumerable<Troop> GetLaunchableTroops(Empire of, int maxToTake = int.MaxValue, bool forceLaunch = false)
        {
            for (int i = TroopsHere.Count - 1; i >= 0; --i)
            {
                Troop troop = TroopsHere[i];
                if (troop.Loyalty == of && (troop.CanLaunch || forceLaunch) && maxToTake-- > 0)
                    yield return troop;
            }
        }

        public IEnumerable<Troop> GetTroopsOf(Empire of)
        {
            for (int i = TroopsHere.Count - 1; i >= 0; --i)
            {
                Troop troop = TroopsHere[i];
                if (troop.Loyalty == of)
                    yield return troop;
            }
        }

        public IEnumerable<Troop> GetTroopsNotOf(Empire notOf)
        {
            for (int i = TroopsHere.Count - 1; i >= 0; --i)
            {
                Troop troop = TroopsHere[i];
                if (troop.Loyalty != notOf)
                    yield return troop;
            }
        }

        // Added by McShooterz: heal builds and troops every turn
        public void HealTroops(int healAmount)
        {
            if (RecentCombat)
                return;

            foreach (Troop troop in TroopsHere)
            {
                if (Ground.CanRepairOrHeal())
                    troop.HealTroop(healAmount);
            }
        }

        public struct Forces
        {
            public readonly int DefendingForces;
            public readonly int InvadingForces;
            public readonly Array<Empire> InvadingEmpires;
            public readonly Array<Empire> DefendingEmpires;

            public Forces(Empire planetOwner, Planet ground)
            {
                InvadingForces = 0;
                InvadingEmpires = new();
                DefendingEmpires = new();

                if (ground.Owner != null)
                    DefendingEmpires.Add(ground.Owner);
                DefendingForces = ground.Troops.NumDefendingTroopCount;

                foreach (Troop t in ground.Troops.GetTroopsNotOf(planetOwner))
                {
                    if (!t.Loyalty.IsAlliedWith(planetOwner))
                    {
                        InvadingForces += 1;
                        InvadingEmpires.AddUnique(t.Loyalty);
                    }
                    else
                    {
                        DefendingForces += 1;
                        DefendingEmpires.AddUnique(t.Loyalty);
                    }
                }

                // PERF TODO: Maybe cache these? It is quite intensive
                foreach (Building b in ground.Buildings)
                    if (b.IsAttackable)
                        ++DefendingForces;
            }
        }
    }

    public enum TargetType
    {
        Soft,
        Hard
    }
}