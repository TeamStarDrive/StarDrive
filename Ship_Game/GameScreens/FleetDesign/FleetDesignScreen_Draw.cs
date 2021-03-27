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
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Viewport viewport;
            SubTexture nodeTexture = ResourceManager.Texture("UI/node");
            ScreenManager.BeginFrameRendering(elapsed, ref View, ref Projection);

            ScreenManager.GraphicsDevice.Clear(Color.Black);
            UniverseScreen us = Empire.Universe;
            us.bg.Draw(us, us.StarField);
            batch.Begin();
            DrawGrid();

            if (SelectedNodeList.Count == 1)
            {
                viewport = Viewport;
                Vector3 screenSpacePosition = viewport.Project(new Vector3(SelectedNodeList[0].FleetOffset.X
                    , SelectedNodeList[0].FleetOffset.Y, 0f), Projection, View, Matrix.Identity);
                var screenPos = new Vector2(screenSpacePosition.X, screenSpacePosition.Y);
                Vector2 radialPos = SelectedNodeList[0].FleetOffset.PointFromAngle(90f,
                    (SelectedNodeList[0].Ship?.SensorRange ?? 500000) * OperationalRadius.RelativeValue);
                viewport = Viewport;
                Vector3 insetRadialPos =
                    viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                float ssRadius = Math.Abs(insetRadialSS.X - screenPos.X);
                Rectangle nodeRect = new Rectangle((int) screenPos.X, (int) screenPos.Y, (int) ssRadius * 2,
                    (int) ssRadius * 2);
                Vector2 origin = new Vector2(nodeTexture.Width / 2f, nodeTexture.Height / 2f);
                batch.Draw(nodeTexture, nodeRect, new Color(0, 255, 0, 75), 0f, origin, SpriteEffects.None, 1f);
            }

            ClickableNodes.Clear();
            foreach (FleetDataNode node in SelectedFleet.DataNodes)
            {
                if (node.Ship == null)
                {
                    var template = ResourceManager.GetShipTemplate(node.ShipName);
                    float radius = template?.Radius ?? 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    ClickableNode cs = new ClickableNode
                    {
                        Radius = radius,
                        ScreenPos = pPos,
                        NodeToClick = node
                    };
                    ClickableNodes.Add(cs);
                }
                else
                {
                    Ship ship = node.Ship;
                    ship.ShowSceneObjectAt(new Vector3(ship.RelativeFleetOffset, 0f));
                    float radius = ship.Radius;//.GetSO().WorldBoundingSphere.Radius;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    var insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    var cs = new ClickableNode
                    {
                        Radius = radius,
                        ScreenPos = pPos,
                        NodeToClick = node
                    };
                    ClickableNodes.Add(cs);
                }
            }

            foreach (FleetDataNode node in HoveredNodeList)
            {
                var ship = node.Ship;
                if (ship == null)
                {
                    var template = ResourceManager.GetShipTemplate(node.ShipName);
                    float radius = template?.Radius ?? 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos);
                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, pPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(pPos, radius, new Color(255, 255, 255, 70), 2f);
                }
                else
                {
                    float radius = ship.Radius;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos);
                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, pPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(pPos, radius, new Color(255, 255, 255, 70), 2f);
                }
            }

            foreach (FleetDataNode node in SelectedNodeList)
            {
                if (node.Ship == null)
                {
                    if (node.Ship != null)
                    {
                        continue;
                    }

                    float radius = 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos);
                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, pPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(pPos, radius, Color.White, 2f);
                }
                else
                {
                    Ship ship = node.Ship;
                    float radius = ship.GetSO().WorldBoundingSphere.Radius;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos);
                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, pPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(pPos, radius, Color.White, 2f);
                }
            }

            DrawFleetManagementIndicators();
            batch.DrawRectangle(SelectionBox, Color.Green);
            batch.End();

            ScreenManager.RenderSceneObjects();

            batch.Begin();
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, "Fleet Hotkeys", TitlePos, Colors.Cream);
            const int numEntries = 9;
            int k = 9;
            int m = 0;
            foreach (KeyValuePair<int, Rectangle> rect in FleetsRects)
            {
                if (m == 9)
                {
                    break;
                }

                Rectangle r = rect.Value;
                float transitionOffset = ((TransitionPosition - 0.5f * k / numEntries) / 0.5f).Clamped(0f, 1f);
                k--;
                if (ScreenState != ScreenState.TransitionOn)
                {
                    r.X = r.X + (int) transitionOffset * 512;
                }
                else
                {
                    r.X = r.X - (int) (transitionOffset * 256f);
                    if (Math.Abs(transitionOffset) < .1f)
                    {
                        GameAudio.BlipClick();
                    }
                }

                var sel = new Selector(r, Color.TransparentBlack);
                batch.Draw(ResourceManager.Texture("NewUI/rounded_square"), r,
                    rect.Key != FleetToEdit ? Color.Black : new Color(0, 0, 255, 80));
                sel.Draw(batch, elapsed);

                Fleet f = EmpireManager.Player.GetFleetsDict()[rect.Key];
                if (f.DataNodes.Count > 0)
                {
                    var firect = new Rectangle(rect.Value.X + 6, rect.Value.Y + 6, rect.Value.Width - 12,
                        rect.Value.Width - 12);
                    batch.Draw(f.Icon, firect, EmpireManager.Player.EmpireColor);
                    if (f.AutoRequisition)
                    {
                        Rectangle autoReq = new Rectangle(firect.X + 54, firect.Y + 12, 20, 27);
                        batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq, ApplyCurrentAlphaToColor(EmpireManager.Player.EmpireColor));
                    }
                    
                }

                Vector2 num = new Vector2(rect.Value.X + 4, rect.Value.Y + 4);
                SpriteFont pirulen12 = Fonts.Pirulen12;
                int key = rect.Key;
                batch.DrawString(pirulen12, key.ToString(), num, Color.Orange);
                num.X = num.X + (rect.Value.Width + 5);
                batch.DrawString(Fonts.Pirulen12, f.Name, num,
                    rect.Key != FleetToEdit ? Color.Gray : Color.White);
                m++;
            }

            if (FleetToEdit != -1)
            {
                ShipDesigns.Draw(batch, elapsed);
                batch.DrawString(Fonts.Laserian14, "Ship Designs", ShipDesignsTitlePos, Colors.Cream);
            }

            EmpireUI.Draw(batch);
            foreach (FleetDataNode node in SelectedFleet.DataNodes)
            {
                if (node.Ship == null || CamPos.Z <= 15000f)
                {
                    if (node.Ship != null || node.ShipName == "Troop Shuttle")
                        continue;

                    if (!ResourceManager.GetShipTemplate(node.ShipName, out Ship ship))
                        continue;

                    float radius = 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos);
                    Rectangle r = new Rectangle((int) pPos.X - (int) radius, (int) pPos.Y - (int) radius,
                        (int) radius * 2, (int) radius * 2);

                    SubTexture icon = ship.GetTacticalIcon(out SubTexture secondary, out _);
                    if (node.GoalGUID == Guid.Empty)
                    {
                        Color color = HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node) ? Color.White : Color.Red;
                        batch.Draw(icon, r, color);
                        if (secondary != null)
                            batch.Draw(secondary, r, color);
                    }
                    else
                    {
                        Color color = HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node) ? Color.White : Color.Yellow;
                        batch.Draw(icon, r, color);
                        if (secondary != null)
                            batch.Draw(secondary, r, color);

                        string buildingAt = "";
                        foreach (Goal g in SelectedFleet.Owner.GetEmpireAI().Goals)
                        {
                            if (g.guid != node.GoalGUID || g.PlanetBuildingAt == null)
                                continue;

                            buildingAt = g.type == GoalType.Refit
                                ? $"Refitting at:\n{g.PlanetBuildingAt.Name}"
                                : $"Building at:\n{g.PlanetBuildingAt.Name}";
                        }

                        if (buildingAt.IsEmpty())
                            buildingAt = "Need spaceport";

                        batch.DrawString(Fonts.Arial8Bold, buildingAt, pPos + new Vector2(5f, -5f), Color.White);
                    }
                }
                else
                {
                    Ship ship = node.Ship;
                    SubTexture icon = ship.GetTacticalIcon(out SubTexture secondary, out _);
                    float radius = ship.GetSO().WorldBoundingSphere.Radius;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos);
                    if (radius < 10f)
                    {
                        radius = 10f;
                    }

                    Rectangle r = new Rectangle((int) pPos.X - (int) radius, (int) pPos.Y - (int) radius,
                        (int) radius * 2, (int) radius * 2);

                    Color color = HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node) ? Color.White : Color.Green;
                    batch.Draw(icon, r, color);
                    if (secondary != null)
                        batch.Draw(secondary, r, color);
                }
            }

            if (ActiveShipDesign != null)
            {
                float scale;
                Vector2 iconOrigin;
                Ship ship       = ActiveShipDesign;
                SubTexture icon = ship.GetTacticalIcon(out SubTexture secondary, out _);
                {
                    scale = ship.SurfaceArea /
                            (float) (30 + ResourceManager.Texture("TacticalIcons/symbol_fighter").Width);
                    iconOrigin = new Vector2(ResourceManager.Texture("TacticalIcons/symbol_fighter").Width / 2f,
                        ResourceManager.Texture("TacticalIcons/symbol_fighter").Width / 2f);
                    scale = scale * 4000f / CamPos.Z;

                    if (scale > 1f)    scale = 1f;
                    if (scale < 0.15f) scale = 0.15f;
                }

                float single     = Mouse.GetState().X;
                MouseState state = Mouse.GetState();
                Vector2 pos      = new Vector2(single, state.Y);
                Color color      = EmpireManager.Player.EmpireColor;
                batch.Draw(icon, pos, color , 0f, iconOrigin, scale, SpriteEffects.None, 1f);
                if (secondary != null)
                    batch.Draw(secondary, pos, color, 0f, iconOrigin, scale, SpriteEffects.None, 1f);
            }

            DrawSelectedData(batch, elapsed);
            
            base.Draw(batch, elapsed); // draw automatic elements on top of everything else
            batch.End();

            ScreenManager.EndFrameRendering();
        }

        void DrawFleetManagementIndicators()
        {
            Viewport viewport = Viewport;
            Vector3 pScreenSpace = viewport.Project(new Vector3(0f, 0f, 0f), Projection, View, Matrix.Identity);
            Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);

            var spriteBatch = ScreenManager.SpriteBatch;
            spriteBatch.FillRectangle(new Rectangle((int) pPos.X - 3, (int) pPos.Y - 3, 6, 6),
                new Color(255, 255, 255, 80));
            spriteBatch.DrawString(Fonts.Arial12Bold, "Fleet Center",
                new Vector2(pPos.X - Fonts.Arial12Bold.MeasureString("Fleet Center").X / 2f, pPos.Y + 5f),
                new Color(255, 255, 255, 70));
            foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in flank)
                {
                    Viewport viewport1 = Viewport;
                    pScreenSpace = viewport1.Project(new Vector3(squad.Offset, 0f), Projection, View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    spriteBatch.FillRectangle(new Rectangle((int) pPos.X - 2, (int) pPos.Y - 2, 4, 4),
                        new Color(0, 255, 0, 110));
                    spriteBatch.DrawString(Fonts.Arial8Bold, "Squad",
                        new Vector2(pPos.X - Fonts.Arial8Bold.MeasureString("Squad").X / 2f, pPos.Y + 5f),
                        new Color(0, 255, 0, 70));
                }
            }
        }

        void DrawGrid()
        {
            var spriteBatch = ScreenManager.SpriteBatch;

            int size = 20000;
            for (int x = 0; x < 21; x++)
            {
                Vector3 origin3 = new Vector3(x * size / 20 - size / 2, -(size / 2), 0f);
                Viewport viewport = Viewport;
                Vector3 originScreenSpace = viewport.Project(origin3, Projection, View, Matrix.Identity);
                Vector3 end3 = new Vector3(x * size / 20 - size / 2, size - size / 2, 0f);
                Viewport viewport1 = Viewport;
                Vector3 endScreenSpace = viewport1.Project(end3, Projection, View, Matrix.Identity);
                Vector2 origin = new Vector2(originScreenSpace.X, originScreenSpace.Y);
                Vector2 end = new Vector2(endScreenSpace.X, endScreenSpace.Y);
                spriteBatch.DrawLine(origin, end, new Color(211, 211, 211, 70));
            }

            for (int y = 0; y < 21; y++)
            {
                Vector3 origin3 = new Vector3(-(size / 2), y * size / 20 - size / 2, 0f);
                Viewport viewport2 = Viewport;
                Vector3 originScreenSpace = viewport2.Project(origin3, Projection, View, Matrix.Identity);
                Vector3 end3 = new Vector3(size - size / 2, y * size / 20 - size / 2, 0f);
                Viewport viewport3 = Viewport;
                Vector3 endScreenSpace = viewport3.Project(end3, Projection, View, Matrix.Identity);
                Vector2 origin = new Vector2(originScreenSpace.X, originScreenSpace.Y);
                Vector2 end = new Vector2(endScreenSpace.X, endScreenSpace.Y);
                spriteBatch.DrawLine(origin, end, new Color(211, 211, 211, 70));
            }
        }

        void DrawSelectedData(SpriteBatch batch, DrawTimes elapsed)
        {
            if (SelectedNodeList.Count == 1)
            {
                StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);
                Vector2 cursor = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
                if (SelectedNodeList[0].Ship == null)
                {
                    batch.DrawString(Fonts.Arial20Bold, string.Concat("(", SelectedNodeList[0].ShipName, ")"), cursor,
                        Colors.Cream);
                }
                else
                {
                    batch.DrawString(Fonts.Arial20Bold,
                        (!string.IsNullOrEmpty(SelectedNodeList[0].Ship.VanityName)
                            ? SelectedNodeList[0].Ship.VanityName
                            : string.Concat(SelectedNodeList[0].Ship.Name, " (", SelectedNodeList[0].Ship.shipData.Role,
                                ")")), cursor, Colors.Cream);
                }

                cursor.Y = OperationsRect.Y + 10;
                batch.DrawString(Fonts.Pirulen12, "Movement Orders", cursor, Colors.Cream);

                OperationsSelector = new Selector(OperationsRect, new Color(0, 0, 0, 180));
                OperationsSelector.Draw(batch, elapsed);
                cursor = new Vector2(OperationsRect.X + 20, OperationsRect.Y + 10);
                batch.DrawString(Fonts.Pirulen12, "Target Selection", cursor, Colors.Cream);
                SliderArmor.Draw(batch);
                SliderAssist.Draw(batch);
                SliderDefend.Draw(batch);
                SliderDps.Draw(batch);
                SliderShield.Draw(batch);
                SliderVulture.Draw(batch);
                Priorityselector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
                Priorityselector.Draw(batch, elapsed);
                cursor = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
                batch.DrawString(Fonts.Pirulen12, "Priorities", cursor, Colors.Cream);
                OperationalRadius.Draw(batch, elapsed);
                SliderSize.Draw(ScreenManager);
                return;
            }

            if (SelectedNodeList.Count > 1)
            {
                StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);
                Vector2 cursor = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
                if (SelectedNodeList[0].Ship == null)
                {
                    SpriteFont arial20Bold = Fonts.Arial20Bold;
                    int count = SelectedNodeList.Count;
                    batch.DrawString(arial20Bold, string.Concat("Group of ", count.ToString(), " ships selected"),
                        cursor, Colors.Cream);
                }
                else
                {
                    SpriteFont spriteFont = Fonts.Arial20Bold;
                    int num = SelectedNodeList.Count;
                    batch.DrawString(spriteFont, string.Concat("Group of ", num.ToString(), " ships selected"), cursor,
                        Colors.Cream);
                }

                cursor.Y = OperationsRect.Y + 10;
                batch.DrawString(Fonts.Pirulen12, "Group Movement Orders", cursor, Colors.Cream);

                OperationsSelector = new Selector(OperationsRect, new Color(0, 0, 0, 180));
                OperationsSelector.Draw(batch, elapsed);
                cursor = new Vector2(OperationsRect.X + 20, OperationsRect.Y + 10);
                batch.DrawString(Fonts.Pirulen12, "Group Target Selection", cursor, Colors.Cream);
                SliderArmor.Draw(batch);
                SliderAssist.Draw(batch);
                SliderDefend.Draw(batch);
                SliderDps.Draw(batch);
                SliderShield.Draw(batch);
                SliderVulture.Draw(batch);
                Priorityselector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
                Priorityselector.Draw(batch, elapsed);
                cursor = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
                batch.DrawString(Fonts.Pirulen12, "Group Priorities", cursor, Colors.Cream);
                OperationalRadius.Draw(batch, elapsed);
                SliderSize.Draw(ScreenManager);
                return;
            }

            if (FleetToEdit == -1)
            {
                float transitionOffset = (float) Math.Pow(TransitionPosition, 2);
                Rectangle r = SelectedStuffRect;
                if (ScreenState == ScreenState.TransitionOn)
                {
                    r.Y = r.Y + (int) (transitionOffset * 256f);
                }

                StuffSelector = new Selector(r, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);
                Vector2 cursor = new Vector2(r.X + 20, r.Y + 10);
                batch.DrawString(Fonts.Arial20Bold, "No Fleet Selected", cursor, Colors.Cream);
                cursor.Y = cursor.Y + (Fonts.Arial20Bold.LineSpacing + 2);
                string txt =
                    "You are not currently editing a fleet. Click a hotkey on the left side of the screen to begin creating or editing the corresponding fleet. \n\nWhen you are finished editing, you can save your fleet design to disk for quick access in the future.";
                txt = Fonts.Arial12Bold.ParseText(txt, SelectedStuffRect.Width - 40);
                batch.DrawString(Fonts.Arial12Bold, txt, cursor, Colors.Cream);
                return;
            }

            StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
            StuffSelector.Draw(batch, elapsed);
            Fleet f = EmpireManager.Player.GetFleetsDict()[FleetToEdit];
            Vector2 cursor1 = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
            FleetNameEntry.Text = f.Name;
            FleetNameEntry.ClickableArea = new Rectangle((int) cursor1.X, (int) cursor1.Y,
                (int) Fonts.Arial20Bold.MeasureString(f.Name).X, Fonts.Arial20Bold.LineSpacing);
            FleetNameEntry.Draw(batch, elapsed, Fonts.Arial20Bold, cursor1,
                (FleetNameEntry.Hover ? Color.Orange : Colors.Cream));
            cursor1.Y = cursor1.Y + (Fonts.Arial20Bold.LineSpacing + 10);
            cursor1 = cursor1 + new Vector2(50f, 30f);
            batch.DrawString(Fonts.Pirulen12, "Fleet Icon", cursor1, Colors.Cream);
            Rectangle ficonrect = new Rectangle((int) cursor1.X + 12, (int) cursor1.Y + Fonts.Pirulen12.LineSpacing + 5,
                64, 64);
            batch.Draw(f.Icon, ficonrect, f.Owner.EmpireColor);
            RequisitionForces.Draw(ScreenManager);
            SaveDesign.Draw(ScreenManager);
            LoadDesign.Draw(ScreenManager);
            Priorityselector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
            Priorityselector.Draw(batch, elapsed);
            cursor1 = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
            batch.DrawString(Fonts.Pirulen12, "Fleet Design Overview", cursor1, Colors.Cream);
            cursor1.Y = cursor1.Y + (Fonts.Pirulen12.LineSpacing + 2);
            string txt0 = Localizer.Token(4043);
            txt0 = Fonts.Arial12Bold.ParseText(txt0, PrioritiesRect.Width - 40);
            batch.DrawString(Fonts.Arial12Bold, txt0, cursor1, Colors.Cream);
        }
    }
}