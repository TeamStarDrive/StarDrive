using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantAI : Goal
    {
        public const string ID = "RemnantAI";
        public override string UID => ID;
        public RemnantAI() : base(GoalType.DeepSpaceConstruction)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateAColony,
                UtilizeColony,
                FinalSolution
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
            return empire.GetShips().FindMinFiltered(assimilate =>
                (assimilate.isColonyShip && assimilate.AI.State != AIState.Colonize) ||
                (assimilate.AI.State != AIState.Refit &&
                assimilate.shipData.ShipStyle != "Remnant" &&
                assimilate.shipData.ShipStyle != null &&
                !assimilate.AI.BadGuysNear), assimilate => assimilate.GetStrength());
        }

        Ship[] GetShipsForRefit()
        {
            return empire.GetShips().Filter(assimilate =>
                !assimilate.isColonyShip &&
                (assimilate.AI.State != AIState.Refit &&
                assimilate.shipData.ShipStyle != "Remnant" &&
                assimilate.shipData.ShipStyle != null &&
                !assimilate.AI.BadGuysNear &&
                assimilate.GetStrength() >= 50)
            );
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
                    colonyShip.TroopList.Add(ResourceManager.CreateTroop("Remnant Defender", ship.loyalty));
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
                ResourceManager.ShipsDict.TryGetValue(shipName, out Ship template);

                if (template != null)
                {
                    Goal refitShip = new RefitShip(ship, template.Name, empire);
                    empire.GetEmpireAI().Goals.Add(refitShip);
                }
            }
            return GoalStep.GoToNextStep;
        }

        GoalStep FinalSolution()
        {
            foreach(var ship in empire.GetShips().Filter(s=> s.Active 
                                                             && s.DesignRole != ShipData.RoleName.colony
                                                             && s.AI.State == AIState.AwaitingOrders
                                                             && s.System != null ))
            {
                Planet target = ship.System.PlanetList.Find(p => p.Owner != empire && p.Owner != null);
                if (target != null)
                    ship.AI.OrderLandAllTroops(target);
            }
            return GoalStep.RestartGoal;
        }

        GoalStep RemnantPlan()
        {
            bool hasPlanets = false;
            foreach (Planet planet in empire.GetPlanets())
            {
                hasPlanets = true;
                planet.Construction.RemnantCheatProduction();
            }

            foreach (Ship assimilate in empire.GetShips())
            {
                if (assimilate.shipData.ShipStyle != "Remnant" && assimilate.shipData.ShipStyle != null &&
                    assimilate.AI.State != AIState.Colonize && assimilate.AI.State != AIState.Refit
                    && (!assimilate.AI.BadGuysNear || !hasPlanets))
                {
                    if (hasPlanets)
                    {
                        if (assimilate.GetStrength() <= 0)
                        {
                            Planet target = null;
                            if (assimilate.System != null)
                            {
                                target = assimilate.System.PlanetList
                                    .Find(owner => owner.Owner != empire && owner.Owner != null);
                            }

                            if (target != null)
                            {
                                assimilate.TroopList.Add(ResourceManager.CreateTroop("Remnant Defender", assimilate.loyalty));
                                assimilate.isColonyShip = true;

                                Planet capture = Empire.Universe.PlanetsDict.Values.ToArray().FindMaxFiltered(
                                    potentials => potentials.Owner == null && potentials.Habitable,
                                    potentials => -assimilate.Center.SqDist(potentials.Center));

                                if (capture != null)
                                    assimilate.AI.OrderColonization(capture);
                            }
                        }
                        else
                        {
                            string shipName = "";
                            if (assimilate.SurfaceArea < 50) shipName = "Heavy Drone";
                            else if (assimilate.SurfaceArea < 100) shipName = "Remnant Slaver";
                            else if (assimilate.SurfaceArea >= 100) shipName = "Remnant Exterminator";
                            ResourceManager.ShipsDict.TryGetValue(shipName, out Ship template);
                            if (template != null)
                            {
                                Goal refitShip = new RefitShip(assimilate, template.Name, empire);
                                empire.GetEmpireAI().Goals.Add(refitShip);
                            }
                        }
                    }
                    else
                    {
                        if (assimilate.GetStrength() <= 0)
                        {
                            assimilate.isColonyShip = true;


                            Planet capture = Empire.Universe.PlanetsDict.Values
                                .Where(potentials => potentials.Owner == null && potentials.Habitable)
                                .OrderBy(potentials => Vector2.Distance(assimilate.Center, potentials.Center))
                                .FirstOrDefault();
                            if (capture != null)
                                assimilate.AI.OrderColonization(capture);
                        }
                    }
                }
                else
                {
                    if (assimilate.System != null && assimilate.AI.State == AIState.AwaitingOrders)
                    {
                        Planet target = assimilate.System.PlanetList.Find(p => p.Owner != empire && p.Owner != null);
                        if (target != null)
                            assimilate.AI.OrderLandAllTroops(target);
                    }
                }
            }

            return GoalStep.TryAgain;
        }
    }
}