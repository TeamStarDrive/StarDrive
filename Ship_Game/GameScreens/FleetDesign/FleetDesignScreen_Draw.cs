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
        Color NeonGreen = new (0, 255, 0, 70);

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
            ScreenManager.BeginFrameRendering(elapsed, ref View, ref Projection);

            ScreenManager.GraphicsDevice.Clear(Color.Black);
            Universe.DrawStarField(batch);

            batch.Begin();
            {
                DrawGrid(batch);
                DrawSelectedNodeSensorRange(batch);
                DrawHoveredNodes(batch);
                DrawSelectedNodes(batch);
                DrawFleetManagementIndicators(batch);

                if (SelectionBox.W > 0)
                    batch.DrawRectangle(SelectionBox, Color.Green);
            }
            batch.End();

            // render 3D
            ScreenManager.RenderSceneObjects();

            batch.Begin();
            {
                DrawUI(batch, elapsed);
                base.Draw(batch, elapsed); // draw automatic elements on top of everything else
            }
            batch.End();

            ScreenManager.EndFrameRendering();
        }

        void DrawUI(SpriteBatch batch, DrawTimes elapsed)
        {
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, "Fleet Hotkeys", TitlePos, Colors.Cream);

            const int numEntries = 9;
            int k = 9;
            int m = 0;

            foreach (KeyValuePair<int, RectF> rect in FleetsRects)
            {
                if (m == 9)
                    break;

                RectF r = rect.Value;
                float transitionOffset = ((TransitionPosition - 0.5f * k / numEntries) / 0.5f).Clamped(0f, 1f);
                k--;
                if (ScreenState != ScreenState.TransitionOn)
                {
                    r.X += (int)transitionOffset * 512;
                }
                else
                {
                    r.X -= (int)(transitionOffset * 256f);
                    if (Math.Abs(transitionOffset) < .1f)
                    {
                        GameAudio.BlipClick();
                    }
                }

                var sel = new Selector(r, Color.TransparentBlack);
                batch.Draw(ResourceManager.Texture("NewUI/rounded_square"), r, rect.Key != FleetToEdit ? Color.Black : new(0, 0, 255, 80));
                sel.Draw(batch, elapsed);

                Fleet f = Universe.Player.GetFleet(rect.Key);
                if (f.DataNodes.Count > 0)
                {
                    RectF firect = new(rect.Value.X + 6, rect.Value.Y + 6, rect.Value.W - 12, rect.Value.W - 12);
                    batch.Draw(f.Icon, firect, Universe.Player.EmpireColor);
                    if (f.AutoRequisition)
                    {
                        RectF autoReq = new(firect.X + 54, firect.Y + 12, 20, 27);
                        batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq,
                            ApplyCurrentAlphaToColor(Universe.Player.EmpireColor));
                    }
                }

                Vector2 num = new(rect.Value.X + 4, rect.Value.Y + 4);
                batch.DrawString(Fonts.Pirulen12, rect.Key.ToString(), num, Color.Orange);
                num.X += (rect.Value.W + 5);
                batch.DrawString(Fonts.Pirulen12, f.Name, num, rect.Key != FleetToEdit ? Color.Gray : Color.White);
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

                    var r = new Rectangle((int)screenPos.X - (int)screenRadius, (int)screenPos.Y - (int)screenRadius,
                        (int)screenRadius * 2, (int)screenRadius * 2);

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
                    (Vector2 screenPos, float screenRadius) =
                        GetPosAndRadiusOnScreen(node.FleetOffset, ship.GetSO().WorldBoundingSphere.Radius);
                    if (screenRadius < 10f)
                    {
                        screenRadius = 10f;
                    }

                    Rectangle r = new Rectangle((int)screenPos.X - (int)screenRadius, (int)screenPos.Y - (int)screenRadius,
                        (int)screenRadius * 2, (int)screenRadius * 2);

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
                    if (Hovered()) return Color.White;
                    if (node.Goal != null) return Color.Yellow;
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
                if (scale > 1f) scale = 1f;
                if (scale < 0.15f) scale = 0.15f;

                Color color = Universe.Player.EmpireColor;
                batch.Draw(icon, Input.CursorPosition, color, 0f, iconOrigin, scale, SpriteEffects.None, 1f);
                if (secondary != null)
                    batch.Draw(secondary, Input.CursorPosition, color, 0f, iconOrigin, scale, SpriteEffects.None, 1f);
            }

            DrawSelectedData(batch, elapsed);
        }

        void DrawSelectedNodes(SpriteBatch batch)
        {
            foreach (FleetDataNode node in SelectedNodeList)
            {
                (Vector2 screenPos, float screenRadius) = GetNodeScreenPosAndRadius(node);
                foreach (ClickableSquad squad in ClickableSquads)
                    if (squad.Squad.DataNodes.Contains(node))
                        batch.DrawLine(squad.Rect.Center, screenPos, NeonGreen, 2f);

                DrawCircle(screenPos, screenRadius, Color.White, 2f);
            }
        }

        void DrawHoveredNodes(SpriteBatch batch)
        {
            foreach (FleetDataNode node in HoveredNodeList)
            {
                (Vector2 screenPos, float screenRadius) = GetNodeScreenPosAndRadius(node);
                foreach (ClickableSquad squad in ClickableSquads)
                    if (squad.Squad.DataNodes.Contains(node))
                        batch.DrawLine(squad.Rect.Center, screenPos, NeonGreen, 2f);
                DrawCircle(screenPos, screenRadius, new(255, 255, 255, 70), 2f);
            }
        }

        void DrawSelectedNodeSensorRange(SpriteBatch batch)
        {
            if (SelectedNodeList.Count == 1)
            {
                SubTexture nodeTexture = ResourceManager.Texture("UI/node");
                FleetDataNode node = SelectedNodeList[0];

                float radius = (node.Ship?.SensorRange ?? 500000) * OperationalRadius.RelativeValue;
                (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(node.FleetOffset, radius);
                RectF nodeRect = new(screenPos, screenRadius * 2, screenRadius * 2);
                batch.Draw(nodeTexture, nodeRect, NeonGreen, 0f, nodeTexture.CenterF);
            }
        }

        void DrawFleetManagementIndicators(SpriteBatch batch)
        {
            Vector2 pPos = ProjectToScreenPos(Vector3.Zero);
            batch.FillRectangle(new(pPos.X - 3, pPos.Y - 3, 6, 6), new(255, 255, 255, 80));

            float textW = Fonts.Arial12Bold.TextWidth("Fleet Center");
            batch.DrawString(Fonts.Arial12Bold, "Fleet Center", new(pPos.X - textW / 2f, pPos.Y + 5f), new Color(255, 255, 255, 70));

            // draw squad node markers
            float squadTextW = Fonts.Arial10.TextWidth("Squad");
            foreach (ClickableSquad squad in ClickableSquads)
            {
                batch.FillRectangle(RectF.FromCenter(squad.Rect.Center, 4, 4), new(0, 255, 0, 110));
                batch.DrawRectangle(squad.Rect, NeonGreen);
                batch.DrawString(Fonts.Arial10, "Squad", new(squad.Rect.CenterX - squadTextW / 2f, squad.Rect.Bottom + 5f), NeonGreen);
            }
        }

        void DrawGrid(SpriteBatch batch)
        {
            int size = 20000;
            for (int x = 0; x < 21; x++)
            {
                float wx = x * size / 20 - size / 2;
                Vector2 origin = ProjectToScreenPos(new(wx, -(size / 2), 0));
                Vector2 end = ProjectToScreenPos(new(wx, size - size / 2, 0));
                batch.DrawLine(origin, end, new(211, 211, 211, 70));
            }
            for (int y = 0; y < 21; y++)
            {
                float wy = y * size / 20 - size / 2;
                Vector2 origin = ProjectToScreenPos(new(-(size / 2), wy, 0));
                Vector2 end = ProjectToScreenPos(new(size - size / 2, wy, 0));
                batch.DrawLine(origin, end, new(211, 211, 211, 70));
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
                txt = Fonts.Arial12Bold.ParseText(txt, SelectedStuffRect.W - 40);
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
                txt0 = Fonts.Arial12Bold.ParseText(txt0, PrioritiesRect.W - 40);
                batch.DrawString(Fonts.Arial12Bold, txt0, cursor1, Colors.Cream);
            }
        }
    }
}
