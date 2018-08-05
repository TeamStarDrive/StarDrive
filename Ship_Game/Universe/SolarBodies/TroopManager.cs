using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class TroopManager
    {
        private readonly SolarSystemBody Ground;

        private Array<PlanetGridSquare> TilesList => Ground.TilesList;
        private Empire Owner => Ground.Owner;
        private BatchRemovalCollection<Troop> TroopsHere => Ground.TroopsHere;
        private Array<Building> BuildingList => Ground.BuildingList;
        private BatchRemovalCollection<Combat> ActiveCombats => Ground.ActiveCombats;       
        private SolarSystem ParentSystem => Ground.ParentSystem;
      
        private float DecisionTimer = 0.5f;        
        private float InCombatTimer;
        private int NumInvadersLast;

        public bool RecentCombat => InCombatTimer > 0.0f;

        public void SetInCombat( float timer = 10f)
        {
            InCombatTimer = timer;
        }

        // ReSharper disable once UnusedParameter.Local Habital concept here is to not use this class if the planet cant have
        // ground combat. but that will be a future project. 
        public TroopManager (SolarSystemBody solarSystemBody, bool habitable)
        {
            Ground = solarSystemBody;
        }
        public void Update(float elapsedTime)
        {
            if (Empire.Universe.Paused) return;
            DecisionTimer -= elapsedTime;
            InCombatTimer -= elapsedTime;
            if (TroopsHere.Count > 0)
            {
                {
                    DoCombats(elapsedTime);
                    if (DecisionTimer <= 0)
                    {
                        MakeCombatDecisions();
                        DecisionTimer = 0.5f;
                    }
                }

            }
            if (TroopsHere.Count != 0 || BuildingList.Count != 0)
                DoTroopTimers(elapsedTime);
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
                        if (pgs.TroopsHere[0].GetOwner() != Empire.Universe.PlayerEmpire || !Empire.Universe.LookingAtPlanet || (!(Empire.Universe.workersPanel is CombatScreen) || (Empire.Universe.workersPanel as CombatScreen).p != (Planet) Ground) || GlobalStats.AutoCombat)
                        {
                            {
                                foreach (PlanetGridSquare planetGridSquare in TilesList)
                                {
                                    if (CombatScreen.TroopCanAttackSquare(pgs, planetGridSquare, (Planet) Ground))
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
                                            CombatScreen.StartCombat(pgs, planetGridSquare, (Planet) Ground);
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
                                if (planetGridSquare != pgs)
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

                else if (pgs.building != null && pgs.building.CombatStrength > 0 && (Owner != Empire.Universe.PlayerEmpire || !Empire.Universe.LookingAtPlanet || (!(Empire.Universe.workersPanel is CombatScreen) || (Empire.Universe.workersPanel as CombatScreen).p != (Planet) Ground) || GlobalStats.AutoCombat) && pgs.building.AvailableAttackActions > 0)
                {
                    for (int i = 0; i < TilesList.Count; i++)
                    {
                        PlanetGridSquare planetGridSquare = TilesList[i];
                        if (CombatScreen.TroopCanAttackSquare(pgs, planetGridSquare, (Planet) Ground))
                        {
                            --pgs.building.AvailableAttackActions;
                            CombatScreen.StartCombat(pgs, planetGridSquare, (Planet) Ground);
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

                ResourceManager.EventsDict[eventLocation.building.EventTriggerUID].TriggerPlanetEvent((Planet) Ground, eventLocation.TroopsHere[0].GetOwner(), eventLocation, Empire.Universe);
            }
            return false;
        }

        public void DoTroopTimers(float elapsedTime)
        {
            //foreach (Building building in (Planet) Ground.BuildingList)
            for (int x = 0; x < BuildingList.Count; x++)
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
            //foreach (Troop troop in (Planet) Ground.TroopsHere)
            for (int x = 0; x < TroopsHere.Count; x++)
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
                        continue;
                    }
                    if (combat.Attacker.TroopsHere.Count > 0)
                    {
                        if (combat.Attacker.TroopsHere[0].Strength <= 0)
                        {
                            ActiveCombats.QueuePendingRemoval(combat);
                            continue;
                        }
                    }
                    else if (combat.Attacker.building != null && combat.Attacker.building.Strength <= 0)
                    {
                        ActiveCombats.QueuePendingRemoval(combat);
                        continue;
                    }
                    if (combat.Defender.TroopsHere.Count == 0 && combat.Defender.building == null)
                    {
                        ActiveCombats.QueuePendingRemoval(combat);
                        continue;
                    }
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

        public void DoCombats(float elapsedTime)
        {
            if (Empire.Universe.LookingAtPlanet)
            {
                if (Empire.Universe.workersPanel is CombatScreen)
                {
                    if ((Empire.Universe.workersPanel as CombatScreen).p == (Planet) Ground)
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
                InCombatTimer = 10f;
            if (TroopsHere.Count <= 0 || Owner == null)
                return;
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
                        }
                        else
                            ++num1;
                    }
                if (planetGridSquare.building != null && planetGridSquare.building.CombatStrength > 0)
                    ++num1;
            }

            if (num2 > NumInvadersLast && NumInvadersLast == 0)
            {
                if (Empire.Universe.PlayerEmpire == Owner)
                    Empire.Universe.NotificationManager.AddEnemyTroopsLandedNotification((Planet) Ground, index, Owner);
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
            NumInvadersLast = num2;
            if (num2 <= 0 || num1 != 0)//|| (Planet) Ground.Owner == null)
                return;
            Ground.ChangeOwnerByInvasion(index);
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

            foreach (PlanetGridSquare PGS in TilesList)
            {
                num += PGS.number_allowed_troops;

            }
            return num; //(int)(this.TilesList.Sum(spots => spots.number_allowed_troops));// * (.25f + this.developmentLevel*.2f));
        }

        public float GetGroundStrengthOther(Empire allButThisEmpire)
        {
            //float num = 0;
            //if (this.Owner == null || this.Owner != empire)
            //    num += this.BuildingList.Sum(offense => offense.CombatStrength > 0 ? offense.CombatStrength : 1);
            //this.TroopsHere.thisLock.EnterReadLock();
            //num += this.TroopsHere.Where(empiresTroops => empiresTroops.GetOwner() == null || empiresTroops.GetOwner() != empire).Sum(strength => strength.Strength);
            //this.TroopsHere.thisLock.ExitReadLock();
            //return num;
            float enemyTroopStrength = 0;
            for (int x = 0; x < TroopsHere.Count; x++)
            {
                Troop trooper = TroopsHere[x];
                if (trooper.OwnerString == allButThisEmpire.data.Traits.Name)
                    continue;
                enemyTroopStrength += trooper.Strength;
            }

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
                    enemyTroopStrength += b.Strength + b.CombatStrength;
            }
            return enemyTroopStrength;            
        }

        public bool TroopsHereAreEnemies(Empire empire)
        {
            bool enemies = false;
            using (TroopsHere.AcquireReadLock())
                foreach (Troop trooper in TroopsHere)
                {
                    if (!empire.TryGetRelations(trooper.GetOwner(), out Relationship trouble) || trouble.AtWar)
                    {
                        enemies = true;
                        break;
                    }

                }
            return enemies;
        }

        public int GetGroundLandingSpots()
        {            
            int spotCount = TilesList.Sum(spots => spots.number_allowed_troops); //.FilterBy(spot => (spot.building?.CombatStrength ?? 0) < 1)
            int troops    = TroopsHere.FilterBy(owner => owner.GetOwner() == Owner).Length;
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
                    troop.Strength = (troop.Strength + 2).Clamped(0, troop.ActualStrengthMax);
        }
    }
}