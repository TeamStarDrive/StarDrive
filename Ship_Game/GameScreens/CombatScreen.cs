using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class CombatScreen : PlanetScreen, IDisposable
    {
        public Planet p;

        private Menu2 TitleBar;

        private Vector2 TitlePos;

        private Menu2 CombatField;

        private Rectangle gridPos;

        private Menu1 OrbitalResources;

        private Submenu orbitalResourcesSub;

        private ScrollList OrbitSL;

        //private bool LowRes;

        private PlanetGridSquare HoveredSquare;

        private Rectangle SelectedItemRect;

        private Rectangle HoveredItemRect;

        private Rectangle AssetsRect;

        private OrbitalAssetsUIElement assetsUI;

        private TroopInfoUIElement tInfo;

        private TroopInfoUIElement hInfo;

        private UIButton LandAll;
        private UIButton LaunchAll;

        private Rectangle GridRect;

        private Array<PointSet> CenterPoints = new Array<PointSet>();

        private Array<PointSet> pointsList = new Array<PointSet>();

        private bool ResetNextFrame;

        public PlanetGridSquare ActiveTroop;

        private Selector selector;

        ScrollList.Entry draggedTroop;

        Array<PlanetGridSquare> ReversedList = new Array<PlanetGridSquare>();

        BatchRemovalCollection<SmallExplosion> Explosions = new BatchRemovalCollection<SmallExplosion>();

        private float[] anglesByColumn = { (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0) };
        private float[] distancesByRow = { 437f, 379f, 311f, 229f, 128f, 0f };
        private float[] widthByRow     = { 110f, 120f, 132f, 144f, 162f, 183f };
        private float[] startXByRow    =  { 254f, 222f, 181f, 133f, 74f, 0f };


        private static bool popup;  //fbedard

        public CombatScreen(GameScreen parent, Planet p) : base(parent)
        {            
            this.p                = p;            
            int screenWidth       = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            GridRect              = new Rectangle(screenWidth / 2 - 639, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 490, 1278, 437);
            Rectangle titleRect   = new Rectangle(screenWidth / 2 - 250, 44, 500, 80);
            TitleBar              = new Menu2(titleRect);
            TitlePos              = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString("Ground Combat").X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            SelectedItemRect      = new Rectangle(screenWidth - 240, 100, 225, 205);
            AssetsRect            = new Rectangle(10, 48, 225, 200);
            HoveredItemRect       = new Rectangle(10, 248, 225, 200);
            assetsUI              = new OrbitalAssetsUIElement(AssetsRect, ScreenManager, Empire.Universe, p);
            tInfo                 = new TroopInfoUIElement(SelectedItemRect, ScreenManager, Empire.Universe);
            hInfo                 = new TroopInfoUIElement(HoveredItemRect, ScreenManager, Empire.Universe);
            Rectangle ColonyGrid  = new Rectangle(screenWidth / 2 - screenWidth * 2 / 3 / 2, 130, screenWidth * 2 / 3, screenWidth * 2 / 3 * 5 / 7);
            CombatField           = new Menu2(ColonyGrid);
            Rectangle OrbitalRect = new Rectangle(5, ColonyGrid.Y, (screenWidth - ColonyGrid.Width) / 2 - 20, ColonyGrid.Height+20);
            OrbitalResources      = new Menu1(OrbitalRect);
            Rectangle psubRect    = new Rectangle(AssetsRect.X + 225, AssetsRect.Y+23, 185, AssetsRect.Height);
            orbitalResourcesSub   = new Submenu(psubRect);
            OrbitSL               = new ScrollList(orbitalResourcesSub);

            orbitalResourcesSub.AddTab("In Orbit");

            LandAll   = Button(orbitalResourcesSub.Menu.X + 20, orbitalResourcesSub.Menu.Y - 2, "Land All", OnLandAllClicked);
            LaunchAll = Button(orbitalResourcesSub.Menu.X + 20, LandAll.Rect.Y - 2 - LandAll.Rect.Height, "Launch All", OnLaunchAllClicked);

            using (Empire.Universe.MasterShipList.AcquireReadLock())
            foreach (Ship ship in Empire.Universe.MasterShipList)			                        
            {
                
                if (ship == null)
                    continue;
                if (Vector2.Distance(p.Center, ship.Center) >= p.ObjectRadius + ship.Radius + 1500f || ship.loyalty != EmpireManager.Player)
                {
                    continue;
                }
                if (ship.shipData.Role != ShipData.RoleName.troop)
                {
                    if (ship.TroopList.Count <= 0 || (!ship.Carrier.HasTroopBays && !ship.Carrier.HasTransporters && !(p.HasSpacePort && p.Owner == ship.loyalty)))  //fbedard
                        continue;
                    int landingLimit = ship.Carrier.AllActiveHangars.Count(ready => ready.IsTroopBay && ready.hangarTimer <= 0);
                    foreach (ShipModule module in ship.Carrier.AllTransporters.Where(module => module.TransporterTimer <= 1))
                        landingLimit += module.TransporterTroopLanding;
                    if (p.HasSpacePort && p.Owner == ship.loyalty) landingLimit = ship.TroopList.Count;  //fbedard: Allows to unload if shipyard
                    for (int i = 0; i < ship.TroopList.Count() && landingLimit > 0; i++)
                    {
                        if (ship.TroopList[i] != null && ship.TroopList[i].GetOwner() == ship.loyalty)
                        {
                            OrbitSL.AddItem(ship.TroopList[i]);
                            landingLimit--;
                        }
                    }
                }
                else
                {
                    OrbitSL.AddItem(ship);
                }
            }
            gridPos = new Rectangle(ColonyGrid.X + 20, ColonyGrid.Y + 20, ColonyGrid.Width - 40, ColonyGrid.Height - 40);
            int xsize = gridPos.Width / 7;
            int ysize = gridPos.Height / 5;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                pgs.ClickRect = new Rectangle(gridPos.X + pgs.x * xsize, gridPos.Y + pgs.y * ysize, xsize, ysize);
                foreach (var troop in pgs.TroopsHere)
                {
                    //@TODO HACK. first frame is getting overwritten or lost somewhere. 
                    troop.WhichFrame = troop.first_frame;
                    
                }
            }
            for (int row = 0; row < 6; row++)
            {
                for (int i = 0; i < 7; i++)
                {
                    var ps = new PointSet
                    {
                        point = new Vector2(GridRect.X + i * widthByRow[row] + widthByRow[row] / 2f + startXByRow[row], GridRect.Y + GridRect.Height - distancesByRow[row]),
                        row = row,
                        column = i
                    };
                    pointsList.Add(ps);
                }
            }
            foreach (PointSet ps in pointsList)
            {
                foreach (PointSet toCheck in pointsList)
                {
                    if (ps.column == toCheck.column && ps.row == toCheck.row - 1)
                    {
                        float distance = Vector2.Distance(ps.point, toCheck.point);
                        Vector2 vtt = toCheck.point - ps.point;
                        vtt = Vector2.Normalize(vtt);
                        Vector2 cPoint = ps.point + ((vtt * distance) / 2f);
                        var cp = new PointSet
                        {
                            point = cPoint,
                            row = ps.row,
                            column = ps.column
                        };
                        CenterPoints.Add(cp);
                    }
                }
            }
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                foreach (PointSet ps in CenterPoints)
                {
                    if (pgs.x == ps.column && pgs.y == ps.row)
                        pgs.ClickRect = new Rectangle((int) ps.point.X - 32, (int) ps.point.Y - 32, 64, 64);
                }
            }
            foreach (PlanetGridSquare pgs in p.TilesList)
                ReversedList.Add(pgs);
        }

        private void DetermineAttackAndMove()
        {
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                pgs.CanAttack = false;
                pgs.CanMoveTo = false;
                if (ActiveTroop == null)
                pgs.ShowAttackHover = false;
            }
            if (ActiveTroop == null)
            {
                //added by gremlin why two loops? moved hover clear to first loop and move null check to third loop.
                //foreach (PlanetGridSquare pgs in this.p.TilesList)
                //{
                //    pgs.CanMoveTo = false;
                //    pgs.CanAttack = false;
                //    pgs.ShowAttackHover = false;
                //}
            }
            if (ActiveTroop != null)
            {
                foreach (PlanetGridSquare pgs in p.TilesList)
                {
                    if (pgs.building != null && pgs.building.CombatStrength > 0)
                    {
                        pgs.CanMoveTo = false;
                    }
                    if (ActiveTroop != pgs)
                    {
                        continue;
                    }
                    if (ActiveTroop.TroopsHere.Count > 0 && ActiveTroop.TroopsHere[0].AvailableAttackActions > 0)
                    {
                        foreach (PlanetGridSquare nearby in p.TilesList)
                        {
                            if (nearby == pgs)
                            {
                                continue;
                            }
                            int XtotalDistance = Math.Abs(pgs.x - nearby.x);
                            int YtotalDistance = Math.Abs(pgs.y - nearby.y);
                            if (XtotalDistance > pgs.TroopsHere[0].Range || YtotalDistance > pgs.TroopsHere[0].Range || nearby.TroopsHere.Count <= 0 && (nearby.building == null || nearby.building.CombatStrength <= 0))
                            {
                                continue;
                            }
                            if ((nearby.TroopsHere.Count > 0 && nearby.TroopsHere[0].GetOwner() != EmpireManager.Player) || (nearby.building != null && nearby.building.CombatStrength > 0 && p.Owner != EmpireManager.Player))  //fbedard: cannot attack allies !
                                nearby.CanAttack = true;
                        }
                    }
                    else if (ActiveTroop.building != null && ActiveTroop.building.CombatStrength > 0 && ActiveTroop.building.AvailableAttackActions > 0)
                    {
                        foreach (PlanetGridSquare nearby in p.TilesList)
                        {
                            if (nearby == pgs)
                            {
                                continue;
                            }
                            int XtotalDistance = Math.Abs(pgs.x - nearby.x);
                            int YtotalDistance = Math.Abs(pgs.y - nearby.y);
                            if (XtotalDistance > 1 || YtotalDistance > 1 || nearby.TroopsHere.Count <= 0 && (nearby.building == null || nearby.building.CombatStrength <= 0))
                            {
                                continue;
                            }
                            if ((nearby.TroopsHere.Count > 0 && nearby.TroopsHere[0].GetOwner() != EmpireManager.Player) || (nearby.building != null && nearby.building.CombatStrength > 0 && p.Owner != EmpireManager.Player))  //fbedard: cannot attack allies !
                                nearby.CanAttack = true;
                        }
                    }
                    if (ActiveTroop.TroopsHere.Count <= 0 || ActiveTroop.TroopsHere[0].AvailableMoveActions <= 0)
                    {
                        continue;
                    }
                    foreach (PlanetGridSquare nearby in p.TilesList)
                    {
                        if (nearby == pgs)
                        {
                            continue;
                        }
                        int XtotalDistance = Math.Abs(pgs.x - nearby.x);
                        int YtotalDistance = Math.Abs(pgs.y - nearby.y);
                        if (XtotalDistance > pgs.TroopsHere[0].Range || YtotalDistance > pgs.TroopsHere[0].Range || nearby.TroopsHere.Count != 0 || nearby.building != null && (nearby.building == null || nearby.building.CombatStrength != 0))
                        {
                            continue;
                        }
                        nearby.CanMoveTo = true;
                    }
                }
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            GameTime gameTime = StarDriveGame.Instance.GameTime;
            batch.Draw(ResourceManager.Texture($"PlanetTiles/{p.PlanetTileId}_tilt"), GridRect, Color.White);
            batch.Draw(ResourceManager.Texture("Ground_UI/grid"), GridRect, Color.White);

            if (assetsUI.LandTroops.Toggled)
            {
                OrbitSL.Draw(batch);
                var bCursor = new Vector2((orbitalResourcesSub.Menu.X + 25), 350f);
                foreach (ScrollList.Entry e in OrbitSL.VisibleExpandedEntries)
                {
                    if (e.item is Ship ship)
                    {
                        if (ship.TroopList.Count == 0)
                            continue;
                        Troop t = ship.TroopList[0];
                        if (e.Hovered)
                        {
                            bCursor.Y = e.Y;
                            batch.Draw(t.TextureDefault, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                            var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                            batch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.White);
                            tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                            batch.DrawString(Fonts.Arial8Bold, t.StrengthText, tCursor, Color.Orange);
                        }
                        else
                        {
                            bCursor.Y = e.Y;
                            batch.Draw(t.TextureDefault, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                            var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                            batch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.LightGray);
                            tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                            batch.DrawString(Fonts.Arial8Bold, t.StrengthText, tCursor, Color.LightGray);
                        }
                    }
                    else if (e.item is Troop t)
                    {
                        if (e.Hovered)
                        {
                            bCursor.Y = e.Y;
                            batch.Draw(t.TextureDefault, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                            Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                            batch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.White);
                            tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
                            batch.DrawString(Fonts.Arial8Bold, t.StrengthText, tCursor, Color.Orange);
                        }
                        else
                        {
                            bCursor.Y = e.Y;
                            batch.Draw(t.TextureDefault, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                            Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                            batch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.LightGray);
                            tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
                            batch.DrawString(Fonts.Arial8Bold, t.StrengthText, tCursor, Color.LightGray);
                        }
                    }
                    e.CheckHover(Input.CursorPosition);
                }

                LandAll.Visible = OrbitSL.NumEntries > 0;
                LaunchAll.Visible = p.TroopsHere.Any(mytroops => mytroops.GetOwner() == EmpireManager.Player && mytroops.Launchtimer <= 0f);
            }
            foreach (PlanetGridSquare pgs in ReversedList)
            {
                if (pgs.building == null)
                    continue;
                var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                batch.Draw(ResourceManager.Texture($"Buildings/icon_{pgs.building.Icon}_64x64"), bRect, Color.White);
            }
            foreach (PlanetGridSquare pgs in ReversedList)
            {
                DrawPGSIcons(pgs);
                DrawCombatInfo(pgs);
            }
            if (ActiveTroop != null)
            {
                tInfo.Draw(gameTime);
            }

            assetsUI.Draw(gameTime);
            if (draggedTroop != null)
            {
                foreach (PlanetGridSquare pgs in ReversedList)
                {
                    if ((pgs.building == null && pgs.TroopsHere.Count == 0) ||
                        (pgs.building != null && pgs.building.CombatStrength == 0 && pgs.TroopsHere.Count == 0))
                    {
                        Vector2 center = pgs.ClickRect.Center();
                        DrawCircle(center, 5f, Color.White, 5f);
                        DrawCircle(center, 5f, Color.Black);
                    }
                }

                Troop troop = draggedTroop.TryGet(out Ship ship) && ship.TroopList.Count > 0
                            ? ship.TroopList.First : draggedTroop.Get<Troop>();

                SubTexture icon = troop.TextureDefault;
                batch.Draw(icon, Input.CursorPosition, Color.White, 0f, icon.CenterF, 0.65f, SpriteEffects.None, 1f);
            }
            if (Empire.Universe.IsActive)
            {
                ToolTip.Draw(batch);
            }
            batch.End();

            batch.Begin(SpriteBlendMode.Additive);
            using (Explosions.AcquireReadLock())
            foreach (SmallExplosion exp in Explosions)
                exp.Draw(batch);
            batch.End();

            batch.Begin();

            if (ScreenManager.NumScreens == 2)
                popup = true;
        }

        private void DrawCombatInfo(PlanetGridSquare pgs)
        {
            if ((ActiveTroop == null || ActiveTroop != pgs) &&
                (pgs.building == null || pgs.building.CombatStrength <= 0 || ActiveTroop == null ||
                 ActiveTroop != pgs))
                return;

            var activeSel = new Rectangle(pgs.TroopClickRect.X - 5, pgs.TroopClickRect.Y - 5, pgs.TroopClickRect.Width + 10, pgs.TroopClickRect.Height + 10);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), activeSel, Color.White);
            foreach (PlanetGridSquare nearby in ReversedList)
            {
                if (nearby != pgs && nearby.ShowAttackHover)
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Attack_Confirm"),
                        nearby.TroopClickRect, Color.White);
            }
        }

        private void DrawPGSIcons(PlanetGridSquare pgs)
        {
            SpriteBatch batch = ScreenManager.SpriteBatch;

            float width = (pgs.y * 15 + 64);
            if (width > 128f)
                width = 128f;
            if (pgs.building != null && pgs.building.CombatStrength > 0)
                width = 64f;

            pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - (int)width / 2, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - (int)width / 2, (int)width, (int)width);
            if (pgs.TroopsHere.Count > 0)
            {
                Troop troop = pgs.TroopsHere[0];
                Rectangle troopClickRect = pgs.TroopClickRect;
                if (troop.MovingTimer > 0f)
                {
                    float amount          = 1f - troop.MovingTimer;
                    troopClickRect.X      = (int)MathHelper.Lerp(troop.GetFromRect().X, pgs.TroopClickRect.X, amount);
                    troopClickRect.Y      = (int)MathHelper.Lerp(troop.GetFromRect().Y, pgs.TroopClickRect.Y, amount);
                    troopClickRect.Width  = (int)MathHelper.Lerp(troop.GetFromRect().Width, pgs.TroopClickRect.Width, amount);
                    troopClickRect.Height = (int)MathHelper.Lerp(troop.GetFromRect().Height, pgs.TroopClickRect.Height, amount);
                }
                troop.Draw(batch, troopClickRect);
                var moveRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 38, 12, 12);
                if (troop.AvailableMoveActions <= 0)
                {
                    int moveTimer = (int)troop.MoveTimer + 1;
                    HelperFunctions.DrawDropShadowText1(batch, moveTimer.ToString(), new Vector2((moveRect.X + 4), moveRect.Y), Fonts.Arial12, Color.White);
                }
                else
                {
                    batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Move"), moveRect, Color.White);
                }
                var attackRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 23, 12, 12);
                if (troop.AvailableAttackActions <= 0)
                {
                    int attackTimer = (int)troop.AttackTimer + 1;
                    HelperFunctions.DrawDropShadowText1(batch, attackTimer.ToString(), new Vector2((attackRect.X + 4), attackRect.Y), Fonts.Arial12, Color.White);
                }
                else
                {
                    batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), attackRect, Color.White);
                }

                var strengthRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 5, 
                                                 Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                DrawTroopData(batch, strengthRect, troop, troop.Strength.String(1), Color.White);

                //Fat Bastard - show TroopLevel
                if (pgs.TroopsHere[0].Level > 0)
                {
                    var levelRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 52, 
                                                  Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    DrawTroopData(batch, levelRect, troop, troop.Level.ToString(), Color.Gold);
                }

                if (ActiveTroop != null && ActiveTroop == pgs)
                {
                    if (ActiveTroop.TroopsHere[0].AvailableAttackActions > 0)
                    {
                        foreach (PlanetGridSquare nearby in p.TilesList)
                        {
                            if (nearby == pgs || !nearby.CanAttack)
                            {
                                continue;
                            }
                            batch.Draw(ResourceManager.Texture("Ground_UI/GC_Potential_Attack"), nearby.ClickRect, Color.White);
                        }
                    }
                    if (ActiveTroop.TroopsHere[0].AvailableMoveActions > 0)
                    {
                        foreach (PlanetGridSquare nearby in p.TilesList)
                        {
                            if (nearby == pgs || !nearby.CanMoveTo)
                            {
                                continue;
                            }
                            batch.FillRectangle(nearby.ClickRect, new Color(255, 255, 255, 30));
                            Vector2 center = nearby.ClickRect.Center();
                            DrawCircle(center, 5f, Color.White, 5f);
                            DrawCircle(center, 5f, Color.Black);
                        }
                    }
                }
            }
            else if (pgs.building != null)
            {
                if (pgs.building.CombatStrength <= 0)
                {
                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    var strengthRect = new Rectangle(bRect.X + bRect.Width + 2, bRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    batch.FillRectangle(strengthRect, new Color(0, 0, 0, 200));
                    batch.DrawRectangle(strengthRect, p.Owner?.EmpireColor ?? Color.Gray);
                    var cursor = new Vector2((strengthRect.X + strengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.building.Strength.ToString()).X / 2f, 
                                             (1 + strengthRect.Y + strengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12, pgs.building.Strength.ToString(), cursor, Color.White);
                }
                else
                {
                    var attackRect = new Rectangle(pgs.TroopClickRect.X + pgs.TroopClickRect.Width + 2, pgs.TroopClickRect.Y + 23, 12, 12);
                    if (pgs.building.AvailableAttackActions <= 0)
                    {
                        int num = (int)pgs.building.AttackTimer + 1;
                        batch.DrawString(Fonts.Arial12, num.ToString(), new Vector2((attackRect.X + 4), attackRect.Y), Color.White);
                    }
                    else
                    {
                        batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), attackRect, Color.White);
                    }
                    var strengthRect = new Rectangle(pgs.TroopClickRect.X + pgs.TroopClickRect.Width + 2, pgs.TroopClickRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    batch.FillRectangle(strengthRect, new Color(0, 0, 0, 200));
                    batch.DrawRectangle(strengthRect, p.Owner?.EmpireColor ?? Color.LightGray);
                    var cursor = new Vector2((strengthRect.X + strengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.building.CombatStrength.ToString()).X / 2f,
                                             (1 + strengthRect.Y + strengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12, pgs.building.CombatStrength.ToString(), cursor, Color.White);
                }

                if (ActiveTroop != null && ActiveTroop == pgs && ActiveTroop.building.AvailableAttackActions > 0)
                {
                    foreach (PlanetGridSquare nearby in p.TilesList)
                    {
                        if (nearby == pgs || !nearby.CanAttack)
                        {
                            continue;
                        }
                        batch.Draw(ResourceManager.Texture("Ground_UI/GC_Potential_Attack"), nearby.ClickRect, Color.White);
                    }
                }
            }
        }

        private void DrawTroopData(SpriteBatch batch, Rectangle rect, Troop troop, string data, Color color)
        {
            SpriteFont font = Fonts.Arial12;
            batch.FillRectangle(rect, new Color(0, 0, 0, 200));
            batch.DrawRectangle(rect, troop.GetOwner().EmpireColor);
            var cursor = new Vector2((rect.X + rect.Width / 2) - font.MeasureString(troop.Strength.String(1)).X / 2f,
                (1 + rect.Y + rect.Height / 2 - font.LineSpacing / 2));
            batch.DrawString(font, data, cursor, color);
        }

        private void OnLandAllClicked(UIButton b)
        {
            GameAudio.TroopLand();
            foreach (ScrollList.Entry e in OrbitSL.AllEntries)
            {
                if (e.item is Ship ship)
                {
                    ship.AI.OrderLandAllTroops(p);
                }
                else if (e.item is Troop troop)
                {
                    troop.GetShip().TroopList.Remove(troop);
                    troop.AssignTroopToTile(p);
                }
            }
            OrbitSL.Reset();
        }

        private void OnLaunchAllClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Empire.Universe.player || pgs.TroopsHere[0].Launchtimer >= 0)
                {
                    continue;
                }
                try
                {
                    pgs.TroopsHere[0].AvailableAttackActions = 0;
                    pgs.TroopsHere[0].AvailableMoveActions = 0;
                    pgs.TroopsHere[0].Launchtimer = pgs.TroopsHere[0].MoveTimerBase;
                    pgs.TroopsHere[0].AttackTimer = pgs.TroopsHere[0].AttackTimerBase;
                    pgs.TroopsHere[0].MoveTimer = pgs.TroopsHere[0].MoveTimerBase;
                    play = true;
                    Ship.CreateTroopShipAtPoint(pgs.TroopsHere[0].GetOwner().data.DefaultTroopShip, pgs.TroopsHere[0].GetOwner(), p.Center, pgs.TroopsHere[0]);
                    p.TroopsHere.Remove(pgs.TroopsHere[0]);
                    pgs.TroopsHere[0].SetPlanet(null);
                    pgs.TroopsHere.Clear();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Troop Launch Crash");
                }
            }
            if (play)
            {
                GameAudio.TroopTakeOff();
                ResetNextFrame = true;
            }
        }

        public override bool HandleInput(InputState input)
        {
            bool selectedSomethingThisFrame = false;
            assetsUI.HandleInput(input);  
            if (ActiveTroop != null && tInfo.HandleInput(input))
            {
                selectedSomethingThisFrame = true;
            }
            selector = null;
            HoveredSquare = null;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (!pgs.ClickRect.HitTest(input.CursorPosition) || pgs.TroopsHere.Count == 0 && pgs.building == null)
                {
                    continue;
                }
                HoveredSquare = pgs;
            }
            LandAll.Enabled = OrbitSL.NumEntries > 0;
            LaunchAll.Enabled = p.TroopsHere.Any(mytroops => mytroops.GetOwner() == Empire.Universe.player);
            OrbitSL.HandleInput(input);
            foreach (ScrollList.Entry e in OrbitSL.AllExpandedEntries)
            {
                if (!e.CheckHover(Input.CursorPosition))
                    continue;
                selector = e.CreateSelector();
                if (input.LeftMouseClick)
                    draggedTroop = e;
            }
            if (draggedTroop != null && input.LeftMouseClick)
            {
                bool foundPlace = false;
                foreach (PlanetGridSquare pgs in p.TilesList)
                {
                    if (!pgs.ClickRect.HitTest(Input.CursorPosition))
                    {
                        continue;
                    }
                    if (!(draggedTroop.item is Ship) || (draggedTroop.item as Ship).TroopList.Count <= 0)
                    {
                        if (!(draggedTroop.item is Troop) || (pgs.building != null || pgs.TroopsHere.Count != 0) && (pgs.building == null || pgs.building.CombatStrength != 0 || pgs.TroopsHere.Count != 0))
                        {
                            continue;
                        }
                        GameAudio.TroopLand();
                        pgs.TroopsHere.Add(draggedTroop.item as Troop);
                        pgs.TroopsHere[0].AvailableAttackActions = 0;
                        pgs.TroopsHere[0].AvailableMoveActions = 0;
                        pgs.TroopsHere[0].Launchtimer = pgs.TroopsHere[0].MoveTimerBase;
                        pgs.TroopsHere[0].AttackTimer = pgs.TroopsHere[0].AttackTimerBase;
                        pgs.TroopsHere[0].MoveTimer = pgs.TroopsHere[0].MoveTimerBase;

                        p.TroopsHere.Add(draggedTroop.item as Troop);
                        (draggedTroop.item as Troop).SetPlanet(p);
                        OrbitSL.Remove(draggedTroop);
                        (draggedTroop.item as Troop).GetShip().TroopList.Remove(draggedTroop.item as Troop);
                        foundPlace = true;
                        draggedTroop = null;
                    }
                    else
                    {
                        if ((pgs.building != null || pgs.TroopsHere.Count != 0) && (pgs.building == null || pgs.building.CombatStrength != 0 || pgs.TroopsHere.Count != 0))
                        {
                            continue;
                        }
                        GameAudio.TroopLand();
                        pgs.TroopsHere.Add((draggedTroop.item as Ship).TroopList[0]);
                        pgs.TroopsHere[0].AvailableAttackActions = 0;
                        pgs.TroopsHere[0].AvailableMoveActions = 0;
                        pgs.TroopsHere[0].Launchtimer = pgs.TroopsHere[0].MoveTimerBase;
                        pgs.TroopsHere[0].AttackTimer = pgs.TroopsHere[0].AttackTimerBase;
                        pgs.TroopsHere[0].MoveTimer = pgs.TroopsHere[0].MoveTimerBase;
                        p.TroopsHere.Add((draggedTroop.item as Ship).TroopList[0]);
                        (draggedTroop.item as Ship).TroopList[0].SetPlanet(p);
                        if (!string.IsNullOrEmpty(pgs.building?.EventTriggerUID) && pgs.TroopsHere.Count > 0 && !pgs.TroopsHere[0].GetOwner().isFaction)
                        {
                            ResourceManager.EventsDict[pgs.building.EventTriggerUID].TriggerPlanetEvent(p, pgs.TroopsHere[0].GetOwner(), pgs, Empire.Universe);
                        }
                        OrbitSL.Remove(draggedTroop);
                        (draggedTroop.item as Ship).QueueTotalRemoval();
                        foundPlace = true;
                        draggedTroop = null;
                    }
                }
                if (!foundPlace)
                {
                    draggedTroop = null;
                    GameAudio.NegativeClick();
                }
            }
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (!pgs.ClickRect.HitTest(Input.CursorPosition))
                {
                    pgs.highlighted = false;
                }
                else
                {
                    if (!pgs.highlighted)
                    {
                        GameAudio.ButtonMouseOver();
                    }
                    pgs.highlighted = true;
                }
                if (pgs.CanAttack)
                {
                    if (!pgs.CanAttack || ActiveTroop == null)
                    {
                        continue;
                    }
                    if (!pgs.TroopClickRect.HitTest(Input.CursorPosition))
                    {
                        pgs.ShowAttackHover = false;
                    }
                    else if (ActiveTroop.TroopsHere.Count <= 0)
                    {
                        if (ActiveTroop.building == null || ActiveTroop.building.CombatStrength <= 0 || ActiveTroop.building.AvailableAttackActions <= 0 || p.Owner == null || p.Owner != EmpireManager.Player)
                        {
                            continue;
                        }
                        if (Input.LeftMouseClick)
                        {
                            ActiveTroop.building.AvailableAttackActions -= 1;
                            ActiveTroop.building.AttackTimer = 10f;
                            StartCombat(ActiveTroop, pgs);
                        }
                        pgs.ShowAttackHover = true;
                    }
                    else
                    {
                        if (ActiveTroop.TroopsHere[0].AvailableAttackActions <= 0 || ActiveTroop.TroopsHere[0].GetOwner() != EmpireManager.Player)
                        {
                            continue;
                        }
                        if (Input.LeftMouseClick)
                        {
                            if      (pgs.x > ActiveTroop.x) ActiveTroop.TroopsHere[0].facingRight = true;
                            else if (pgs.x < ActiveTroop.x) ActiveTroop.TroopsHere[0].facingRight = false;

                            Troop item = ActiveTroop.TroopsHere[0];
                            item.AvailableAttackActions = item.AvailableAttackActions - 1;
                            ActiveTroop.TroopsHere[0].AttackTimer = ActiveTroop.TroopsHere[0].AttackTimerBase;
                            Troop availableMoveActions = ActiveTroop.TroopsHere[0];
                            availableMoveActions.AvailableMoveActions = availableMoveActions.AvailableMoveActions - 1;
                            ActiveTroop.TroopsHere[0].MoveTimer = ActiveTroop.TroopsHere[0].MoveTimerBase;
                            StartCombat(ActiveTroop, pgs);
                        }
                        pgs.ShowAttackHover = true;
                    }
                }
                else
                {
                    if (pgs.TroopsHere.Count > 0)
                    {
                        if (pgs.TroopClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                        {
                            if (pgs.TroopsHere[0].GetOwner() != EmpireManager.Player)
                            {
                                ActiveTroop = pgs;
                                tInfo.SetPGS(pgs);
                                selectedSomethingThisFrame = true;
                            }
                            else
                            {
                                foreach (PlanetGridSquare p1 in p.TilesList)
                                {
                                    p1.CanAttack = false;
                                    p1.CanMoveTo = false;
                                    p1.ShowAttackHover = false;
                                }
                                ActiveTroop = pgs;
                                tInfo.SetPGS(pgs);
                                selectedSomethingThisFrame = true;
                            }
                        }
                    }
                    else if (pgs.building != null && !pgs.CanMoveTo && pgs.TroopClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                    {
                        if (p.Owner != EmpireManager.Player)
                        {
                            ActiveTroop = pgs;
                            tInfo.SetPGS(pgs);
                            selectedSomethingThisFrame = true;
                        }
                        else
                        {
                            foreach (PlanetGridSquare p1 in p.TilesList)
                            {
                                p1.CanAttack = false;
                                p1.CanMoveTo = false;
                                p1.ShowAttackHover = false;
                            }
                            ActiveTroop = pgs;
                            tInfo.SetPGS(pgs);
                            selectedSomethingThisFrame = true;
                        }
                    }
                    if (ActiveTroop == null || !pgs.CanMoveTo || ActiveTroop.TroopsHere.Count == 0 || !pgs.ClickRect.HitTest(Input.CursorPosition) || ActiveTroop.TroopsHere[0].GetOwner() != EmpireManager.Player || Input.LeftMouseReleased || ActiveTroop.TroopsHere[0].AvailableMoveActions <= 0)
                    {
                        continue;
                    }
                    if (pgs.x > ActiveTroop.x)
                    {
                        ActiveTroop.TroopsHere[0].facingRight = true;
                    }
                    else if (pgs.x < ActiveTroop.x)
                    {
                        ActiveTroop.TroopsHere[0].facingRight = false;
                    }
                    pgs.TroopsHere.Add(ActiveTroop.TroopsHere[0]);
                    Troop troop = pgs.TroopsHere[0];
                    troop.AvailableMoveActions = troop.AvailableMoveActions - 1;
                    pgs.TroopsHere[0].MoveTimer = pgs.TroopsHere[0].MoveTimerBase;
                    pgs.TroopsHere[0].MovingTimer = 0.75f;
                    pgs.TroopsHere[0].SetFromRect(ActiveTroop.TroopClickRect);
                    GameAudio.PlaySfxAsync(pgs.TroopsHere[0].MovementCue);
                    ActiveTroop.TroopsHere.Clear();
                    ActiveTroop = null;
                    ActiveTroop = pgs;
                    pgs.CanMoveTo = false;
                    selectedSomethingThisFrame = true;
                }
            }
            if (ActiveTroop != null && !selectedSomethingThisFrame && Input.LeftMouseClick && !SelectedItemRect.HitTest(input.CursorPosition))
            {
                ActiveTroop = null;
            }
            if (ActiveTroop != null)
            {
                tInfo.pgs = ActiveTroop;
            }
            DetermineAttackAndMove();
            hInfo.SetPGS(HoveredSquare);
            
            if (popup)
            {
                if (input.MouseCurr.RightButton != ButtonState.Released || input.MousePrev.RightButton != ButtonState.Released)
                        return true;
                    popup = false;
            }
            else if (input.MouseCurr.RightButton != ButtonState.Released || input.MousePrev.RightButton != ButtonState.Released)
            {
                Empire.Universe.ShipsInCombat.Visible = true;
                Empire.Universe.PlanetsInCombat.Visible = true;
            }
            return base.HandleInput(input);
        }
        
        private void ResetTroopList()
        {
            OrbitSL.Reset();
            for (int i = 0; i < p.ParentSystem.ShipList.Count; i++)
            {
                Ship ship = p.ParentSystem.ShipList[i];
                if (p.Center.Distance(ship.Center) >= 15000f || ship.loyalty != EmpireManager.Player)
                    continue;

                if (ship.shipData.Role == ShipData.RoleName.troop && !Empire.Universe.MasterShipList.IsPendingRemoval(ship))
                {
                    OrbitSL.AddItem(ship);
                }
                else if (ship.Carrier.HasTroopBays || ship.Carrier.HasTransporters)
                {
                    int landingLimit = ship.Carrier.AllActiveHangars.Count(ready => ready.IsTroopBay && ready.hangarTimer <= 0);
                    foreach (ShipModule module in ship.Carrier.AllTransporters.Where(module => module.TransporterTimer <= 1f))
                        landingLimit += module.TransporterTroopLanding;

                    for (int x = 0; x < ship.TroopList.Count && landingLimit > 0; x++)
                    {
                        if (ship.TroopList[x].GetOwner() == ship.loyalty)
                        {
                            OrbitSL.AddItem(ship.TroopList[x]);
                            landingLimit--;
                        }
                    }
                }
            }
        }

        public void StartCombat(PlanetGridSquare Attacker, PlanetGridSquare Defender)
        {
            Combat c = new Combat
            {
                Attacker = Attacker
            };
            if (Attacker.TroopsHere.Count <= 0)
            {
                GameAudio.PlaySfxAsync("sd_weapon_bigcannon_01");
                GameAudio.PlaySfxAsync("uzi_loop");
            }
            else
            {
                Attacker.TroopsHere[0].DoAttack();
                GameAudio.PlaySfxAsync(Attacker.TroopsHere[0].sound_attack);
            }
            c.Defender = Defender;
            p.ActiveCombats.Add(c);
        }

        public static void StartCombat(PlanetGridSquare Attacker, PlanetGridSquare Defender, Planet p)
        {
            Combat c = new Combat
            {
                Attacker = Attacker
            };
            if (Attacker.TroopsHere.Count > 0)
            {
                Attacker.TroopsHere[0].DoAttack();
            }
            c.Defender = Defender;
            p.ActiveCombats.Add(c);
        }

        public static bool TroopCanAttackSquare(PlanetGridSquare ActiveTroop, PlanetGridSquare squareToAttack, Planet p)
        {
            if (ActiveTroop != null)
            {
                if (ActiveTroop.TroopsHere.Count != 0)
                {
                    foreach (PlanetGridSquare planetGridSquare1 in p.TilesList)
                    {
                        if (ActiveTroop != planetGridSquare1) continue;
                        foreach (PlanetGridSquare planetGridSquare2 in p.TilesList)
                        {
                            if (planetGridSquare2 != ActiveTroop && planetGridSquare2 == squareToAttack)
                            {
                                //Added by McShooterz: Prevent troops from firing on own buildings
                                if (planetGridSquare2.TroopsHere.Count == 0 && 
                                    (planetGridSquare2.building == null || 
                                     (planetGridSquare2.building != null && 
                                      planetGridSquare2.building.CombatStrength == 0) || 
                                      p.Owner?.IsEmpireAttackable(ActiveTroop.TroopsHere[0].GetOwner()) == false
                                      ))
                                    return false;
                                int num1 = Math.Abs(planetGridSquare1.x - planetGridSquare2.x);
                                int num2 = Math.Abs(planetGridSquare1.y - planetGridSquare2.y);
                                if (planetGridSquare2.TroopsHere.Count > 0)
                                {
                                    if (planetGridSquare1.TroopsHere.Count != 0 && 
                                        num1 <= planetGridSquare1.TroopsHere[0].Range && 
                                        (num2 <= planetGridSquare1.TroopsHere[0].Range &&                                          
                                         planetGridSquare2.TroopsHere[0].GetOwner().IsEmpireAttackable(ActiveTroop.TroopsHere[0].GetOwner()) 
                                         ))
                                        return true;
                                }
                                else if (planetGridSquare2.building != null && 
                                         planetGridSquare2.building.CombatStrength > 0 && 
                                         (num1 <= planetGridSquare1.TroopsHere[0].Range && 
                                          num2 <= planetGridSquare1.TroopsHere[0].Range))
                                {
                                    if (p.Owner == null)
                                        return false;
                                    if (p.Owner?.IsEmpireAttackable(ActiveTroop.TroopsHere[0].GetOwner()) == true)
                                        return true;
                                }
                            }
                        }
                    }
                }
                else if (ActiveTroop.building != null && ActiveTroop.building.CombatStrength > 0)
                {
                    foreach (PlanetGridSquare planetGridSquare1 in p.TilesList)
                    {
                        if (ActiveTroop == planetGridSquare1)
                        {
                            foreach (PlanetGridSquare planetGridSquare2 in p.TilesList)
                            {
                                if (planetGridSquare2 != ActiveTroop && planetGridSquare2 == squareToAttack)
                                {
                                    //Added by McShooterz: Prevent buildings from firing on buildings
                                    if (planetGridSquare2.TroopsHere.Count == 0)
                                        return false;
                                    int num1 = Math.Abs(planetGridSquare1.x - planetGridSquare2.x);
                                    int num2 = Math.Abs(planetGridSquare1.y - planetGridSquare2.y);
                                    if (planetGridSquare2.TroopsHere.Count > 0)
                                    {
                                        if (num1 <= 1 && num2 <= 1 && 
                                            p.Owner?.IsEmpireAttackable(planetGridSquare2.TroopsHere[0].GetOwner()) == true)
                                            return true;
                                    }
                                    else if (planetGridSquare2.building != null && planetGridSquare2.building.CombatStrength > 0 && (num1 <= 1 && num2 <= 1))
                                        return p.Owner != null;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public override void Update(float elapsedTime)
        {
            if (ResetNextFrame)
            {
                ResetTroopList();
                ResetNextFrame = false;
            }
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (pgs.TroopsHere.Count <= 0)
                {
                    continue;
                }
                pgs.TroopsHere[0].Update(elapsedTime);
            }
            using (Explosions.AcquireWriteLock())
            {
                foreach (SmallExplosion exp in Explosions)
                {
                    if (exp.Update(elapsedTime))
                        Explosions.QueuePendingRemoval(exp);
                }
                Explosions.ApplyPendingRemovals();
            }
            p.ActiveCombats.ApplyPendingRemovals();
            base.Update(elapsedTime);
        }

        public void AddExplosion(Rectangle grid, int size)
        {
            var exp = new SmallExplosion(grid, size);
            using (Explosions.AcquireWriteLock())
                Explosions.Add(exp);
        }

        private struct PointSet
        {
            public Vector2 point;

            public int row;

            public int column;
        }

        // small explosion in planetary combat screen
        public class SmallExplosion
        {
            float Time;
            int Frame;
            const float Duration = 2.25f;
            readonly TextureAtlas Animation;
            readonly Rectangle Grid;

            public SmallExplosion(Rectangle grid, int size)
            {
                Grid = grid;
                string anim = size <= 3 ? "Textures/sd_explosion_12a_cc" : "Textures/sd_explosion_07a_cc";
                Animation = ResourceManager.RootContent.LoadTextureAtlas(anim);
            }

            public bool Update(float elapsedTime)
            {
                Time += elapsedTime;
                if (Time > Duration)
                    return true;
                Frame = ((int)(Time / Duration)).Clamped(0, Animation.Count-1);
                return false;
            }

            public void Draw(SpriteBatch batch)
            {
                batch.Draw(Animation[Frame], Grid, Color.White);
            }
        }

        protected override void Destroy()
        {
            Explosions?.Dispose(ref Explosions);
            base.Destroy();
        }
    }
}