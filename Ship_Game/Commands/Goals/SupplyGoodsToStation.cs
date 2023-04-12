using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using static Ship_Game.AI.ShipAI;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class SupplyGoodsToStation : Goal
    {
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public Goods Goods { get; private set; }

        public override bool IsSupplyingGoodsToStationStationGoal(Ship targetStation) => TargetStation == targetStation;

        Ship TargetStation => TargetShip;
        Ship Freighter => FinishedShip;

        bool StationApplicable => TargetShip != null && TargetShip.Active && TargetShip.Loyalty == Owner;

        [StarDataConstructor]
        public SupplyGoodsToStation(Empire owner) : base(GoalType.SupplyGoodsToStation, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                SetupTrade,
                WaitForFrieghter
            };
        }

        public SupplyGoodsToStation(Empire owner, Ship targetStation, Goods goods)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            Owner = owner;
            TargetShip= targetStation;
            Goods = goods;
        }


        GoalStep SetupTrade()
        {
            if (!StationApplicable)
                return GoalStep.GoalFailed;

            if (Owner.TryDispatchGoodsSupplyToStation(Goods, TargetStation, out Empire.ExportPlanetAndFreighter exportAndFreighter))
            {
                Planet exportPlanet = exportAndFreighter.Planet;
                FinishedShip = exportAndFreighter.Freighter;
                exportAndFreighter.Freighter.AI.SetupFreighterPlan(exportPlanet, TargetStation, Goods);
                return GoalStep.GoToNextStep;
            }

            return LifeTime > 2 ? GoalStep.GoalFailed : GoalStep.TryAgain;
        }

        GoalStep WaitForFrieghter()
        {
            if (!StationApplicable)
                return GoalStep.GoalFailed;

            if (Freighter == null || !Freighter.Active)
                return GoalStep.GoalFailed;

            if (Freighter.AI.OrderQueue.TryPeekFirst(out ShipGoal shipGoal))
            {
                if (shipGoal.Trade?.TargetStation == TargetStation)
                    return GoalStep.TryAgain;
            }

            return GoalStep.GoalComplete;
        }
    }
}
