using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class ShipGroupProject
    {
        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }
        public Vector2 Direction { get; private set; }
        public Vector2 FleetCenter { get; private set; }
        public bool Started;

        public void Update(UniverseScreen universe, Fleet fleet, Ship ship)
        {
            InputState input = universe.Input;
            if (!Started)
            {
                Start = universe.UnprojectToWorldPosition(input.StartRightHold);
                Started = true;
            }

            End = universe.UnprojectToWorldPosition(input.EndRightHold);
            Direction = Start.DirectionToTarget(End).LeftVector();

            if (fleet != null)
            {
                FleetCenter = fleet.GetProjectedMidPoint(Start, End, fleet.GetRelativeSize());
            }
        }
    }

    public partial class UniverseScreen
    {
        public ShipGroup CurrentGroup { get; private set; }
        public ShipGroupProject Project { get; } = new ShipGroupProject();
        
        void HandleShipSelectionAndOrders()
        {
            if (NotificationManager.HitTest)
                return;

            if (Input.RightMouseClick)
                SelectedSomethingTimer = 3f;

            // prevent projection while player is in manual control
            if (SelectedShip != null && SelectedShip.AI.State == AIState.ManualControl)
            {
                Vector2 worldPos = UnprojectToWorldPosition(Input.StartRightHold);
                if (worldPos.InRadius(SelectedShip.Center, 5000f))
                {
                    Log.Info("Input.StartRightHold.InRadius(SelectedShip.Center, 5000f)");
                    return;
                }
            }

            if (Input.RightMouseHeld(0.1f))
            {
                // active RMB projection
                ProjectSelectedShipsToFleetPositions();
            }
            else if (Project.Started && Input.RightMouseUp)
            {
                // terminate RMB projection
                MoveSelectedShipsToProjectedPositions();
            }
            else if (!Project.Started && Input.RightMouseReleased)
            {
                MoveSelectedShipsToMouse();
            }
        }

        void ProjectSelectedShipsToFleetPositions()
        {
            if (Input.StartRightHold.AlmostEqual(Input.EndRightHold))
                return; // not dragging yet

            Project.Update(this, SelectedFleet, SelectedShip);

            //Log.Info($"ProjectingPos  screenStart:{Input.StartRightHold} current:{Input.EndRightHold}  D:{direction}");

            if (SelectedFleet != null && SelectedFleet.Owner == EmpireManager.Player)
            {
                SelectedFleet.ProjectPos(Project.FleetCenter, Project.Direction);
                CurrentGroup = SelectedFleet;
            }
            else if (SelectedShip != null && SelectedShip.loyalty == player)
            {
                if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
                {
                    if (SelectedShip != null && previousSelection != SelectedShip) // fbedard
                        previousSelection = SelectedShip;
                    SelectedShip = null;
                    GameAudio.NegativeClick();
                }
                else // single-ship group
                {
                    var shipGroup = new ShipGroup();
                    shipGroup.AddShip(SelectedShip);
                    shipGroup.ProjectPosNoOffset(Project.Start, Project.Direction);
                    CurrentGroup = shipGroup;
                }
            }
            else if (SelectedShipList.Count > 0)
            {
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.loyalty != player)
                        return;
                }

                CurrentGroup = new ShipGroup(SelectedShipList, Project.Start, Project.End, Project.Direction, player);
            }
        }

        void MoveSelectedShipsToProjectedPositions()
        {
            Log.Info($"MoveSelectedShipsToProjectedPositions  start:{Input.StartRightHold}");

            Project.Started = false;
            if (SelectedFleet != null && SelectedFleet.Owner == player)
            {
                SelectedSomethingTimer = 3f;
                MoveFleetToMouse(SelectedFleet, null, null, wasProjecting: true);
            }
            else if (SelectedShip != null && SelectedShip?.loyalty == player)
            {
                player.GetEmpireAI().DefensiveCoordinator.Remove(SelectedShip);
                SelectedSomethingTimer = 3f;
                if (UnselectableShip())
                {
                    if (SelectedShip != null && previousSelection != SelectedShip) // fbedard
                        previousSelection = SelectedShip;
                    return;
                }

                MoveShipToMouse(SelectedShip, wasProjecting: true);
            }
            else if (SelectedShipList.Count > 0)
            {
                SelectedSomethingTimer = 3f;
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.loyalty != player || UnselectableShip(ship))
                        return;
                }

                GameAudio.AffirmativeClick();
                MoveShipGroupToMouse(wasProjecting: true);
            }
        }

        void MoveSelectedShipsToMouse()
        {
            Log.Info($"MoveSelectedShipsToMouse {Input.CursorPosition}");
            Ship shipClicked = CheckShipClick(Input);
            Planet planetClicked = CheckPlanetClick();
            
            Project.Started = false;

            if (SelectedFleet != null && SelectedFleet.Owner.isPlayer)
            {
                SelectedSomethingTimer = 3f;
                MoveFleetToMouse(SelectedFleet, planetClicked, shipClicked, wasProjecting: false);
            }
            else if (SelectedShip != null && SelectedShip.loyalty.isPlayer)
            {
                player.GetEmpireAI().DefensiveCoordinator.Remove(SelectedShip);
                SelectedSomethingTimer = 3f;

                if (shipClicked != null && shipClicked != SelectedShip)
                {
                    if (UnselectableShip())
                        return;
                    GameAudio.AffirmativeClick();
                    AttackSpecificShip(SelectedShip, shipClicked);
                }
                else if (ShipPieMenu(shipClicked))
                {
                }
                else if (planetClicked != null)
                {
                    RightClickOnPlanet(SelectedShip, planetClicked, true);
                }
                else if (!UnselectableShip())
                {
                    MoveShipToMouse(SelectedShip, wasProjecting: false /*click*/);
                }
                return;
            }
            else if (SelectedShipList.Count > 0)
            {
                SelectedSomethingTimer = 3f;
                foreach (Ship ship in SelectedShipList)
                    if (UnselectableShip(ship) || !ship.loyalty.isPlayer)
                        return;

                GameAudio.AffirmativeClick();

                if (shipClicked != null || planetClicked != null)
                {
                    foreach (Ship selectedShip in SelectedShipList)
                    {
                        player.GetEmpireAI().DefensiveCoordinator.Remove(selectedShip);
                        RightClickOnShip(selectedShip, shipClicked);
                        RightClickOnPlanet(selectedShip, planetClicked);
                    }
                }
                else
                {
                    MoveShipGroupToMouse(wasProjecting: false /*click*/);
                }
            }

            if (SelectedFleet != null || SelectedItem != null || SelectedShip != null || SelectedPlanet != null ||
                SelectedShipList.Count != 0)
                return;
            if (shipClicked == null || shipClicked.Mothership != null || shipClicked.isConstructor)
                return;
            if (SelectedShip != null && previousSelection != SelectedShip && SelectedShip != shipClicked)
                previousSelection = SelectedShip;
            SelectedShip = shipClicked;
            ShipPieMenu(SelectedShip);
        }


        void MoveFleetToMouse(Fleet fleet, Planet targetPlanet, Ship targetShip, bool wasProjecting)
        {
            if (wasProjecting)
            {
                MoveFleetToLocation(targetShip, targetPlanet, Project.FleetCenter, Project.Direction, fleet);
            }
            else
            {
                Vector2 start = UnprojectToWorldPosition(Input.StartRightHold);
                Vector2 dir = fleet.Position.DirectionToTarget(start);
                MoveFleetToLocation(targetShip, targetPlanet, start, dir, fleet);
            }
        }

        void MoveShipToMouse(Ship selectedShip, bool wasProjecting)
        {
            if (wasProjecting)
            {
                MoveShipToLocation(Project.Start, Project.Direction, selectedShip);
            }
            else
            {
                Vector2 start = UnprojectToWorldPosition(Input.StartRightHold);
                Vector2 dir = selectedShip.Position.DirectionToTarget(start);
                MoveShipToLocation(start, dir, selectedShip);
            }
        }

        void MoveShipGroupToMouse(bool wasProjecting)
        {
            if (wasProjecting) // dragging right mouse
            {
                if (CurrentGroup == null)
                {
                    Log.Warning("MoveShipGroupToMouse (CurrentGroup was NULL)");
                    return; // projection is not valid YET, come back next update
                }

                Log.Info("MoveShipGroupToMouse (CurrentGroup)");
                foreach (Ship selectedShip in SelectedShipList)
                {
                    MoveShipToLocation(selectedShip.projectedPosition, CurrentGroup.ProjectedDirection, selectedShip);
                }
                return;
            }

            // right mouse was clicked
            Vector2 start = UnprojectToWorldPosition(Input.CursorPosition);
            Vector2 fleetCenter = ShipGroup.AveragePosition(SelectedShipList);
            Vector2 direction = fleetCenter.DirectionToTarget(start);

            if (CurrentGroup == null || !CurrentGroup.IsShipListEqual(SelectedShipList))
            {
                Log.Info("MoveShipGroupToMouse (NEW)");
                // assemble brand new group
                CurrentGroup = new ShipGroup(SelectedShipList, start, start, direction, player);
                foreach (Ship selectedShip in SelectedShipList)
                    MoveShipToLocation(selectedShip.projectedPosition, direction, selectedShip);
            }
            else // move existing group
            {
                Log.Info("MoveShipGroupToMouse (existing)");
                CurrentGroup.MoveToNow(start, direction);
            }
        }

    }
}