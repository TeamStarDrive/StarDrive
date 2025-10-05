using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

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
                for (int i = 0; i < ScreenManager.Screens.Count; i++)
                {
                    GameScreen screen = ScreenManager.Screens[i];
                    if (screen.IsExiting)
                        return; // don't process right click if screen is exiting or not universe
                }

                MoveSelectedShipsToMouse();
            }
        }

        void ProjectSelectedShipsToFleetPositions()
        {
            if (Input.StartRightHold.AlmostEqual(Input.EndRightHold))
                return; // not dragging yet

            Project.Update(this, SelectedFleet, SelectedShip);

            //Log.Info($"ProjectingPos  screenStart:{Input.StartRightHold} current:{Input.EndRightHold}  D:{direction}");

            if (SelectedFleet != null && SelectedFleet.Owner == Player)
            {
                SelectedFleet.ProjectPos(Project.FleetCenter, Project.Direction);
                CurrentGroup = SelectedFleet;
            }
            else if (SelectedShip != null && SelectedShip.Loyalty == Player)
            {
                if (SelectedShip.IsConstructor || SelectedShip.IsSupplyShuttle)
                {
                    SetSelectedShip(null);
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
                    if (ship.Loyalty != Player)
                        return;
                }

                CurrentGroup = new ShipGroup(SelectedShipList, Project.Start, Project.End, Project.Direction, Player);
            }
        }

        void MoveSelectedShipsToProjectedPositions()
        {
            Log.Info($"MoveSelectedShipsToProjectedPositions  start:{Input.StartRightHold}");

            Project.Started = false;
            if (SelectedFleet != null && SelectedFleet.Owner == Player)
            {
                SelectedSomethingTimer = 3f;
                MoveFleetToMouse(SelectedFleet, null, null, wasProjecting: true);
            }
            else if (SelectedShip != null && SelectedShip.Loyalty == Player)
            {
                SelectedSomethingTimer = 3f;
                if (UnselectableShip())
                    return;

                MoveShipToMouse(SelectedShip, wasProjecting: true);
            }
            else if (SelectedShipList.Count > 0)
            {
                SelectedSomethingTimer = 3f;
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.Loyalty != Player || UnselectableShip(ship))
                        return;
                }

                GameAudio.AffirmativeClick();
                MoveShipGroupToMouse(wasProjecting: true);
            }
        }

        void MoveSelectedShipsToMouse()
        {
            Log.Info($"MoveSelectedShipsToMouse {Input.CursorPosition}");
            Ship shipClicked = FindClickedShip(Input);
            Planet planetClicked = FindPlanetUnderCursor();

            Project.Started = false;

            if (SelectedFleet != null && SelectedFleet.Owner.isPlayer)
            {
                SelectedSomethingTimer = 3f;
                MoveFleetToMouse(SelectedFleet, planetClicked, shipClicked, wasProjecting: false);
            }
            else if (SelectedShip != null && SelectedShip.Loyalty.isPlayer)
            {
                SelectedSomethingTimer = 3f;
                if (shipClicked != null && shipClicked != SelectedShip)
                {
                    if (UnselectableShip())
                        return;
                    GameAudio.AffirmativeClick();
                    ShipCommands.AttackSpecificShip(SelectedShip, shipClicked);
                }
                else if (planetClicked != null)
                {
                    ShipCommands.RightClickOnPlanet(SelectedShip, planetClicked, true);
                }
                else if (!UnselectableShip() && !SelectedShip.IsPlatformOrStation && !SelectedShip.IsSubspaceProjector)
                {
                    MoveShipToMouse(SelectedShip, wasProjecting: false /*click*/);
                }
                return;
            }
            else if (SelectedShipList.Count > 0)
            {
                SelectedSomethingTimer = 3f;
                foreach (Ship ship in SelectedShipList)
                    if (UnselectableShip(ship) || !ship.Loyalty.isPlayer)
                        return;

                GameAudio.AffirmativeClick();

                if (shipClicked != null || planetClicked != null)
                {
                    foreach (Ship selectedShip in SelectedShipList)
                    {
                        ShipCommands.RightClickOnShip(selectedShip, shipClicked);
                        if (planetClicked != null)
                            ShipCommands.RightClickOnPlanet(selectedShip, planetClicked, true);
                    }
                }
                else
                {
                    MoveShipGroupToMouse(wasProjecting: false /*click*/);
                }
            }

            if (!HasSelectedItem && shipClicked is {IsHangarShip: false, IsConstructor: false})
            {
                SetSelectedShip(shipClicked);
            }
        }

        // depending on current input state, either gives straight direction from center to final pos
        // or if queueing waypoints, gives direction from last waypoint to final pos
        Vector2 GetDirectionToFinalPos(Ship ship, Vector2 finalPos)
        {
            Vector2 fleetPos = Input.QueueAction && ship.AI.HasWayPoints
                             ? ship.AI.MovePosition : ship.Position;
            Vector2 finalDir = fleetPos.DirectionToTarget(finalPos);
            return finalDir;
        }

        void MoveFleetToMouse(Fleet fleet, Planet targetPlanet, Ship targetShip, bool wasProjecting)
        {
            if (fleet.Ships.Count == 0) 
                return;

            bool attackSingleEnemyShip = targetShip != null && !targetShip.Loyalty.isPlayer == true;
            Ship[] enemyShips = targetPlanet == null && (targetShip == null || attackSingleEnemyShip)
                ?  GetVisibleEnemyShipsInScreen() 
                : HelperFunctions.GetAllPotentialTargetsIfInWarp(fleet.Ships);

            // When attacking enemy ship, allow moving even if in warp at the code will over shoot our ships if needed
            if (!attackSingleEnemyShip 
                && !HelperFunctions.CanExitWarpForChangingDirectionByCommand(fleet.Ships.ToArray(), enemyShips))
            {
                GameAudio.NegativeClick();
                return;
            }

            fleet.ClearPatrol();
            if (wasProjecting)
            {
                ShipCommands.MoveFleetToLocation(enemyShips, targetShip, targetPlanet, Project.FleetCenter, Project.Direction, fleet);
            }
            else
            {
                Vector2 finalPos = UnprojectToWorldPosition(Input.StartRightHold);
                Ship centerMost = fleet.GetClosestShipTo(fleet.AveragePosition(force: true));
                Vector2 finalDir = GetDirectionToFinalPos(centerMost, finalPos);

                ShipCommands.MoveFleetToLocation(enemyShips, targetShip, targetPlanet, finalPos, finalDir, fleet);
            }
        }

        void MoveShipToMouse(Ship selectedShip, bool wasProjecting)
        {
            Ship[] enemyShips = GetVisibleEnemyShipsInScreen();
            if (wasProjecting)
            {
                ShipCommands.MoveShipToLocation(enemyShips, Project.Start, Project.Direction, selectedShip);
            }
            else
            {
                Vector2 finalPos = UnprojectToWorldPosition(Input.StartRightHold);
                Vector2 finalDir = GetDirectionToFinalPos(selectedShip, finalPos);
                ShipCommands.MoveShipToLocation(enemyShips, finalPos, finalDir, selectedShip);
            }
        }

        void MoveShipGroupToMouse(bool wasProjecting)
        {
            Ship[] enemyShips = GetVisibleEnemyShipsInScreen();
            MoveOrder moveType = ShipCommands.GetMoveOrderType() | MoveOrder.ForceReassembly;
            if (wasProjecting) // dragging right mouse
            {
                if (CurrentGroup == null)
                    return; // projection is not valid YET, come back next update

                if (!HelperFunctions.CanExitWarpForChangingDirectionByCommand(CurrentGroup.Ships.ToArray(), enemyShips))
                {
                    GameAudio.NegativeClick();
                    return;
                }

                Vector2 correctedPos = HelperFunctions.GetCorrectedMovePosWithAudio(CurrentGroup.Ships, enemyShips, CurrentGroup.ProjectedPos);
                Log.Info("MoveShipGroupToMouse (CurrentGroup)");
                CurrentGroup.MoveTo(correctedPos, CurrentGroup.ProjectedDirection, moveType);
                return;
            }

            // right mouse was clicked
            Vector2 finalPos = UnprojectToWorldPosition(Input.CursorPosition);

            if (CurrentGroup == null || !CurrentGroup.IsShipListEqual(SelectedShipList))
            {
                // assemble brand new group
                Vector2 fleetCenter = ShipGroup.GetAveragePosition(SelectedShipList);
                Vector2 direction = fleetCenter.DirectionToTarget(finalPos);
                CurrentGroup = new ShipGroup(SelectedShipList, finalPos, finalPos, direction, Player);
                if (!HelperFunctions.CanExitWarpForChangingDirectionByCommand(CurrentGroup.Ships.ToArray(), enemyShips))
                {
                    GameAudio.NegativeClick();
                    return;
                }

                Vector2 correctedPos = HelperFunctions.GetCorrectedMovePosWithAudio(CurrentGroup.Ships, enemyShips, CurrentGroup.ProjectedPos);
                Log.Info("MoveShipGroupToMouse (NEW)");
                CurrentGroup.MoveTo(correctedPos, direction, moveType);
            }
            else // move existing group
            {
                if (!HelperFunctions.CanExitWarpForChangingDirectionByCommand(CurrentGroup.Ships.ToArray(), enemyShips))
                {
                    GameAudio.NegativeClick();
                    return;
                }

                Log.Info("MoveShipGroupToMouse (existing)");
                Ship centerMost = CurrentGroup.GetClosestShipTo(CurrentGroup.AveragePosition(force: true));
                Vector2 finalDir = GetDirectionToFinalPos(centerMost, finalPos);
                Vector2 correctedPos = HelperFunctions.GetCorrectedMovePosWithAudio(CurrentGroup.Ships, enemyShips, finalPos);
                CurrentGroup.MoveTo(correctedPos, finalDir, moveType);
            }
        }
    }
}