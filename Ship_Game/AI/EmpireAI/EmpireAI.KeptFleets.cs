using System;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        // once per AI turn, before tasks: small idle fleets break up, fleets over the cap join, fleets in orbit of their rally planet refit, the largest splits, and the strength each keeps fades
        void UpdateIdleFleets()
        {
            if (!OwnerEmpire.KeepsFleets)
                return;

            DissolveSmallIdleFleets();
            JoinIdleFleetsOverTheCap();
            RefitIdleFleetsAtRally(ship => ShipBuilder.PickShipToRefit(ship, OwnerEmpire));
            SplitTheLargestIdleFleet();

            foreach (Fleet fleet in OwnerEmpire.ActiveFleets)
            {
                if (fleet.IsIdle)
                    fleet.FadeStrengthToKeep();
            }
        }

        // under the cap, an idle fleet that holds more than all the empire's other fleets together splits in two; the empire's only fleet stays whole
        void SplitTheLargestIdleFleet()
        {
            FleetShips pool = OwnerEmpire.ShipsReadyForFleet;
            if (pool == null || OwnerEmpire.KeptFleetCount() >= OwnerEmpire.KeptFleetCap())
                return;

            Fleet largest = null;
            float largestStrength = 0f;
            float total = 0f;
            int count = 0;
            foreach (Fleet fleet in OwnerEmpire.ActiveFleets)
            {
                if (!fleet.CountsTowardFleetCap)
                    continue;

                float strength = fleet.GetStrength();
                total += strength;
                ++count;
                if (strength > largestStrength)
                {
                    largest = fleet;
                    largestStrength = strength;
                }
            }

            if (count >= 2 && largest is { IsIdle: true } && largestStrength > total - largestStrength)
                largest.SplitOff(pool.ShipsPerFullFleet);
        }

        // an idle fleet with fewer ships than half a fleet ratio, ships away refitting counted, breaks up into the pool
        void DissolveSmallIdleFleets()
        {
            FleetShips pool = OwnerEmpire.ShipsReadyForFleet;
            if (pool == null)
                return;

            float halfARatio = pool.ShipsPerFullFleet / 2f;
            foreach (Fleet fleet in OwnerEmpire.ActiveFleets.ToArrayList())
            {
                if (fleet.IsIdle && fleet.Ships.Count + fleet.ShipsAwayRefitting() < halfARatio)
                    fleet.Reset(fleeIfOutmatched: true);
            }
        }

        // the cap's floor follows the most it has been; over the cap, the weakest idle fleet joins the nearest until the count is back at it
        void JoinIdleFleetsOverTheCap()
        {
            if (!OwnerEmpire.KeepsFleets)
                return;

            OwnerEmpire.UpdatePeakFleetCap();

            for (int over = OwnerEmpire.KeptFleetCount() - OwnerEmpire.KeptFleetCapForJoins(); over > 0; --over)
            {
                Fleet weakest = null;
                float weakestStrength = float.MaxValue;
                foreach (Fleet fleet in OwnerEmpire.ActiveFleets)
                {
                    if (fleet.IsIdle && fleet.GetStrength() < weakestStrength)
                    {
                        weakest = fleet;
                        weakestStrength = fleet.GetStrength();
                    }
                }

                if (weakest == null)
                    return;

                Fleet[] nearest = OwnerEmpire.IdleFleetsNearest(weakest.AveragePosition(force: true));
                Fleet joinTo = nearest.Find(f => f != weakest);
                if (joinTo == null)
                    return;

                joinTo.TakeInIdleFleet(weakest);
            }
        }

        // an idle fleet in orbit of its rally planet sends its oldest designs that have a better one to refit, a third of the fleet at a time
        void RefitIdleFleetsAtRally(Func<Ship, IShipDesign> betterDesign)
        {
            if (OwnerEmpire.SafeSpacePorts.Length == 0)
                return;

            var designs = new Map<string, IShipDesign>(); // one look per design
            foreach (Fleet fleet in OwnerEmpire.ActiveFleets.ToArrayList())
            {
                if (!fleet.IsIdle || !fleet.TakeRefitCheckAtRally())
                    continue;

                int room = fleet.RoomToRefit();
                Ship[] oldestFirst = fleet.Ships.Filter(s => s.Active && !s.Resupplying && !s.InCombat && MayRefit(s))
                                                .Sorted(s => s.ShipData.TechsNeeded.Count);
                for (int i = 0; i < oldestFirst.Length && room > 0; ++i)
                {
                    Ship ship = oldestFirst[i];
                    if (!designs.TryGetValue(ship.Name, out IShipDesign design))
                    {
                        design = betterDesign(ship);
                        designs[ship.Name] = design;
                    }

                    if (design != null)
                    {
                        StartRefit(ship, design);
                        --room;
                    }
                }
            }
        }
    }
}
