using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    // an empire's order to salvage one derelict, worked by a salvage unit of its station in the derelict's system
    [StarDataType]
    public class SalvageDerelict : Goal
    {
        [StarData] public int DerelictId;
        [StarData] public SolarSystem TargetSystem;
        Derelict Found;

        public Derelict Derelict => Found ??= UState.Derelicts.Find(DerelictId);

        [StarDataConstructor]
        public SalvageDerelict(Empire owner) : base(GoalType.SalvageDerelict, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                LaunchConstructor
            };
        }

        public SalvageDerelict(Empire owner, Derelict derelict) : this(owner)
        {
            DerelictId = derelict.Id;
            TargetSystem = derelict.System;
            Found = derelict;
        }

        public enum Block { None, Ordered, ClaimedByOther, NoStation, NoFreeUnit, Gone }

        // from any thread: why the empire cannot order the derelict salvaged, Ordered while its order stands, or None when it can
        public static Block WhyNoOrder(Empire empire, Derelict d)
        {
            if (!empire.Universe.P.SalvageOps || !d.CanBeSalvaged || !d.IsKnownBy(empire))
                return Block.Gone;
            if (d.Claimant != null && d.Claimant != empire)
                return Block.ClaimedByOther;
            if (d.Claimant == empire || OrderOf(empire, d) != null)
                return Block.Ordered;

            Ship station = SalvageOps.StationIn(empire, d.System);
            if (station == null)
                return Block.NoStation;
            return LiveUnits(station) > NumOrdersIn(empire, d.System) ? Block.None : Block.NoFreeUnit;
        }

        public static SalvageDerelict OrderOf(Empire empire, Derelict d)
            => empire.AI.FindGoal(g => g is SalvageDerelict s && s.DerelictId == d.Id) as SalvageDerelict;

        // every order of the empire's in the system takes a unit of its one station there
        static int NumOrdersIn(Empire empire, SolarSystem system)
            => empire.AI.CountGoals(g => g is SalvageDerelict s && s.TargetSystem == system);

        public static int LiveUnits(Ship station)
        {
            return station.Modules.Count(m => m.Salvage && m.Active);
        }

        // the derelict panel's and the derelicts window's Salvage button; false when the empire cannot order it
        public static bool Order(Empire empire, Derelict d)
        {
            if (WhyNoOrder(empire, d) != Block.None)
                return false;
            empire.AI.AddGoalAndEvaluate(new SalvageDerelict(empire, d));
            return true;
        }

        // the button's second click calls the order off on the sim thread, where its claim is given up
        public static void Cancel(Empire empire, Derelict d)
        {
            if (empire.Universe.Screen is { IsOnSimThread: false } screen)
            {
                screen.RunOnSimThread(() => Cancel(empire, d));
                return;
            }
            if (OrderOf(empire, d) is { } order)
                empire.AI.RemoveGoal(order);
            else if (d.Claimant == empire) // a claim kept without its order, as a save can catch it
                d.ReleaseClaim();
        }

        // an order stands while salvage ops are on and its derelict can be salvaged, unless another empire claimed it first
        bool Stands => Owner.Universe.P.SalvageOps && Derelict is { CanBeSalvaged: true } d && (d.Claimant == null || d.Claimant == Owner);

        protected override GoalStep? PreEvaluate() => Stands ? null : GoalStep.GoalFailed;

        // an order that ends gives up its claim, so the derelict's timer runs on, unless another order of its empire's holds the derelict
        public override void OnRemoved()
        {
            if (Derelict is { } d && d.Claimant == Owner
                && !Owner.AI.HasGoal(g => g != this && g is SalvageDerelict s && s.DerelictId == DerelictId))
                d.ReleaseClaim();
        }

        GoalStep LaunchConstructor()
        {
            return HoldsAUnit() ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        // the first orders that stand hold the station's units, and a second for the same derelict ends; the goals are evaluated last to first
        bool HoldsAUnit()
        {
            Ship station = SalvageOps.StationIn(Owner, TargetSystem);
            if (station == null)
                return false;

            int ahead = 0;
            var goals = Owner.AI.Goals;
            for (int i = 0; i < goals.Count && goals[i] != this; ++i)
            {
                if (goals[i] is SalvageDerelict s && s.TargetSystem == TargetSystem && s.Stands)
                {
                    if (s.DerelictId == DerelictId)
                        return false;
                    ++ahead;
                }
            }
            return ahead < LiveUnits(station);
        }
    }
}
