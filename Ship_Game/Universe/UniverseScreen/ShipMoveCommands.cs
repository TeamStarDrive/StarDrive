using System;
using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game.Universe
{
    // Helper class for encapsulating Ship Movement commands
    public class ShipMoveCommands
    {
        readonly UniverseScreen Universe;
        readonly InputState Input;
        public ShipMoveCommands(UniverseScreen universe)
        {
            Universe = universe;
            Input = universe.Input;
        }

        bool OffensiveMove => Input.IsCtrlKeyDown;

        public bool RightClickOnShip(Ship selectedShip, Ship targetShip)
        {
            if (targetShip == null || selectedShip == targetShip)
                return false;

            if (targetShip.loyalty == Universe.player)
            {
                if (selectedShip.DesignRole == ShipData.RoleName.troop)
                {
                    if (targetShip.TroopCount < targetShip.TroopCapacity)
                        selectedShip.AI.OrderTroopToShip(targetShip);
                    else
                        selectedShip.AI.AddEscortGoal(targetShip);
                }
                else
                    selectedShip.AI.AddEscortGoal(targetShip);
            }
            else if (selectedShip.DesignRole == ShipData.RoleName.troop)
                selectedShip.AI.OrderTroopToBoardShip(targetShip);
            else if (Input.QueueAction)
                selectedShip.AI.OrderQueueSpecificTarget(targetShip);
            else
                selectedShip.AI.OrderAttackSpecificTarget(targetShip);

            return true;
        }

        public void RightClickOnPlanet(Ship ship, Planet planet, bool audio = false)
        {
            Log.Assert(planet != null, "RightClickOnPlanet: planet cannot be null!");
            if (ship.IsConstructor)
            {
                if (audio)
                {
                    GameAudio.NegativeClick();
                }
                return;
            }

            bool offensiveMove = Input.IsCtrlKeyDown;
            if (Input.IsShiftKeyDown) // Always order orbit if shift is down when right clicking on a planet
            {
                ship.OrderToOrbit(planet, offensiveMove);
            }
            else
            {
                if (audio)
                    GameAudio.AffirmativeClick();

                if (ship.isColonyShip)
                    PlanetRightClickColonyShip(ship, planet); // This ship can colonize planets
                else if (ship.Carrier.AnyAssaultOpsAvailable)
                    PlanetRightClickTroopShip(ship, planet); // This ship can assault planets
                else if (ship.HasBombs)
                    PlanetRightClickBomber(ship, planet); // This ship can bomb planets
                else
                    ship.OrderToOrbit(planet, offensiveMove); // Default logic of right clicking
            }
        }

        void PlanetRightClickColonyShip(Ship ship, Planet planet)
        {
            if (planet.Owner == null && planet.Habitable)
            {
                ship.AI.OrderColonization(planet);
                EmpireManager.Player.GetEmpireAI().Goals.Add(new MarkForColonization(ship, planet, EmpireManager.Player));
            }
            else
            {
                ship.AI.OrderToOrbit(planet);
            }
        }

        void PlanetRightClickTroopShip(Ship ship, Planet planet, bool offensiveMove = false)
        {
            if (planet.Owner != null && planet.Owner == Universe.player)
            {
                if (ship.IsDefaultTroopTransport)
                    // Rebase to this planet if it is ours and this is a single troop transport
                    ship.AI.OrderRebase(planet, true);
                else if (planet.ForeignTroopHere(ship.loyalty))
                    // If our planet is being invaded, land the troops there
                    ship.AI.OrderLandAllTroops(planet);
                else
                    ship.OrderToOrbit(planet, offensiveMove); // Just orbit
            }
            else if (planet.Habitable && (planet.Owner == null ||
                                          ship.loyalty.IsEmpireAttackable(planet.Owner)))
            {
                // Land troops on unclaimed planets or enemy planets
                ship.AI.OrderLandAllTroops(planet);
            }
            else
            {
                ship.OrderToOrbit(planet, offensiveMove);
            }
        }

        void PlanetRightClickBomber(Ship ship, Planet planet)
        {
            if (ship?.Active != true) return;

            Empire player    = Universe.player;
            if (planet.Owner != player)
            {
                if (player.IsEmpireAttackable(planet.Owner))
                    ship.AI.OrderBombardPlanet(planet);
                else
                    ship.AI.OrderToOrbit(planet);
            }
            else if (Input.IsShiftKeyDown) // Owner is player
            {
                ship.AI.OrderBombardPlanet(planet);
            }
            else
            {
                ship.AI.OrderToOrbit(planet);
            }
        }

        bool MoveFleetToPlanet(Planet planetClicked, ShipGroup fleet)
        {
            if (planetClicked == null || fleet == null)
                return false;

            fleet.FinalPosition = planetClicked.Center; //fbedard: center fleet on planet
            foreach (Ship ship in fleet.Ships)
                RightClickOnPlanet(ship, planetClicked, false);
            return true;
        }

        public bool AttackSpecificShip(Ship ship, Ship target)
        {
            if (ship.IsConstructor ||
                ship.shipData.Role == ShipData.RoleName.supply)
            {
                GameAudio.NegativeClick();
                return false;
            }

            GameAudio.AffirmativeClick();
            if (target.loyalty == Universe.player)
            {
                if (ship.shipData.Role == ShipData.RoleName.troop)
                {
                    if (ship.TroopCount < ship.TroopCapacity)
                        ship.AI.OrderTroopToShip(target);
                    else
                        ship.AI.AddEscortGoal(target);
                }
                else
                    ship.AI.AddEscortGoal(target);
                return true;
            }

            //if (ship.loyalty == player)
            {
                if (ship.shipData.Role == ShipData.RoleName.troop)
                    ship.AI.OrderTroopToBoardShip(target);
                else if (Input.QueueAction)
                    ship.AI.OrderQueueSpecificTarget(target);
                else
                    ship.AI.OrderAttackSpecificTarget(target);
            }
            return true;
        }

        bool TryFleetAttackShip(ShipGroup fleet, Ship shipToAttack)
        {
            if (shipToAttack == null || shipToAttack.loyalty == Universe.player)
                return false;

            fleet.FinalPosition = shipToAttack.Center;
            fleet.AssignPositions(Vectors.Up);
            foreach (Ship fleetShip in fleet.Ships)
                AttackSpecificShip(fleetShip, shipToAttack);
            return true;
        }

        bool QueueFleetMovement(Vector2 movePosition, Vector2 direction, ShipGroup fleet)
        {
            if (Input.QueueAction && fleet.Ships[0].AI.HasWayPoints)
            {
                foreach (Ship ship in fleet.Ships)
                    ship.AI.ClearOrdersIfCombat();

                fleet.FormationWarpTo(movePosition, direction, queueOrder: true, OffensiveMove);
                return true;
            }

            return false;
        }

        public void MoveFleetToLocation(Ship shipClicked, Planet planetClicked,
            Vector2 movePosition, Vector2 facingDir, ShipGroup fleet = null)
        {
            fleet = fleet ?? Universe.SelectedFleet;
            GameAudio.AffirmativeClick();

            foreach (Ship ship in fleet.Ships)
            {
                ship.AI.Target = null;
                ship.AI.ResetPriorityOrder(!Input.QueueAction);
            }

            Universe.player.GetEmpireAI().DefensiveCoordinator.RemoveShipList(Universe.SelectedShipList);

            if (TryFleetAttackShip(fleet, shipClicked))
                return;

            if (MoveFleetToPlanet(planetClicked, fleet))
                return;

            if (QueueFleetMovement(movePosition, facingDir, fleet))
                return;

            foreach (var ship in fleet.Ships)
                ship.AI.ClearOrders();

            if (Input.IsAltKeyDown)
                fleet.MoveToNow(movePosition, facingDir);
            else
                fleet.FormationWarpTo(movePosition, facingDir, false, offensiveMove: OffensiveMove);
        }

        public void MoveShipToLocation(Vector2 pos, Vector2 direction, Ship ship)
        {
            if  (ship.IsPlatformOrStation)
            {
                GameAudio.NegativeClick();
                return;
            }

            GameAudio.AffirmativeClick();
            if (Input.QueueAction)
            {
                if (Input.OrderOption)
                    ship.AI.OrderMoveDirectlyTo(pos, direction, false, AI.AIState.MoveTo, offensiveMove: OffensiveMove);
                else
                    ship.AI.OrderMoveTo(pos, direction, false, AI.AIState.MoveTo, offensiveMove: OffensiveMove);
            }
            else if (Input.OrderOption)
            {
                ship.AI.OrderMoveDirectlyTo(pos, direction, true, AI.AIState.MoveTo, offensiveMove: OffensiveMove);
            }
            else
            {
                ship.AI.OrderMoveTo(pos, direction, true, AI.AIState.MoveTo, offensiveMove: OffensiveMove);
            }

            ship.AI.OrderHoldPositionOffensive(pos, direction);
        }
    }
}
