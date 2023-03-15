using Ship_Game.Audio;
using Ship_Game.Ships;
using SDGraphics;
using SDUtils;
using static Ship_Game.Fleets.Fleet;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Ship_Game.GameScreens.FleetDesign;

namespace Ship_Game
{
    public sealed partial class FleetDesignScreen
    {
        void InputSelectFleet(FleetButton b)
        {
            GameAudio.AffirmativeClick();
            ChangeFleet(b.FleetKey);
        }
        
        void OnDesignShipItemClicked(FleetDesignShipListItem item)
        {
            SelectedNodeList.Clear();
            SelectedSquad = null;

            // set the design as active so it can be placed
            if (item.Ship != null)
                ActiveShipDesign = item.Ship;
            else if (item.Design != null)
                ActiveShipDesign = ResourceManager.Ships.Get(item.Design.Name);
        }
        
        // if double clicked, automatically add this ship or design into a squad
        void OnDesignShipItemDoubleClicked(FleetDesignShipListItem item)
        {
            SelectedNodeList.Clear();
            SelectedSquad = null;
            // always clear the design, because OnDesignShipItemClicked will set it
            if (ActiveShipDesign != null) // this can happen sometimes due to click order?
                AddDesignToFleet(ActiveShipDesign, null);
            ActiveShipDesign = null;
        }

        public override bool HandleInput(InputState input)
        {
            if (Input.FleetExitScreen && !GlobalStats.TakingInput)
            {
                ExitScreen();
                return true;
            }

            if (EmpireUI.HandleInput(input, caller:this))
                return true;

            if (SelectedNodeList.Count != 1 && FleetNameEntry.HandleInput(input))
                return true;

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
                    return true;

                if (PrioritiesRect.HitTest(input.CursorPosition))
                {
                    OperationalRadius.HandleInput(input);
                    SelectedNodeList[0].OrdersRadius = OperationalRadius.RelativeValue;
                    return true;
                }
            }

            if (ActiveShipDesign != null && HandleActiveShipDesignInput(input))
                return true;

            HandleSelectionBox(input);
            HandleCameraMovement(input);

            if (Input.FleetRemoveSquad)
                RemoveSelectedSquad();

            return false;
        }

        bool HandleActiveShipDesignInput(InputState input)
        {
            Contract.Requires(ActiveShipDesign != null, "ActiveShipDesign cannot be null here");

            // we're dragging an active ship design,
            // assign it to the fleet on Left click
            if (input.LeftMouseClick && !ShipSL.HitTest(input.CursorPosition))
            {
                Vector2 pickedPosition = CursorWorldPosition2D;
                AddDesignToFleet(ActiveShipDesign, pickedPosition);

                // if we're holding shift key down, allow placing multiple designs
                if (!input.IsShiftKeyDown)
                    ActiveShipDesign = null;
            }

            if (input.RightMouseClick)
            {
                ActiveShipDesign = null;
                return true;
            }
            return false;
        }

        public IEnumerable<Squad> AllSquads
        {
            get
            {
                foreach (var flank in SelectedFleet.AllFlanks)
                    foreach (var squad in flank)
                        yield return squad;
            }
        }

        void AddDesignToFleet(Ship shipOrTemplate, Vector2? fleetOffset)
        {
            FleetDataNode node = new() { ShipName = shipOrTemplate.Name };

            if (fleetOffset.HasValue)
            {
                node.RelativeFleetOffset = fleetOffset.Value;
            }
            else
            {
                // TODO
            }

            SelectedFleet.DataNodes.Add(node);

            // is this an actual alive ship?
            bool isActualActiveShip = ActiveShips.ContainsRef(shipOrTemplate);
            if (isActualActiveShip)
            {
                if (SelectedFleet.Ships.Count == 0)
                    SelectedFleet.FinalPosition = shipOrTemplate.Position;

                // if so, immediately assigned the node
                node.Ship = shipOrTemplate;
                node.Ship.RelativeFleetOffset = node.RelativeFleetOffset;
                ActiveShips.RemoveRef(shipOrTemplate);
                SelectedFleet.AddShip(node.Ship);
                ShipSL.RemoveFirstIf(item => item.Ship == shipOrTemplate);

                // always clear active design for alive ships
                ActiveShipDesign = null;
            }
            // else: otherwise we will just have a data node
        }

        // delete all selected ships
        void RemoveSelectedSquad()
        {
            if (SelectedSquad != null)
            {
                SelectedFleet.CenterFlank.Remove(SelectedSquad);
                SelectedFleet.LeftFlank.Remove(SelectedSquad);
                SelectedFleet.RearFlank.Remove(SelectedSquad);
                SelectedFleet.RightFlank.Remove(SelectedSquad);
                SelectedFleet.ScreenFlank.Remove(SelectedSquad);
                SelectedSquad = null;
            }

            if (SelectedNodeList.NotEmpty)
            {
                foreach (Squad squad in AllSquads)
                {
                    foreach (FleetDataNode node in SelectedNodeList)
                    {
                        if (squad.DataNodes.Contains(node))
                        {
                            squad.DataNodes.Remove(node);
                            if (node.Ship != null)
                                squad.Ships.Remove(node.Ship);
                        }
                    }
                }

                foreach (FleetDataNode node in SelectedNodeList)
                {
                    SelectedFleet.DataNodes.Remove(node);
                    if (node.Ship != null)
                    {
                        node.Ship.ClearFleet(returnToManagedPools: true, clearOrders: true);
                        node.Ship.RemoveSceneObject();
                    }
                }

                SelectedNodeList.Clear();
            }

            // need to reset the list if any active ships were removed and return to global pool
            if (SubShips.SelectedIndex == 1)
                ResetLists();
        }
        
