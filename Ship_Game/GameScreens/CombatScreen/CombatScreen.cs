using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Ships;
using System;
using System.Linq;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    // TODO: GroundCombatScreen
    public sealed class CombatScreen : PlanetScreen
    {
        public Planet p;
        Rectangle gridPos;
        Submenu orbitalResourcesSub;

        ScrollList<CombatScreenOrbitListItem> OrbitSL;
        //private bool LowRes;
        PlanetGridSquare HoveredSquare;
        Rectangle SelectedItemRect;
        Rectangle HoveredItemRect;
        Rectangle AssetsRect;
        OrbitalAssetsUIElement assetsUI;
        TroopInfoUIElement tInfo;
        TroopInfoUIElement hInfo;
        UIButton LandAll;
        UIButton LaunchAll;
        Rectangle GridRect;
        Array<PointSet> CenterPoints = new Array<PointSet>();
        Array<PointSet> pointsList   = new Array<PointSet>();
        bool ResetNextFrame;
        public PlanetGridSquare ActiveTile;
        float OrbitalAssetsTimer; // 2 seconds per update

        Array<PlanetGridSquare> ReversedList              = new Array<PlanetGridSquare>();
        BatchRemovalCollection<SmallExplosion> Explosions = new BatchRemovalCollection<SmallExplosion>();

        float[] distancesByRow = { 437f, 379f, 311f, 229f, 128f, 0f };
        float[] widthByRow     = { 110f, 120f, 132f, 144f, 162f, 183f };
        float[] startXByRow    =  { 254f, 222f, 181f, 133f, 74f, 0f };


        public CombatScreen(GameScreen parent, Planet p) : base(parent)
        {
            this.p                = p;
            int screenWidth       = ScreenWidth;
            GridRect              = new Rectangle(screenWidth / 2 - 639, ScreenHeight - 490, 1278, 437);
            Rectangle titleRect   = new Rectangle(screenWidth / 2 - 250, 44, 500, 80);
            var TitleBar              = new Menu2(titleRect);
            var TitlePos              = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString("Ground Combat").X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            SelectedItemRect      = new Rectangle(screenWidth - 240, 100, 225, 205);
            AssetsRect            = new Rectangle(10, 48, 225, 200);
            HoveredItemRect       = new Rectangle(10, 248, 225, 200);
            assetsUI              = new OrbitalAssetsUIElement(AssetsRect, ScreenManager, Empire.Universe, p);
            tInfo                 = new TroopInfoUIElement(SelectedItemRect, ScreenManager, Empire.Universe);
            hInfo                 = new TroopInfoUIElement(HoveredItemRect, ScreenManager, Empire.Universe);
            var colonyGrid  = new Rectangle(screenWidth / 2 - screenWidth * 2 / 3 / 2, 130, screenWidth * 2 / 3, screenWidth * 2 / 3 * 5 / 7);
            var CombatField = new Menu2(colonyGrid);
            var orbitalRect = new Rectangle(5, colonyGrid.Y, (screenWidth - colonyGrid.Width) / 2 - 20, colonyGrid.Height+20);
            var OrbitalResources = new Menu1(orbitalRect);
            var psubRect    = new Rectangle(AssetsRect.X + 225, AssetsRect.Y+23, 200, AssetsRect.Height * 2);
            orbitalResourcesSub = new Submenu(psubRect);

            OrbitSL = Add(new ScrollList<CombatScreenOrbitListItem>(orbitalResourcesSub));
            OrbitSL.OnDoubleClick = OnTroopItemDoubleClick;
            OrbitSL.OnDrag = OnTroopItemDrag;

            orbitalResourcesSub.AddTab("In Orbit");

            LandAll   = Button(orbitalResourcesSub.X + 20, orbitalResourcesSub.Y - 2, "Land All", OnLandAllClicked);
            LaunchAll = Button(orbitalResourcesSub.X + 20, LandAll.Y - 2 - LandAll.Rect.Height, "Launch All", OnLaunchAllClicked);
            LandAll.Tooltip   = Localizer.Token(1951);
            LaunchAll.Tooltip = Localizer.Token(1952);

            gridPos   = new Rectangle(colonyGrid.X + 20, colonyGrid.Y + 20, colonyGrid.Width - 40, colonyGrid.Height - 40);
            int xSize = gridPos.Width / 7;
            int ySize = gridPos.Height / 5;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                pgs.ClickRect = new Rectangle(gridPos.X + pgs.x * xSize, gridPos.Y + pgs.y * ySize, xSize, ySize);
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

        void DetermineAttackAndMove()
        {
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                pgs.CanAttack = false;
                pgs.CanMoveTo = false;
                if (ActiveTile == null)
                    pgs.ShowAttackHover = false;
            }
            if (ActiveTile == null)
            {
                //added by gremlin why two loops? moved hover clear to first loop and move null check to third loop.
                //foreach (PlanetGridSquare pgs in this.p.TilesList)
                //{
                //    pgs.CanMoveTo = false;
                //    pgs.CanAttack = false;
                //    pgs.ShowAttackHover = false;
                //}
            }
            if (ActiveTile != null)
            {
                foreach (PlanetGridSquare pgs in p.TilesList)
                {
                    if (pgs.CombatBuildingOnTile)
                        pgs.CanMoveTo = false;

                    if (ActiveTile != pgs)
                        continue;

                    if (ActiveTile.TroopsAreOnTile && ActiveTile.SingleTroop.CanAttack)
                    {
                        foreach (PlanetGridSquare nearby in p.TilesList)
                        {
                            if (nearby == pgs)
                                continue;

                            int xTotalDistance = Math.Abs(pgs.x - nearby.x);
                            int yTotalDistance = Math.Abs(pgs.y - nearby.y);
                            if (xTotalDistance > pgs.SingleTroop.Range || yTotalDistance > pgs.SingleTroop.Range || nearby.NoTroopsOnTile && (nearby.building == null || nearby.building.CombatStrength <= 0))
                                continue;

                            if ((nearby.TroopsAreOnTile && nearby.SingleTroop.Loyalty != EmpireManager.Player) || (nearby.CombatBuildingOnTile && p.Owner != EmpireManager.Player))  //fbedard: cannot attack allies !
                                nearby.CanAttack = true;
                        }
                    }
                    else if (ActiveTile.CombatBuildingOnTile && ActiveTile.building.CanAttack)
                    {
                        foreach (PlanetGridSquare nearby in p.TilesList)
                        {
                            if (nearby == pgs)
                                continue;

                            int xTotalDistance = Math.Abs(pgs.x - nearby.x);
                            int yTotalDistance = Math.Abs(pgs.y - nearby.y);
                            if (xTotalDistance > 1 || yTotalDistance > 1 || nearby.NoTroopsOnTile && (nearby.building == null || nearby.building.CombatStrength <= 0))
                                continue;

                            if ((nearby.TroopsAreOnTile && nearby.SingleTroop.Loyalty != EmpireManager.Player) || (nearby.CombatBuildingOnTile && p.Owner != EmpireManager.Player))  //fbedard: cannot attack allies !
                                nearby.CanAttack = true;
                        }
                    }
                    if (ActiveTile.NoTroopsOnTile || !ActiveTile.SingleTroop.CanAttack)
                        continue;

                    foreach (PlanetGridSquare nearby in p.TilesList)
                    {
                        if (nearby == pgs)
                            continue;

                        int xTotalDistance = Math.Abs(pgs.x - nearby.x);
                        int yTotalDistance = Math.Abs(pgs.y - nearby.y);
                        if (xTotalDistance > pgs.SingleTroop.Range || yTotalDistance > pgs.SingleTroop.Range || nearby.TroopsAreOnTile || nearby.BuildingOnTile && (nearby.NoBuildingOnTile || nearby.building.CombatStrength != 0))
                            continue;

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

            LaunchAll.Draw(batch);
            LandAll.Draw(batch);

            foreach (PlanetGridSquare pgs in ReversedList)
            {
                if (pgs.BuildingOnTile)
                {
                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture($"Buildings/icon_{pgs.building.Icon}_64x64"), bRect, Color.White);
                }
            }
            foreach (PlanetGridSquare pgs in ReversedList)
            {
                DrawTileIcons(pgs);
                DrawCombatInfo(pgs);
            }
            if (ActiveTile != null)
            {
                tInfo.Draw(gameTime);
            }

            assetsUI.Draw(gameTime);

            if (IsDraggingTroop)
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
            }
            
            base.Draw(batch);
            batch.End();

            batch.Begin(SpriteBlendMode.Additive);
            using (Explosions.AcquireReadLock())
            {
                foreach (SmallExplosion exp in Explosions)
                    exp.Draw(batch);
            }
            batch.End();

            batch.Begin();
        }

        void DrawCombatInfo(PlanetGridSquare pgs)
        {
            if ((ActiveTile == null || ActiveTile != pgs) &&
                (pgs.building == null || pgs.building.CombatStrength <= 0 || ActiveTile == null ||
                 ActiveTile != pgs))
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

        void DrawTileIcons(PlanetGridSquare pgs)
        {
            SpriteBatch batch = ScreenManager.SpriteBatch;

            float width = (pgs.y * 15 + 64);
            if (width > 128f)
                width = 128f;
            if (pgs.building != null && pgs.building.CombatStrength > 0)
                width = 64f;

            pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - (int)width / 2, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - (int)width / 2, (int)width, (int)width);
            if (pgs.TroopsAreOnTile)
            {
                Troop troop = pgs.SingleTroop;
                Rectangle troopClickRect = pgs.TroopClickRect;
                if (troop.MovingTimer > 0f)
                {
                    float amount          = 1f - troop.MovingTimer;
                    troopClickRect.X      = (int)MathHelper.Lerp(troop.FromRect.X, pgs.TroopClickRect.X, amount);
                    troopClickRect.Y      = (int)MathHelper.Lerp(troop.FromRect.Y, pgs.TroopClickRect.Y, amount);
                    troopClickRect.Width  = (int)MathHelper.Lerp(troop.FromRect.Width, pgs.TroopClickRect.Width, amount);
                    troopClickRect.Height = (int)MathHelper.Lerp(troop.FromRect.Height, pgs.TroopClickRect.Height, amount);
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
                if (pgs.SingleTroop.Level > 0)
                {
                    var levelRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 52,
                                                  Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    DrawTroopData(batch, levelRect, troop, troop.Level.ToString(), Color.Gold);
                }

                if (ActiveTile != null && ActiveTile == pgs)
                {
                    if (ActiveTile.SingleTroop.AvailableAttackActions > 0)
                    {
                        foreach (PlanetGridSquare nearby in p.TilesList)
                        {
                            if (nearby == pgs || !nearby.CanAttack)
                                continue;

                            batch.Draw(ResourceManager.Texture("Ground_UI/GC_Potential_Attack"), nearby.ClickRect, Color.White);
                        }
                    }

                    if (ActiveTile.SingleTroop.CanMove)
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
            else if (pgs.BuildingOnTile)
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

                if (ActiveTile != null && ActiveTile == pgs && ActiveTile.building.AvailableAttackActions > 0)
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

        void DrawTroopData(SpriteBatch batch, Rectangle rect, Troop troop, string data, Color color)
        {
            SpriteFont font = Fonts.Arial12;
            batch.FillRectangle(rect, new Color(0, 0, 0, 200));
            batch.DrawRectangle(rect, troop.Loyalty.EmpireColor);
            var cursor = new Vector2((rect.X + rect.Width / 2) - font.MeasureString(troop.Strength.String(1)).X / 2f,
                (1 + rect.Y + rect.Height / 2 - font.LineSpacing / 2));
            batch.DrawString(font, data, cursor, color);
        }

        void OnLandAllClicked(UIButton b)
        {
            GameAudio.TroopLand();
            foreach (CombatScreenOrbitListItem item in OrbitSL.AllEntries)
            {
                item.Troop.TryLandTroop(p);
            }
            OrbitSL.Reset();
        }

        void OnLaunchAllClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (pgs.NoTroopsOnTile || pgs.SingleTroop.Loyalty != Empire.Universe.player || !pgs.SingleTroop.CanMove)
                    continue;

                try
                {

                    pgs.SingleTroop.UpdateAttackActions(-pgs.SingleTroop.MaxStoredActions);
                    pgs.SingleTroop.ResetAttackTimer();
                    Ship troopShip = pgs.SingleTroop.Launch(pgs);
                    if (troopShip != null)
                        play = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Troop Launch Crash");
                }
            }

            if (!play)
                GameAudio.NegativeClick();
            else
            {
                GameAudio.TroopTakeOff();
                ResetNextFrame = true;
            }
        }

        void OnTroopItemDoubleClick(CombatScreenOrbitListItem item)
        {
            TryLandTroop(item, where: null);
        }

        bool IsDraggingTroop;

        void OnTroopItemDrag(CombatScreenOrbitListItem item, DragEvent evt)
        {
            if (evt == DragEvent.Begin)
            {
                IsDraggingTroop = true;
            }
            else if (evt == DragEvent.End)
            {
                IsDraggingTroop = false;
                PlanetGridSquare toLand = p.TilesList.Find(pgs => pgs.NoTroopsOnTile && !pgs.CombatBuildingOnTile);
                TryLandTroop(item, toLand);
            }
        }

        public void TryLandTroop(CombatScreenOrbitListItem item, PlanetGridSquare where)
        {
            if (item.Troop.TryLandTroop(p, where))
            {
                GameAudio.TroopLand();
                OrbitSL.Remove(item);
                OrbitalAssetsTimer = 0;
            }
            else
            {
                GameAudio.NegativeClick();
            }
        }

        public override bool HandleInput(InputState input)
        {
            bool selectedSomethingThisFrame = false;

            assetsUI.HandleInput(input);
            if (Empire.Universe?.Debug == true && input.SpawnRemnant)
            {
                if (EmpireManager.Remnants == null)
                    Log.Warning("Remnant faction missing!");
                else
                {
                    Troop troop = ResourceManager.CreateTroop("Wyvern", EmpireManager.Remnants);
                    if (!troop.TryLandTroop(p))
                        return false; // eek-eek
                }
            }

            if (ActiveTile != null && tInfo.HandleInput(input))
                selectedSomethingThisFrame = true;

            HoveredSquare = null;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (pgs.ClickRect.HitTest(input.CursorPosition) && (pgs.TroopsHere.Count != 0 || pgs.building != null))
                    HoveredSquare = pgs;
            }

            UpdateLaunchAllButton(p.TroopsHere.Count(t => t.Loyalty == Empire.Universe.player && t.CanMove));

            selectedSomethingThisFrame |= HandleInputPlanetGridSquares();
            
            if (ActiveTile != null && !selectedSomethingThisFrame && Input.LeftMouseClick && !SelectedItemRect.HitTest(input.CursorPosition))
                ActiveTile = null;

            if (ActiveTile != null)
                tInfo.pgs = ActiveTile;

            DetermineAttackAndMove();
            hInfo.SetPGS(HoveredSquare);

            return base.HandleInput(input);
        }

        bool HandleInputPlanetGridSquares()
        {
            bool capturedInput = false;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (!pgs.ClickRect.HitTest(Input.CursorPosition))
                    pgs.Highlighted = false;
                else
                {
                    if (!pgs.Highlighted)
                        GameAudio.ButtonMouseOver();

                    pgs.Highlighted = true;
                }

                if (pgs.CanAttack)
                {
                    if (!pgs.CanAttack || ActiveTile == null)
                        continue;

                    if (!pgs.TroopClickRect.HitTest(Input.CursorPosition))
                        pgs.ShowAttackHover = false;
                    else if (ActiveTile.NoTroopsOnTile)
                    {
                        if (ActiveTile.NoBuildingOnTile || ActiveTile.building.CombatStrength <= 0 ||
                            !ActiveTile.building.CanAttack || p.Owner == null || p.Owner != EmpireManager.Player)
                            continue;

                        if (Input.LeftMouseClick)
                        {
                            ActiveTile.building.UpdateAttackActions(-1);
                            ActiveTile.building.ResetAttackTimer();
                            StartCombat(ActiveTile, pgs);
                        }

                        pgs.ShowAttackHover = true;
                    }
                    else
                    {
                        if (!ActiveTile.SingleTroop.CanAttack || ActiveTile.SingleTroop.Loyalty != EmpireManager.Player)
                            continue;

                        if (Input.LeftMouseClick)
                        {
                            if (pgs.x > ActiveTile.x) ActiveTile.SingleTroop.facingRight = true;
                            else if (pgs.x < ActiveTile.x) ActiveTile.SingleTroop.facingRight = false;

                            Troop item = ActiveTile.SingleTroop;
                            item.UpdateAttackActions(-1);
                            ActiveTile.SingleTroop.ResetAttackTimer();
                            Troop availableMoveActions = ActiveTile.SingleTroop;
                            availableMoveActions.UpdateMoveActions(-1);
                            ActiveTile.SingleTroop.ResetMoveTimer();
                            StartCombat(ActiveTile, pgs);
                        }

                        pgs.ShowAttackHover = true;
                    }
                }
                else
                {
                    if (pgs.TroopsAreOnTile)
                    {
                        if (pgs.TroopClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                        {
                            if (pgs.SingleTroop.Loyalty != EmpireManager.Player)
                            {
                                ActiveTile = pgs;
                                tInfo.SetPGS(pgs);
                                capturedInput = true;
                            }
                            else
                            {
                                foreach (PlanetGridSquare p1 in p.TilesList)
                                {
                                    p1.CanAttack = false;
                                    p1.CanMoveTo = false;
                                    p1.ShowAttackHover = false;
                                }

                                ActiveTile = pgs;
                                tInfo.SetPGS(pgs);
                                capturedInput = true;
                            }
                        }
                    }
                    else if (pgs.building != null && !pgs.CanMoveTo && pgs.TroopClickRect.HitTest(Input.CursorPosition) &&
                             Input.LeftMouseClick)
                    {
                        if (p.Owner != EmpireManager.Player)
                        {
                            ActiveTile = pgs;
                            tInfo.SetPGS(pgs);
                            capturedInput = true;
                        }
                        else
                        {
                            foreach (PlanetGridSquare p1 in p.TilesList)
                            {
                                p1.CanAttack = false;
                                p1.CanMoveTo = false;
                                p1.ShowAttackHover = false;
                            }

                            ActiveTile = pgs;
                            tInfo.SetPGS(pgs);
                            capturedInput = true;
                        }
                    }

                    if (ActiveTile == null || !pgs.CanMoveTo || ActiveTile.NoTroopsOnTile ||
                        !pgs.ClickRect.HitTest(Input.CursorPosition) ||
                        ActiveTile.SingleTroop.Loyalty != EmpireManager.Player || Input.LeftMouseReleased ||
                        !ActiveTile.SingleTroop.CanMove)
                        continue;

                    if (Input.LeftMouseClick)
                    {
                        if (pgs.x > ActiveTile.x)
                            ActiveTile.SingleTroop.facingRight = true;
                        else if (pgs.x < ActiveTile.x)
                            ActiveTile.SingleTroop.facingRight = false;

                        pgs.TroopsHere.Add(ActiveTile.SingleTroop);
                        Troop troop = pgs.SingleTroop;
                        troop.UpdateMoveActions(-1);
                        pgs.SingleTroop.ResetMoveTimer();
                        pgs.SingleTroop.MovingTimer = 0.75f;
                        pgs.SingleTroop.SetFromRect(ActiveTile.TroopClickRect);
                        GameAudio.PlaySfxAsync(pgs.SingleTroop.MovementCue);
                        ActiveTile.TroopsHere.Clear();
                        ActiveTile = null;
                        ActiveTile = pgs;
                        pgs.CanMoveTo = false;
                        capturedInput = true;
                    }
                }
            }

            return capturedInput;
        }

        public void StartCombat(PlanetGridSquare attacker, PlanetGridSquare defender)
        {
            Combat c = new Combat { AttackTile = attacker };

            if (attacker.TroopsHere.Count <= 0)
            {
                GameAudio.PlaySfxAsync("sd_weapon_bigcannon_01");
                GameAudio.PlaySfxAsync("uzi_loop");
            }
            else
            {
                attacker.SingleTroop.DoAttack();
                GameAudio.PlaySfxAsync(attacker.SingleTroop.sound_attack);
            }

            c.DefenseTile = defender;
            p.ActiveCombats.Add(c);
        }

        public static void StartCombat(PlanetGridSquare attacker, PlanetGridSquare defender, Planet p)
        {
            Combat c = new Combat { AttackTile = attacker };

            if (attacker.TroopsAreOnTile)
                attacker.SingleTroop.DoAttack();

            c.DefenseTile = defender;
            p.ActiveCombats.Add(c);
        }

        public override void Update(float elapsedTime)
        {
            if (ResetNextFrame)
            {
                OrbitalAssetsTimer = 2;
                ResetNextFrame     = false;
            }

            OrbitSL.Visible = assetsUI.LandTroops.Toggled;

            UpdateOrbitalAssets(elapsedTime);

            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (!pgs.NoTroopsOnTile)
                    pgs.SingleTroop.Update(elapsedTime);
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

        void UpdateOrbitalAssets(float elapsedTime)
        {
            OrbitalAssetsTimer -= elapsedTime;
            if (OrbitalAssetsTimer > 0f)
                return;

            OrbitalAssetsTimer = 2;

            OrbitSL.Reset();
            using (EmpireManager.Player.GetShips().AcquireReadLock())
            {
                foreach (Ship ship in EmpireManager.Player.GetShips())
                {
                    if (ship == null)
                        continue;

                    if (Vector2.Distance(p.Center, ship.Center) >= p.ObjectRadius + ship.Radius + 1500f)
                        continue;

                    if (ship.shipData.Role != ShipData.RoleName.troop)
                    {
                        if (ship.TroopList.Count <= 0 || (!ship.Carrier.HasActiveTroopBays && !ship.Carrier.HasTransporters && !(p.HasSpacePort && p.Owner == ship.loyalty)))  // fbedard
                            continue; // if the ship has no troop bays and there is no other means of landing them (like a spaceport)

                        int landingLimit = LandingLimit(ship);
                        for (int i = 0; i < ship.TroopList.Count && landingLimit > 0; i++)
                        {
                            if (ship.TroopList[i] != null && ship.TroopList[i].Loyalty == ship.loyalty)
                            {
                                OrbitSL.AddItem(new CombatScreenOrbitListItem(ship.TroopList[i]));
                                landingLimit--;
                            }
                        }
                    }
                    else if (ship.AI.State != AI.AIState.Rebase
                             && ship.AI.State != AI.AIState.RebaseToShip
                             && ship.AI.State != AI.AIState.AssaultPlanet)
                    {
                        if (ship.HasTroops)  // this the default 1 troop ship or assault shuttle
                            OrbitSL.AddItem(new CombatScreenOrbitListItem(ship.TroopList[0]));
                    }
                }
            }
            UpdateLandAllButton(OrbitSL.NumEntries);
        }

        int LandingLimit(Ship ship)
        {
            int landingLimit;
            if (p.WeCanLandTroopsViaSpacePort(ship.loyalty))
                landingLimit = ship.TroopList.Count;  // fbedard: Allows to unload all troops if there is a space port
            else
            {
                landingLimit  = ship.Carrier.AllActiveTroopBays.Count(bay => bay.hangarTimer <= 0);
                landingLimit += ship.Carrier.AllTransporters.Where(module => module.TransporterTimer <= 1).Sum(m => m.TransporterTroopLanding);
            }
            return landingLimit;
        }

        void UpdateLandAllButton(int numTroops)
        {
            string text;
            if (numTroops > 0)
            {
                LandAll.Enabled = true;
                text            = $"Land All ({Math.Min(OrbitSL.NumEntries, p.FreeTiles)})";
            }
            else
            {
                LandAll.Enabled = false;
                text            = "Land All";
            }

            LandAll.Text = text;
        }

        void UpdateLaunchAllButton(int numTroopsCanLaunch)
        {
            string text;
            if (numTroopsCanLaunch > 0)
            {
                LaunchAll.Enabled = true;
                text              = $"Launch All ({numTroopsCanLaunch})";
            }
            else
            {
                LaunchAll.Enabled = false;
                text              = "Launch All";
            }
            LaunchAll.Text = text;
        }

        public void AddExplosion(Rectangle grid, int size)
        {
            var exp = new SmallExplosion(grid, size);
            using (Explosions.AcquireWriteLock())
                Explosions.Add(exp);
        }

        struct PointSet
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
                string anim = size <= 3 ? "Textures/sd_explosion_12a_bb" : "Textures/sd_explosion_14a_bb";
                Animation = ResourceManager.RootContent.LoadTextureAtlas(anim);
            }

            public bool Update(float elapsedTime)
            {
                Time += elapsedTime;
                if (Time > Duration)
                    return true;

                int frame = (int)(Time / Duration * Animation.Count) ;
                Frame = frame.Clamped(0, Animation.Count-1);
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