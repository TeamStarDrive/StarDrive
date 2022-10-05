using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using SDGraphics;
using SDUtils;
using Ray = Microsoft.Xna.Framework.Ray;
using static Ship_Game.Fleets.Fleet;
using Ship_Game.PathFinder;

namespace Ship_Game
{
    public sealed partial class FleetDesignScreen
    {
        Vector3 GetPointInWorld(in Vector2 screenPoint, float screenZ)
        {
            return (Vector3)Viewport.Unproject(new Vector3(screenPoint, screenZ), Projection, View, Matrix.XnaIdentity);
        }

        Vector2 GetWorldSpaceFromScreenSpace(Vector2 screenSpace)
        {
            Vector3 nearPoint = GetPointInWorld(screenSpace, 0f);
            Vector3 farPoint = GetPointInWorld(screenSpace, 1f);

            Vector3 direction = (farPoint - nearPoint).Normalized();
            var pickRay = new Ray(nearPoint, direction);
            float k = -pickRay.Position.Z / pickRay.Direction.Z;
            var pickedPosition = new Vector3(
                pickRay.Position.X + k * pickRay.Direction.X,
                pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
            return new Vector2(pickedPosition.X, pickedPosition.Y);
        }

        void HandleEdgeDetection(InputState input)
        {
            EmpireUI.HandleInput(input, this);
            if (FleetNameEntry.HandlingInput)
                return;

            Vector2 mousePos = input.CursorPosition;
            PresentationParameters pp = ScreenManager.GraphicsDevice.PresentationParameters;
            Vector2 upperLeftWorldSpace = GetWorldSpaceFromScreenSpace(new Vector2(0f, 0f));
            Vector2 lowerRightWorldSpace = GetWorldSpaceFromScreenSpace(new Vector2(pp.BackBufferWidth, pp.BackBufferHeight));
            float xDist = lowerRightWorldSpace.X - upperLeftWorldSpace.X;
            if ((int) mousePos.X == 0 || input.KeysCurr.IsKeyDown(Keys.Left) || input.KeysCurr.IsKeyDown(Keys.A))
            {
                CamPos.X = CamPos.X - 0.008f * xDist;
            }

            if ((int) mousePos.X == pp.BackBufferWidth - 1 || input.KeysCurr.IsKeyDown(Keys.Right) ||
                input.KeysCurr.IsKeyDown(Keys.D))
            {
                CamPos.X = CamPos.X + 0.008f * xDist;
            }

            if ((int) mousePos.Y == 0 || input.KeysCurr.IsKeyDown(Keys.Up) || input.KeysCurr.IsKeyDown(Keys.W))
            {
                CamPos.Y = CamPos.Y - 0.008f * xDist;
            }

            if ((int) mousePos.Y == pp.BackBufferHeight - 1 || input.KeysCurr.IsKeyDown(Keys.Down) ||
                input.KeysCurr.IsKeyDown(Keys.S))
            {
                CamPos.Y = CamPos.Y + 0.008f * xDist;
            }
        }

        void InputSelectFleet(int whichFleet, bool keyPressed)
        {
            if (keyPressed)
            {
                GameAudio.AffirmativeClick();
                ChangeFleet(whichFleet);
            }
        }
        
        void OnDesignShipItemClicked(FleetDesignShipListItem item)
        {
            if (FleetToEdit != -1 && item.Ship != null)
            {
                ActiveShipDesign = item.Ship;
                SelectedNodeList.Clear();
                SelectedSquad = null;
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (Input.FleetExitScreen && !GlobalStats.TakingInput)
            {
                ExitScreen();
                return true;
            }

            if (SelectedNodeList.Count != 1 && FleetToEdit != -1 && FleetNameEntry.HandleInput(input))
                return true;

            InputSelectFleet(1, Input.Fleet1);
            InputSelectFleet(2, Input.Fleet2);
            InputSelectFleet(3, Input.Fleet3);
            InputSelectFleet(4, Input.Fleet4);
            InputSelectFleet(5, Input.Fleet5);
            InputSelectFleet(6, Input.Fleet6);
            InputSelectFleet(7, Input.Fleet7);
            InputSelectFleet(8, Input.Fleet8);
            InputSelectFleet(9, Input.Fleet9);

            foreach (KeyValuePair<int, RectF> rect in FleetsRects)
            {
                if (rect.Value.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    FleetToEdit = rect.Key;
                    InputSelectFleet(FleetToEdit, true);
                }
            }

            ShipSL.Visible = FleetToEdit != -1;
            if (base.HandleInput(input))
                return true;

            if (SelectedNodeList.Count > 0 && Input.RightMouseClick)
            {
                SelectedNodeList.Clear();
            }

            if (HandleSingleNodeSelection(input, input.CursorPosition))
                return false;

            if (SelectedNodeList.Count > 1)
            {
                SliderDps.HandleInput(input);
                SliderVulture.HandleInput(input);
                SliderArmor.HandleInput(input);
                SliderDefend.HandleInput(input);
                SliderAssist.HandleInput(input);
                SliderSize.HandleInput(input);
                foreach (FleetDataNode node in SelectedNodeList)
                {
                    node.DPSWeight = SliderDps.Amount;
                    node.VultureWeight = SliderVulture.Amount;
                    node.ArmoredWeight = SliderArmor.Amount;
                    node.DefenderWeight = SliderDefend.Amount;
                    node.AssistWeight = SliderAssist.Amount;
                    node.SizeWeight = SliderSize.amount;
                }

                if (OperationsRect.HitTest(input.CursorPosition))
                {
                    //DragTimer = 0f;
                    return true;
                }

                if (PrioritiesRect.HitTest(input.CursorPosition))
                {
                    //DragTimer = 0f;
                    OperationalRadius.HandleInput(input);
                    SelectedNodeList[0].OrdersRadius = OperationalRadius.RelativeValue;
                    return true;
                }
            }
            else if (FleetToEdit != -1 && SelectedNodeList.Count == 0 &&
                     SelectedStuffRect.HitTest(input.CursorPosition))
            {
                if (RequisitionForces.HandleInput(input))
                {
                    ScreenManager.AddScreen(new RequisitionScreen(this));
                }
                if (SaveDesign.HandleInput(input))
                {
                    ScreenManager.AddScreen(new SaveFleetDesignScreen(this, SelectedFleet));
                }
                if (LoadDesign.HandleInput(input))
                {
                    ScreenManager.AddScreen(new LoadSavedFleetDesignScreen(this));
                }
            }

            if (ActiveShipDesign != null)
            {
                if (input.LeftMouseClick && !ShipSL.HitTest(input.CursorPosition))
                {
                    Vector2 pickedPosition = GetWorldSpaceFromScreenSpace(input.CursorPosition);

                    FleetDataNode node = new FleetDataNode
                    {
                        FleetOffset = pickedPosition,
                        ShipName = ActiveShipDesign.Name
                    };
                    SelectedFleet.DataNodes.Add(node);
                    if (AvailableShips.Contains(ActiveShipDesign))
                    {
                        if (SelectedFleet.Ships.Count == 0)
                        {
                            SelectedFleet.FinalPosition = ActiveShipDesign.Position;
                        }

                        node.Ship = ActiveShipDesign;
                        node.Ship.RelativeFleetOffset = node.FleetOffset;
                        node.Ship.ShowSceneObjectAt(node.Ship.RelativeFleetOffset, -500000f);
                        AvailableShips.Remove(ActiveShipDesign);
                        SelectedFleet.AddShip(node.Ship);

                        if (SubShips.SelectedIndex == 1)
                        {
                            ShipSL.RemoveFirstIf(item => item.Ship != null && item.Ship == ActiveShipDesign);
                        }

                        ActiveShipDesign = null;
                    }

                    if (!input.KeysCurr.IsKeyDown(Keys.LeftShift))
                    {
                        ActiveShipDesign = null;
                    }
                }

                if (input.RightMouseClick)
                {
                    ActiveShipDesign = null;
                }
            }

            HandleEdgeDetection(input);
            HandleSelectionBox(input);
            HandleCameraMovement(input);

            if (Input.FleetRemoveSquad)
            {
                if (SelectedSquad != null)
                {
                    SelectedFleet.CenterFlank.Remove(SelectedSquad);
                    SelectedFleet.LeftFlank.Remove(SelectedSquad);
                    SelectedFleet.RearFlank.Remove(SelectedSquad);
                    SelectedFleet.RightFlank.Remove(SelectedSquad);
                    SelectedFleet.ScreenFlank.Remove(SelectedSquad);
                    SelectedSquad = null;
                    SelectedNodeList.Clear();
                }

                if (SelectedNodeList.Count > 0)
                {
                    foreach (Array<Fleet.Squad> flanks in SelectedFleet.AllFlanks)
                    {
                        foreach (Fleet.Squad squad in flanks)
                        {
                            foreach (FleetDataNode node in SelectedNodeList)
                            {
                                if (squad.DataNodes.Contains(node))
                                {
                                    squad.DataNodes.RemoveRef(node);
                                    if (node.Ship != null)
                                        squad.Ships.RemoveRef(node.Ship);
                                }
                            }
                        }
                    }

                    foreach (FleetDataNode node in SelectedNodeList)
                    {
                        SelectedFleet.DataNodes.Remove(node);
                        if (node.Ship == null)
                        {
                            continue;
                        }

                        node.Ship.ShowSceneObjectAt(node.Ship.RelativeFleetOffset, -500000f);
                        node.Ship.Fleet?.RemoveShip(node.Ship, returnToEmpireAI: true, clearOrders: true);
                    }

                    SelectedNodeList.Clear();
                }
            }

            return false;
        }
        
        Vector2 StartDragPos;

        void HandleCameraMovement(InputState input)
        {
            const float minHeight = 3000;
            const float maxHeight = 100_000;
            float scrollSpeed = minHeight + (CamPos.Z / maxHeight)*10_000;

            if      (input.ScrollIn)  DesiredCamPos.Z -= scrollSpeed;
            else if (input.ScrollOut) DesiredCamPos.Z += scrollSpeed;

            if (input.MiddleMouseClick)
            {
                StartDragPos = input.CursorPosition;
            }

            if (input.MiddleMouseHeld())
            {
                Vector2 dv = input.CursorPosition - StartDragPos;
                StartDragPos = input.CursorPosition;
                DesiredCamPos.X += -dv.X * (float)VisibleWorldRect.Width * 0.001f;
                DesiredCamPos.Y += -dv.Y * (float)VisibleWorldRect.Width * 0.001f;
            }
            
            DesiredCamPos.X = DesiredCamPos.X.Clamped(-20_000, 20_000);
            DesiredCamPos.Y = DesiredCamPos.Y.Clamped(-20_000, 20_000);
            DesiredCamPos.Z = DesiredCamPos.Z.Clamped(minHeight, maxHeight);
        }

        bool HandleSingleNodeSelection(InputState input, Vector2 mousePos)
        {
            if (SelectedNodeList.Count != 1)
                return false;

            var node = SelectedNodeList[0];

            bool setReturn = false;
            setReturn |= SliderShield.HandleInput(input, ref node.AttackShieldedWeight);
            setReturn |= SliderDps.HandleInput(input, ref node.DPSWeight);
            setReturn |= SliderVulture.HandleInput(input, ref node.VultureWeight);
            setReturn |= SliderArmor.HandleInput(input, ref node.ArmoredWeight);
            setReturn |= SliderDefend.HandleInput(input, ref node.DefenderWeight);
            setReturn |= SliderAssist.HandleInput(input, ref node.AssistWeight);
            setReturn |= SliderSize.HandleInput(input, ref node.SizeWeight);
            setReturn |= OperationalRadius.HandleInput(input, ref node.OrdersRadius,
            node.Ship?.SensorRange ?? 500000);
            if (setReturn)
                return false;

            if (OperationsRect.HitTest(mousePos))
                return true;

            if (PrioritiesRect.HitTest(mousePos))
            {
                //OperationalRadius.HandleInput(input);
                //node.OrdersRadius = OperationalRadius.RelativeValue;
                return true;
            }

            return false;
        }

        bool IsDragging;

        void HandleSelectionBox(InputState input)
        {
            if (LeftMenu.HitTest(input.CursorPosition) || RightMenu.HitTest(input.CursorPosition))
            {
                SelectionBox = new(0, 0, -1, -1);
                return;
            }

            UpdateHoveredNodesList(input);
            UpdateClickableNodes();

            if (input.LeftMouseClick)
            {
                SelectedSquad = SelectNodesUnderMouse(input);
            }
            else if (input.LeftMouseHeld(0.05f))
            {
                if (!IsDragging && SelectedSquad != null)
                {
                    HandleSelectedSquadMove(input.CursorPosition, SelectedSquad);
                }
                else if (!IsDragging && SelectedNodeList.Count == 1)
                {
                    Vector2 newSpot = GetWorldSpaceFromScreenSpace(input.CursorPosition);
                    if (newSpot.Distance(SelectedNodeList[0].FleetOffset) <= 1000f)
                    {
                        HandleSelectedNodeMove(newSpot, SelectedNodeList[0], input.CursorPosition);
                    }
                }
                else
                {
                    // start dragging state
                    if (HoveredNodeList.IsEmpty)
                        IsDragging = true;

                    if (IsDragging)
                    {
                        SelectionBox = input.LeftHold.GetSelectionBox();
                        SelectedNodeList.Clear();
                        foreach (ClickableNode node in ClickableNodes)
                            if (SelectionBox.HitTest(node.ScreenPos))
                                SelectedNodeList.Add(node.NodeToClick);
                    }
                }
            }
            else if (input.LeftMouseReleased)
            {
                IsDragging = false;
                SelectionBox = new(0, 0, -1, -1);
            }
        }

        bool GetRoundedNodeMove(Vector2 newSpot, Vector2 oldPos, out Vector2 difference)
        {
            difference = newSpot - oldPos;
            if (difference.Length() > 30f)
            {
                newSpot.X = newSpot.X.RoundUpTo(500);
                newSpot.Y = newSpot.Y.RoundUpTo(500);
                difference = (newSpot - oldPos);
                return true;
            }
            return false;
        }


        // moving a single ship node
        void HandleSelectedNodeMove(Vector2 newSpot, FleetDataNode node, Vector2 mousePos)
        {
            if (GetRoundedNodeMove(newSpot, node.FleetOffset, out Vector2 difference))
            {
                node.FleetOffset += difference;
                if (node.Ship != null)
                {
                    node.Ship.RelativeFleetOffset = node.FleetOffset;
                }
            }

            // this is some kind of cleanup function? TODO
            foreach (ClickableSquad cs in ClickableSquads)
            {
                if (cs.Rect.HitTest(mousePos) && !cs.Squad.DataNodes.Contains(node))
                {
                    foreach (Array<Squad> flank in SelectedFleet.AllFlanks)
                    {
                        foreach (Squad squad in flank)
                        {
                            squad.DataNodes.Remove(node);
                            if (node.Ship != null)
                                squad.Ships.Remove(node.Ship);
                        }
                    }

                    cs.Squad.DataNodes.Add(node);
                    if (node.Ship != null)
                        cs.Squad.Ships.Add(node.Ship);
                }
            }
        }

        // move an entire squad
        void HandleSelectedSquadMove(Vector2 mousePos, Squad selectedSquad)
        {
            Vector2 newSpot = GetWorldSpaceFromScreenSpace(mousePos);
            if (GetRoundedNodeMove(newSpot, selectedSquad.Offset, out Vector2 difference))
            {
                selectedSquad.Offset += difference;
                foreach (FleetDataNode node in selectedSquad.DataNodes)
                {
                    node.FleetOffset += difference;
                    if (node.Ship != null)
                    {
                        Ship ship = node.Ship;
                        ship.RelativeFleetOffset += difference;
                    }
                }
            }
        }

        void UpdateHoveredNodesList(InputState input)
        {
            HoveredNodeList.Clear();

            bool hovering = false;
            foreach (ClickableSquad squad in ClickableSquads)
            {
                if (squad.Rect.HitTest(input.CursorPosition))
                {
                    hovering = true;
                    HoveredSquad = squad.Squad;
                    foreach (FleetDataNode node in HoveredSquad.DataNodes)
                        HoveredNodeList.Add(node);
                    break;
                }
            }

            if (!hovering)
            {
                foreach (ClickableNode node in ClickableNodes)
                {
                    if (input.CursorPosition.InRadius(node.ScreenPos, node.Radius))
                        HoveredNodeList.Add(node.NodeToClick);
                }
            }
        }

        Squad SelectNodesUnderMouse(InputState input)
        {
            bool hitSomething = false;
            Squad selected = null;

            foreach (ClickableNode node in ClickableNodes)
            {
                if (input.CursorPosition.InRadius(node.ScreenPos, node.Radius))
                {
                    if (SelectedNodeList.Count > 0 && !input.IsShiftKeyDown)
                        SelectedNodeList.Clear();

                    GameAudio.FleetClicked();
                    hitSomething = true;

                    if (!SelectedNodeList.Contains(node.NodeToClick))
                        SelectedNodeList.Add(node.NodeToClick);
                    
                    UpdateSliders(node.NodeToClick);
                    break;
                }
            }

            foreach (ClickableSquad squad in ClickableSquads)
            {
                if (squad.Rect.HitTest(input.CursorPosition))
                {
                    hitSomething = true;
                    selected = squad.Squad;
                    if (SelectedNodeList.Count > 0 && !input.IsShiftKeyDown)
                        SelectedNodeList.Clear();

                    GameAudio.FleetClicked();
                    SelectedNodeList.Assign(selected.DataNodes);

                    UpdateSliders(selected.MasterDataNode);
                    break;
                }
            }

            if (!hitSomething)
            {
                SelectedNodeList.Clear();
            }

            OrdersButtons.ResetButtons(SelectedNodeList);

            return selected;
        }

        void UpdateClickableNodes()
        {
            ClickableNodes.Clear();
            if (SelectedFleet == null)
                return;

            foreach (FleetDataNode node in SelectedFleet.DataNodes)
            {
                (Vector2 screenPos, float screenRadius) = GetNodeScreenPosAndRadius(node);
                ClickableNodes.Add(new()
                {
                    Radius = screenRadius,
                    ScreenPos = screenPos,
                    NodeToClick = node
                });
            }
        }

        static (Vector2 offset, float radius) GetNodeOffsetAndRadius(FleetDataNode node)
        {
            if (node.Ship != null)
                return (node.Ship.RelativeFleetOffset, node.Ship.Radius);
            return (node.FleetOffset, ResourceManager.Ships.Get(node.ShipName).Radius);
        }

        (Vector2 screenPos, float screenRadius) GetNodeScreenPosAndRadius(FleetDataNode node)
        {
            (Vector2 offset, float radius) = GetNodeOffsetAndRadius(node);
            return GetPosAndRadiusOnScreen(offset, radius);
        }

        void UpdateSliders(FleetDataNode node)
        {
            SliderArmor.SetAmount(node.ArmoredWeight);
            SliderAssist.SetAmount(node.AssistWeight);
            SliderDefend.SetAmount(node.DefenderWeight);
            SliderDps.SetAmount(node.DPSWeight);
            SliderShield.SetAmount(node.AttackShieldedWeight);
            SliderVulture.SetAmount(node.VultureWeight);
            SliderSize.SetAmount(node.SizeWeight);
            OperationalRadius.RelativeValue = node.OrdersRadius;
        }
    }
}