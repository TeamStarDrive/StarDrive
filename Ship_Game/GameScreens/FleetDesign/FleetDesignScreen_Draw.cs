using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using SDGraphics;
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

            batch.SafeBegin();
            {
                DrawGrid(batch);
                DrawSelectedNodeSensorRange(batch);
                DrawHoveredNodes(batch);
                DrawSelectedNodes(batch);
                DrawFleetManagementIndicators(batch);

                if (SelectionBox.W > 0)
                    batch.DrawRectangle(SelectionBox, Color.Green);
            }
            batch.SafeEnd();

            // render 3D
            if (SelectedFleet != null)
            {
                foreach (FleetDataNode node in SelectedFleet.DataNodes)
                {
                    if (node.Ship != null)
                    {
                        node.Ship.RelativeFleetOffset = node.RelativeFleetOffset;
                        node.Ship.ShowSceneObjectAt(node.Ship.RelativeFleetOffset, 0);
                    }
                }
            }
            ScreenManager.RenderSceneObjects();

            batch.SafeBegin();
            {
                DrawUI(batch, elapsed);
                base.Draw(batch, elapsed); // draw automatic elements on top of everything else
            }
            batch.SafeEnd();

            ScreenManager.EndFrameRendering();
        }

        void DrawUI(SpriteBatch batch, DrawTimes elapsed)
        {
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, "Fleet Hotkeys", TitlePos, Colors.Cream);

            ShipDesigns.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, "Ship Designs", ShipDesignsTitlePos, Colors.Cream);

            EmpireUI.Draw(batch);

            if (SelectedFleet != null)
            {
                foreach (FleetDataNode node in SelectedFleet.DataNodes)
                    DrawFleetNode(batch, node);

            }

            if (ActiveShipDesign != null)
                DrawActiveShipDesign(batch);

            DrawSelectedData(batch, elapsed);
        }

        void DrawFleetNode(SpriteBatch batch, FleetDataNode node)
        {
            Ship ship = node.Ship;
            // if ship doesn't exist, grab a template instead
            if (ship == null || CamPos.Z <= 15000f)
                if (!ResourceManager.GetShipTemplate(node.ShipName, out ship))
                    return;

            float radius = node.Ship?.Radius ?? ship.Radius;
            (Vector2 screenPos, float screenR) = GetPosAndRadiusOnScreen(node.RelativeFleetOffset, radius);
            if (screenR < 10f) screenR = 10f;
            RectF r = RectF.FromPointRadius(screenPos, screenR*0.5f);

            Color color = GetTacticalIconColor(node);
            DrawIcon(batch, node, ship, r, color);

            if (node.Ship?.Resupplying == true)
            {
                batch.DrawString(Fonts.Arial8Bold, "Resupplying", screenPos + new Vector2(5f, -5f), Color.White);
            }
            else if (node.Goal != null)
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

        Color GetTacticalIconColor(FleetDataNode node)
        {
            if (HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node))
                return Color.White;
            if (node.Goal != null) return Color.Yellow;
            if (node.Ship?.Resupplying == true) return Color.Gray;

            return node.Ship != null ? Color.Green : Color.Red;
        }

        // this is the active ship or ship template that we're trying to place
        // into the fleet
        void DrawActiveShipDesign(SpriteBatch batch)
        {
            float radius = (float)ProjectToScreenSize(ActiveShipDesign.Radius);
            RectF screenR = RectF.FromPointRadius(Input.CursorPosition, radius);
            
            (SubTexture icon, SubTexture secondary) = ActiveShipDesign.TacticalIcon();
            batch.Draw(icon, screenR, Player.EmpireColor);
            if (secondary != null)
                batch.Draw(secondary, screenR, Player.EmpireColor);

            float boundingR = Math.Max(radius*1.5f, 16);
            DrawCircle(Input.CursorPosition, boundingR, Player.EmpireColor);
        }
        
        void DrawIcon(SpriteBatch batch, FleetDataNode node, Ship ship, in RectF r, Color color)
        {
            if (CamPos.Z > 5000f || node.Ship == null || node.Ship?.Resupplying == true)
            {
                (SubTexture icon, SubTexture secondary) = ship.TacticalIcon();
                batch.Draw(icon, r, color);
                if (secondary != null)
                    batch.Draw(secondary, r, color);
            }
        }

        void DrawSelectedNodes(SpriteBatch batch)
        {
            foreach (FleetDataNode node in SelectedNodeList)
            {
                (Vector2 screenPos, float screenR) = GetNodeScreenPosAndRadius(node);
                foreach (ClickableSquad squad in ClickableSquads)
                    if (squad.Squad.DataNodes.Contains(node))
                        batch.DrawLine(squad.Rect.Center, screenPos, NeonGreen, 2f);

                DrawCircle(screenPos, screenR, Color.White, 2f);
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
                (Vector2 screenPos, float screenRadius) = GetPosAndRadiusOnScreen(node.RelativeFleetOffset, radius);
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
                bool isSelected = SelectedSquad == squad.Squad;
                Color squadNode = isSelected ? Color.Yellow : NeonGreen;

                batch.FillRectangle(RectF.FromCenter(squad.Rect.Center, 4, 4), new(0, 255, 0, 110));
                batch.DrawRectangle(squad.Rect, squadNode);
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
            RequisitionForces.Visible = false;
            SaveDesign.Visible = false;
            LoadDesign.Visible = false;
            AutoArrange.Visible = false;

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
                PrioritySelector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
                PrioritySelector.Draw(batch, elapsed);
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
                PrioritySelector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
                PrioritySelector.Draw(batch, elapsed);
                cursor = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
                batch.DrawString(Fonts.Pirulen12, "Group Priorities", cursor, Colors.Cream);
                OperationalRadius.Draw(batch, elapsed);
                SliderSize.Draw(ScreenManager);
            }
            else
            {
                StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(batch, elapsed);

                Fleet f = SelectedFleet;
                if (f == null)
                    return;

                Vector2 cursor1 = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
                FleetNameEntry.Text = f.Name;
                FleetNameEntry.SetPos(cursor1);
                FleetNameEntry.Draw(batch, elapsed);

                cursor1.Y += (Fonts.Arial20Bold.LineSpacing + 10);
                cursor1 += new Vector2(50f, 30f);
                batch.DrawString(Fonts.Pirulen12, "Fleet Icon", cursor1, Colors.Cream);
                var iconR = new RectF(cursor1.X + 12, cursor1.Y + Fonts.Pirulen12.LineSpacing + 5, 64, 64);
                batch.Draw(f.Icon, iconR, f.Owner.EmpireColor);
                PrioritySelector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
                PrioritySelector.Draw(batch, elapsed);
                cursor1 = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
                batch.DrawString(Fonts.Pirulen12, "Fleet Design Overview", cursor1, Colors.Cream);
                cursor1.Y += (Fonts.Pirulen12.LineSpacing + 2);
                string txt0 = Localizer.Token(GameText.AddShipDesignsToThis);
                txt0 = Fonts.Arial12Bold.ParseText(txt0, PrioritiesRect.W - 40);
                batch.DrawString(Fonts.Arial12Bold, txt0, cursor1, Colors.Cream);

                RequisitionForces.Visible = true;
                SaveDesign.Visible = true;
                LoadDesign.Visible = true;
                if (f.Ships.Count > 0 )
                    AutoArrange.Visible = true;
            }
        }
    }
}
