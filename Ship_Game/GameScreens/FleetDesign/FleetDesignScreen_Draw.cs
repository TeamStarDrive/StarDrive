using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using SDGraphics;
using SDUtils;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed partial class FleetDesignScreen
    {
        (Vector2 pos, float radius) GetPosAndRadiusOnScreen(Vector2 fleetOffset, float radius)
        {
            Vector2 pos1 = ProjectToScreenPos(new Vector3(fleetOffset, 0f));
            Vector2 pos2 = ProjectToScreenPos(new Vector3(fleetOffset.PointFromAngle(90f, radius), 0f));
            float radiusOnScreen = pos1.Distance(pos2) + 10f;
            return (pos1, radiusOnScreen);
        }

        Vector2 ProjectToScreenPos(in Vector3 worldPos)
        {
            var p = new Vector3(Viewport.Project(worldPos, Projection, View, Matrix.Identity));
            return new Vector2(p.X, p.Y);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Viewport viewport;
            SubTexture nodeTexture = ResourceManager.Texture("UI/node");
            ScreenManager.BeginFrameRendering(elapsed, ref View, ref Projection);

            ScreenManager.GraphicsDevice.Clear(Color.Black);
            Universe.DrawStarField(batch);

            batch.Begin();
            DrawGrid();
            
            if (SelectedNodeList.Count == 1)
            {
                viewport = Viewport;
                Vector3 screenSpacePosition = new Vector3(
                    viewport.Project(new Vector3(SelectedNodeList[0].FleetOffset.X
                    , SelectedNodeList[0].FleetOffset.Y, 0f), Projection, View, Matrix.Identity)
                );
                var screenPos = new Vector2(screenSpacePosition.X, screenSpacePosition.Y);
                Vector2 radialPos = SelectedNodeList[0].FleetOffset.PointFromAngle(90f,
                    (SelectedNodeList[0].Ship?.SensorRange ?? 500000) * OperationalRadius.RelativeValue);
                viewport = Viewport;
                Vector3 insetRadialPos = new Vector3(
                    viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity)
                );
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
                    Vector3 pScreenSpace = new Vector3(
                        viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View,
                        Matrix.Identity)
                    );
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointFromAngle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos = new Vector3(
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity)
                    );
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = insetRadialSS.Distance(pPos) + 10f;
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
                    ship.ShowSceneObjectAt(ship.RelativeFleetOffset, 0f);

                    (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(ship.RelativeFleetOffset, ship.Radius);
                    var cs = new ClickableNode
                    {
                        Radius = screenRadius,
                        ScreenPos = screenPos,
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

                    (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(node.FleetOffset, template?.Radius ?? 150f);

                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, screenPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(screenPos, screenRadius, new Color(255, 255, 255, 70), 2f);
                }
                else
                {
                    (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(ship.RelativeFleetOffset, ship.Radius);

                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, screenPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(screenPos, screenRadius, new Color(255, 255, 255, 70), 2f);
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
                    
                    (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(node.FleetOffset, 150f);

                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, screenPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(screenPos, screenRadius, Color.White, 2f);
                }
                else
                {
                    Ship ship = node.Ship;
                    (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(ship.RelativeFleetOffset, ship.GetSO().WorldBoundingSphere.Radius);

                    foreach (ClickableSquad squad in ClickableSquads)
                    {
                        if (!squad.Squad.DataNodes.Contains(node))
                        {
                            continue;
                        }

                        batch.DrawLine(squad.ScreenPos, screenPos, new Color(0, 255, 0, 70), 2f);
                    }

                    DrawCircle(screenPos, screenRadius, Color.White, 2f);
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

                Fleet f = Universe.Player.GetFleet(rect.Key);
                if (f.DataNodes.Count > 0)
                {
                    var firect = new Rectangle(rect.Value.X + 6, rect.Value.Y + 6, rect.Value.Width - 12,
                        rect.Value.Width - 12);
                    batch.Draw(f.Icon, firect, Universe.Player.EmpireColor);
                    if (f.AutoRequisition)
                    {
                        Rectangle autoReq = new Rectangle(firect.X + 54, firect.Y + 12, 20, 27);
                        batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq, ApplyCurrentAlphaToColor(Universe.Player.EmpireColor));
                    }
                    
                }

                Vector2 num = new Vector2(rect.Value.X + 4, rect.Value.Y + 4);
                Graphics.Font pirulen12 = Fonts.Pirulen12;
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
                Color color = GetTacticalIconColor();
                if (node.Ship == null || CamPos.Z <= 15000f)
                {
                    if (!ResourceManager.GetShipTemplate(node.ShipName, out Ship ship))
                        continue;
                    
                    (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(node.FleetOffset, 150f);

                    var r = new Rectangle((int) screenPos.X - (int) screenRadius, (int) screenPos.Y - (int) screenRadius, 
                                          (int) screenRadius * 2, (int) screenRadius * 2);

                    DrawIcon(ship, r);
                    if (node.Goal == null)
                    {
                        if (NodeShipResupplying())
                            batch.DrawString(Fonts.Arial8Bold, "Resupplying", screenPos + new Vector2(5f, -5f), Color.White);
                    }
                    else
                    {
                        string buildingAt = "";
                        foreach (Goal g in SelectedFleet.Owner.AI.Goals)
                        {
                            if (g != node.Goal || g.PlanetBuildingAt == null)
                                continue;

                            buildingAt = g.Type == GoalType.Refit
                                ? $"Refitting at:\n{g.PlanetBuildingAt.Name}"
                                : $"Building at:\n{g.PlanetBuildingAt.Name}";
                        }

                        if (buildingAt.IsEmpty())
                            buildingAt = "Need spaceport";

                        batch.DrawString(Fonts.Arial8Bold, buildingAt, screenPos + new Vector2(5f, -5f), Color.White);
                    }
                }
                else
                {
                    Ship ship = node.Ship;
                    (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(node.FleetOffset, ship.GetSO().WorldBoundingSphere.Radius);
                    if (screenRadius < 10f)
                    {
                        screenRadius = 10f;
                    }

                    Rectangle r = new Rectangle((int) screenPos.X - (int) screenRadius, (int) screenPos.Y - (int) screenRadius,
                        (int) screenRadius * 2, (int) screenRadius * 2);

                    DrawIcon(ship, r);   
                    if (NodeShipResupplying())
                        batch.DrawString(Fonts.Arial8Bold, "Resupplying", screenPos + new Vector2(5f, -5f), Color.White);
                }

                void DrawIcon(Ship ship, Rectangle r)
                {
                    if (!ShouldDrawTacticalIcon())
                        return;

                    (SubTexture icon, SubTexture secondary) = ship.TacticalIcon();
                    batch.Draw(icon, r, color);
                    if (secondary != null)
                        batch.Draw(secondary, r, color);
                }

                Color GetTacticalIconColor()
                {
                    if (Hovered())             return Color.White;
                    if (node.Goal != null)     return Color.Yellow;
                    if (NodeShipResupplying()) return Color.Gray;

                    return node.Ship != null ? Color.Green : Color.Red;
                }

                bool ShouldDrawTacticalIcon()
                {
                    return CamPos.Z > 15000f || node.Ship == null || NodeShipResupplying();
                }

                bool NodeShipResupplying() => node.Ship?.Resupplying == true;
                bool Hovered() => HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node);
            }

            if (ActiveShipDesign != null)
            {
                (SubTexture icon, SubTexture secondary) = ActiveShipDesign.TacticalIcon();
                Vector2 iconOrigin = new Vector2(icon.Width, icon.Width) / 2f;
                float scale = ActiveShipDesign.SurfaceArea / (float)(30 + icon.Width);
                scale = scale * 4000f / CamPos.Z;
                if (scale > 1f)    scale = 1f;
                if (scale < 0.15f) scale = 0.15f;

                Color color = Universe.Player.EmpireColor;
                batch.Draw(icon, Input.CursorPosition, color, 0f, iconOrigin, scale, SpriteEffects.None, 1f);
                if (secondary != null)
                    batch.Draw(secondary, Input.CursorPosition, color, 0f, iconOrigin, scale, SpriteEffects.None, 1f);
            }

            DrawSelectedData(batch, elapsed);
            
            base.Draw(batch, elapsed); // draw automatic elements on top of everything else
            batch.End();

            ScreenManager.EndFrameRendering();
        }

        void DrawFleetManagementIndicators()
        {
            Vector2 pPos = ProjectToScreenPos(new Vector3(0f, 0f, 0f));

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
                    pPos = ProjectToScreenPos(new Vector3(squad.Offset, 0f));
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
                Vector3 end3 = new Vector3(x * size / 20 - size / 2, size - size / 2, 0f);

                Vector2 origin = ProjectToScreenPos(origin3);
                Vector2 end = ProjectToScreenPos(end3);

                spriteBatch.DrawLine(origin, end, new Color(211, 211, 211, 70));
            }

            for (int y = 0; y < 21; y++)
            {
                Vector3 origin3 = new Vector3(-(size / 2), y * size / 20 - size / 2, 0f);
                Vector3 end3 = new Vector3(size - size / 2, y * size / 20 - size / 2, 0f);
                
                Vector2 origin = ProjectToScreenPos(origin3);
                Vector2 end = ProjectToScreenPos(end3);

                spriteBatch.DrawLine(origin, end, new Color(211, 211, 211, 70));
            }
        }

        void DrawSelectedData(SpriteBatch batch, DrawTimes elapsed)
        {
            if (SelectedNodeList.Count == 1)
            {
                StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);
                var cursor = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);

                FleetDataNode node = SelectedNodeList[0];
                Ship ship = node.Ship;
                if (ship == null)
                {
                    batch.DrawString(Fonts.Arial20Bold, $"({node.ShipName})", cursor, Colors.Cream);
                }
                else
                {
                    string text = ship.VanityName.NotEmpty()
                                ? ship.VanityName : $"{ship.Name} ({ship.ShipData.Role})";
                    batch.DrawString(Fonts.Arial20Bold, text, cursor, Colors.Cream);
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
            }
            else if (SelectedNodeList.Count > 1)
            {
                StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);
                var cursor = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);

                batch.DrawString(Fonts.Arial20Bold, $"Group of {SelectedNodeList.Count} ships selected", cursor, Colors.Cream);

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
            }
            else if (FleetToEdit == -1)
            {
                float transitionOffset = (float) Math.Pow(TransitionPosition, 2);
                Rectangle r = SelectedStuffRect;
                if (ScreenState == ScreenState.TransitionOn)
                {
                    r.Y += (int) (transitionOffset * 256f);
                }

                StuffSelector = new Selector(r, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);
                Vector2 cursor = new Vector2(r.X + 20, r.Y + 10);
                batch.DrawString(Fonts.Arial20Bold, "No Fleet Selected", cursor, Colors.Cream);
                cursor.Y += (Fonts.Arial20Bold.LineSpacing + 2);
                string txt = "You are not currently editing a fleet. Click a hotkey on the left side of the screen to begin creating or editing the corresponding fleet. \n\nWhen you are finished editing, you can save your fleet design to disk for quick access in the future.";
                txt = Fonts.Arial12Bold.ParseText(txt, SelectedStuffRect.Width - 40);
                batch.DrawString(Fonts.Arial12Bold, txt, cursor, Colors.Cream);
            }
            else
            {
                StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);

                Fleet f = Universe.Player.GetFleet(FleetToEdit);
                Vector2 cursor1 = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
                FleetNameEntry.Text = f.Name;
                FleetNameEntry.SetPos(cursor1);
                FleetNameEntry.Draw(batch, elapsed);

                cursor1.Y += (Fonts.Arial20Bold.LineSpacing + 10);
                cursor1 += new Vector2(50f, 30f);
                batch.DrawString(Fonts.Pirulen12, "Fleet Icon", cursor1, Colors.Cream);
                var iconR = new RectF(cursor1.X + 12, cursor1.Y + Fonts.Pirulen12.LineSpacing + 5, 64, 64);
                batch.Draw(f.Icon, iconR, f.Owner.EmpireColor);
                RequisitionForces.Draw(ScreenManager);
                SaveDesign.Draw(ScreenManager);
                LoadDesign.Draw(ScreenManager);
                Priorityselector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
                Priorityselector.Draw(batch, elapsed);
                cursor1 = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
                batch.DrawString(Fonts.Pirulen12, "Fleet Design Overview", cursor1, Colors.Cream);
                cursor1.Y += (Fonts.Pirulen12.LineSpacing + 2);
                string txt0 = Localizer.Token(GameText.AddShipDesignsToThis);
                txt0 = Fonts.Arial12Bold.ParseText(txt0, PrioritiesRect.Width - 40);
                batch.DrawString(Fonts.Arial12Bold, txt0, cursor1, Colors.Cream);
            }
        }
    }
}
