using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class FleetDesignScreen : GameScreen, IListScreen
    {
        public static bool Open;

        public Camera2D Camera;

        public ShipData ActiveHull;

        //private Background bg = new Background();

        private StarField StarField;

        public EmpireUIOverlay EmpireUI;

        private Menu2 TitleBar;

        private Menu2 ShipDesigns;

        private Vector2 TitlePos;

        private Vector2 ShipDesignsTitlePos;

        private Menu1 LeftMenu;

        private Menu1 RightMenu;

        public Fleet SelectedFleet;

        private ScrollList FleetSL;

        private ScrollList ShipSL;

        private CloseButton Close;

        private BlueButton RequisitionForces;

        private BlueButton SaveDesign;

        private BlueButton LoadDesign;

        private Rectangle SelectedStuffRect;

        private Rectangle OperationsRect;

        private Rectangle PrioritiesRect;

        private WeightSlider SliderAssist;

        private WeightSlider SliderVulture;

        private WeightSlider SliderDefend;

        private WeightSlider SliderDps;

        private WeightSlider SliderArmor;

        private WeightSlider SliderShield;

        private readonly Array<ToggleButton> OrdersButtons = new Array<ToggleButton>();

        private FloatSlider OperationalRadius;

        private SizeSlider SliderSize;

        private Submenu SubShips;

        private BatchRemovalCollection<Ship> AvailableShips = new BatchRemovalCollection<Ship>();

        private Vector3 CamPos = new Vector3(0f, 0f, 14000f);

        private readonly Map<int, Rectangle> FleetsRects = new Map<int, Rectangle>();
        
        private readonly Array<ClickableSquad> ClickableSquads = new Array<ClickableSquad>();

        private Vector2 CamVelocity = Vector2.Zero;

        private float DesiredCamHeight = 14000f;

        private Ship ActiveShipDesign;

        public int FleetToEdit = -1;

        private readonly UITextEntry FleetNameEntry = new UITextEntry();

        private Selector StuffSelector;

        private Selector OperationsSelector;

        private Selector Priorityselector;

        private readonly Array<ClickableNode> ClickableNodes = new Array<ClickableNode>();

        private Fleet.Squad SelectedSquad;

        private Fleet.Squad HoveredSquad;

        private Rectangle SelectionBox;

        public static UniverseScreen Screen;

        private readonly Array<FleetDataNode> SelectedNodeList = new Array<FleetDataNode>();

        private readonly Array<FleetDataNode> HoveredNodeList = new Array<FleetDataNode>();


        public FleetDesignScreen(GameScreen parent, EmpireUIOverlay empireUI, Fleet f) : base(parent)
        {
            SelectedFleet = f;
            EmpireUI = empireUI;
            TransitionOnTime = TimeSpan.FromSeconds(0.75);
        }

        public FleetDesignScreen(GameScreen parent, EmpireUIOverlay empireUI, string audioCue ="") : base(parent)
        {
            if (!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);
            SelectedFleet = new Fleet();
            EmpireUI = empireUI;
            TransitionOnTime = TimeSpan.FromSeconds(0.75);
            EmpireUI.empire.UpdateShipsWeCanBuild();
            Open = true;
        }

        private void AdjustCamera()
        {
            CamPos.Z = MathHelper.SmoothStep(CamPos.Z, DesiredCamHeight, 0.2f);
        }

        public void ChangeFleet(int which)
        {
            SelectedNodeList.Clear();
            if (FleetToEdit != -1)
            {
                foreach (var kv in EmpireManager.Player.GetFleetsDict())
                {
                    using (kv.Value.Ships.AcquireReadLock())
                    {
                        foreach (Ship ship in kv.Value.Ships)
                        {
                            ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, -1000000f));
                        }
                    }
                }
            }
            FleetToEdit = which;
            Fleet fleet = EmpireManager.Player.GetFleetsDict()[FleetToEdit];
            Array<FleetDataNode> toRemove = new Array<FleetDataNode>();
            foreach (FleetDataNode node in fleet.DataNodes)
            {
                if ((ResourceManager.ShipsDict.ContainsKey(node.ShipName) || node.Ship!= null) && (node.Ship!= null || EmpireManager.Player.WeCanBuildThis(node.ShipName)))
                    continue;
                toRemove.Add(node);
            }
            var squadsToRemove = new Array<Fleet.Squad>();
            foreach (FleetDataNode node in toRemove)
            {
                fleet.DataNodes.Remove(node);
                foreach (Array<Fleet.Squad> flanks in fleet.AllFlanks)
                {
                    foreach (Fleet.Squad squad in flanks)
                    {
                        if (squad.DataNodes.Contains(node))
                        {
                            squad.DataNodes.Remove(node);
                        }
                        if (squad.DataNodes.Count != 0)
                        {
                            continue;
                        }
                        squadsToRemove.Add(squad);
                    }
                }
            }
            foreach (Array<Fleet.Squad> flanks in fleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in squadsToRemove)
                {
                    if (flanks.Contains(squad))
                        flanks.Remove(squad);
                }
            }
            SelectedFleet = EmpireManager.Player.GetFleetsDict()[which];
            using (SelectedFleet.Ships.AcquireReadLock())
            {
                foreach (Ship ship in SelectedFleet.Ships)
                {
                    ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, 0f));
                    ship.GetSO().Visibility = ObjectVisibility.Rendered;
                }
            }
        }

        protected override void Destroy()
        {
            lock (this)
            {
                StarField?.Dispose(ref StarField);
                SelectedFleet = null;
                AvailableShips?.Dispose(ref AvailableShips);
            }
            base.Destroy();
        }

        public override void Draw(SpriteBatch batch)
        {
            Viewport viewport;
            SubTexture nodeTexture = ResourceManager.Texture("UI/node");
            ScreenManager.BeginFrameRendering(StarDriveGame.Instance.GameTime, ref View, ref Projection);

            ScreenManager.GraphicsDevice.Clear(Color.Black);
            Screen.bg.Draw(Screen, Screen.StarField);
            batch.Begin();
            DrawGrid();
            if (SelectedNodeList.Count == 1)
            {
                viewport = Viewport;
                Vector3 screenSpacePosition = viewport.Project(new Vector3(SelectedNodeList[0].FleetOffset.X
                    , SelectedNodeList[0].FleetOffset.Y, 0f), Projection, View, Matrix.Identity);
                var screenPos = new Vector2(screenSpacePosition.X, screenSpacePosition.Y);
                Vector2 radialPos = SelectedNodeList[0].FleetOffset.PointOnCircle(90f, (SelectedNodeList[0].Ship?.SensorRange ?? 500000) * OperationalRadius.RelativeValue);
                viewport = Viewport;
                Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                float ssRadius = Math.Abs(insetRadialSS.X - screenPos.X);
                Rectangle nodeRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)ssRadius * 2, (int)ssRadius * 2);
                Vector2 origin = new Vector2(nodeTexture.Width / 2f, nodeTexture.Height / 2f);
                batch.Draw(nodeTexture, nodeRect, new Color(0, 255, 0, 75), 0f, origin, SpriteEffects.None, 1f);
            }
            ClickableNodes.Clear();
            foreach (FleetDataNode node in SelectedFleet.DataNodes)
            {
                if (node.Ship== null)
                {
                    float radius = 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointOnCircle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
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
                    ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, 0f));
                    float radius = ship.GetSO().WorldBoundingSphere.Radius;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointOnCircle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
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
                if (node.Ship == null)
                {
                    if (node.Ship != null)
                        continue;

                    float radius = 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointOnCircle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
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
                    Ship ship = node.Ship;
                    float radius = ship.GetSO().WorldBoundingSphere.Radius;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointOnCircle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
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
                if (node.Ship== null)
                {
                    if (node.Ship!= null)
                    {
                        continue;
                    }
                    float radius = 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointOnCircle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
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
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointOnCircle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
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
            TitleBar.Draw(batch);
            batch.DrawString(Fonts.Laserian14, "Fleet Hotkeys", TitlePos, new Color(255, 239, 208));
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
                float transitionOffset = MathHelper.Clamp((TransitionPosition - 0.5f * k / numEntries) / 0.5f, 0f, 1f);
                k--;
                if (ScreenState != ScreenState.TransitionOn)
                {
                    r.X = r.X + (int)transitionOffset * 512;
                }
                else
                {
                    r.X = r.X - (int)(transitionOffset * 256f);
                    if (Math.Abs(transitionOffset) < .1f)
                    {
                        GameAudio.BlipClick();
                    }
                }
                Selector sel = new Selector(r, Color.TransparentBlack);
                batch.Draw(ResourceManager.Texture("NewUI/rounded_square"), r,
                    rect.Key != FleetToEdit ? Color.Black : new Color(0, 0, 255, 80));
                sel.Draw(batch);
                Fleet f = EmpireManager.Player.GetFleetsDict()[rect.Key];
                if (f.DataNodes.Count > 0)
                {
                    var firect = new Rectangle(rect.Value.X + 6, rect.Value.Y + 6, rect.Value.Width - 12, rect.Value.Width - 12);
                    batch.Draw(ResourceManager.Texture(string.Concat("FleetIcons/", f.FleetIconIndex.ToString())), firect, EmpireManager.Player.EmpireColor);
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
                ShipDesigns.Draw(batch);
                batch.DrawString(Fonts.Laserian14, "Ship Designs", ShipDesignsTitlePos, new Color(255, 239, 208));
                batch.FillRectangle(SubShips.Menu, new Color(0, 0, 0, 130));
                SubShips.Draw(batch);
                ShipSL.Draw(batch);
                var bCursor = new Vector2(RightMenu.Menu.X + 5, RightMenu.Menu.Y + 25);
                foreach (ScrollList.Entry e in ShipSL.VisibleExpandedEntries)
                {
                    bCursor.Y = e.Y;
                    if (e.item is ModuleHeader header)
                    {
                        header.DrawWidth(ScreenManager, bCursor, 265);
                    }
                    else if (e.Hovered)
                    {
                        var ship = (Ship)e.item;
                        bCursor.Y = e.Y;
                        batch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                        var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                        batch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                        tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                        batch.DrawString(Fonts.Arial8Bold, ship.shipData.GetRole(), tCursor, Color.Orange);
                        e.DrawPlusEdit(batch);
                    }
                    else
                    {
                        var ship = (Ship)e.item;
                        batch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                        var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                        batch.DrawString(Fonts.Arial12Bold, (!string.IsNullOrEmpty(ship.VanityName) ? ship.VanityName : ship.Name), tCursor, Color.White);
                        tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                        if (SubShips.Tabs[0].Selected)
                        {
                            batch.DrawString(Fonts.Arial12Bold, ship.shipData.GetRole(), tCursor, Color.Orange);
                        }
                        else if (ship.System== null)
                        {
                            batch.DrawString(Fonts.Arial12Bold, "Deep Space", tCursor, Color.Orange);
                        }
                        else
                        {
                            batch.DrawString(Fonts.Arial12Bold, $"{ship.System.Name} system", tCursor, Color.Orange);
                        }
                        e.DrawPlusEdit(batch);
                    }
                }
            }
            EmpireUI.Draw(batch);
            foreach (FleetDataNode node in SelectedFleet.DataNodes)
            {
                if (node.Ship == null || CamPos.Z <= 15000f)
                {
                    if (node.Ship != null || node.ShipName == "Troop Shuttle")
                    {
                        continue;
                    }
                    Ship ship = ResourceManager.ShipsDict[node.ShipName];
                    float radius = 150f;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = node.FleetOffset.PointOnCircle(90f, radius);
                    viewport = Viewport;
                    Vector3 insetRadialPos =
                        viewport.Project(new Vector3(radialPos, 0f), Projection, View, Matrix.Identity);
                    Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos);
                    Rectangle r = new Rectangle((int) pPos.X - (int) radius, (int) pPos.Y - (int) radius,
                        (int) radius * 2, (int) radius * 2);
                    if (node.GoalGUID == Guid.Empty)
                    {
                        batch.Draw(ship.GetTacticalIcon(), r,
                            (HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node)
                                ? Color.White
                                : Color.Red));
                    }
                    else
                    {
                        batch.Draw(ship.GetTacticalIcon(), r,
                            (HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node)
                                ? Color.White
                                : Color.Yellow));
                        string buildingat = "";
                        foreach (Goal g in SelectedFleet.Owner.GetEmpireAI().Goals)
                        {
                            if (!(g.guid == node.GoalGUID) || g.PlanetBuildingAt == null)
                            {
                                continue;
                            }
                            buildingat = g.PlanetBuildingAt.Name;
                        }
                        batch.DrawString(Fonts.Arial8Bold,
                            (!string.IsNullOrEmpty(buildingat)
                                ? string.Concat("Building at:\n", buildingat)
                                : "Need spaceport"), pPos + new Vector2(5f, -5f), Color.White);
                    }
                }
                else
                {
                    Ship ship = node.Ship;
                    float radius = ship.GetSO().WorldBoundingSphere.Radius;
                    viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), Projection, View,
                        Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    Vector2 radialPos = ship.RelativeFleetOffset.PointOnCircle(90f, radius);
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
                    batch.Draw(ship.GetTacticalIcon(), r,
                        (HoveredNodeList.Contains(node) || SelectedNodeList.Contains(node)
                            ? Color.White
                            : Color.Green));
                }
            }
            if (ActiveShipDesign != null)
            {
                float scale;
                Vector2 iconOrigin;
                SubTexture item;
                Ship ship = ActiveShipDesign;
                {
                    scale = ship.SurfaceArea / (float) (30 + ResourceManager.Texture("TacticalIcons/symbol_fighter").Width);
                    iconOrigin = new Vector2(ResourceManager.Texture("TacticalIcons/symbol_fighter").Width / 2f, ResourceManager.Texture("TacticalIcons/symbol_fighter").Width / 2f);
                    scale = scale * 4000f / CamPos.Z;
                    if (scale > 1f)
                        scale = 1f;
                    if (scale < 0.15f)
                        scale = 0.15f;
                    item = ship.GetTacticalIcon();
                }
                float single = Mouse.GetState().X;
                MouseState state = Mouse.GetState();
                batch.Draw(item, new Vector2(single, state.Y), EmpireManager.Player.EmpireColor, 0f, iconOrigin, scale, SpriteEffects.None, 1f);
            }
            DrawSelectedData(StarDriveGame.Instance.GameTime);
            Close.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();

            ScreenManager.EndFrameRendering();
        }

        private void DrawFleetManagementIndicators()
        {
            Viewport viewport = Viewport;
            Vector3 pScreenSpace = viewport.Project(new Vector3(0f, 0f, 0f), Projection, View, Matrix.Identity);
            Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);

            var spriteBatch = ScreenManager.SpriteBatch;
            spriteBatch.FillRectangle(new Rectangle((int)pPos.X - 3, (int)pPos.Y - 3, 6, 6), new Color(255, 255, 255, 80));
            spriteBatch.DrawString(Fonts.Arial12Bold, "Fleet Center", new Vector2(pPos.X - Fonts.Arial12Bold.MeasureString("Fleet Center").X / 2f, pPos.Y + 5f), new Color(255, 255, 255, 70));
            foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in flank)
                {
                    Viewport viewport1 = Viewport;
                    pScreenSpace = viewport1.Project(new Vector3(squad.Offset, 0f), Projection, View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    spriteBatch.FillRectangle(new Rectangle((int)pPos.X - 2, (int)pPos.Y - 2, 4, 4), new Color(0, 255, 0, 110));
                    spriteBatch.DrawString(Fonts.Arial8Bold, "Squad", new Vector2(pPos.X - Fonts.Arial8Bold.MeasureString("Squad").X / 2f, pPos.Y + 5f), new Color(0, 255, 0, 70));
                }
            }
        }

        private void DrawGrid()
        {
            var spriteBatch = ScreenManager.SpriteBatch;

            int size = 20000;
            for (int x = 0; x < 21; x++)
            {
                Vector3 origin3            = new Vector3(x * size / 20 - size / 2, -(size / 2), 0f);
                Viewport viewport         = Viewport;
                Vector3 originScreenSpace = viewport.Project(origin3, Projection, View, Matrix.Identity);
                Vector3 end3               = new Vector3(x * size / 20 - size / 2, size - size / 2, 0f);
                Viewport viewport1        = Viewport;
                Vector3 endScreenSpace    = viewport1.Project(end3, Projection, View, Matrix.Identity);
                Vector2 origin            = new Vector2(originScreenSpace.X, originScreenSpace.Y);
                Vector2 end               = new Vector2(endScreenSpace.X, endScreenSpace.Y);
                spriteBatch.DrawLine(origin, end, new Color(211, 211, 211, 70));
            }
            for (int y = 0; y < 21; y++)
            {
                Vector3 origin3            = new Vector3(-(size / 2), y * size / 20 - size / 2, 0f);
                Viewport viewport2        = Viewport;
                Vector3 originScreenSpace = viewport2.Project(origin3, Projection, View, Matrix.Identity);
                Vector3 end3               = new Vector3(size - size / 2, y * size / 20 - size / 2, 0f);
                Viewport viewport3        = Viewport;
                Vector3 endScreenSpace    = viewport3.Project(end3, Projection, View, Matrix.Identity);
                Vector2 origin            = new Vector2(originScreenSpace.X, originScreenSpace.Y);
                Vector2 end               = new Vector2(endScreenSpace.X, endScreenSpace.Y);
                spriteBatch.DrawLine(origin, end, new Color(211, 211, 211, 70));
            }
        }

        private void DrawSelectedData(GameTime gameTime)
        {
            var spriteBatch = ScreenManager.SpriteBatch;
            if (SelectedNodeList.Count == 1)
            {
                StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(spriteBatch);
                Vector2 cursor = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
                if (SelectedNodeList[0].Ship== null)
                {
                    spriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("(", SelectedNodeList[0].ShipName, ")"), cursor, new Color(255, 239, 208));
                }
                else
                {
                    spriteBatch.DrawString(Fonts.Arial20Bold, (!string.IsNullOrEmpty(SelectedNodeList[0].Ship.VanityName) ? SelectedNodeList[0].Ship.VanityName : string.Concat(SelectedNodeList[0].Ship.Name, " (", SelectedNodeList[0].Ship.shipData.Role, ")")), cursor, new Color(255, 239, 208));
                }
                cursor.Y = OperationsRect.Y + 10;
                spriteBatch.DrawString(Fonts.Pirulen12, "Movement Orders", cursor, new Color(255, 239, 208));
                foreach (ToggleButton button in OrdersButtons)
                {
                    button.Draw(ScreenManager);
                }
                OperationsSelector = new Selector(OperationsRect, new Color(0, 0, 0, 180));
                OperationsSelector.Draw(spriteBatch);
                cursor = new Vector2(OperationsRect.X + 20, OperationsRect.Y + 10);
                spriteBatch.DrawString(Fonts.Pirulen12, "Target Selection", cursor, new Color(255, 239, 208));
                SliderArmor.Draw(ScreenManager);
                SliderAssist.Draw(ScreenManager);
                SliderDefend.Draw(ScreenManager);
                SliderDps.Draw(ScreenManager);
                SliderShield.Draw(ScreenManager);
                SliderVulture.Draw(ScreenManager);
                Priorityselector = new Selector( PrioritiesRect, new Color(0, 0, 0, 180));
                Priorityselector.Draw(spriteBatch);
                cursor = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
                spriteBatch.DrawString(Fonts.Pirulen12, "Priorities", cursor, new Color(255, 239, 208));
                OperationalRadius.Draw(spriteBatch);
                SliderSize.Draw(ScreenManager);
                return;
            }
            if (SelectedNodeList.Count > 1)
            {
                StuffSelector = new Selector( SelectedStuffRect, new Color(0, 0, 0, 180));
                StuffSelector.Draw(spriteBatch);
                Vector2 cursor = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
                if (SelectedNodeList[0].Ship== null)
                {
                    SpriteFont arial20Bold = Fonts.Arial20Bold;
                    int count = SelectedNodeList.Count;
                    spriteBatch.DrawString(arial20Bold, string.Concat("Group of ", count.ToString(), " ships selected"), cursor, new Color(255, 239, 208));
                }
                else
                {
                    SpriteFont spriteFont = Fonts.Arial20Bold;
                    int num = SelectedNodeList.Count;
                    spriteBatch.DrawString(spriteFont, string.Concat("Group of ", num.ToString(), " ships selected"), cursor, new Color(255, 239, 208));
                }
                cursor.Y = OperationsRect.Y + 10;
                spriteBatch.DrawString(Fonts.Pirulen12, "Group Movement Orders", cursor, new Color(255, 239, 208));
                foreach (ToggleButton button in OrdersButtons)
                {
                    button.Draw(ScreenManager);
                }
                OperationsSelector = new Selector(OperationsRect, new Color(0, 0, 0, 180));
                OperationsSelector.Draw(spriteBatch);
                cursor = new Vector2(OperationsRect.X + 20, OperationsRect.Y + 10);
                spriteBatch.DrawString(Fonts.Pirulen12, "Group Target Selection", cursor, new Color(255, 239, 208));
                SliderArmor.Draw(ScreenManager);
                SliderAssist.Draw(ScreenManager);
                SliderDefend.Draw(ScreenManager);
                SliderDps.Draw(ScreenManager);
                SliderShield.Draw(ScreenManager);
                SliderVulture.Draw(ScreenManager);
                Priorityselector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
                Priorityselector.Draw(spriteBatch);
                cursor = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
                spriteBatch.DrawString(Fonts.Pirulen12, "Group Priorities", cursor, new Color(255, 239, 208));
                OperationalRadius.Draw(spriteBatch);
                SliderSize.Draw(ScreenManager);
                return;
            }
            if (FleetToEdit == -1)
            {
                float transitionOffset = (float)Math.Pow(TransitionPosition, 2);
                Rectangle r = SelectedStuffRect;
                if (ScreenState == ScreenState.TransitionOn)
                {
                    r.Y = r.Y + (int)(transitionOffset * 256f);
                }
                StuffSelector = new Selector(r, new Color(0, 0, 0, 180));
                StuffSelector.Draw(spriteBatch);
                Vector2 cursor = new Vector2(r.X + 20, r.Y + 10);
                spriteBatch.DrawString(Fonts.Arial20Bold, "No Fleet Selected", cursor, new Color(255, 239, 208));
                cursor.Y = cursor.Y + (Fonts.Arial20Bold.LineSpacing + 2);
                string txt = "You are not currently editing a fleet. Click a hotkey on the left side of the screen to begin creating or editing the corresponding fleet. \n\nWhen you are finished editing, you can save your fleet design to disk for quick access in the future.";
                txt = Fonts.Arial12Bold.ParseText(txt, SelectedStuffRect.Width - 40);
                spriteBatch.DrawString(Fonts.Arial12Bold, txt, cursor, new Color(255, 239, 208));
                return;
            }
            StuffSelector = new Selector(SelectedStuffRect, new Color(0, 0, 0, 180));
            StuffSelector.Draw(spriteBatch);
            Fleet f = EmpireManager.Player.GetFleetsDict()[FleetToEdit];
            Vector2 cursor1 = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 10);
            FleetNameEntry.Text = f.Name;
            FleetNameEntry.ClickableArea = new Rectangle((int)cursor1.X, (int)cursor1.Y, (int)Fonts.Arial20Bold.MeasureString(f.Name).X, Fonts.Arial20Bold.LineSpacing);
            FleetNameEntry.Draw(Fonts.Arial20Bold, spriteBatch, cursor1, gameTime, (FleetNameEntry.Hover ? Color.Orange : new Color(255, 239, 208)));
            cursor1.Y = cursor1.Y + (Fonts.Arial20Bold.LineSpacing + 10);
            cursor1 = cursor1 + new Vector2(50f, 30f);
            spriteBatch.DrawString(Fonts.Pirulen12, "Fleet Icon", cursor1, new Color(255, 239, 208));
            Rectangle ficonrect = new Rectangle((int)cursor1.X + 12, (int)cursor1.Y + Fonts.Pirulen12.LineSpacing + 5, 64, 64);
            spriteBatch.Draw(ResourceManager.Texture(string.Concat("FleetIcons/", f.FleetIconIndex.ToString())), ficonrect, f.Owner.EmpireColor);
            RequisitionForces.Draw(ScreenManager);
            SaveDesign.Draw(ScreenManager);
            LoadDesign.Draw(ScreenManager);
            Priorityselector = new Selector(PrioritiesRect, new Color(0, 0, 0, 180));
            Priorityselector.Draw(spriteBatch);
            cursor1 = new Vector2(PrioritiesRect.X + 20, PrioritiesRect.Y + 10);
            spriteBatch.DrawString(Fonts.Pirulen12, "Fleet Design Overview", cursor1, new Color(255, 239, 208));
            cursor1.Y = cursor1.Y + (Fonts.Pirulen12.LineSpacing + 2);
            string txt0 = Localizer.Token(4043);
            txt0 = Fonts.Arial12Bold.ParseText(txt0, PrioritiesRect.Width - 40);
            spriteBatch.DrawString(Fonts.Arial12Bold, txt0, cursor1, new Color(255, 239, 208));
        }

        public override void ExitScreen()
        {
            if (!StarDriveGame.Instance.IsExiting) // RedFox: if game is exiting, we don't need to restore universe screen
            {
                Empire.Universe.AssignLightRig("example/NewGamelight_rig");
                Empire.Universe.RecomputeFleetButtons(true);
            }
            StarField.UnloadContent();
            base.ExitScreen();
        }

    
        private Vector2 GetWorldSpaceFromScreenSpace(Vector2 screenSpace)
        {
            Viewport viewport = Viewport;
            Vector3 nearPoint = viewport.Unproject(new Vector3(screenSpace, 0f), Projection, View, Matrix.Identity);
            Viewport viewport1 = Viewport;
            Vector3 farPoint = viewport1.Unproject(new Vector3(screenSpace, 1f), Projection, View, Matrix.Identity);
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);
            float k = -pickRay.Position.Z / pickRay.Direction.Z;
            Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
            return new Vector2(pickedPosition.X, pickedPosition.Y);
        }

        private void HandleEdgeDetection(InputState input)
        {
            EmpireUI.HandleInput(input, this);
            if (FleetNameEntry.HandlingInput)
            {
                return;
            }
            Vector2 mousePos = new Vector2(input.MouseCurr.X, input.MouseCurr.Y);
            PresentationParameters pp = ScreenManager.GraphicsDevice.PresentationParameters;
            Vector2 upperLeftWorldSpace = GetWorldSpaceFromScreenSpace(new Vector2(0f, 0f));
            Vector2 lowerRightWorldSpace = GetWorldSpaceFromScreenSpace(new Vector2(pp.BackBufferWidth, pp.BackBufferHeight));
            float xDist = lowerRightWorldSpace.X - upperLeftWorldSpace.X;
            if ((int)mousePos.X == 0 || input.KeysCurr.IsKeyDown(Keys.Left) || input.KeysCurr.IsKeyDown(Keys.A))
            {
                CamPos.X = CamPos.X - 0.008f * xDist;
            }
            if ((int)mousePos.X == pp.BackBufferWidth - 1 || input.KeysCurr.IsKeyDown(Keys.Right) || input.KeysCurr.IsKeyDown(Keys.D))
            {
                CamPos.X = CamPos.X + 0.008f * xDist;
            }
            if ((int)mousePos.Y == 0 || input.KeysCurr.IsKeyDown(Keys.Up) || input.KeysCurr.IsKeyDown(Keys.W))
            {
                CamPos.Y = CamPos.Y - 0.008f * xDist;
            }
            if ((int)mousePos.Y == pp.BackBufferHeight - 1 || input.KeysCurr.IsKeyDown(Keys.Down) || input.KeysCurr.IsKeyDown(Keys.S))
            {
                CamPos.Y = CamPos.Y + 0.008f * xDist;
            }
        }
        private void InputSelectFleet(int whichFleet, bool keyPressed)
        {
            if (!keyPressed) return;
            GameAudio.AffirmativeClick();
            ChangeFleet(whichFleet);
        }

        private void InputCombatStateButtons()
        {
            foreach (ToggleButton button in OrdersButtons)
            {
                if (!button.Rect.HitTest(Input.CursorPosition))
                {
                    button.Hover = false;
                }
                else
                {
                    button.Hover = true;
                    if (!Input.LeftMouseClick)
                    {
                        continue;
                    }
                    button.Active = false;
                    foreach (ToggleButton b in OrdersButtons)
                    {
                        b.Active = false;
                    }
                    button.Active = true;
                    foreach (FleetDataNode node in SelectedNodeList)
                    {
                        string action1 = button.Action;
                        string str1 = action1;
                        if (action1 != null)
                        {
                            switch (str1)
                            {
                                case "attack":
                                    node.CombatState = CombatState.AttackRuns;
                                    break;
                                case "arty":
                                    node.CombatState = CombatState.Artillery;
                                    break;
                                case "hold":
                                    node.CombatState = CombatState.HoldPosition;
                                    break;
                                case "orbit_left":
                                    node.CombatState = CombatState.OrbitLeft;
                                    break;
                                case "broadside_left":
                                    node.CombatState = CombatState.BroadsideLeft;
                                    break;
                                case "orbit_right":
                                    node.CombatState = CombatState.OrbitRight;
                                    break;
                                case "broadside_right":
                                    node.CombatState = CombatState.BroadsideRight;
                                    break;
                                case "evade":
                                    node.CombatState = CombatState.Evade;
                                    break;
                                case "short":
                                    node.CombatState = CombatState.ShortRange;
                                    break;
                            }
                        }
                        if (node.Ship == null)
                        {
                            continue;
                        }
                        node.Ship.AI.CombatState = node.CombatState;
                    }                    
                    if (SelectedNodeList[0].Ship == null)
                    {
                        continue;
                    }
                    SelectedNodeList[0].Ship.AI.CombatState = SelectedNodeList[0].CombatState;
                    button.Active = true;
                    GameAudio.EchoAffirmative();
                    break;
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (Close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }
            if (Input.FleetExitScreen && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            if (SelectedNodeList.Count != 1 && FleetToEdit != -1)
            {
                if (!FleetNameEntry.ClickableArea.HitTest(input.CursorPosition))
                {
                    FleetNameEntry.Hover = false;
                }
                else
                {
                    FleetNameEntry.Hover = true;                    
                    if (Input.LeftMouseClick)
                    {
                        FleetNameEntry.HandlingInput = true;
                        return true;
                    }
                }
            }
            if (!FleetNameEntry.HandlingInput)
                GlobalStats.TakingInput = false;
            else
            {
                GlobalStats.TakingInput = true;
                FleetNameEntry.HandleTextInput(ref EmpireManager.Player.GetFleetsDict()[FleetToEdit].Name, input);
            }
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
                if (!rect.Value.HitTest(input.CursorPosition) || !input.LeftMouseClick)
                {
                    continue;
                }
                FleetToEdit = rect.Key;
                InputSelectFleet(FleetToEdit, true);                                                
            }
            if (FleetToEdit != -1)
            {
                SubShips.HandleInput(input);
                if (ShipSL.HandleInput(input))
                {
                    return true;
                }
            }
            if(SelectedNodeList.Count >0 && Input.RightMouseClick)
            {
                SelectedNodeList.Clear();
            }
            if (HandleSingleNodeSelection(input, input.CursorPosition)) return false;
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
                if (SelectedStuffRect.HitTest(input.CursorPosition))
                {                    
                    foreach (ToggleButton button in OrdersButtons)
                    {
                        if (!button.Rect.HitTest(input.CursorPosition))
                        {
                            button.Hover = false;
                        }
                        else
                        {
                            button.Hover = true;
                            if (Input.LeftMouseHeldUp)
                            {
                                continue;
                            }
                            foreach (ToggleButton b in OrdersButtons)
                            {
                                b.Active = false;
                            }
                            GameAudio.EchoAffirmative();
                            button.Active = true;
                            foreach (FleetDataNode node in SelectedNodeList)
                            {
                                string action1 = button.Action;
                                string str1 = action1;
                                if (action1 != null)
                                {
                                    switch (str1) {
                                        case "attack":
                                            node.CombatState = CombatState.AttackRuns;
                                            break;
                                        case "arty":
                                            node.CombatState = CombatState.Artillery;
                                            break;
                                        case "hold":
                                            node.CombatState = CombatState.HoldPosition;
                                            break;
                                        case "orbit_left":
                                            node.CombatState = CombatState.OrbitLeft;
                                            break;
                                        case "broadside_left":
                                            node.CombatState = CombatState.BroadsideLeft;
                                            break;
                                        case "orbit_right":
                                            node.CombatState = CombatState.OrbitRight;
                                            break;
                                        case "broadside_right":
                                            node.CombatState = CombatState.BroadsideRight;
                                            break;
                                        case "evade":
                                            node.CombatState = CombatState.Evade;
                                            break;
                                        case "short":
                                            node.CombatState = CombatState.ShortRange;
                                            break;
                                    }
                                }
                                if (node.Ship == null)
                                {
                                    continue;
                                }
                                node.Ship.AI.CombatState = node.CombatState;
                            }
                        }
                    }
                    return false;
                }
                
            }
            else if (FleetToEdit != -1 && SelectedNodeList.Count == 0 && SelectedStuffRect.HitTest(input.CursorPosition))
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
                if (input.LeftMouseClick)
                {
                    Viewport viewport = Viewport;
                    Vector3 nearPoint = viewport.Unproject(new Vector3(input.CursorPosition, 0f), Projection, View, Matrix.Identity);
                    Viewport viewport1 = Viewport;
                    Vector3 farPoint = viewport1.Unproject(new Vector3(input.CursorPosition, 1f), Projection, View, Matrix.Identity);
                    Vector3 direction = farPoint - nearPoint;
                    direction.Normalize();
                    Ray pickRay = new Ray(nearPoint, direction);
                    float k = -pickRay.Position.Z / pickRay.Direction.Z;
                    Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
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
                            SelectedFleet.Position = ActiveShipDesign.Position;
                        }
                        node.Ship = ActiveShipDesign;
                        node.Ship.GetSO().World = Matrix.CreateTranslation(new Vector3(node.FleetOffset, 0f));
                        node.Ship.RelativeFleetOffset = node.FleetOffset;
                        AvailableShips.Remove(ActiveShipDesign);                        
                        SelectedFleet.AddShip(node.Ship);                
                        
                        if (SubShips.Tabs[1].Selected)
                        {
                            ShipSL.RemoveFirstIf<Ship>(ship => ship == ActiveShipDesign);
                        }
                        ActiveShipDesign = null;
                    }
                    if (!input.KeysCurr.IsKeyDown(Keys.LeftShift) )
                    {
                        ActiveShipDesign = null;
                    }
                }
                if (input.MouseCurr.RightButton == ButtonState.Pressed && input.MousePrev.RightButton == ButtonState.Released)
                {
                    ActiveShipDesign = null;
                }
            }
            if (FleetToEdit != -1)
            {
                ScrollList.Entry[] items = ShipSL.AllExpandedEntries.ToArray();
                foreach (ScrollList.Entry e in items)
                {
                    if (e.item is ModuleHeader header)
                    {
                        if (header.HandleInput(input, e))
                            break;
                    }
                    else
                    {
                        if (!e.WasClicked(input))
                            continue;

                        ActiveShipDesign = e.item as Ship;
                        SelectedNodeList.Clear();
                        SelectedSquad = null;
                    }
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
                if (Input.StartRighthold.OutsideRadius(Input.CursorPosition, 10f))
                {
                    CamVelocity = Input.CursorPosition.DirectionToTarget(Input.StartRighthold);
                    CamVelocity = Vector2.Normalize(CamVelocity) *
                              Vector2.Distance(Input.StartRighthold, Input.CursorPosition);
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
                                if (!squad.DataNodes.Contains(node))
                                {
                                    continue;
                                }
                                squad.DataNodes.QueuePendingRemoval(node);
                                if (node.Ship == null)
                                {
                                    continue;
                                }
                                squad.Ships.QueuePendingRemoval(node.Ship);
                            }
                            squad.DataNodes.ApplyPendingRemovals();
                            squad.Ships.ApplyPendingRemovals();
                        }
                    }
                    foreach (FleetDataNode node in SelectedNodeList)
                    {
                        SelectedFleet.DataNodes.Remove(node);
                        if (node.Ship == null)
                        {
                            continue;
                        }
                        node.Ship.GetSO().World = Matrix.CreateTranslation(new Vector3(node.Ship.RelativeFleetOffset, -500000f));
                        SelectedFleet.Ships.Remove(node.Ship);
                        node.Ship.fleet?.RemoveShip(node.Ship);
                    }
                    SelectedNodeList.Clear();
                    ResetLists();
                }
            }
            if (input.Escaped)
            {
                Open = false;
                ExitScreen();
                return true;
            }
            return false;
        }

        private void ApplySliderValuesToNode(InputState input, WeightSlider slider, ref float currentValue)
        {            

            if (!slider.rect.HitTest(input.CursorPosition))
                slider.SetAmount(currentValue);
            currentValue = slider.HandleInput(input);
            
        }

        private bool HandleSingleNodeSelection(InputState input, Vector2 mousePos)
        {            
            if (SelectedNodeList.Count != 1) return false;
            bool setReturn = false;
            setReturn |= SliderShield.HandleInput(input, ref SelectedNodeList[0].AttackShieldedWeight);
            setReturn |= SliderDps.HandleInput(input, ref SelectedNodeList[0].DPSWeight);
            setReturn |= SliderVulture.HandleInput(input , ref SelectedNodeList[0].VultureWeight);
            setReturn |= SliderArmor.HandleInput(input , ref SelectedNodeList[0].ArmoredWeight);
            setReturn |= SliderDefend.HandleInput(input, ref SelectedNodeList[0].DefenderWeight);
            setReturn |= SliderAssist.HandleInput(input, ref SelectedNodeList[0].AssistWeight);
            setReturn |= SliderSize.HandleInput(input, ref SelectedNodeList[0].SizeWeight);
            setReturn |= OperationalRadius.HandleInput(input, ref SelectedNodeList[0].OrdersRadius, SelectedNodeList[0].Ship?.SensorRange ?? 500000);
            if (setReturn) return false;
            if (OperationsRect.HitTest(mousePos))
            {
                return true;
            }

            if (PrioritiesRect.HitTest(mousePos))
            {
                //OperationalRadius.HandleInput(input);
                //SelectedNodeList[0].OrdersRadius = OperationalRadius.RelativeValue;
                return true;
            }

            if (!SelectedStuffRect.HitTest(mousePos)) return false;

            InputCombatStateButtons();
            return true;

        }

        private void HandleSelectionBox(InputState input)
        {
            if (LeftMenu.Menu.HitTest(input.CursorPosition) || RightMenu.Menu.HitTest(input.CursorPosition))
            {
                SelectionBox = new Rectangle(0, 0, -1, -1);
                return;
            }
            Vector2 mousePosition = new Vector2(input.MouseCurr.X, input.MouseCurr.Y);
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
            bool hitsomething = false;
            if (Input.LeftMouseClick)
            {
                SelectedSquad = null;
                foreach (ClickableNode node in ClickableNodes)
                {
                    if (input.CursorPosition.OutsideRadius(node.ScreenPos, node.Radius))
                    {
                        continue;
                    }
                    if (SelectedNodeList.Count > 0 && !Input.IsShiftKeyDown)
                    {
                        SelectedNodeList.Clear();
                    }
                    GameAudio.FleetClicked();
                    hitsomething = true;
                    if (!SelectedNodeList.Contains(node.NodeToClick))
                    {
                        SelectedNodeList.Add(node.NodeToClick);
                    }
                    foreach (ToggleButton button in OrdersButtons)
                    {
                        button.Active = false;
                        CombatState toset = CombatState.Artillery;
                        string action = button.Action;
                        string str = action;
                        if (action != null)
                        {
                            if (str == "attack")
                            {
                                toset = CombatState.AttackRuns;
                            }
                            else if (str == "arty")
                            {
                                toset = CombatState.Artillery;
                            }
                            else if (str == "hold")
                            {
                                toset = CombatState.HoldPosition;
                            }
                            else if (str == "orbit_left")
                            {
                                toset = CombatState.OrbitLeft;
                            }
                            else if (str == "broadside_left")
                            {
                                toset = CombatState.BroadsideLeft;
                            }
                            else if (str == "orbit_right")
                            {
                                toset = CombatState.OrbitRight;
                            }
                            else if (str == "broadside_right")
                            {
                                toset = CombatState.BroadsideRight;
                            }
                            else if (str == "evade")
                            {
                                toset = CombatState.Evade;
                            }
                            else if (str == "short")
                            {
                                toset = CombatState.ShortRange;
                            }
                        }
                        if (node.NodeToClick.CombatState != toset)
                        {
                            continue;
                        }
                        button.Active = true;
                    }
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
                foreach (ClickableSquad squad in ClickableSquads)
                {
                    if (Vector2.Distance(input.CursorPosition, squad.ScreenPos) > 4f)
                    {
                        continue;
                    }
                    SelectedSquad = squad.Squad;
                    if (SelectedNodeList.Count > 0 && !input.KeysCurr.IsKeyDown(Keys.LeftShift))
                    {
                        SelectedNodeList.Clear();
                    }
                    hitsomething = true;
                    GameAudio.FleetClicked();
                    SelectedNodeList.Clear();
                    foreach (FleetDataNode node in SelectedSquad.DataNodes)
                    {
                        SelectedNodeList.Add(node);
                    }
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
                if (!hitsomething)
                {
                    SelectedSquad = null;
                    SelectedNodeList.Clear();
                }
            }
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
                    SelectionBox = new Rectangle(input.MouseCurr.X, input.MouseCurr.Y, 0, 0);                    
                }
                if (Input.LeftMouseWasHeld)
                {
                    if (input.MouseCurr.X < SelectionBox.X)
                    {
                        SelectionBox.X = input.MouseCurr.X;
                    }
                    if (input.MouseCurr.Y < SelectionBox.Y)
                    {
                        SelectionBox.Y = input.MouseCurr.Y;
                    }
                    SelectionBox.Width = Math.Abs(SelectionBox.Width);
                    SelectionBox.Height = Math.Abs(SelectionBox.Height);
                    foreach (ClickableNode node in ClickableNodes)
                    {
                        if (!SelectionBox.Contains(new Point((int)node.ScreenPos.X, (int)node.ScreenPos.Y)))
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
                    if (input.MouseCurr.X < SelectionBox.X)
                    {
                        SelectionBox.X = input.MouseCurr.X;
                    }
                    if (input.MouseCurr.Y < SelectionBox.Y)
                    {
                        SelectionBox.Y = input.MouseCurr.Y;
                    }
                    SelectionBox.Width = Math.Abs(SelectionBox.Width);
                    SelectionBox.Height = Math.Abs(SelectionBox.Height);
                    foreach (ClickableNode node in ClickableNodes)
                    {
                        if (!SelectionBox.Contains(new Point((int)node.ScreenPos.X, (int)node.ScreenPos.Y)))
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
                Vector3 nearPoint = viewport2.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0f), Projection, View, Matrix.Identity);
                Viewport viewport3 = Viewport;
                Vector3 farPoint = viewport3.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 1f), Projection, View, Matrix.Identity);
                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();
                Ray pickRay = new Ray(nearPoint, direction);
                float k = -pickRay.Position.Z / pickRay.Direction.Z;
                Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
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
                    if (SelectedNodeList[0].Ship!= null)
                    {
                        SelectedNodeList[0].Ship.RelativeFleetOffset = SelectedNodeList[0].FleetOffset;
                    }
                }
                foreach (ClickableSquad cs in ClickableSquads)
                {
                    if (Vector2.Distance(cs.ScreenPos, mousePosition) >= 5f || cs.Squad.DataNodes.Contains(SelectedNodeList[0]))
                    {
                        continue;
                    }
                    foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
                    {
                        foreach (Fleet.Squad squad in flank)
                        {
                            squad.DataNodes.Remove(SelectedNodeList[0]);
                            if (SelectedNodeList[0].Ship== null)
                            {
                                continue;
                            }
                            squad.Ships.Remove(SelectedNodeList[0].Ship);
                        }
                    }
                    cs.Squad.DataNodes.Add(SelectedNodeList[0]);
                    if (SelectedNodeList[0].Ship== null)
                    {
                        continue;
                    }
                    cs.Squad.Ships.Add(SelectedNodeList[0].Ship);
                }
            }
        }

        public override void LoadContent()
        {
            Close = new CloseButton(this, new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 38, 97, 20, 20));
            AssignLightRig("example/ShipyardLightrig");
            StarField = new StarField(this);
            Rectangle titleRect = new Rectangle(2, 44, 250, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2f - Fonts.Laserian14.MeasureString("Fleet Hotkeys").X / 2f
                , titleRect.Y + titleRect.Height / 2f - Fonts.Laserian14.LineSpacing / 2f);
            Rectangle leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, titleRect.Width, 500);
            LeftMenu = new Menu1(ScreenManager, leftRect, true);
            FleetSL = new ScrollList(LeftMenu.subMenu, 40);
            int i = 0;
            foreach (KeyValuePair<int, Fleet> fleet in EmpireManager.Player.GetFleetsDict())
            {
                FleetsRects.Add(fleet.Key, new Rectangle(leftRect.X + 2, leftRect.Y + i * 53, 52, 48));
                i++;
            }
            Rectangle shipRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 282, 140, 280, 80);
            ShipDesigns = new Menu2(shipRect);
            ShipDesignsTitlePos = new Vector2(shipRect.X + shipRect.Width / 2 - Fonts.Laserian14.MeasureString("Ship Designs").X / 2f, shipRect.Y + shipRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            Rectangle shipDesignsRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - shipRect.Width - 2, shipRect.Y + shipRect.Height + 5, shipRect.Width, 500);
            RightMenu = new Menu1(shipDesignsRect);
            SubShips = new Submenu(shipDesignsRect);
            ShipSL = new ScrollList(SubShips, 40);
            SubShips.AddTab("Designs");
            SubShips.AddTab("Owned");
            foreach (Ship ship in EmpireManager.Player.GetShips())
            {
                if (ship.fleet != null || !ship.Active)
                {
                    continue;
                }
                AvailableShips.Add(ship);
            }
            ResetLists();
            SelectedStuffRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 220, -13 + ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 200, 440, 210);
            

            var ordersBarPos = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 65);
            void AddOrdersBtn(string action, string icon, int toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, icon, this)
                {
                    Action       = action,
                    HasToolTip   = true,
                    WhichToolTip = toolTip
                };
                Add(button);
                OrdersButtons.Add(button);
                ordersBarPos.X += 29f;
            }

            AddOrdersBtn("attack",      "SelectionBox/icon_formation_headon", toolTip: 1);
            AddOrdersBtn("arty",        "SelectionBox/icon_formation_aft",    toolTip: 2);
            AddOrdersBtn("hold",        "SelectionBox/icon_formation_x",      toolTip: 65);
            AddOrdersBtn("orbit_left",  "SelectionBox/icon_formation_left",   toolTip: 3);
            AddOrdersBtn("orbit_right", "SelectionBox/icon_formation_right",  toolTip: 4);
            AddOrdersBtn("evade",       "SelectionBox/icon_formation_stop",   toolTip: 6);

            ordersBarPos = new Vector2(SelectedStuffRect.X + 20 + 3*29f, ordersBarPos.Y + 29f);
            AddOrdersBtn("broadside_left",  "SelectionBox/icon_formation_bleft",  toolTip: 159);
            AddOrdersBtn("broadside_right", "SelectionBox/icon_formation_bright", toolTip: 160);


            RequisitionForces = new BlueButton(new Vector2(SelectedStuffRect.X + 240, SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20), "Requisition...");
            SaveDesign = new BlueButton(new Vector2(SelectedStuffRect.X + 240, SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20 + 50), "Save Design...");
            LoadDesign = new BlueButton(new Vector2(SelectedStuffRect.X + 240, SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20 + 100), "Load Design...");
            RequisitionForces.ToggleOn = true;
            SaveDesign.ToggleOn = true;
            LoadDesign.ToggleOn = true;
            OperationsRect = new Rectangle(SelectedStuffRect.X + SelectedStuffRect.Width + 2, SelectedStuffRect.Y + 30, 360, SelectedStuffRect.Height - 30);
            Rectangle assistRect = new Rectangle(OperationsRect.X + 15, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 150, 40);
            SliderAssist = new WeightSlider(assistRect, "Assist Nearby Weight")
            {
                Tip_ID = 7
            };
            Rectangle defenderRect = new Rectangle(OperationsRect.X + 15, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 150, 40);
            SliderDefend = new WeightSlider(defenderRect, "Defend Nearby Weight")
            {
                Tip_ID = 8
            };
            Rectangle vultureRect = new Rectangle(OperationsRect.X + 15, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 120, 150, 40);
            SliderVulture = new WeightSlider(vultureRect, "Target Damaged Weight")
            {
                Tip_ID = 9
            };
            Rectangle armoredRect = new Rectangle(OperationsRect.X + 15 + 180, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 150, 40);
            SliderArmor = new WeightSlider(armoredRect, "Target Armored Weight")
            {
                Tip_ID = 10
            };
            Rectangle shieldedRect = new Rectangle(OperationsRect.X + 15 + 180, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 150, 40);
            SliderShield = new WeightSlider(shieldedRect, "Target Shielded Weight")
            {
                Tip_ID = 11
            };
            Rectangle dpsRect = new Rectangle(OperationsRect.X + 15 + 180, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 120, 150, 40);
            SliderDps = new WeightSlider(dpsRect, "Target DPS Weight")
            {
                Tip_ID = 12
            };
            PrioritiesRect = new Rectangle(SelectedStuffRect.X - OperationsRect.Width - 2, OperationsRect.Y, OperationsRect.Width, OperationsRect.Height);
            Rectangle oprect = new Rectangle(PrioritiesRect.X + 15, PrioritiesRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 300, 40);
            OperationalRadius = new FloatSlider(this, oprect, "Operational Radius", max: 500000, value: 10000)
            {
                RelativeValue = 0.2f,
                TooltipId = 13
            };
            Rectangle sizerect = new Rectangle(PrioritiesRect.X + 15, PrioritiesRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 300, 40);
            SliderSize = new SizeSlider(sizerect, "Target UniverseRadius Preference");
            SliderSize.SetAmount(0.5f);
            SliderSize.Tip_ID = 14;
            StarField = new StarField(this);
            //bg = new Background();
            float width = Viewport.Width;
            Viewport viewport = Viewport;
            float aspectRatio = width / viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 100f, 15000f);
            using (SelectedFleet.Ships.AcquireReadLock())
            foreach (Ship ship in SelectedFleet.Ships)
            {
                ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, 0f));
            }
            base.LoadContent();
        }

        public void LoadData(FleetDesign data)
        {
            var fleet = EmpireManager.Player.GetFleetsDict()[FleetToEdit];

            for (int i = fleet.Ships.Count - 1; i >= 0; i--)
            {
                Ship ship = fleet.Ships[i];
                ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, -1000000f));
                ship?.fleet?.RemoveShip(ship);         
            }
            SelectedFleet.DataNodes.Clear();
            SelectedFleet.Ships.Clear();
            foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
            {
                flank.Clear();
            }
            SelectedFleet.Name = data.Name;
            foreach (FleetDataNode node in data.Data)
            {
                SelectedFleet.DataNodes.Add(node);
            }
            SelectedFleet.FleetIconIndex = data.FleetIconIndex;
        }

        public void ResetLists()
        {
            AvailableShips.Clear();
            foreach (Ship ship in EmpireManager.Player.GetShips())
            {
                if (ship.fleet != null)
                {
                    continue;
                }
                AvailableShips.Add(ship);
            }
            ShipSL.Reset();
            if (SubShips.Tabs[0].Selected)
            {
                var roles = new Array<string>();
                foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
                {
                    Ship ship = ResourceManager.GetShipTemplate(shipname);
                    if (roles.Contains(ship.DesignRoleName))                    
                        continue;                    
                    roles.Add(ship.DesignRoleName);

                    ShipSL.AddItem(new ModuleHeader(ship.DesignRoleName, 295));
                }
                foreach (ScrollList.Entry e in ShipSL.AllEntries)
                {
                    foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
                    {
                        Ship ship = ResourceManager.ShipsDict[shipname];
                        if (ship.DesignRoleName != (e.item as ModuleHeader)?.Text)
                            continue;
                        e.AddSubItem(ship);
                    }
                }
            }
            else if (SubShips.Tabs[1].Selected)
            {
                Array<string> roles = new Array<string>();
                foreach (Ship ship in AvailableShips)
                {
                    if (roles.Contains(ship.DesignRoleName) || ship.shipData.Role == ShipData.RoleName.troop)
                    {
                        continue;
                    }
                    roles.Add(ship.DesignRoleName);
                    ShipSL.AddItem(new ModuleHeader(ship.DesignRoleName, 295));
                }
                foreach (ScrollList.Entry e in ShipSL.AllEntries)
                {
                    foreach (Ship ship in AvailableShips)
                    {
                        if (ship.shipData.Role == ShipData.RoleName.troop || ship.DesignRoleName != (e.item as ModuleHeader)?.Text)
                            continue;
                        e.AddSubItem(ship);
                    }
                }
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            AdjustCamera();
            CamPos.X = CamPos.X + CamVelocity.X;
            CamPos.Y = CamPos.Y + CamVelocity.Y;
            View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(new Vector3(-CamPos.X, CamPos.Y, CamPos.Z), new Vector3(-CamPos.X, CamPos.Y, 0f), new Vector3(0f, -1f, 0f));
            ClickableSquads.Clear();
            foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in flank)
                {
                    Viewport viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(squad.Offset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    ClickableSquad cs = new ClickableSquad
                    {
                        ScreenPos = pPos,
                        Squad = squad
                    };
                    ClickableSquads.Add(cs);
                }
            }
            Vector2 p = SelectedFleet.Position.PointFromRadians(SelectedFleet.Facing, 1f);
            Vector2 fvec = SelectedFleet.Position.DirectionToTarget(p);
            SelectedFleet.AssembleFleet(SelectedFleet.Facing, fvec);
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public struct ClickableNode
        {
            public Vector2 ScreenPos;

            public float Radius;

            public FleetDataNode NodeToClick;
        }

        private struct ClickableSquad
        {
            public Fleet.Squad Squad;

            public Vector2 ScreenPos;
        }
    }
}