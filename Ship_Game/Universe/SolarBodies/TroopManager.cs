using Ship_Game.Gameplay;
using System;
using System.Linq;
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
            if (TroopsAreOnPlanet) // FB  - && TroopsHereAreEnemies  maybe?
            {
                ResolvePlanetaryBattle(elapsedTime);
                if (DecisionTimer <= 0)
                {
                    MakeCombatDecisions();
                    DecisionTimer = 0.5f;
                }
            }
            DoBuildingTimers(elapsedTime);
            DoTroopTimers(elapsedTime);
        }

        private void MakeCombatDecisions()
        {
            if (!HostileTargetsOnPlanet)
                return;

            for (int i = 0; i < TilesList.Count; ++i)
            {
                PlanetGridSquare pgs = TilesList[i];
                if (pgs.TroopsAreOnTile)
                    CourseOfAction(pgs.SingleTroop, pgs);

                else if (pgs.BuildingPerformesAutoCombat(Ground))
                    CourseOfAction(pgs.building, pgs);
            }
        }

        private void CourseOfAction(Building b, PlanetGridSquare ourTile)
        {
            // scan for enemies
            PlanetGridSquare nearestTargetTile = SpotClosestHostile(ourTile, Owner); 
            if (nearestTargetTile == null)
                return; // no targets on planet

            // find range
            if (!nearestTargetTile.InRangeOf(ourTile, 1)) // right now all buildings have range 1
                return;

            // start combat
            b.UpdateAttackActions(-1); // why not when actually resolving combat?
            CombatScreen.StartCombat(ourTile, nearestTargetTile, Ground);
        }

        private void CourseOfAction(Troop t, PlanetGridSquare ourTile)
        {
            if (!t.CanMove && !t.CanAttack)
                return;

            // scan for enemies
            PlanetGridSquare nearestTargetTile = SpotClosestHostile(ourTile, t.Loyalty);
            if (nearestTargetTile == null)
                return; // no targets on planet. no need to move or attack

            // find range
            if (t.CanAttack && nearestTargetTile.InRangeOf(ourTile, t.Range)) 
            {
                // start combat
                t.UpdateAttackActions(-1); // why not when actually resolving combat?
                t.UpdateMoveActions(-1);
                t.facingRight = nearestTargetTile.x >= ourTile.x;
                CombatScreen.StartCombat(ourTile, nearestTargetTile, Ground);
            }

            // move - need support in movement more than 1
            MoveTowardsTarget(t, ourTile, nearestTargetTile);
        }

        private void MoveTowardsTarget(Troop t, PlanetGridSquare ourTile, PlanetGridSquare targetTile)
        {
            if (!t.CanMove)
                return;

            TargetDirection direction   = ourTile.GetDirectionTo(targetTile);
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
        
        // try 3 directions to move into, based on general direction to thetarget
        private PlanetGridSquare PickTileToMoveTo(TargetDirection direction, PlanetGridSquare ourTile)
        {
            switch (direction)
            {
                case TargetDirection.North:     return FreeTile(direction, ourTile) ?? 
                                                       FreeTile(TargetDirection.NorthEast, ourTile) ?? 
                                                       FreeTile(TargetDirection.NorthWest, ourTile);
                case TargetDirection.South:     return FreeTile(direction, ourTile) ??
                                                       FreeTile(TargetDirection.SouthEast, ourTile) ??
                                                       FreeTile(TargetDirection.SouthWest, ourTile);
                case TargetDirection.East:      return FreeTile(direction, ourTile) ??
                                                       FreeTile(TargetDirection.NorthEast, ourTile) ??
                                                       FreeTile(TargetDirection.SouthEast, ourTile);
                case TargetDirection.West:      return FreeTile(direction, ourTile) ??
                                                       FreeTile(TargetDirection.NorthWest, ourTile) ??
                                                       FreeTile(TargetDirection.SouthWest, ourTile);
                case TargetDirection.NorthEast: return FreeTile(direction, ourTile) ??
                                                       FreeTile(TargetDirection.North, ourTile) ??
                                                       FreeTile(TargetDirection.East, ourTile);
                case TargetDirection.NorthWest: return FreeTile(direction, ourTile) ??
                                                       FreeTile(TargetDirection.North, ourTile) ??
                                                       FreeTile(TargetDirection.West, ourTile);
                case TargetDirection.SouthEast: return FreeTile(direction, ourTile) ??
                                                       FreeTile(TargetDirection.South, ourTile) ??
                                                       FreeTile(TargetDirection.East, ourTile);
                case TargetDirection.SouthWest: return FreeTile(direction, ourTile) ??
                                                       FreeTile(TargetDirection.South, ourTile) ??
                                                       FreeTile(TargetDirection.West, ourTile);
                default: return null;
            }
        }

        private PlanetGridSquare FreeTile(TargetDirection direction, PlanetGridSquare ourTile)
        {
            ourTile.ConvertDirectionToCoordinates(direction, out int x, out int y);
            PlanetGridSquare tile = Ground.GetTileByCoordinates(x, y);
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

        private void TryAttackWithCombatBuilding(PlanetGridSquare pgs)
        {
            for (int i = 0; i < TilesList.Count; i++)
            {
                PlanetGridSquare planetGridSquare = TilesList[i];
                if (CombatScreen.TroopCanAttackSquare(pgs, planetGridSquare, Ground))
                {
                    pgs.building.UpdateAttackActions(-1);
                    CombatScreen.StartCombat(pgs, planetGridSquare, Ground);
                    break;
                }
            }
        }

        private void MoveTroop(PlanetGridSquare ourTile)
        {
            bool hasAttacked = false;
            Troop troop = ourTile.SingleTroop;
            if (troop.CanAttack)
            {
                if (!troop.Loyalty.isPlayer || !Empire.Universe.LookingAtPlanet ||
                    (!(Empire.Universe.workersPanel is CombatScreen) ||
                     ((CombatScreen) Empire.Universe.workersPanel).p != Ground) || GlobalStats.AutoCombat)
                {
                    foreach (PlanetGridSquare targetTile in TilesList)
                    {
                        if (!CombatScreen.TroopCanAttackSquare(ourTile, targetTile, Ground))
                            continue;

                        hasAttacked = true;
                        if (troop.CanAttack)
                        {
                            troop.UpdateAttackActions(-1);
                            troop.UpdateMoveActions(-1);
                            if (targetTile.x > ourTile.x)
                                troop.facingRight = true;
                            else if (targetTile.x < ourTile.x)
                                troop.facingRight = false;
                            CombatScreen.StartCombat(ourTile, targetTile, Ground);
                        }
                        break;
                    }
                }
                else return;
            }

            try
            {
                if (hasAttacked || ourTile.NoTroopsOnTile || !ourTile.SingleTroop.CanMove)
                    return;

                foreach (PlanetGridSquare tileToMoveTo in TilesList.OrderBy(tile =>
                    Math.Abs(tile.x - ourTile.x) + Math.Abs(tile.y - ourTile.y)))
                {
                    if (ourTile.NoTroopsOnTile)
                        break;

                    if (tileToMoveTo == ourTile)
                        continue; // same tile

                    if (tileToMoveTo.TroopsAreOnTile)
                    {
                        if (tileToMoveTo.SingleTroop.Loyalty == ourTile.SingleTroop.Loyalty)
                            continue; // friendlies

                        if (tileToMoveTo.x > ourTile.x)
                        {
                            if (tileToMoveTo.y > ourTile.y)
                            {
                                if (TryTroopMove(1, 1, ourTile))
                                    break;
                            }

                            if (tileToMoveTo.y < ourTile.y)
                            {
                                if (TryTroopMove(1, -1, ourTile))
                                    break;
                            }

                            if (!TryTroopMove(1, 0, ourTile))
                            {
                                if (!TryTroopMove(1, -1, ourTile))
                                {
                                    if (TryTroopMove(1, 1, ourTile))
                                        break;
                                }
                                else
                                    break;
                            }
                            else
                                break;
                        }
                        else if (tileToMoveTo.x < ourTile.x)
                        {
                            if (tileToMoveTo.y > ourTile.y)
                            {
                                if (TryTroopMove(-1, 1, ourTile))
                                    break;
                            }

                            if (tileToMoveTo.y < ourTile.y)
                            {
                                if (TryTroopMove(-1, -1, ourTile))
                                    break;
                            }

                            if (!TryTroopMove(-1, 0, ourTile))
                            {
                                if (!TryTroopMove(-1, -1, ourTile))
                                {
                                    if (TryTroopMove(-1, 1, ourTile))
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
                            if (tileToMoveTo.y > ourTile.y)
                            {
                                if (TryTroopMove(0, 1, ourTile))
                                    break;
                            }

                            if (tileToMoveTo.y < ourTile.y)
                            {
                                if (TryTroopMove(0, -1, ourTile))
                                    break;
                            }
                        }
                    }
                    else if (tileToMoveTo.building != null &&
                             (tileToMoveTo.building.CombatStrength > 0 ||
                              !string.IsNullOrEmpty(tileToMoveTo.building.EventTriggerUID)) &&
                             (Owner != ourTile.SingleTroop.Loyalty ||
                              !string.IsNullOrEmpty(tileToMoveTo.building.EventTriggerUID)))
                    {
                        if (tileToMoveTo.x > ourTile.x)
                        {
                            if (tileToMoveTo.y > ourTile.y)
                            {
                                if (TryTroopMove(1, 1, ourTile))
                                    break;
                            }

                            if (tileToMoveTo.y < ourTile.y)
                            {
                                if (TryTroopMove(1, -1, ourTile))
                                    break;
                            }

                            if (!TryTroopMove(1, 0, ourTile))
                            {
                                if (!TryTroopMove(1, -1, ourTile))
                                {
                                    if (TryTroopMove(1, 1, ourTile))
                                        break;
                                }
                                else
                                    break;
                            }
                            else
                                break;
                        }
                        else if (tileToMoveTo.x < ourTile.x)
                        {
                            if (tileToMoveTo.y > ourTile.y)
                            {
                                if (TryTroopMove(-1, 1, ourTile))
                                    break;
                            }

                            if (tileToMoveTo.y < ourTile.y)
                            {
                                if (TryTroopMove(-1, -1, ourTile))
                                    break;
                            }

                            if (!TryTroopMove(-1, 0, ourTile))
                            {
                                if (!TryTroopMove(-1, -1, ourTile))
                                {
                                    if (TryTroopMove(-1, 1, ourTile))
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
                            if (tileToMoveTo.y > ourTile.y)
                            {
                                if (!TryTroopMove(0, 1, ourTile))
                                {
                                    if (!TryTroopMove(1, 1, ourTile))
                                    {
                                        if (TryTroopMove(-1, 1, ourTile))
                                            break;
                                    }
                                    else
                                        break;
                                }
                                else
                                    break;
                            }

                            if (tileToMoveTo.y < ourTile.y)
                            {
                                if (!TryTroopMove(0, -1, ourTile))
                                {
                                    if (!TryTroopMove(1, -1, ourTile))
                                    {
                                        if (TryTroopMove(-1, -1, ourTile))
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
            catch
            {
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
                    if (start.TroopsAreOnTile)
                    {
                        troop = start.SingleTroop;
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
                        troop.UpdateMoveActions(-1);
                        troop.ResetMoveTimer();
                        eventLocation.TroopsHere.Add(troop);
                        start.TroopsHere.Clear();
                    }
                    if (!eventLocation.EventOnTile || (eventLocation.NoTroopsOnTile || eventLocation.SingleTroop.Loyalty.isFaction))
                        return true;
                }

                ResourceManager.EventsDict[eventLocation.building?.EventTriggerUID].TriggerPlanetEvent(Ground, eventLocation.SingleTroop.Loyalty, eventLocation, Empire.Universe);
            }
            return false;
        }

        private void DoBuildingTimers(float elapsedTime)
        {
            if (BuildingList.Count <= 0)
                return;

            for (int i = 0; i < BuildingList.Count; i++)
            {
                Building building = BuildingList[i];
                if (building == null)
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
                    attacker.SingleTroop.AddKill(); // FB - for now multi troops on same tile is not supported
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

            foreach (PlanetGridSquare PGS in TilesList)
            {
                num += PGS.MaxAllowedTroops;
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