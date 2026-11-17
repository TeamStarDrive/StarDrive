using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        // war and defense tasks keep their fleet when done; expansion forces break up
        public static bool UsesKeptFleets(TaskType type)
        {
            switch (type)
            {
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

            for (int i = 0; i < idle.Length; ++i)
            {
                if (ready[i] >= wanted)
                    return TakeKeptForce(idle[i], null, rallyPoint, highTroopPriority);
            }

            for (int i = 0; i < idle.Length; ++i)
            {
                if (ready[i] + poolStrength >= wanted)
                    return TakeKeptForce(idle[i], null, rallyPoint, highTroopPriority);
            }

            int troopsNeeded = NeededTroopStrength;
            int bombingNeeded = TaskBombTimeNeeded;
            if (DefendsOwnSystem || Owner.KeptFleetCount() < Owner.KeptFleetCap())
            {
                RequisitionStatus status = FormFleetFromPool(rallyPoint, battleFleetSize, highTroopPriority, wantedFleets: 1);
                if (status == RequisitionStatus.Complete || QueuedForRemoval)
                    return status;
            }

            for (int i = 0; i < idle.Length; ++i)
            {
                for (int j = i + 1; j < idle.Length; ++j)
                {
                    if (ready[i] + ready[j] + poolStrength >= wanted)
                    {
                        NeededTroopStrength = troopsNeeded; // a failed new fleet weighed them against the pool's bombers alone
                        TaskBombTimeNeeded = bombingNeeded;
                        return TakeKeptForce(idle[i], idle[j], rallyPoint, highTroopPriority);
                    }
                }
            }

            return RequisitionStatus.FailedToCreateAFleet;
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
        RequisitionStatus TakeKeptForce(Fleet fleet, Fleet second, Planet rallyPoint, bool highTroopPriority)
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

            if (FleetWouldCallItselfOff(strength))
            {
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
