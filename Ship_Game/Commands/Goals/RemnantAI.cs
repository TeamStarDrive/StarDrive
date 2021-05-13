using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantAI : Goal  // TODO FB - this is legacy code
    {
        public const string ID = "RemnantAI";
        public override string UID => ID;

        public RemnantAI(): base(GoalType.RemnantAI)
        {
            Steps = new Func<GoalStep>[]
            {
                DummyStep
            };
        }

        public RemnantAI(Empire owner) : this()
        {
            empire = owner;
        }

        Planet NearestColonyTarget(Vector2 shipPosition)
        {
            return Empire.Universe.PlanetsDict.Values.ToArray()
                .FindMinFiltered(potentials => potentials.Owner == null && potentials.Habitable
                , potentials => Vector2.Distance(shipPosition, potentials.Center));
        }

        Ship GetAvailableColonyShips()
        {
            var ships = (Array<Ship>)empire.OwnedShips;
            return ships.FindMinFiltered(assimilate =>
                (assimilate.isColonyShip && assimilate.AI.State != AIState.Colonize) ||
                (assimilate.AI.State != AIState.Refit &&
                assimilate.shipData.ShipStyle != "Remnant" &&
                assimilate.shipData.ShipStyle != null &&
                !assimilate.AI.BadGuysNear), assimilate => assimilate.GetStrength());
        }

        Ship[] GetShipsForRefit()
        {
            var ships = empire.OwnedShips;
            return ships.Filter(assimilate =>
                !assimilate.isColonyShip &&
                (assimilate.AI.State != AIState.Refit &&
                assimilate.shipData.ShipStyle != "Remnant" &&
                assimilate.shipData.ShipStyle != null &&
                !assimilate.AI.BadGuysNear &&
                assimilate.GetStrength() >= 50)
            );
        }

        GoalStep DummyStep() // For Support in pre remnant logic saves
        {
            return GoalStep.GoalComplete;
        }

        GoalStep CreateAColony()
        {
            Ship ship = GetAvailableColonyShips();

            if (ship == null)
            {
                if (empire.GetPlanets().Count < 1) return GoalStep.TryAgain;
                return GoalStep.GoToNextStep;
            }

            var colonyTarget = NearestColonyTarget(ship.Position);

            if (colonyTarget == null)
            {
                if (empire.GetPlanets().Count < 1) return GoalStep.TryAgain;

                return GoalStep.GoToNextStep;
            }

            if (!empire.GetEmpireAI().Goals.Any(g => g.type == GoalType.Colonize &&
                                                    g.ColonizationTarget == colonyTarget))
            {
                if (ship.DesignRole != ShipData.RoleName.colony)
                {
                    var colonyShip = Ship.CreateShipAtPoint(
                        EmpireManager.Player.data.ColonyShip, empire, ship.Center);
                    ship.QueueTotalRemoval();
                    var troop = ResourceManager.CreateTroop("Remnant Defender", ship.loyalty);

                    troop.LandOnShip(ship);
                }
                Goal goal = new MarkForColonization(colonyTarget, empire);
                empire.GetEmpireAI().AddGoal(goal);
            }
            if (empire.GetPlanets().Count < 1) return GoalStep.TryAgain;
            return GoalStep.GoToNextStep;
        }

        GoalStep UtilizeColony()
        {
            var ships = GetShipsForRefit();
            if (ships.Length < 1)
            {
                return GoalStep.GoToNextStep;
            }
            foreach (var ship in ships)
            {
                string shipName = "";
                if (ship.SurfaceArea < 50) shipName = "Heavy Drone";
                else if (ship.SurfaceArea < 100) shipName = "Remnant Slaver";
                else if (ship.SurfaceArea >= 100) shipName = "Remnant Exterminator";
                ResourceManager.GetShipTemplate(shipName, out Ship template);

                if (template != null)
                {
                    Goal refitShip = new RefitShip(ship, template.Name, empire);
                    empire.GetEmpireAI().Goals.Add(refitShip);
                }
            }
            return GoalStep.GoToNextStep;
        }

        GoalStep Exterminate()
        {
            var ships = empire.OwnedShips;
            foreach (var ship in ships.Filter(s=> s.Active
                                                             && s.DesignRole != ShipData.RoleName.colony
                                                             && s.AI.State == AIState.AwaitingOrders
                                                             && s.System != null ))
            {
                Planet target = ship.System.PlanetList.Find(p => p.Owner != empire && p.Owner != null);
                if (target != null)
                    ship.AI.OrderLandAllTroops(target);
            }
            return GoalStep.GoalComplete;
        }
    }
}