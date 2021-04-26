using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed partial class FleetDesignScreen
    {
        Vector2 GetWorldSpaceFromScreenSpace(Vector2 screenSpace)
        {
            Viewport viewport = Viewport;
            Vector3 nearPoint = viewport.Unproject(new Vector3(screenSpace, 0f), Projection, View, Matrix.Identity);
            Viewport viewport1 = Viewport;
            Vector3 farPoint = viewport1.Unproject(new Vector3(screenSpace, 1f), Projection, View, Matrix.Identity);
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);
            float k = -pickRay.Position.Z / pickRay.Direction.Z;
            Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X,
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
            Vector2 lowerRightWorldSpace =
                GetWorldSpaceFromScreenSpace(new Vector2(pp.BackBufferWidth, pp.BackBufferHeight));
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

        void OnOrderButtonClicked(ToggleButton b, CombatState state)
        {
            foreach (ToggleButton other in OrdersButtons) // disable others
                if (other != b) other.IsToggled = false;

            foreach (FleetDataNode node in SelectedNodeList)
            {
                node.CombatState = state;
                if (node.Ship != null)
                    node.Ship.AI.CombatState = node.CombatState;
            }

            if (SelectedNodeList[0].Ship != null)
            {
                SelectedNodeList[0].Ship.AI.CombatState = SelectedNodeList[0].CombatState;
                GameAudio.EchoAffirmative();
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

            GlobalStats.TakingInput = FleetNameEntry.HandlingInput;

            InputSelectFleet(1, Input.Fleet1);
            InputSelectFleet(2, Input.Fleet2);
            InputSelectFleet(3, Input.Fleet3);
            InputSelectFleet(4, Input.Fleet4);
            InputSelectFleet(5, Input.Fleet5);
            InputSelectFleet(6, Input.Fleet6);
            InputSelectFleet(7, Input.Fleet7);
            InputSelectFleet(8, Input.Fleet8);
            InputSelectFleet(9, Input.Fleet9);

            foreach (KeyValuePair<int, Rectangle> rect in FleetsRects)
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
                    node.DPSWeight = SliderDps.amount;
                    node.VultureWeight = SliderVulture.amount;
                    node.ArmoredWeight = SliderArmor.amount;
                    node.DefenderWeight = SliderDefend.amount;
                    node.AssistWeight = SliderAssist.amount;
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
                    Viewport viewport = Viewport;
                    Vector3 nearPoint = viewport.Unproject(new Vector3(input.CursorPosition, 0f), Projection, View,
                        Matrix.Identity);
                    Viewport viewport1 = Viewport;
                    Vector3 farPoint = viewport1.Unproject(new Vector3(input.CursorPosition, 1f), Projection, View,
                        Matrix.Identity);
                    Vector3 direction = farPoint - nearPoint;
                    direction.Normalize();
                    Ray pickRay = new Ray(nearPoint, direction);
                    float k = -pickRay.Position.Z / pickRay.Direction.Z;
                    Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X,
                        pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
                    FleetDataNode node = new FleetDataNode
                    {
                        FleetOffset = new Vector2(pickedPosition.X, pickedPosition.Y),
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
                        node.Ship.ShowSceneObjectAt(new Vector3(node.Ship.RelativeFleetOffset, -500000f));
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
            if (input.ScrollIn)
            {
                FleetDesignScreen desiredCamHeight = this;
                desiredCamHeight.DesiredCamHeight = desiredCamHeight.DesiredCamHeight - 1500f;
            }

            if (input.ScrollOut)
            {
                FleetDesignScreen fleetDesignScreen = this;
                fleetDesignScreen.DesiredCamHeight = fleetDesignScreen.DesiredCamHeight + 1500f;
            }

            if (DesiredCamHeight < 3000f)
            {
                DesiredCamHeight = 3000f;
            }
            else if (DesiredCamHeight > 100000f)
            {
                DesiredCamHeight = 100000f;
            }

            if (Input.RightMouseHeld())
                if (Input.StartRightHold.OutsideRadius(Input.CursorPosition, 10f))
                {
                    CamVelocity = Input.CursorPosition.DirectionToTarget(Input.StartRightHold);
                    CamVelocity = Vector2.Normalize(CamVelocity) *
                                  Vector2.Distance(Input.StartRightHold, Input.CursorPosition);
                }
                else
                {
                    CamVelocity = Vector2.Zero;
                }

            if (!Input.RightMouseHeld() && !Input.LeftMouseHeld())
            {
                CamVelocity = Vector2.Zero;
            }

            if (CamVelocity.Length() > 150f)
            {
                CamVelocity = Vector2.Normalize(CamVelocity) * 150f;
            }

            if (float.IsNaN(CamVelocity.X) || float.IsNaN(CamVelocity.Y))
            {
                CamVelocity = Vector2.Zero;
            }

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

                        if (node.Ship.GetSO() != null)
                            node.Ship.GetSO().World = Matrix.CreateTranslation(new Vector3(node.Ship.RelativeFleetOffset, -500000f));
                        SelectedFleet.Ships.Remove(node.Ship);
                        node.Ship.fleet?.RemoveShip(node.Ship);
                    }

                    SelectedNodeList.Clear();
                }
            }

            return false;
        }

        bool HandleSingleNodeSelection(InputState input, Vector2 mousePos)
        {
            if (SelectedNodeList.Count != 1)
                return false;

            bool setReturn = false;
            setReturn |= SliderShield.HandleInput(input, ref SelectedNodeList[0].AttackShieldedWeight);
            setReturn |= SliderDps.HandleInput(input, ref SelectedNodeList[0].DPSWeight);
            setReturn |= SliderVulture.HandleInput(input, ref SelectedNodeList[0].VultureWeight);
            setReturn |= SliderArmor.HandleInput(input, ref SelectedNodeList[0].ArmoredWeight);
            setReturn |= SliderDefend.HandleInput(input, ref SelectedNodeList[0].DefenderWeight);
            setReturn |= SliderAssist.HandleInput(input, ref SelectedNodeList[0].AssistWeight);
            setReturn |= SliderSize.HandleInput(input, ref SelectedNodeList[0].SizeWeight);
            setReturn |= OperationalRadius.HandleInput(input, ref SelectedNodeList[0].OrdersRadius,
            SelectedNodeList[0].Ship?.SensorRange ?? 500000);
            if (setReturn)
                return false;

            if (OperationsRect.HitTest(mousePos))
                return true;

            if (PrioritiesRect.HitTest(mousePos))
            {
                //OperationalRadius.HandleInput(input);
                //SelectedNodeList[0].OrdersRadius = OperationalRadius.RelativeValue;
                return true;
            }

            return false;
        }

        void HandleSelectionBox(InputState input)
        {
            if (LeftMenu.HitTest(input.CursorPosition) || RightMenu.HitTest(input.CursorPosition))
            {
                SelectionBox = new Rectangle(0, 0, -1, -1);
                return;
            }

            Vector2 mousePosition = input.CursorPosition;
            HoveredNodeList.Clear();
            bool hovering = false;
            foreach (ClickableSquad squad in ClickableSquads)
            {
                if (input.CursorPosition.OutsideRadius(squad.ScreenPos, 8f))
                {
                    continue;
                }

                HoveredSquad = squad.Squad;
                hovering = true;
                foreach (FleetDataNode node in HoveredSquad.DataNodes)
                {
                    HoveredNodeList.Add(node);
                }

                break;
            }

            if (!hovering)
            {
                foreach (ClickableNode node in ClickableNodes)
                {
                    if (Vector2.Distance(input.CursorPosition, node.ScreenPos) > node.Radius)
                    {
                        continue;
                    }

                    HoveredNodeList.Add(node.NodeToClick);
                    hovering = true;
                }
            }

            if (!hovering)
            {
                HoveredNodeList.Clear();
            }

            HandleInputShipSelect(input);

            if (SelectedSquad != null)
            {
                if (!Input.LeftMouseHeld()) return;
                Viewport viewport = Viewport;
                Vector3 nearPoint = viewport.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0f),
                    Projection, View, Matrix.Identity);
                Viewport viewport1 = Viewport;
                Vector3 farPoint = viewport1.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 1f),
                    Projection, View, Matrix.Identity);
                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();
                Ray pickRay = new Ray(nearPoint, direction);
                float k = -pickRay.Position.Z / pickRay.Direction.Z;
                Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X,
                    pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
                Vector2 newspot = new Vector2(pickedPosition.X, pickedPosition.Y);
                Vector2 difference = newspot - SelectedSquad.Offset;
                if (difference.Length() > 30f)
                {
                    Fleet.Squad selectedSquad = SelectedSquad;
                    selectedSquad.Offset = selectedSquad.Offset + difference;
                    foreach (FleetDataNode node in SelectedSquad.DataNodes)
                    {
                        FleetDataNode fleetOffset = node;
                        fleetOffset.FleetOffset = fleetOffset.FleetOffset + difference;
                        if (node.Ship == null)
                        {
                            continue;
                        }

                        Ship ship = node.Ship;
                        ship.RelativeFleetOffset = ship.RelativeFleetOffset + difference;
                    }
                }
            }
            else if (SelectedNodeList.Count != 1)
            {
                if (Input.LeftMouseHeld())
                {
                    SelectionBox = new Rectangle(input.MouseX, input.MouseY, 0, 0);
                }

                if (Input.LeftMouseHeldDown)
                {
                    if (input.MouseX < SelectionBox.X)
                    {
                        SelectionBox.X = input.MouseX;
                    }

                    if (input.MouseY < SelectionBox.Y)
                    {
                        SelectionBox.Y = input.MouseY;
                    }

                    SelectionBox.Width = Math.Abs(SelectionBox.Width);
                    SelectionBox.Height = Math.Abs(SelectionBox.Height);
                    foreach (ClickableNode node in ClickableNodes)
                    {
                        if (!SelectionBox.Contains(new Point((int) node.ScreenPos.X, (int) node.ScreenPos.Y)))
                        {
                            continue;
                        }

                        SelectedNodeList.Add(node.NodeToClick);
                    }

                    SelectionBox = new Rectangle(0, 0, -1, -1);
                    return;
                }

                if (input.LeftMouseClick)
                {
                    if (input.MouseX < SelectionBox.X)
                    {
                        SelectionBox.X = input.MouseX;
                    }

                    if (input.MouseY < SelectionBox.Y)
                    {
                        SelectionBox.Y = input.MouseY;
                    }

                    SelectionBox.Width = Math.Abs(SelectionBox.Width);
                    SelectionBox.Height = Math.Abs(SelectionBox.Height);
                    foreach (ClickableNode node in ClickableNodes)
                    {
                        if (!SelectionBox.Contains(new Point((int) node.ScreenPos.X, (int) node.ScreenPos.Y)))
                        {
                            continue;
                        }

                        SelectedNodeList.Add(node.NodeToClick);
                    }

                    SelectionBox = new Rectangle(0, 0, -1, -1);
                }
            }
            else if (Input.LeftMouseHeld())
            {
                Viewport viewport2 = Viewport;
                Vector3 nearPoint = viewport2.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0f), Projection,
                    View, Matrix.Identity);
                Viewport viewport3 = Viewport;
                Vector3 farPoint = viewport3.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 1f), Projection,
                    View, Matrix.Identity);
                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();
                Ray pickRay = new Ray(nearPoint, direction);
                float k = -pickRay.Position.Z / pickRay.Direction.Z;
                Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X,
                    pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
                Vector2 newspot = new Vector2(pickedPosition.X, pickedPosition.Y);
                if (Vector2.Distance(newspot, SelectedNodeList[0].FleetOffset) > 1000f)
                {
                    return;
                }

                Vector2 difference = newspot - SelectedNodeList[0].FleetOffset;
                if (difference.Length() > 30f)
                {
                    FleetDataNode item = SelectedNodeList[0];
                    item.FleetOffset = item.FleetOffset + difference;
                    if (SelectedNodeList[0].Ship != null)
                    {
                        SelectedNodeList[0].Ship.RelativeFleetOffset = SelectedNodeList[0].FleetOffset;
                    }
                }

                foreach (ClickableSquad cs in ClickableSquads)
                {
                    if (Vector2.Distance(cs.ScreenPos, mousePosition) >= 5f ||
                        cs.Squad.DataNodes.Contains(SelectedNodeList[0]))
                    {
                        continue;
                    }

                    foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
                    {
                        foreach (Fleet.Squad squad in flank)
                        {
                            squad.DataNodes.Remove(SelectedNodeList[0]);
                            if (SelectedNodeList[0].Ship == null)
                            {
                                continue;
                            }

                            squad.Ships.Remove(SelectedNodeList[0].Ship);
                        }
                    }

                    cs.Squad.DataNodes.Add(SelectedNodeList[0]);
                    if (SelectedNodeList[0].Ship == null)
                    {
                        continue;
                    }

                    cs.Squad.Ships.Add(SelectedNodeList[0].Ship);
                }
            }
        }

        void HandleInputShipSelect(InputState input)
        {
            if (!input.LeftMouseClick)
                return;

            bool hitSomething = false;
            SelectedSquad = null;

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

                    SliderArmor.SetAmount(node.NodeToClick.ArmoredWeight);
                    SliderAssist.SetAmount(node.NodeToClick.AssistWeight);
                    SliderDefend.SetAmount(node.NodeToClick.DefenderWeight);
                    SliderDps.SetAmount(node.NodeToClick.DPSWeight);
                    SliderShield.SetAmount(node.NodeToClick.AttackShieldedWeight);
                    SliderVulture.SetAmount(node.NodeToClick.VultureWeight);
                    OperationalRadius.RelativeValue = node.NodeToClick.OrdersRadius;
                    SliderSize.SetAmount(node.NodeToClick.SizeWeight);
                    break;
                }
            }

            foreach (ClickableSquad squad in ClickableSquads)
            {
                if (input.CursorPosition.InRadius(squad.ScreenPos, 4))
                {
                    SelectedSquad = squad.Squad;
                    if (SelectedNodeList.Count > 0 && !input.IsShiftKeyDown)
                        SelectedNodeList.Clear();

                    hitSomething = true;
                    GameAudio.FleetClicked();
                    SelectedNodeList.Clear();
                    SelectedNodeList.AddRange(SelectedSquad.DataNodes);

                    SliderArmor.SetAmount(SelectedSquad.MasterDataNode.ArmoredWeight);
                    SliderAssist.SetAmount(SelectedSquad.MasterDataNode.AssistWeight);
                    SliderDefend.SetAmount(SelectedSquad.MasterDataNode.DefenderWeight);
                    SliderDps.SetAmount(SelectedSquad.MasterDataNode.DPSWeight);
                    SliderShield.SetAmount(SelectedSquad.MasterDataNode.AttackShieldedWeight);
                    SliderVulture.SetAmount(SelectedSquad.MasterDataNode.VultureWeight);
                    OperationalRadius.RelativeValue = SelectedSquad.MasterDataNode.OrdersRadius;
                    SliderSize.SetAmount(SelectedSquad.MasterDataNode.SizeWeight);
                    break;
                }
            }

            if (!hitSomething)
            {
                SelectedSquad = null;
                SelectedNodeList.Clear();
            }

            Log.Info("Reset OrdersButtons");

            // reset the buttons
            foreach (ToggleButton button in OrdersButtons)
            {
                button.Visible = SelectedNodeList.Count > 0;
                button.IsToggled = false;
            }

            // mark combined combat state statuses
            foreach (FleetDataNode fleetNode in SelectedNodeList)
            {
                foreach (ToggleButton button in OrdersButtons)
                {
                    button.IsToggled |= (fleetNode.CombatState == button.CombatState);
                }
            }
        }
    }
}