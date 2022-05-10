using System;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

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

        /// Depending on User Input: Aggressive, Defensive, StandGround Movement Types
        public AI.MoveOrder GetMoveOrderType()
        {
            AI.MoveOrder addWayPoint = Input.QueueAction ? AI.MoveOrder.AddWayPoint : AI.MoveOrder.Regular;
            return addWayPoint|GetStanceType();
        }

        public AI.MoveOrder GetStanceType()
        {
            if (Input.IsCtrlKeyDown) return AI.MoveOrder.Aggressive;
            if (Input.IsAltKeyDown)  return AI.MoveOrder.StandGround;
            return AI.MoveOrder.Regular;
        }

        public bool RightClickOnShip(Ship selectedShip, Ship targetShip)
        {
            if (targetShip == null || selectedShip == targetShip || !selectedShip.PlayerShipCanTakeFleetOrders())
                return false;

            if (targetShip.Loyalty == Universe.Player)
            {
                if (selectedShip.DesignRole == RoleName.troop)
                {
                    if (targetShip.TroopCount < targetShip.TroopCapacity)
                        selectedShip.AI.OrderTroopToShip(targetShip);
                    else
                        selectedShip.AI.AddEscortGoal(targetShip);
                }
                else
                    selectedShip.AI.AddEscortGoal(targetShip);
            }
            else if (selectedShip.DesignRole == RoleName.troop)
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

            if (audio)
                GameAudio.AffirmativeClick();

            bool clearOrders = !Input.IsShiftKeyDown;

            // if ALT key is down, always Orbit the planet
            if (Input.IsAltKeyDown)
                ship.OrderToOrbit(planet, clearOrders, GetStanceType());
            else if (ship.ShipData.IsColonyShip)
                PlanetRightClickColonyShip(ship, planet, clearOrders); // This ship can colonize planets
            else if (ship.Carrier.AnyAssaultOpsAvailable)
                PlanetRightClickTroopShip(ship, planet, clearOrders, AI.MoveOrder.Regular); // This ship can assault planets
            else if (ship.HasBombs)
                PlanetRightClickBomber(ship, planet, clearOrders); // This ship can bomb planets
            else
                ship.OrderToOrbit(planet, clearOrders, GetStanceType()); // Default logic of right clicking
        }

        void PlanetRightClickColonyShip(Ship ship, Planet planet, bool clearOrders)
        {
            if (planet.Owner == null && planet.Habitable)
            {
                ship.AI.OrderColonization(planet);
                Universe.Player.GetEmpireAI().Goals.Add(new MarkForColonization(ship, planet, EmpireManager.Player));
            }
            else
            {
                ship.OrderToOrbit(planet, clearOrders);
            }
        }

        void PlanetRightClickTroopShip(Ship ship, Planet planet, bool clearOrders, AI.MoveOrder order)
        {
            if (planet.Owner != null && planet.Owner == Universe.Player)
            {
                if (ship.IsDefaultTroopTransport)
                    // Rebase to this planet if it is ours and this is a single troop transport
                    ship.AI.OrderRebase(planet, clearOrders);
                else if (planet.ForeignTroopHere(ship.Loyalty))
                    // If our planet is being invaded, land the troops there
                    ship.AI.OrderLandAllTroops(planet, clearOrders);
                else
                    ship.OrderToOrbit(planet, clearOrders, order); // Just orbit
            }
            else if (planet.Habitable && (planet.Owner == null ||
                                          ship.Loyalty.IsEmpireAttackable(planet.Owner)))
            {
                // Land troops on unclaimed planets or enemy planets
                ship.AI.OrderLandAllTroops(planet, clearOrders);
            }
            else
            {
                ship.OrderToOrbit(planet, clearOrders, order);
            }
        }

        void PlanetRightClickBomber(Ship ship, Planet planet, bool clearOrders)
        {
            if (ship?.Active != true) return;

            if (planet.Owner != Universe.Player)
            {
                if (Universe.Player.IsEmpireAttackable(planet.Owner))
                    ship.AI.OrderBombardPlanet(planet, clearOrders);
                else
                    ship.OrderToOrbit(planet, clearOrders);
            }
            else if (Input.IsShiftKeyDown) // Owner is player
            {
                ship.AI.OrderBombardPlanet(planet, clearOrders);
            }
            else
            {
                ship.OrderToOrbit(planet, clearOrders);
            }
        }

        bool MoveFleetToPlanet(Planet planetClicked, ShipGroup fleet)
        {
            if (planetClicked == null || fleet == null)
                return false;

            fleet.FinalPosition = planetClicked.Position; //fbedard: center fleet on planet
            foreach (Ship ship in fleet.Ships)
                RightClickOnPlanet(ship, planetClicked, false);
            return true;
        }

        public bool AttackSpecificShip(Ship ship, Ship target)
        {
            if (ship.IsConstructor ||
                ship.IsSupplyShuttle)
            {
                GameAudio.NegativeClick();
                return false;
            }

            GameAudio.AffirmativeClick();
            if (target.Loyalty == Universe.Player)
            {
                if (ship.ShipData.Role == RoleName.troop)
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
                if (ship.ShipData.Role == RoleName.troop)
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
            if (shipToAttack == null || shipToAttack.Loyalty == Universe.Player)
                return false;

            fleet.FinalPosition = shipToAttack.Position;
            fleet.AssignPositions(Vectors.Up);
            foreach (Ship fleetShip in fleet.Ships)
            {
                if (fleetShip.PlayerShipCanTakeFleetOrders())
                    AttackSpecificShip(fleetShip, shipToAttack);
            }

            return true;
        }

        bool QueueFleetMovement(Vector2 movePosition, Vector2 direction, ShipGroup fleet)
        {
            if (Input.QueueAction && fleet.Ships[0].AI.HasWayPoints)
            {
                foreach (Ship ship in fleet.Ships)
                    ship.AI.ClearOrdersIfCombat();

                fleet.MoveTo(movePosition, direction, GetMoveOrderType());
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
                if (ship.PlayerShipCanTakeFleetOrders())
                    ship.AI.ResetPriorityOrder(!Input.QueueAction);
            }

            Universe.Player.GetEmpireAI().DefensiveCoordinator.RemoveShipList(Universe.SelectedShipList);

            if (TryFleetAttackShip(fleet, shipClicked))
                return;

            if (MoveFleetToPlanet(planetClicked, fleet))
                return;

            if (QueueFleetMovement(movePosition, facingDir, fleet))
                return;

            foreach (var ship in fleet.Ships)
                if (ship.PlayerShipCanTakeFleetOrders())
                    ship.AI.ClearOrders();

            fleet.MoveTo(movePosition, facingDir, GetMoveOrderType());
        }

        public void MoveShipToLocation(Vector2 pos, Vector2 direction, Ship ship)
        {
            if (ship.IsPlatformOrStation)
            {
                GameAudio.NegativeClick();
                return;
            }

            GameAudio.AffirmativeClick();
            ship.AI.OrderMoveTo(pos, direction, GetMoveOrderType());
        }
    }
}
