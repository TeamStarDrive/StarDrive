using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        // war, defense and claim tasks keep their fleet when done; guard and exploration forces break up
        public static bool UsesKeptFleets(TaskType type)
        {
            switch (type)
            {
                case TaskType.DefendClaim:
                case TaskType.StrikeForce:
                case TaskType.AssaultPlanet:
                case TaskType.GlassPlanet:
                case TaskType.StageFleet:
                case TaskType.ReclaimPlanet:
                case TaskType.DefendPostInvasion:
                case TaskType.ClearAreaOfEnemies:
                case TaskType.DefendVsRemnants:
                case TaskType.AssaultPirateBase:
                case TaskType.InhibitorInvestigate: return true;
                default:                            return false;
            }
        }

        bool KeepsItsFleet => Fleet.Kept || UsesKeptFleets(Type);

        // a task ended again must not take back a fleet that has moved on to another task
        bool StillHoldsItsFleet => Fleet.FleetTask == this;

        // a force has at least some strength, even for a task that asks for none
        float KeptForceStrengthWanted => MinimumTaskForceStrength.LowerBound(1f);

        // the defense of the empire's own system may form a new fleet over the cap
        bool DefendsOwnSystem => Type == TaskType.DefendVsRemnants
                              || Type == TaskType.ClearAreaOfEnemies && TargetSystem?.HasPlanetsOwnedBy(Owner) == true;

        // an idle fleet with the strength, else one topped up from the pool, else a new fleet under the cap, else two idle fleets joined
        RequisitionStatus FindKeptForce(Planet rallyPoint, float battleFleetSize, bool highTroopPriority)
        {
            float wanted = KeptForceStrengthWanted;
            float poolStrength = Owner.ShipsReadyForFleet.CombatStrength;
            Fleet[] idle = Owner.IdleFleetsNearest(AO);
            float[] ready = new float[idle.Length];
            for (int i = 0; i < idle.Length; ++i)
                ready[i] = idle[i].ReadyStrength;

            var search = new KeptForceSearch(NeededTroopStrength, TaskBombTimeNeeded);
            for (int i = 0; i < idle.Length; ++i)
            {
                if (ready[i] >= wanted && TryKeptForce(idle[i], null, rallyPoint, highTroopPriority, search))
                    return RequisitionStatus.Complete;
            }

            for (int i = 0; i < idle.Length; ++i)
            {
                if (ready[i] < wanted && ready[i] + poolStrength >= wanted
                    && TryKeptForce(idle[i], null, rallyPoint, highTroopPriority, search))
                    return RequisitionStatus.Complete;
            }

            // a new fleet has only the pool's troops and bombing, so it falls short wherever an idle fleet did
            if ((DefendsOwnSystem || Owner.KeptFleetCount() < Owner.KeptFleetCap()) && !search.ShortOfSupport)
            {
                search.RestoreNeeds(this);
                RequisitionStatus status = FormFleetFromPool(rallyPoint, battleFleetSize, highTroopPriority, wantedFleets: 1,
                                                             endIfOutmatched: false);
                if (status == RequisitionStatus.Complete)
                    return status;
                search.Note(status, bombs: 0, troops: 0f);
            }

            for (int i = 0; i < idle.Length; ++i)
            {
                for (int j = i + 1; j < idle.Length; ++j)
                {
                    if (ready[i] + ready[j] + poolStrength >= wanted
                        && TryKeptForce(idle[i], idle[j], rallyPoint, highTroopPriority, search))
                        return RequisitionStatus.Complete;
                }
            }

            // every force with the troops and bombing was outmatched, so the task ends; one short of them waits instead
            if (search.Outmatched && !search.ShortOfSupport)
            {
                EndTask();
                return RequisitionStatus.NotEnoughShipStrength;
            }
            return search.Status;
        }

        // the task's troop and bombing needs before any try, which the support check may trade between, and what the tries fell short of
        sealed class KeptForceSearch
        {
            readonly int TroopsNeeded;
            readonly int BombingNeeded;
            public RequisitionStatus Status = RequisitionStatus.FailedToCreateAFleet;
            public bool Outmatched;
            Array<(int Bombs, float Troops)> FellShort; // the bombing and troops of the forces short of them

            public KeptForceSearch(int troopsNeeded, int bombingNeeded)
            {
                TroopsNeeded = troopsNeeded;
                BombingNeeded = bombingNeeded;
            }

            public bool ShortOfSupport => FellShort != null;

            public void RestoreNeeds(MilitaryTask task)
            {
                task.NeededTroopStrength = TroopsNeeded;
                task.TaskBombTimeNeeded = BombingNeeded;
            }

            // a force with no more bombing and troops than one that fell short of them falls short too
            public bool FallsShortLikeAnother(int bombs, float troops)
            {
                if (FellShort == null)
                    return false;
                for (int i = 0; i < FellShort.Count; ++i)
                {
                    if (bombs <= FellShort[i].Bombs && troops <= FellShort[i].Troops)
                        return true;
                }
                return false;
            }

            public void Note(RequisitionStatus status, int bombs, float troops)
            {
                if (status is RequisitionStatus.NotEnoughTroopStrength or RequisitionStatus.NotEnoughBomberStrength)
                {
                    if (FellShort == null)
                    {
                        Status = status;
                        FellShort = new();
                    }
                    FellShort.Add((bombs, troops));
                }
                else if (status == RequisitionStatus.NotEnoughShipStrength)
                {
                    Outmatched = true;
                }
            }
        }

        // a force short of troops or bombing, or outmatched, leaves the search to the next one
        bool TryKeptForce(Fleet fleet, Fleet second, Planet rallyPoint, bool highTroopPriority, KeptForceSearch search)
        {
            int bombs = fleet.ReadyBombSecs + (second?.ReadyBombSecs ?? 0);
            float troops = fleet.ReadyTroopStrength + (second?.ReadyTroopStrength ?? 0f);
            if (search.FallsShortLikeAnother(bombs, troops))
                return false;

            search.RestoreNeeds(this);
            RequisitionStatus status = TakeKeptForce(fleet, second, rallyPoint, highTroopPriority, endIfOutmatched: false);
            if (status == RequisitionStatus.Complete)
                return true;

            search.Note(status, bombs, troops);
            return false;
        }

        // a small task takes the nearest idle fleet that has the strength it needs, when it has no more than twice that
        Fleet IdleFleetForSmallTask()
        {
            float wanted = KeptForceStrengthWanted;
            Fleet[] idle = Owner.IdleFleetsNearest(AO);
            for (int i = 0; i < idle.Length; ++i)
            {
                float ready = idle[i].ReadyStrength;
                if (ready >= wanted && ready <= wanted * 2f)
                    return idle[i];
            }
            return null;
        }

        // the fleet takes the task with the second fleet's ships, and pool ships for the strength, bombing and troops it lacks
        RequisitionStatus TakeKeptForce(Fleet fleet, Fleet second, Planet rallyPoint, bool highTroopPriority, bool endIfOutmatched = true)
        {
            FleetShips pool = Owner.ShipsReadyForFleet;
            float strength = fleet.ReadyStrength + (second?.ReadyStrength ?? 0f);
            HashSet<Ship> combatShips = pool.NearestCombatShips(AO, KeptForceStrengthWanted - strength);
            foreach (Ship ship in combatShips)
                strength += ship.GetStrength();

            int bombSecs = fleet.ReadyBombSecs + (second?.ReadyBombSecs ?? 0);
            float troops = fleet.ReadyTroopStrength + (second?.ReadyTroopStrength ?? 0f);
            RequisitionStatus needs = CheckSupportNeeds(pool, bombSecs, troops, rallyPoint, highTroopPriority, out Array<Troop> planetTroops);
            if (needs != RequisitionStatus.Complete)
                return needs;

            if (ForceTooWeakToGoIn(strength))
            {
                if (endIfOutmatched)
                    EndTask();
                return RequisitionStatus.NotEnoughShipStrength;
            }

            int shipCount = fleet.Ships.Count + (second?.Ships.Count ?? 0) + combatShips.Count;
            int fleetSets = ((int)(shipCount / pool.ShipsPerFullFleet)).LowerBound(1);
            Array<Ship> joining = pool.ExtractShips(combatShips);
            joining.AddRange(pool.ExtractBombersAndTroops(AO, TaskBombTimeNeeded - bombSecs, NeededTroopStrength - (int)troops,
                                                          fleetSets, planetTroops));
            second?.JoinInto(fleet);
            Fleet = fleet;
            fleet.TakeTask(this, GetFleetName(), joining);
            Log.Info($"{Owner.Name} Took Kept Fleet {fleet.Name} NumShips={fleet.Ships.Count} Strength={fleet.GetStrength()} MinStrength={MinimumTaskForceStrength}");
            return RequisitionStatus.Complete;
        }
    }
}