        Vector2 StartDragPos;

        void HandleCameraMovement(InputState input)
        {
            const float minHeight = 3000;
            const float maxHeight = 100_000;
            float scrollSpeed = minHeight + (CamPos.Z / maxHeight)*10_000;
            float worldWidthOnScreen = (float)VisibleWorldRect.Width;

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
                DesiredCamPos.X += -dv.X * worldWidthOnScreen * 0.001f;
                DesiredCamPos.Y += -dv.Y * worldWidthOnScreen * 0.001f;
            }
            else
            {
                float outer = -50f;
                float inner = +5.0f;
                float minLeft = outer, maxLeft = inner;
                float minTop  = outer, maxTop  = inner;
                float minRight  = ScreenWidth  - inner, maxRight  = ScreenWidth  - outer;
                float minBottom = ScreenHeight - inner, maxBottom = ScreenHeight - outer;
                bool InRange(float pos, float min, float max) => min <= pos && pos <= max;

                if (InRange(input.CursorX, minLeft, maxLeft) || input.KeysLeftHeld(arrowKeys:true))
                    DesiredCamPos.X -= 0.008f * worldWidthOnScreen;
                if (InRange(input.CursorX, minRight, maxRight) || input.KeysRightHeld(arrowKeys:true))
                    DesiredCamPos.X += 0.008f * worldWidthOnScreen;
                if (InRange(input.CursorY, minTop, maxTop) || input.KeysUpHeld(arrowKeys:true))
                    DesiredCamPos.Y -= 0.008f * worldWidthOnScreen;
                if (InRange(input.CursorY, minBottom, maxBottom) || input.KeysDownHeld(arrowKeys:true))
                    DesiredCamPos.Y += 0.008f * worldWidthOnScreen;
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

            return OperationsRect.HitTest(mousePos) || PrioritiesRect.HitTest(mousePos);
        }

        bool IsDragging;

        void HandleSelectionBox(InputState input)
        {
            if (LeftMenu.HitTest(input.CursorPosition) ||
                RightMenu.HitTest(input.CursorPosition) ||
                ShipSL.HitTest(input.CursorPosition))
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
                    HandleSelectedSquadMove(SelectedSquad);
                }
                else if (!IsDragging && SelectedNodeList.Count == 1)
                {
                    Vector2 newSpot = CursorWorldPosition2D;
                    if (newSpot.Distance(SelectedNodeList[0].RelativeFleetOffset) <= 1000f)
                    {
                        HandleSelectedNodeMove(newSpot, SelectedNodeList[0], input.CursorPosition);
                    }
                }
                else
                {
                    // start dragging state
                    // but only if we're not placing ActiveShipDesign
                    if (HoveredNodeList.IsEmpty && ActiveShipDesign == null)
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
                newSpot.X = (float)System.Math.Round(newSpot.X / 250) * 250;
                newSpot.Y = (float)System.Math.Round(newSpot.Y / 250) * 250;
                difference = (newSpot - oldPos);
                return true;
            }
            return false;
        }


        // moving a single ship node
        void HandleSelectedNodeMove(Vector2 newSpot, FleetDataNode node, Vector2 mousePos)
        {
            if (GetRoundedNodeMove(newSpot, node.RelativeFleetOffset, out Vector2 difference))
            {
                node.RelativeFleetOffset += difference;
                if (node.Ship != null)
                {
                    node.Ship.RelativeFleetOffset = node.RelativeFleetOffset;
                }
            }

            // this is some kind of cleanup function? TODO
            foreach (ClickableSquad cs in ClickableSquads)
            {
                if (cs.Rect.HitTest(mousePos) && !cs.Squad.DataNodes.Contains(node))
                {
                    foreach (Squad squad in AllSquads)
                    {
                        squad.DataNodes.Remove(node);
                        if (node.Ship != null)
                            squad.Ships.Remove(node.Ship);
                    }

                    cs.Squad.DataNodes.Add(node);
                    if (node.Ship != null)
                        cs.Squad.Ships.Add(node.Ship);
                }
            }
        }

        // move an entire squad
        void HandleSelectedSquadMove(Squad selectedSquad)
        {
            Vector2 newSpot = CursorWorldPosition2D;
            if (GetRoundedNodeMove(newSpot, selectedSquad.Offset, out Vector2 difference))
            {
                selectedSquad.Offset += difference;
                foreach (FleetDataNode node in selectedSquad.DataNodes)
                {
                    node.RelativeFleetOffset += difference;
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
            return (node.RelativeFleetOffset, ResourceManager.Ships.Get(node.ShipName).Radius);
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