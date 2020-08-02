﻿using System;
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
        private Remnants Remnants;

        public RemnantAI() : base(GoalType.RemnantAI)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateGuardians
                //CreateAColony,
                //UtilizeColony,
                //Exterminate
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

        GoalStep CreateGuardians()
        {
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                foreach (Planet p in solarSystem.PlanetList)
                {
                    empire.Remnants.GenerateRemnantPresence(p);
                }
            }

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
                ResourceManager.ShipsDict.TryGetValue(shipName, out Ship template);

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
            foreach(var ship in empire.GetShips().Filter(s=> s.Active
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