using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
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

        private Array<CombatScreen.PointSet> CenterPoints = new Array<CombatScreen.PointSet>();

        private Array<CombatScreen.PointSet> pointsList = new Array<CombatScreen.PointSet>();

        private bool ResetNextFrame;

        public PlanetGridSquare ActiveTroop;

        private Selector selector;

        private ScrollList.Entry draggedTroop;

        private Array<PlanetGridSquare> ReversedList = new Array<PlanetGridSquare>();

        public BatchRemovalCollection<SmallExplosion> Explosions = new BatchRemovalCollection<SmallExplosion>();

        private float[] anglesByColumn = { (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0) };
        private float[] distancesByRow = { 437f, 379f, 311f, 229f, 128f, 0f };
        private float[] widthByRow     = { 110f, 120f, 132f, 144f, 162f, 183f };
        private float[] startXByRow    =  { 254f, 222f, 181f, 133f, 74f, 0f };


        private static bool popup = false;  //fbedard

        public CombatScreen(GameScreen parent, Planet p) : base(parent)
        {            
            this.p                = p;            
            int screenWidth       = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            GridRect              = new Rectangle(screenWidth / 2 - 639, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 490, 1278, 437);
            Rectangle titleRect   = new Rectangle(screenWidth / 2 - 250, 44, 500, 80);
            TitleBar              = new Menu2(titleRect);
            TitlePos              = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString("Ground Combat").X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            SelectedItemRect      = new Rectangle(screenWidth - 240, 100, 225, 205);
            AssetsRect            = new Rectangle(10, 48, 225, 200);
            HoveredItemRect       = new Rectangle(10, 248, 225, 200);
            assetsUI              = new OrbitalAssetsUIElement(this.AssetsRect, this.ScreenManager, Empire.Universe, p);
            tInfo                 = new TroopInfoUIElement(this.SelectedItemRect, this.ScreenManager, Empire.Universe);
            hInfo                 = new TroopInfoUIElement(this.HoveredItemRect, this.ScreenManager, Empire.Universe);
            Rectangle ColonyGrid  = new Rectangle(screenWidth / 2 - screenWidth * 2 / 3 / 2, 130, screenWidth * 2 / 3, screenWidth * 2 / 3 * 5 / 7);
            CombatField           = new Menu2(ColonyGrid);
            Rectangle OrbitalRect = new Rectangle(5, ColonyGrid.Y, (screenWidth - ColonyGrid.Width) / 2 - 20, ColonyGrid.Height+20);
            OrbitalResources      = new Menu1(OrbitalRect);
            Rectangle psubRect    = new Rectangle(this.AssetsRect.X + 225, this.AssetsRect.Y+23, 185, this.AssetsRect.Height);
            orbitalResourcesSub   = new Submenu(psubRect);
            OrbitSL               = new ScrollList(this.orbitalResourcesSub);

            orbitalResourcesSub.AddTab("In Orbit");

            Empire.Universe.ShipsInCombat.Visible   = false;
            Empire.Universe.PlanetsInCombat.Visible = false;

            LandAll   = Button(orbitalResourcesSub.Menu.X + 20, orbitalResourcesSub.Menu.Y - 2, "Land", "Land All");
            LaunchAll = Button(orbitalResourcesSub.Menu.X + 20, LandAll.Rect.Y - 2 - LandAll.Rect.Height, "LaunchAll", "Launch All");

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
                    if (ship.TroopList.Count <= 0 || (!ship.Carrier.HasTroopBays && !ship.Carrier.HasTransporters && !(p.HasShipyard && p.Owner == ship.loyalty)))  //fbedard
                        continue;
                    int landingLimit = ship.Carrier.AllActiveHangars.Count(ready => ready.IsTroopBay && ready.hangarTimer <= 0);
                    foreach (ShipModule module in ship.Carrier.AllTransporters.Where(module => module.TransporterTimer <= 1))
                        landingLimit += module.TransporterTroopLanding;
                    if (p.HasShipyard && p.Owner == ship.loyalty) landingLimit = ship.TroopList.Count;  //fbedard: Allows to unload if shipyard
                    for (int i = 0; i < ship.TroopList.Count() && landingLimit > 0; i++)
                    {
                        if (ship.TroopList[i] != null && ship.TroopList[i].GetOwner() == ship.loyalty)
                        {
                            this.OrbitSL.AddItem(ship.TroopList[i]);
                            landingLimit--;
                        }
                    }
                }
                else
                {
                    this.OrbitSL.AddItem(ship);
                }
            }
            this.gridPos = new Rectangle(ColonyGrid.X + 20, ColonyGrid.Y + 20, ColonyGrid.Width - 40, ColonyGrid.Height - 40);
            int xsize = this.gridPos.Width / 7;
            int ysize = this.gridPos.Height / 5;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                pgs.ClickRect = new Rectangle(this.gridPos.X + pgs.x * xsize, this.gridPos.Y + pgs.y * ysize, xsize, ysize);
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
                    var ps = new PointSet()
                    {
                        point = new Vector2((float)this.GridRect.X + (float)i * this.widthByRow[row] + this.widthByRow[row] / 2f + this.startXByRow[row], (float)(this.GridRect.Y + this.GridRect.Height) - this.distancesByRow[row]),
                        row = row,
                        column = i
                    };
                    this.pointsList.Add(ps);
                }
            }
            foreach (PointSet ps in this.pointsList)
            {
                foreach (PointSet toCheck in this.pointsList)
                {
                    if (ps.column == toCheck.column && ps.row == toCheck.row - 1)
                    {
                        float distance = Vector2.Distance(ps.point, toCheck.point);
                        Vector2 vtt = toCheck.point - ps.point;
                        vtt = Vector2.Normalize(vtt);
                        Vector2 cPoint = ps.point + ((vtt * distance) / 2f);
                        var cp = new PointSet()
                        {
                            point = cPoint,
                            row = ps.row,
                            column = ps.column
                        };
                        this.CenterPoints.Add(cp);
                    }
                }
            }
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                foreach (PointSet ps in this.CenterPoints)
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
            foreach (PlanetGridSquare pgs in this.p.TilesList)
            {
                pgs.CanAttack = false;
                pgs.CanMoveTo = false;
                if (this.ActiveTroop == null)
                pgs.ShowAttackHover = false;
            }
            if (this.ActiveTroop == null)
            {
                //added by gremlin why two loops? moved hover clear to first loop and move null check to third loop.
                //foreach (PlanetGridSquare pgs in this.p.TilesList)
                //{
                //    pgs.CanMoveTo = false;
                //    pgs.CanAttack = false;
                //    pgs.ShowAttackHover = false;
                //}
            }
            if (this.ActiveTroop != null)
            {
                foreach (PlanetGridSquare pgs in this.p.TilesList)
                {
                    if (pgs.building != null && pgs.building.CombatStrength > 0)
                    {
                        pgs.CanMoveTo = false;
                    }
                    if (this.ActiveTroop != pgs)
                    {
                        continue;
                    }
                    if (this.ActiveTroop.TroopsHere.Count > 0 && this.ActiveTroop.TroopsHere[0].AvailableAttackActions > 0)
                    {
                        foreach (PlanetGridSquare nearby in this.p.TilesList)
                        {
                            if (nearby == pgs)
                            {
                                continue;
                            }
                            int XtotalDistance = Math.Abs(pgs.x - nearby.x);
                            int YtotalDistance = Math.Abs(pgs.y - nearby.y);
                            if ((float)XtotalDistance > pgs.TroopsHere[0].Range || (float)YtotalDistance > pgs.TroopsHere[0].Range || nearby.TroopsHere.Count <= 0 && (nearby.building == null || nearby.building.CombatStrength <= 0))
                            {
                                continue;
                            }
                            if ((nearby.TroopsHere.Count > 0 && nearby.TroopsHere[0].GetOwner() != EmpireManager.Player) || (nearby.building != null && nearby.building.CombatStrength > 0 && p.Owner != EmpireManager.Player))  //fbedard: cannot attack allies !
                                nearby.CanAttack = true;
                        }
                    }
                    else if (this.ActiveTroop.building != null && this.ActiveTroop.building.CombatStrength > 0 && this.ActiveTroop.building.AvailableAttackActions > 0)
                    {
                        foreach (PlanetGridSquare nearby in this.p.TilesList)
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
                    if (this.ActiveTroop.TroopsHere.Count <= 0 || this.ActiveTroop.TroopsHere[0].AvailableMoveActions <= 0)
                    {
                        continue;
                    }
                    foreach (PlanetGridSquare nearby in this.p.TilesList)
                    {
                        if (nearby == pgs)
                        {
                            continue;
                        }
                        int XtotalDistance = Math.Abs(pgs.x - nearby.x);
                        int YtotalDistance = Math.Abs(pgs.y - nearby.y);
                        if ((float)XtotalDistance > pgs.TroopsHere[0].Range || (float)YtotalDistance > pgs.TroopsHere[0].Range || nearby.TroopsHere.Count != 0 || nearby.building != null && (nearby.building == null || nearby.building.CombatStrength != 0))
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
            GameTime gameTime = Game1.Instance.GameTime;
            batch.Draw(ResourceManager.Texture($"PlanetTiles/{p.GetTile()}_tilt"), GridRect, Color.White);
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
                            bCursor.Y = (float)e.Y;
                            batch.Draw(t.TextureDefault, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                            Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                            batch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.White);
                            tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            batch.DrawString(Fonts.Arial8Bold, t.StrengthText, tCursor, Color.Orange);
                        }
                        else
                        {
                            bCursor.Y = (float)e.Y;
                            batch.Draw(t.TextureDefault, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                            Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                            batch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.LightGray);
                            tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            batch.DrawString(Fonts.Arial8Bold, t.StrengthText, tCursor, Color.LightGray);
                        }
                    }
                    e.CheckHover(Input.CursorPosition);
                }
                if (OrbitSL.NumEntries > 0)
                {
                    LandAll.Draw(batch);
                }
                if (p.TroopsHere.Any(mytroops => mytroops.GetOwner() == EmpireManager.Player && mytroops.Launchtimer <= 0f))
                {
                    LaunchAll.Draw(batch);
                }
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

                Texture2D icon = troop.TextureDefault;
                batch.Draw(icon, Input.CursorPosition, null, Color.White, 0f, icon.Center(), 0.65f, SpriteEffects.None, 1f);
            }
            if (Empire.Universe.IsActive)
            {
                ToolTip.Draw(batch);
            }
            batch.End();
            batch.Begin(SpriteBlendMode.Additive);
            lock (GlobalStats.ExplosionLocker)
            {
                foreach (SmallExplosion exp in Explosions)
                {
                    batch.Draw(exp.AnimationTex, exp.grid, Color.White);
                }
            }
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
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Sthis.ScreenManager.SpriteBatchelection"), activeSel, Color.White);
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
                Rectangle troopClickRect = pgs.TroopClickRect;
                if (pgs.TroopsHere[0].MovingTimer > 0f)
                {
                    float amount = 1f - pgs.TroopsHere[0].MovingTimer;
                    troopClickRect.X = (int)MathHelper.Lerp(pgs.TroopsHere[0].GetFromRect().X, pgs.TroopClickRect.X, amount);
                    troopClickRect.Y = (int)MathHelper.Lerp(pgs.TroopsHere[0].GetFromRect().Y, pgs.TroopClickRect.Y, amount);
                    troopClickRect.Width = (int)MathHelper.Lerp(pgs.TroopsHere[0].GetFromRect().Width, pgs.TroopClickRect.Width, amount);
                    troopClickRect.Height = (int)MathHelper.Lerp(pgs.TroopsHere[0].GetFromRect().Height, pgs.TroopClickRect.Height, amount);
                }
                pgs.TroopsHere[0].Draw(batch, troopClickRect);
                var moveRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 38, 12, 12);
                if (pgs.TroopsHere[0].AvailableMoveActions <= 0)
                {
                    int moveTimer = (int)pgs.TroopsHere[0].MoveTimer + 1;
                    HelperFunctions.DrawDropShadowText1(ScreenManager, moveTimer.ToString(), new Vector2((moveRect.X + 4), moveRect.Y), Fonts.Arial12, Color.White);
                }
                else
                {
                    batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Move"), moveRect, Color.White);
                }
                var attackRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 23, 12, 12);
                if (pgs.TroopsHere[0].AvailableAttackActions <= 0)
                {
                    int attackTimer = (int)pgs.TroopsHere[0].AttackTimer + 1;
                    HelperFunctions.DrawDropShadowText1(ScreenManager, attackTimer.ToString(), new Vector2((attackRect.X + 4), attackRect.Y), Fonts.Arial12, Color.White);
                }
                else
                {
                    batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), attackRect, Color.White);
                }

                var strengthRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                batch.FillRectangle(strengthRect, new Color(0, 0, 0, 200));
                batch.DrawRectangle(strengthRect, pgs.TroopsHere[0].GetOwner().EmpireColor);
                var cursor = new Vector2((strengthRect.X + strengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.TroopsHere[0].Strength.ToString("0.")).X / 2f,
                                         (1 + strengthRect.Y + strengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                batch.DrawString(Fonts.Arial12, pgs.TroopsHere[0].Strength.ToString("0."), cursor, Color.White);
                
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

        public override bool HandleInput(InputState input)
        {
            bool selectedSomethingThisFrame = false;
            this.assetsUI.HandleInput(input);  
            if (this.ActiveTroop != null && this.tInfo.HandleInput(input))
            {
                selectedSomethingThisFrame = true;
            }
            this.selector = null;
            this.HoveredSquare = null;
            foreach (PlanetGridSquare pgs in this.p.TilesList)
            {
                if (!pgs.ClickRect.HitTest(input.CursorPosition) || pgs.TroopsHere.Count == 0 && pgs.building == null)
                {
                    continue;
                }
                this.HoveredSquare = pgs;
            }
            if (this.OrbitSL.NumEntries > 0)
            {
                if (!this.LandAll.Rect.HitTest(input.CursorPosition))
                {
                    this.LandAll.State = UIButton.PressState.Default;
                }
                else
                {
                    this.LandAll.State = UIButton.PressState.Hover;
                    if (input.InGameSelect)
                    {
                        GameAudio.PlaySfxAsync("sd_troop_land");
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
                }
            }
            if (p.TroopsHere.Any(mytroops => mytroops.GetOwner() == Empire.Universe.player))
            {
                if (!this.LaunchAll.Rect.HitTest(input.CursorPosition))
                {
                    this.LaunchAll.State = UIButton.PressState.Default;
                }
                else
                {
                    this.LaunchAll.State = UIButton.PressState.Hover;
                    if (input.InGameSelect)
                    {
                        bool play = false;
                        foreach (PlanetGridSquare pgs in this.p.TilesList)
                        {
                            if ( pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Empire.Universe.player || pgs.TroopsHere[0].Launchtimer >= 0)
                            {
                                continue;
                            }
                            try
                            {
                                pgs.TroopsHere[0].AvailableAttackActions = 0;
                                pgs.TroopsHere[0].AvailableMoveActions = 0;
                                pgs.TroopsHere[0].Launchtimer = pgs.TroopsHere[0].MoveTimerBase;
                                pgs.TroopsHere[0].AttackTimer = (float)pgs.TroopsHere[0].AttackTimerBase;
                                pgs.TroopsHere[0].MoveTimer = (float)pgs.TroopsHere[0].MoveTimerBase;
                                play = true;
                                Ship.CreateTroopShipAtPoint(pgs.TroopsHere[0].GetOwner().data.DefaultTroopShip, pgs.TroopsHere[0].GetOwner(), this.p.Center, pgs.TroopsHere[0]);
                                this.p.TroopsHere.Remove(pgs.TroopsHere[0]);
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
                            GameAudio.PlaySfxAsync("sd_troop_takeoff");
                            this.ResetNextFrame = true;

                        }

                        
                    }
                }
            }
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
                        if (!(this.draggedTroop.item is Troop) || (pgs.building != null || pgs.TroopsHere.Count != 0) && (pgs.building == null || pgs.building.CombatStrength != 0 || pgs.TroopsHere.Count != 0))
                        {
                            continue;
                        }
                        GameAudio.PlaySfxAsync("sd_troop_land");
                        pgs.TroopsHere.Add(this.draggedTroop.item as Troop);
                        pgs.TroopsHere[0].AvailableAttackActions = 0;
                        pgs.TroopsHere[0].AvailableMoveActions = 0;
                        pgs.TroopsHere[0].Launchtimer = pgs.TroopsHere[0].MoveTimerBase;
                        pgs.TroopsHere[0].AttackTimer = (float)pgs.TroopsHere[0].AttackTimerBase;
                        pgs.TroopsHere[0].MoveTimer = (float)pgs.TroopsHere[0].MoveTimerBase;

                        this.p.TroopsHere.Add(this.draggedTroop.item as Troop);
                        (this.draggedTroop.item as Troop).SetPlanet(this.p);
                        OrbitSL.Remove(draggedTroop);
                        (this.draggedTroop.item as Troop).GetShip().TroopList.Remove(this.draggedTroop.item as Troop);
                        foundPlace = true;
                        this.draggedTroop = null;
                    }
                    else
                    {
                        if ((pgs.building != null || pgs.TroopsHere.Count != 0) && (pgs.building == null || pgs.building.CombatStrength != 0 || pgs.TroopsHere.Count != 0))
                        {
                            continue;
                        }
                        GameAudio.PlaySfxAsync("sd_troop_land");
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
                            ResourceManager.EventsDict[pgs.building.EventTriggerUID].TriggerPlanetEvent(this.p, pgs.TroopsHere[0].GetOwner(), pgs, Empire.Universe);
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
                    GameAudio.PlaySfxAsync("UI_Misc20");
                }
            }
            foreach (PlanetGridSquare pgs in this.p.TilesList)
            {
                if (!pgs.ClickRect.HitTest(Input.CursorPosition))
                {
                    pgs.highlighted = false;
                }
                else
                {
                    if (!pgs.highlighted)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    pgs.highlighted = true;
                }
                if (pgs.CanAttack)
                {
                    if (!pgs.CanAttack || this.ActiveTroop == null)
                    {
                        continue;
                    }
                    if (!pgs.TroopClickRect.HitTest(Input.CursorPosition))
                    {
                        pgs.ShowAttackHover = false;
                    }
                    else if (this.ActiveTroop.TroopsHere.Count <= 0)
                    {
                        if (this.ActiveTroop.building == null || this.ActiveTroop.building.CombatStrength <= 0 || this.ActiveTroop.building.AvailableAttackActions <= 0 || this.p.Owner == null || this.p.Owner != EmpireManager.Player)
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
                        if (this.ActiveTroop.TroopsHere[0].AvailableAttackActions <= 0 || this.ActiveTroop.TroopsHere[0].GetOwner() != EmpireManager.Player)
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
                                this.ActiveTroop = pgs;
                                this.tInfo.SetPGS(pgs);
                                selectedSomethingThisFrame = true;
                            }
                            else
                            {
                                foreach (PlanetGridSquare p1 in this.p.TilesList)
                                {
                                    p1.CanAttack = false;
                                    p1.CanMoveTo = false;
                                    p1.ShowAttackHover = false;
                                }
                                this.ActiveTroop = pgs;
                                this.tInfo.SetPGS(pgs);
                                selectedSomethingThisFrame = true;
                            }
                        }
                    }
                    else if (pgs.building != null && !pgs.CanMoveTo && pgs.TroopClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                    {
                        if (this.p.Owner != EmpireManager.Player)
                        {
                            this.ActiveTroop = pgs;
                            this.tInfo.SetPGS(pgs);
                            selectedSomethingThisFrame = true;
                        }
                        else
                        {
                            foreach (PlanetGridSquare p1 in this.p.TilesList)
                            {
                                p1.CanAttack = false;
                                p1.CanMoveTo = false;
                                p1.ShowAttackHover = false;
                            }
                            this.ActiveTroop = pgs;
                            this.tInfo.SetPGS(pgs);
                            selectedSomethingThisFrame = true;
                        }
                    }
                    if (ActiveTroop == null || !pgs.CanMoveTo || ActiveTroop.TroopsHere.Count == 0 || !pgs.ClickRect.HitTest(Input.CursorPosition) || this.ActiveTroop.TroopsHere[0].GetOwner() != EmpireManager.Player || Input.LeftMouseReleased || ActiveTroop.TroopsHere[0].AvailableMoveActions <= 0)
                    {
                        continue;
                    }
                    if (pgs.x > this.ActiveTroop.x)
                    {
                        this.ActiveTroop.TroopsHere[0].facingRight = true;
                    }
                    else if (pgs.x < this.ActiveTroop.x)
                    {
                        this.ActiveTroop.TroopsHere[0].facingRight = false;
                    }
                    pgs.TroopsHere.Add(this.ActiveTroop.TroopsHere[0]);
                    Troop troop = pgs.TroopsHere[0];
                    troop.AvailableMoveActions = troop.AvailableMoveActions - 1;
                    pgs.TroopsHere[0].MoveTimer = (float)pgs.TroopsHere[0].MoveTimerBase;
                    pgs.TroopsHere[0].MovingTimer = 0.75f;
                    pgs.TroopsHere[0].SetFromRect(this.ActiveTroop.TroopClickRect);
                    GameAudio.PlaySfxAsync(pgs.TroopsHere[0].MovementCue);
                    this.ActiveTroop.TroopsHere.Clear();
                    this.ActiveTroop = null;
                    this.ActiveTroop = pgs;
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
            this.DetermineAttackAndMove();
            this.hInfo.SetPGS(this.HoveredSquare);
            
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
            Combat c = new Combat()
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
            this.p.ActiveCombats.Add(c);
        }

        public static void StartCombat(PlanetGridSquare Attacker, PlanetGridSquare Defender, Planet p)
        {
            Combat c = new Combat()
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
            if (this.ResetNextFrame)
            {
                this.ResetTroopList();
                this.ResetNextFrame = false;
            }
            foreach (PlanetGridSquare pgs in this.p.TilesList)
            {
                if (pgs.TroopsHere.Count <= 0)
                {
                    continue;
                }
                pgs.TroopsHere[0].Update(elapsedTime);
            }
            lock (GlobalStats.ExplosionLocker)
            {
                foreach (CombatScreen.SmallExplosion exp in this.Explosions)
                {
                    exp.Update(elapsedTime);
                    if (exp.frame < 100)
                    {
                        continue;
                    }
                    this.Explosions.QueuePendingRemoval(exp);
                }
                this.Explosions.ApplyPendingRemovals();
            }
            this.p.ActiveCombats.ApplyPendingRemovals();
            base.Update(elapsedTime);

        }

        private struct PointSet
        {
            public Vector2 point;

            public int row;

            public int column;
        }

        public class SmallExplosion
        {
            private string fmt = "00000.##";
            public string AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
            public string AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
            public Rectangle grid;
            public int frame;

            public Texture2D AnimationTex => ResourceManager.Texture(AnimationTexture);

            public SmallExplosion(int Size)
            {
                switch (Size)
                {
                    case 1:
                    {
                        this.AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
                        this.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
                        return;
                    }
                    case 2:
                    {
                        this.AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
                        this.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
                        return;
                    }
                    case 3:
                    {
                        this.AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
                        this.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
                        return;
                    }
                    case 4:
                    {
                        this.AnimationTexture = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
                        this.AnimationBasePath = "sd_explosion_07a_cc/sd_explosion_07a_cc_";
                        return;
                    }
                    default:
                    {
                        return;
                    }
                }
            }

            public void Update(float elapsedTime)
            {
                if (this.frame < 100)
                {
                    CombatScreen.SmallExplosion smallExplosion = this;
                    smallExplosion.frame = smallExplosion.frame + 1;
                }
                string remainder = this.frame.ToString(this.fmt);
                this.AnimationTexture = string.Concat(this.AnimationBasePath, remainder);
            }
        }

        protected override void Destroy()
        {
            Explosions?.Dispose(ref Explosions);
            base.Destroy();
        }
    }
}