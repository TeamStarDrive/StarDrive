using System;
using Microsoft.Xna.Framework;
using Ship_Game.Audio;
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

        public bool RightClickOnShip(Ship selectedShip, Ship targetShip)
        {
            if (targetShip == null || selectedShip == targetShip)
                return false;

            if (targetShip.loyalty == Universe.player)
            {
                if (selectedShip.DesignRole == ShipData.RoleName.troop)
                {
                    if (targetShip.TroopList.Count < targetShip.TroopCapacity)
                        selectedShip.AI.OrderTroopToShip(targetShip);
                    else
                        selectedShip.DoEscort(targetShip);
                }
                else
                    selectedShip.DoEscort(targetShip);
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
            if (ship.IsConstructor)
            {
                if (audio)
                {
                    GameAudio.NegativeClick();
                }
                return;
            }

            if (Input.IsShiftKeyDown) // Always order orbit if shift is down when right clicking on a planet
            {
                ship.AI.OrderToOrbit(planet);
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
                    ship.AI.OrderToOrbit(planet); // Default logic of right clicking
            }
        }

        void PlanetRightClickColonyShip(Ship ship, Planet planet)
        {
            if (planet.Owner == null && planet.Habitable)
                ship.AI.OrderColonization(planet);
            else
                ship.AI.OrderToOrbit(planet);
        }

        void PlanetRightClickTroopShip(Ship ship, Planet planet)
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
                    ship.AI.OrderToOrbit(planet); // Just orbit
            }
            else if (planet.Habitable && (planet.Owner == null ||
                                          ship.loyalty.IsEmpireAttackable(planet.Owner)))
            {
                // Land troops on unclaimed planets or enemy planets
                ship.AI.OrderLandAllTroops(planet);
            }
            else
            {
                ship.AI.OrderToOrbit(planet);
            }
        }

        void PlanetRightClickBomber(Ship ship, Planet planet)
        {
            Empire player = Universe.player;
            float enemies = planet.GetGroundStrengthOther(player) * 1.5f;
            float friendlies = planet.GetGroundStrength(player);
            if (planet.Owner != player)
            {
                if (player.IsEmpireAttackable(planet.Owner) && (enemies > friendlies || planet.Population > 0f))
                    ship.AI.OrderBombardPlanet(planet);
                else
                    ship.AI.OrderToOrbit(planet);
            }
            else if (enemies > friendlies && Input.IsShiftKeyDown)
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
                    if (ship.TroopList.Count < ship.TroopCapacity)
                        ship.AI.OrderTroopToShip(target);
                    else
                        ship.DoEscort(target);
                }
                else
                    ship.DoEscort(target);
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

                fleet.FormationWarpTo(movePosition, direction, queueOrder: true);
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
                ship.AI.SetPriorityOrder(!Input.QueueAction);
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
                fleet.FormationWarpTo(movePosition, facingDir);
        }

        public void MoveShipToLocation(Vector2 pos, Vector2 direction, Ship ship)
        {
            GameAudio.AffirmativeClick();
            if (Input.QueueAction)
            {
                if (Input.OrderOption)
                    ship.AI.OrderMoveDirectlyTo(pos, direction, false);
                else
                    ship.AI.OrderMoveTo(pos, direction, false, null);
            }
            else if (Input.OrderOption)
            {
                ship.AI.OrderMoveDirectlyTo(pos, direction, true);
            }
            else if (Input.IsCtrlKeyDown)
            {
                ship.AI.OrderMoveTo(pos, direction, true, null);
                ship.AI.OrderHoldPosition(pos, direction);
            }
            else
            {
                ship.AI.OrderMoveTo(pos, direction, true, null);
            }
        }
    }
}
