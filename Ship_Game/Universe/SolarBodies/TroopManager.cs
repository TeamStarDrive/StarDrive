using Ship_Game.Gameplay;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Audio;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class TroopManager // Refactored by Fat Bastard. Feb 6, 2019
    {
        private readonly Planet Ground;

        private Empire Owner           => Ground.Owner;
        public bool RecentCombat       => InCombatTimer > 0.0f;
        private bool NoTroopsOnPlanet  => Ground.TroopsHere.Count <= 0;
        private bool TroopsAreOnPlanet => Ground.TroopsHere.Count > 0;

        private Array<PlanetGridSquare> TilesList => Ground.TilesList;
        private Array<Building> BuildingList      => Ground.BuildingList;
        private SolarSystem ParentSystem          => Ground.ParentSystem;
        public int NumEmpireTroops(Empire us)     => TroopList.Count(t => t.Loyalty == us);
        public int NumDefendingTroopCount         => NumEmpireTroops(Owner);
        public bool WeHaveTroopsHere(Empire us)   => TroopList.Any(t => t.Loyalty == us);

        private BatchRemovalCollection<Troop> TroopList      => Ground.TroopsHere;
        private BatchRemovalCollection<Combat> ActiveCombats => Ground.ActiveCombats;

        private float DecisionTimer = 0.5f;        
        private float InCombatTimer;
        private int NumInvadersLast;

        public void SetInCombat(float timer = 10f)
        {
            InCombatTimer = timer;
        }

        // ReSharper disable once UnusedParameter.Local Habital concept here is to not use this class if the planet cant have
        // ground combat. but that will be a future project. 
        public TroopManager(Planet planet)      
        {
            Ground = planet;
        }

        public void Update(float elapsedTime)
        {
            if (Empire.Universe.Paused) return;
            DecisionTimer -= elapsedTime;
            InCombatTimer -= elapsedTime;
            if (RecentCombat || TroopsAreOnPlanet)
            {
                ResolvePlanetaryBattle(elapsedTime);
                if (DecisionTimer <= 0)
                {
                    MakeCombatDecisions();
                    DecisionTimer = 0.5f;
                }
                DoBuildingTimers(elapsedTime);
                DoTroopTimers(elapsedTime);
            }
        }

        private void MakeCombatDecisions()
        {
            if (!HostileTargetsOnPlanet)
                return;

            for (int i = 0; i < TilesList.Count; ++i)
            {
                PlanetGridSquare pgs = TilesList[i];
                if (pgs.TroopsAreOnTile)
                    PerformGroundActions(pgs.SingleTroop, pgs);

                else if (pgs.BuildingPerformsAutoCombat(Ground))
                    PerformGroundActions(pgs.building, pgs);
            }
        }

        private void PerformGroundActions(Building b, PlanetGridSquare ourTile)
        {
            if (!b.CanAttack)
                return;

            // scan for enemies
            PlanetGridSquare nearestTargetTile = SpotClosestHostile(ourTile, Owner); 
            if (nearestTargetTile == null)
                return; // no targets on planet

            // find range
            if (!nearestTargetTile.InRangeOf(ourTile, 1) || nearestTargetTile.EventOnTile) // right now all buildings have range 1
                return;

            // start combat
            b.UpdateAttackActions(-1);
            CombatScreen.StartCombat(ourTile, nearestTargetTile, Ground);
        }

        private void PerformGroundActions(Troop t, PlanetGridSquare ourTile)
        {
            if (!t.CanMove && !t.CanAttack)
                return;

            // scan for enemies
            PlanetGridSquare nearestTargetTile = SpotClosestHostile(ourTile, t.Loyalty);
            if (nearestTargetTile == null)
                return; // no targets on planet. no need to move or attack

            // find range
            if (nearestTargetTile.InRangeOf(ourTile, t.ActualRange) && !nearestTargetTile.EventOnTile) 
            {
                // start combat
                t.UpdateAttackActions(-1);
                t.UpdateMoveActions(-1);
                t.facingRight = nearestTargetTile.x >= ourTile.x;
                CombatScreen.StartCombat(ourTile, nearestTargetTile, Ground);
            }
            else // move to targets
                MoveTowardsTarget(t, ourTile, nearestTargetTile);

            // resolve possible events 
            ResolveEvents(ourTile, nearestTargetTile, t.Loyalty);
        }

        private void ResolveEvents(PlanetGridSquare troopTile, PlanetGridSquare possibleEventTile, Empire empire)
        {
            if (troopTile == possibleEventTile)
                possibleEventTile.CheckAndTriggerEvent(Ground, empire);
        }

        private void MoveTowardsTarget(Troop t, PlanetGridSquare ourTile, PlanetGridSquare targetTile)
        {
            if (!t.CanMove || ourTile == targetTile)
                return;

            TileDirection direction   = ourTile.GetDirectionTo(targetTile);
            PlanetGridSquare moveToTile = PickTileToMoveTo(direction, ourTile);
            if (moveToTile == null)
                return; // no free tile

            // move to selected direction
            using(ourTile.TroopsHere.AcquireWriteLock())
            {
                t.SetFromRect(ourTile.TroopClickRect);
                t.MovingTimer = 0.75f;
                t.UpdateMoveActions(-1);
                t.ResetMoveTimer();
                moveToTile.TroopsHere.Add(t);
                ourTile.TroopsHere.Clear();
            }
        }
        
        // try 3 directions to move into, based on general direction to the target
        private PlanetGridSquare PickTileToMoveTo(TileDirection direction, PlanetGridSquare ourTile)
        {
            PlanetGridSquare bestFreeTile = FreeTile(direction, ourTile);
            if (bestFreeTile != null)
                return bestFreeTile;

            // try alternate tiles
            switch (direction)
            {
                case TileDirection.North:     return FreeTile(TileDirection.NorthEast, ourTile) ?? 
                                                     FreeTile(TileDirection.NorthWest, ourTile);
                case TileDirection.South:     return FreeTile(TileDirection.SouthEast, ourTile) ??
                                                     FreeTile(TileDirection.SouthWest, ourTile);
                case TileDirection.East:      return FreeTile(TileDirection.NorthEast, ourTile) ??
                                                     FreeTile(TileDirection.SouthEast, ourTile);
                case TileDirection.West:      return FreeTile(TileDirection.NorthWest, ourTile) ??
                                                     FreeTile(TileDirection.SouthWest, ourTile);
                case TileDirection.NorthEast: return FreeTile(TileDirection.North, ourTile) ??
                                                     FreeTile(TileDirection.East, ourTile);
                case TileDirection.NorthWest: return FreeTile(TileDirection.North, ourTile) ??
                                                     FreeTile(TileDirection.West, ourTile);
                case TileDirection.SouthEast: return FreeTile(TileDirection.South, ourTile) ??
                                                     FreeTile(TileDirection.East, ourTile);
                case TileDirection.SouthWest: return FreeTile(TileDirection.South, ourTile) ??
                                                     FreeTile(TileDirection.West, ourTile);
                default: return null;
            }
        }

        private PlanetGridSquare FreeTile(TileDirection direction, PlanetGridSquare ourTile)
        {
            Point newCords        = ourTile.ConvertDirectionToCoordinates(direction);
            PlanetGridSquare tile = Ground.GetTileByCoordinates(newCords.X, newCords.Y);

            if (tile != null && tile.FreeForMovement && tile != ourTile)
                return tile;
            return null;
        }

        private PlanetGridSquare SpotClosestHostile(PlanetGridSquare spotterTile, Empire spotterOwner)
        {
            foreach (PlanetGridSquare scannedTile in TilesList.OrderBy(tile =>
                Math.Abs(tile.x - spotterTile.x) + Math.Abs(tile.y - spotterTile.y)))
            {
                if (scannedTile.HostilesTargetsOnTile(spotterOwner, Owner))
                    return scannedTile;
            }
            return null;
        }

        private bool HostileTargetsOnPlanet
        {
            get
            {
                foreach (PlanetGridSquare pgs in TilesList)
                    if ((pgs.TroopsAreOnTile && pgs.SingleTroop.Loyalty != Owner) ||
                        (pgs.EventOnTile))
                        return true;
                return false;
            }
        }

        private void DoBuildingTimers(float elapsedTime)
        {
            if (BuildingList.Count <= 0)
                return;

            for (int i = 0; i < BuildingList.Count; i++)
            {
                Building building = BuildingList[i];
                if (building == null || !building.IsAttackable)
                    continue;

                building.UpdateAttackTimer(-elapsedTime);
                if (building.AttackTimer < 0.0)
                {
                    building.UpdateAttackActions(1);
                    building.ResetAttackTimer();
                }
            }
        }

        private void DoTroopTimers(float elapsedTime)
        {
            if (NoTroopsOnPlanet)
                return;

            Array<Troop> list = new Array<Troop>();
            for (int x = 0; x < TroopList.Count; x++)
            {
                Troop troop = TroopList[x];
                if (troop == null)
                    continue;
                if (troop.Strength <= 0)
                {
                    list.Add(troop);
                    foreach (PlanetGridSquare planetGridSquare in TilesList)
                        planetGridSquare.TroopsHere.Remove(troop);
                }
                troop.UpdateLaunchTimer(-elapsedTime);
                troop.UpdateMoveTimer(-elapsedTime);
                troop.MovingTimer -= elapsedTime;
                if (troop.MoveTimer < 0.0)
                {
                    troop.UpdateMoveActions(troop.MaxStoredActions);
                    troop.ResetMoveTimer();
                }
                troop.UpdateAttackTimer(-elapsedTime);
                if (troop.AttackTimer < 0.0)
                {
                    troop.UpdateAttackActions(troop.MaxStoredActions);
                    troop.ResetAttackTimer();
                }
            }
            foreach (Troop troop in list)
                TroopList.Remove(troop);
        }

        private bool CombatDone(PlanetGridSquare attacker, PlanetGridSquare defender)
        {
            return attacker.NothingOnTile || defender.NothingOnTile ||
                    attacker.AllDestroyed || defender.AllDestroyed;
        }

        private int RollDamage(PlanetGridSquare attacker, PlanetGridSquare defender)
        {
            TargetType attackType = defender.BuildingOnTile ? TargetType.Hard : defender.SingleTroop.TargetType;
            var attackerStats     = new AttackerStats(attacker);
            float attackValue     = attackType == TargetType.Soft ? attackerStats.SoftAttack : attackerStats.HardAttack;
            int damage = 0;
            for (int index = 0; index < attackerStats.Strength; ++index)
            {
                if (RandomMath.RandomBetween(0.0f, 100f) < attackValue)
                    ++damage;
            }
            return damage;
        }

        private void DealDamage(Combat combat, int damage, bool isViewing = false)
        {
            PlanetGridSquare attacker = combat.AttackTile;
            PlanetGridSquare defender = combat.DefenseTile;
            if (damage == 0)
            {
                if (isViewing) GameAudio.PlaySfxAsync("sd_troop_attack_miss");
                return;
            }

            if (isViewing)
            {
                GameAudio.PlaySfxAsync("sd_troop_attack_hit");
                ((CombatScreen)Empire.Universe.workersPanel).AddExplosion(defender.TroopClickRect, 1);
            }

            if (defender.TroopsAreOnTile)
            {
                defender.SingleTroop.DamageTroop(damage);
                if (!defender.AllTroopsDead) // Troops are still alive
                    return;

                TroopList.Remove(defender.SingleTroop);
                defender.TroopsHere.Clear();
                ActiveCombats.QueuePendingRemoval(combat);
                if (isViewing)
                {
                    GameAudio.PlaySfxAsync("Explo1");
                    ((CombatScreen)Empire.Universe.workersPanel).AddExplosion(defender.TroopClickRect, 4);
                }
                if (attacker.TroopsAreOnTile)
                    attacker.SingleTroop.LevelUp(); // FB - for now multi troops on same tile is not supported
            }
            else if (defender.CombatBuildingOnTile)
            {
                defender.building.Strength       -= damage;
                defender.building.CombatStrength -= damage;
                if (!defender.BuildingDestroyed)
                    return; // Building still stands

                BuildingList.Remove(defender.building);
                defender.building = null; // make pgs building private in the future
            }
        }

        private void ResolveTacticalCombats(float elapsedTime, bool isViewing = false)
        {
            using (ActiveCombats.AcquireReadLock())
                foreach (Combat combat in ActiveCombats)
                {
                    if (CombatDone(combat.AttackTile, combat.DefenseTile))
                    {
                        ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }

                    combat.Timer -= elapsedTime;
                    if (combat.Timer < 3.0 && combat.phase == 1)
                    {
                        int damage = RollDamage(combat.AttackTile, combat.DefenseTile);
                        DealDamage(combat, damage, isViewing);
                        combat.phase = 2;
                    }
                    else if (combat.phase == 2)
                        ActiveCombats.QueuePendingRemoval(combat);
                }
        }

        private void ResolveDiplomacy(int invadingForces, Empire invadingEmpire)
        {
            if (invadingForces <= NumInvadersLast || NumInvadersLast != 0)
                return; // FB - nothing to change if no new troops invade

            if (Empire.Universe.PlayerEmpire == Owner) // notify player of invasion
                Empire.Universe.NotificationManager.AddEnemyTroopsLandedNotification(Ground, invadingEmpire, Owner);
            else if (invadingEmpire == Empire.Universe.PlayerEmpire && !Owner.isFaction && !Empire.Universe.PlayerEmpire.GetRelations(Owner).AtWar)
            {
                if (Empire.Universe.PlayerEmpire.GetRelations(Owner).Treaty_NAPact)
                {
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Owner, Empire.Universe.PlayerEmpire, "Invaded NA Pact", ParentSystem));
                    Empire.Universe.PlayerEmpire.GetEmpireAI().DeclareWarOn(Owner, WarType.ImperialistWar);
                    Owner.GetRelations(Empire.Universe.PlayerEmpire).Trust -= 50f;
                    Owner.GetRelations(Empire.Universe.PlayerEmpire).Anger_DiplomaticConflict += 50f;
                }
                else
                {
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Owner, Empire.Universe.PlayerEmpire, "Invaded Start War", ParentSystem));
                    Empire.Universe.PlayerEmpire.GetEmpireAI().DeclareWarOn(Owner, WarType.ImperialistWar);
                    Owner.GetRelations(Empire.Universe.PlayerEmpire).Trust -= 25f;
                    Owner.GetRelations(Empire.Universe.PlayerEmpire).Anger_DiplomaticConflict += 25f;
                }
            }
        }

        private void ResolvePlanetaryBattle(float elapsedTime)
        {
            if (Empire.Universe.LookingAtPlanet 
                && Empire.Universe.workersPanel is CombatScreen screen 
                && screen.p == Ground)
            {
                ResolveTacticalCombats(elapsedTime, isViewing: true);
            }
            else
            {
                ResolveTacticalCombats(elapsedTime);
                ActiveCombats.ApplyPendingRemovals();
            }

            if (ActiveCombats.Count > 0)
                InCombatTimer = 10f;

            if (NoTroopsOnPlanet || Owner == null)
                return;

            var forces = new Forces(Owner, TilesList);
            ResolveDiplomacy(forces.InvadingForces, forces.InvadingEmpire);
            NumInvadersLast = forces.InvadingForces;

            if (forces.InvadingForces <= 0 || forces.DefendingForces != 0)
                return; // Planet is still fighting or all invading forces are destroyed

            Ground.ChangeOwnerByInvasion(forces.InvadingEmpire);
        }

        public float GroundStrength(Empire empire)
        {
            float strength = 0;
            if (Owner == empire)
                strength += BuildingList.Sum(offense => offense.CombatStrength);

            using (TroopList.AcquireReadLock())
                strength += TroopList.Where(t => t.Loyalty == empire).Sum(str => str.Strength);
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
            float enemyTroopStrength = TroopList.Where(t => t.OwnerString != allButThisEmpire.data.Traits.Name).Sum(t => t.Strength);
            for (int i = 0; i < BuildingList.Count; i++)
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

                if (b?.CombatStrength > 0)
                    enemyTroopStrength += b.CombatStrength;
            }
            return enemyTroopStrength;            
        }

        public bool TroopsHereAreEnemies(Empire empire)
        {
            bool enemies = false;
            using (TroopList.AcquireReadLock())
                foreach (Troop t in TroopList)
                {
                    if (!empire.TryGetRelations(t.Loyalty, out Relationship trouble) || trouble.AtWar)
                    {
                        enemies = true;
                        break;
                    }
                }
            return enemies;
        }

        public int NumGroundLandingSpots()
        {            
            int spotCount = TilesList.Sum(spots => spots.MaxAllowedTroops); //.FilterBy(spot => (spot.building?.CombatStrength ?? 0) < 1)
            int troops    = TroopList.Filter(owner => owner.Loyalty == Owner).Length;
            return spotCount - troops;
        }

        public Array<Troop> EmpireTroops(Empire empire, int maxToTake)
        {
            var troops = new Array<Troop>();
            foreach (Troop troop in TroopList)
            {
                if (troop.Loyalty != empire) continue;

                if (maxToTake-- < 0)
                    troops.Add(troop);
            }
            return troops;
        }

        //Added by McShooterz: heal builds and troops every turn
        public void HealTroops(int healAmount)
        {
            if (RecentCombat)
                return;

            using (TroopList.AcquireReadLock())
                foreach (Troop troop in TroopList)
                    troop.HealTroop(healAmount);
        }

        public struct AttackerStats
        {
            public readonly float Strength;
            public readonly int HardAttack;
            public readonly int SoftAttack;

            public AttackerStats(PlanetGridSquare attacker)
            {
                if (attacker.TroopsAreOnTile)
                {
                    Strength   = attacker.TroopsStrength;
                    HardAttack = attacker.TroopsHardAttack;
                    SoftAttack = attacker.TroopsSoftAttack;
                }
                else // building stats
                {
                    Strength   = attacker.building.Strength;
                    HardAttack = attacker.building.HardAttack;
                    SoftAttack = attacker.building.SoftAttack;
                }
            }
        }

        public struct Forces
        {
            public readonly int DefendingForces;
            public readonly int InvadingForces;
            public readonly Empire InvadingEmpire;

            public Forces(Empire defendingEmpire, Array<PlanetGridSquare> tileList)
            {
                DefendingForces = 0;
                InvadingForces  = 0;
                InvadingEmpire  = null;
                foreach (PlanetGridSquare planetGridSquare in tileList)
                {
                    using (planetGridSquare.TroopsHere.AcquireReadLock())
                        foreach (Troop troop in planetGridSquare.TroopsHere)
                        {
                            if (troop.Loyalty != null && troop.Loyalty != defendingEmpire)
                            {
                                ++InvadingForces;
                                InvadingEmpire = troop.Loyalty;
                            }
                            else
                                ++DefendingForces;
                        }
                    if (planetGridSquare.building != null && planetGridSquare.building.CombatStrength > 0)
                        ++DefendingForces;
                }
            }
        }
    }

    public enum TargetType
    {
        Soft,
        Hard
    }
}