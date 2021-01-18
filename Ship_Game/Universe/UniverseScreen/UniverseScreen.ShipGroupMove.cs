using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
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
                if (SelectedShip.IsConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
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
                    ShipCommands.AttackSpecificShip(SelectedShip, shipClicked);
                }
                else if (ShipPieMenu(shipClicked))
                {
                }
                else if (planetClicked != null)
                {
                    ShipCommands.RightClickOnPlanet(SelectedShip, planetClicked, true);
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
                        ShipCommands.RightClickOnShip(selectedShip, shipClicked);
                        if (planetClicked != null)
                            ShipCommands.RightClickOnPlanet(selectedShip, planetClicked);
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
            if (shipClicked == null || shipClicked.IsHangarShip || shipClicked.IsConstructor)
                return;
            if (SelectedShip != null && previousSelection != SelectedShip && SelectedShip != shipClicked)
                previousSelection = SelectedShip;
            SelectedShip = shipClicked;
            ShipPieMenu(SelectedShip);
        }

        // depending on current input state, either gives straight direction from center to final pos
        // or if queueing waypoints, gives direction from last waypoint to final pos
        Vector2 GetDirectionToFinalPos(Ship ship, Vector2 finalPos)
        {
            Vector2 fleetPos = Input.QueueAction && ship.AI.HasWayPoints
                             ? ship.AI.MovePosition : ship.Center;
            Vector2 finalDir = fleetPos.DirectionToTarget(finalPos);
            return finalDir;
        }

        void MoveFleetToMouse(Fleet fleet, Planet targetPlanet, Ship targetShip, bool wasProjecting)
        {
            if (fleet.Ships.Count == 0)
                return;

            if (wasProjecting)
            {
                ShipCommands.MoveFleetToLocation(targetShip, targetPlanet, Project.FleetCenter, Project.Direction, fleet);
            }
            else
            {
                Vector2 finalPos = UnprojectToWorldPosition(Input.StartRightHold);
                Ship centerMost = fleet.GetClosestShipTo(fleet.AveragePosition());
                Vector2 finalDir = GetDirectionToFinalPos(centerMost, finalPos);
                ShipCommands.MoveFleetToLocation(targetShip, targetPlanet, finalPos, finalDir, fleet);
            }
        }

        void MoveShipToMouse(Ship selectedShip, bool wasProjecting)
        {
            if (wasProjecting)
            {
                ShipCommands.MoveShipToLocation(Project.Start, Project.Direction, selectedShip);
            }
            else
            {
                Vector2 finalPos = UnprojectToWorldPosition(Input.StartRightHold);
                Vector2 finalDir = GetDirectionToFinalPos(selectedShip, finalPos);
                ShipCommands.MoveShipToLocation(finalPos, finalDir, selectedShip);
            }
        }

        void MoveShipGroupToMouse(bool wasProjecting)
        {
            bool queue = Input.QueueAction;
            if (wasProjecting) // dragging right mouse
            {
                if (CurrentGroup == null)
                    return; // projection is not valid YET, come back next update

                Log.Info("MoveShipGroupToMouse (CurrentGroup)");
                CurrentGroup.FormationWarpTo(CurrentGroup.ProjectedPos, 
                    CurrentGroup.ProjectedDirection, queue, offensiveMove: Input.IsCtrlKeyDown);

                return;
            }

            // right mouse was clicked
            Vector2 finalPos = UnprojectToWorldPosition(Input.CursorPosition);

            if (CurrentGroup == null || !CurrentGroup.IsShipListEqual(SelectedShipList))
            {
                Log.Info("MoveShipGroupToMouse (NEW)");
                // assemble brand new group
                
                Vector2 fleetCenter = ShipGroup.GetAveragePosition(SelectedShipList);
                Vector2 direction = fleetCenter.DirectionToTarget(finalPos);
                CurrentGroup = new ShipGroup(SelectedShipList, finalPos, finalPos, direction, player);
                CurrentGroup.FormationWarpTo(CurrentGroup.ProjectedPos, direction, queue, offensiveMove: Input.IsCtrlKeyDown);
            }
            else // move existing group
            {
                Log.Info("MoveShipGroupToMouse (existing)");
                Ship centerMost = CurrentGroup.GetClosestShipTo(CurrentGroup.AveragePosition());
                Vector2 finalDir = GetDirectionToFinalPos(centerMost, finalPos);
                CurrentGroup.FormationWarpTo(finalPos, finalDir, queue, offensiveMove: Input.IsCtrlKeyDown, true);
            }
        }
    }
}