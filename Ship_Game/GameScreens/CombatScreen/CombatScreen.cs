using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public Planet P;
        readonly Vector2 TitlePos;
        readonly Rectangle GridPos;

        readonly ScrollList2<CombatScreenOrbitListItem> OrbitSL;
        PlanetGridSquare HoveredSquare;
        readonly Rectangle SelectedItemRect;
        Rectangle HoveredItemRect;
        Rectangle AssetsRect;
        readonly OrbitalAssetsUIElement AssetsUI;
        readonly TroopInfoUIElement TInfo;
        readonly TroopInfoUIElement HInfo;
        readonly UIButton LandAll;
        readonly UIButton LaunchAll;
        readonly UIButton Bombard;
        readonly Rectangle GridRect;
        readonly Array<PointSet> CenterPoints = new Array<PointSet>();
        readonly Array<PointSet> PointsList   = new Array<PointSet>();
        readonly Array<PlanetGridSquare> MovementTiles = new Array<PlanetGridSquare>();
        readonly Array<PlanetGridSquare> AttackTiles = new Array<PlanetGridSquare>();
        bool ResetNextFrame;
        public PlanetGridSquare ActiveTile;
        float OrbitalAssetsTimer; // X seconds per Orbital Assets update

        readonly Array<PlanetGridSquare> ReversedList              = new Array<PlanetGridSquare>();
        readonly BatchRemovalCollection<SmallExplosion> Explosions = new BatchRemovalCollection<SmallExplosion>();

        readonly float[] DistancesByRow = { 437f, 379f, 311f, 229f, 128f, 0f };
        readonly float[] WidthByRow     = { 110f, 120f, 132f, 144f, 162f, 183f };
        readonly float[] StartXByRow    =  { 254f, 222f, 181f, 133f, 74f, 0f };
        const string BombardDefaultText = "Bombard";

        public CombatScreen(GameScreen parent, Planet p) : base(parent)
        {
            this.P              = p;
            int screenWidth     = ScreenWidth;
            GridRect            = new Rectangle(screenWidth / 2 - 639, ScreenHeight - 490, 1278, 437);
            Rectangle titleRect = new Rectangle(screenWidth / 2 - 250, 44, 500, 80);
            TitlePos            = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Arial20Bold.MeasureString(p.Name).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            AssetsRect          = new Rectangle(10, 48, 225, 200);
            SelectedItemRect    = new Rectangle(10, 250, 225, 250);
            HoveredItemRect     = new Rectangle(10, 248, 225, 200);
            AssetsUI            = new OrbitalAssetsUIElement(AssetsRect, ScreenManager, Empire.Universe, p);
            TInfo               = new TroopInfoUIElement(SelectedItemRect, ScreenManager, Empire.Universe);
            HInfo               = new TroopInfoUIElement(HoveredItemRect, ScreenManager, Empire.Universe);
            var colonyGrid      = new Rectangle(screenWidth / 2 - screenWidth * 2 / 3 / 2, 130, screenWidth * 2 / 3, screenWidth * 2 / 3 * 5 / 7);
            
            int assetsX = AssetsRect.X + 20;

            LandAll   = Button(ButtonStyle.DanButtonBlue, assetsX, AssetsRect.Y + 80, "Land All", OnLandAllClicked);
            LaunchAll = Button(ButtonStyle.DanButtonBlue, assetsX, AssetsRect.Y + 110, "Launch All", OnLaunchAllClicked);
            Bombard   = Button(ButtonStyle.DanButtonBlue, assetsX, AssetsRect.Y + 140, BombardDefaultText, OnBombardClicked);
            LandAll.Tooltip   = GameText.LandAllTroopsListedIn;
            LaunchAll.Tooltip = GameText.LaunchToSpaceAllTroops;
            Bombard.Tooltip   = new LocalizedText(GameText.OrdersAllBombequippedShipsIn).Text;
            LandAll.TextAlign = LaunchAll.TextAlign = Bombard.TextAlign = ButtonTextAlign.Left;

            if (IsPlayerBombing())
                Bombard.Style = ButtonStyle.DanButtonRed;

            var orbitalAssetsTab = new Submenu(assetsX + 220, AssetsRect.Y, 200, AssetsRect.Height * 2, SubmenuStyle.Blue);
            orbitalAssetsTab.AddTab("In Orbit");
            OrbitSL = Add(new ScrollList2<CombatScreenOrbitListItem>(orbitalAssetsTab, ListStyle.Blue));
            OrbitSL.OnDoubleClick = OnTroopItemDoubleClick;
            OrbitSL.OnDragOut = OnTroopItemDrag;
            OrbitSL.EnableDragOutEvents = true;

            GridPos   = new Rectangle(colonyGrid.X + 20, colonyGrid.Y + 20, colonyGrid.Width - 40, colonyGrid.Height - 40);
            int xSize = GridPos.Width / 7;
            int ySize = GridPos.Height / 5;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                pgs.ClickRect = new Rectangle(GridPos.X + pgs.x * xSize, GridPos.Y + pgs.y * ySize, xSize, ySize);
                using (pgs.TroopsHere.AcquireReadLock())
                {
                    foreach (var troop in pgs.TroopsHere)
                    {
                        //@TODO HACK. first frame is getting overwritten or lost somewhere.
                        troop.WhichFrame = troop.first_frame;
                    }
                }
            }
            for (int row = 0; row < 6; row++)
            {
                for (int i = 0; i < 7; i++)
                {
                    var ps = new PointSet
                    {
                        point = new Vector2(GridRect.X + i * WidthByRow[row] + WidthByRow[row] / 2f + StartXByRow[row], GridRect.Y + GridRect.Height - DistancesByRow[row]),
                        row = row,
                        column = i
                    };
                    PointsList.Add(ps);
                }
            }

            foreach (PointSet ps in PointsList)
            {
                foreach (PointSet toCheck in PointsList)
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
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (ActiveTile == null)
                    pgs.ShowAttackHover = false;
            }

            if (ActiveTile != null)
            {
                int range;
                MovementTiles.Clear();
                AttackTiles.Clear();
                if (!ActiveTile.LockOnPlayerTroop(out Troop troop))
                {
                    if (ActiveTile.CombatBuildingOnTile)
                        range = 1;
                    else
                        return; // Nothing on this tile can move or attack
                }
                else
                {
                    range = troop.ActualRange;
                }

                foreach (PlanetGridSquare tile in P.TilesList)
                {
                    if (tile == ActiveTile)
                        continue;

                    int xTotalDistance = Math.Abs(ActiveTile.x - tile.x);
                    int yTotalDistance = Math.Abs(ActiveTile.y - tile.y);
                    int rangeToTile    = Math.Max(xTotalDistance, yTotalDistance);
                    if (rangeToTile <= range && tile.IsTileFree(EmpireManager.Player))
                    {
                        if (!ActiveTile.CombatBuildingOnTile) 
                            MovementTiles.Add(tile); // Movement options only for mobile assets

                        if (tile.LockOnEnemyTroop(EmpireManager.Player, out _) || tile.CombatBuildingOnTile && P.Owner != EmpireManager.Player)
                            AttackTiles.Add(tile);
                    }
                }
            }
        }

        void DrawTroopDragDestinations()
        {
            if (!IsDraggingTroop)
                return;

            foreach (PlanetGridSquare pgs in ReversedList)
            {
                if ((pgs.Building == null && pgs.TroopsHere.Count == 0) ||
                    (pgs.Building != null && pgs.Building.CombatStrength == 0 && pgs.TroopsHere.Count == 0))
                {
                    Vector2 center = pgs.ClickRect.Center();
                    DrawCircle(center, 8f, Color.White, 4f);
                    DrawCircle(center, 6f, Color.Black, 3f);
                }
            }

            PlanetGridSquare toLand = P.FindTileUnderMouse(Input.CursorPosition);
            if (toLand != null)
            {
                DrawCircle(toLand.ClickRect.Center(), 12f, Color.Orange, 2f);
            }
        }

        Color OwnerColor => P.Owner?.EmpireColor ?? Color.Gray;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.Draw(ResourceManager.Texture($"PlanetTiles/{P.PlanetTileId}_tilt"), GridRect, Color.White);
            batch.Draw(ResourceManager.Texture("Ground_UI/grid"), GridRect, Color.White);
            batch.DrawString(Fonts.Arial20Bold, P.Name, TitlePos, OwnerColor);

            LaunchAll.Draw(batch, elapsed);
            LandAll.Draw(batch, elapsed);
            foreach (PlanetGridSquare pgs in ReversedList)
            {
                if (pgs.BuildingOnTile)
                {
                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture($"Buildings/icon_{pgs.Building.Icon}_64x64"), bRect, Color.White);
                }
            }
            foreach (PlanetGridSquare pgs in ReversedList)
            {
                DrawTileIcons(pgs);
                DrawCombatInfo(pgs);
            }
            if (ActiveTile != null)
            {
                TInfo.Draw(batch, elapsed);
            }

            AssetsUI.Draw(batch, elapsed);

            DrawTroopDragDestinations();
            
            base.Draw(batch, elapsed);
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
                (pgs.Building == null || pgs.Building.CombatStrength <= 0 || ActiveTile == null ||
                 ActiveTile != pgs))
                return;

            var activeSel = new Rectangle(pgs.ClickRect.X - 5, pgs.ClickRect.Y - 5, pgs.ClickRect.Width + 10, pgs.ClickRect.Height + 10);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), activeSel, Color.White);
            foreach (PlanetGridSquare nearby in ReversedList)
            {
                if (nearby != pgs && nearby.ShowAttackHover)
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Attack_Confirm"),
                        nearby.ClickRect, Color.White);
            }
        }

        void DrawTileIcons(PlanetGridSquare pgs)
        {
            SpriteBatch batch = ScreenManager.SpriteBatch;

            int width = (pgs.y * 15 + 64).UpperBound(128);
            if (pgs.CombatBuildingOnTile)
                width = 64;

            if (pgs.TroopsAreOnTile)
            {
                using (pgs.TroopsHere.AcquireReadLock())
                {
                    for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                    {
                        Troop troop = pgs.TroopsHere[i];
                        troop.SetCombatScreenRect(pgs, width);
                        Rectangle troopClickRect = troop.ClickRect;
                        if (troop.MovingTimer > 0f)
                        {
                            float amount = 1f - troop.MovingTimer;
                            troopClickRect.X = (int)MathHelper.Lerp(troop.FromRect.X, troop.ClickRect.X, amount);
                            troopClickRect.Y = (int)MathHelper.Lerp(troop.FromRect.Y, troop.ClickRect.Y, amount);
                            troopClickRect.Width = (int)MathHelper.Lerp(troop.FromRect.Width, troop.ClickRect.Width, amount);
                            troopClickRect.Height = (int)MathHelper.Lerp(troop.FromRect.Height, troop.ClickRect.Height, amount);
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
                        if (troop.Level > 0)
                        {
                            var levelRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 52,
                                                          Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                            DrawTroopData(batch, levelRect, troop, troop.Level.ToString(), Color.Gold);
                        }
                        if (ActiveTile != null && ActiveTile == pgs)
                        {
                            if (troop.AvailableAttackActions > 0)
                            {
                                foreach (PlanetGridSquare attackTile in AttackTiles)
                                {
                                    batch.Draw(ResourceManager.Texture("Ground_UI/GC_Potential_Attack"), attackTile.ClickRect, Color.White);
                                }
                            }

                            if (troop.CanMove)
                            {
                                foreach (PlanetGridSquare moveTile in MovementTiles)
                                {
                                    batch.FillRectangle(moveTile.ClickRect, new Color(255, 255, 255, 30));
                                    Vector2 center = moveTile.ClickRect.Center();
                                    DrawCircle(center, 5f, Color.White, 5f);
                                    DrawCircle(center, 5f, Color.Black);
                                }
                            }
                        }
                    }
                }
            }
            else if (pgs.BuildingOnTile)
            {
                if (!pgs.CombatBuildingOnTile)
                {
                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    var strengthRect = new Rectangle(bRect.X + bRect.Width + 2, bRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    batch.FillRectangle(strengthRect, new Color(0, 0, 0, 200));
                    batch.DrawRectangle(strengthRect, P.Owner?.EmpireColor ?? Color.Gray);
                    var cursor = new Vector2((strengthRect.X + strengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.Building.Strength.ToString()).X / 2f,
                                             (1 + strengthRect.Y + strengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12, pgs.Building.Strength.ToString(), cursor, Color.White);
                }
                else
                {
                    var attackRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width + 2, pgs.ClickRect.Y + 23, 12, 12);
                    if (pgs.Building.AvailableAttackActions <= 0)
                    {
                        int num = (int)pgs.Building.AttackTimer + 1;
                        batch.DrawString(Fonts.Arial12, num.ToString(), new Vector2((attackRect.X + 4), attackRect.Y), Color.White);
                    }
                    else
                    {
                        batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), attackRect, Color.White);
                    }
                    var strengthRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width + 2, pgs.ClickRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    batch.FillRectangle(strengthRect, new Color(0, 0, 0, 200));
                    batch.DrawRectangle(strengthRect, P.Owner?.EmpireColor ?? Color.LightGray);
                    var cursor = new Vector2((strengthRect.X + strengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.Building.CombatStrength.ToString()).X / 2f,
                                             (1 + strengthRect.Y + strengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12, pgs.Building.CombatStrength.ToString(), cursor, Color.White);
                }

                if (ActiveTile != null && ActiveTile == pgs && ActiveTile.Building.CanAttack)
                {
                    foreach (PlanetGridSquare attackTile in AttackTiles)
                    {
                        batch.Draw(ResourceManager.Texture("Ground_UI/GC_Potential_Attack"), attackTile.ClickRect, Color.White);
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
            bool instantLand = P.WeCanLandTroopsViaSpacePort(EmpireManager.Player);
            if (instantLand)
                GameAudio.TroopLand();

            for (int i = OrbitSL.AllEntries.Count-1; i >= 0; i--)
            {
                CombatScreenOrbitListItem item = OrbitSL.AllEntries[i];
                if (instantLand)
                {
                    TryLandTroop(item);
                    continue;
                }

                Ship troopShip = item.Troop.HostShip;
                if (troopShip != null
                    && troopShip.AI.State != AI.AIState.Rebase
                    && troopShip.AI.State != AI.AIState.RebaseToShip
                    && troopShip.AI.State != AI.AIState.AssaultPlanet)
                {
                    troopShip.AI.OrderLandAllTroops(P);
                }
            }
            OrbitSL.Reset();
        }

        void OnLaunchAllClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.NoTroopsOnTile || !pgs.LockOnPlayerTroop(out Troop troop) || !troop.CanMove)
                    continue;

                try
                {

                    troop.UpdateAttackActions(-troop.MaxStoredActions);
                    troop.ResetAttackTimer();
                    Ship troopShip = troop.Launch(pgs);
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

        bool IsPlayerBombing()
        {
            if (!TryGetNumBombersCanBomb(out Ship[] bomberList))
                return false;

            return bomberList.Any(s => s.AI.State == AI.AIState.Bombard && s.AI.OrderQueue.Any(o => o.TargetPlanet == P));
        }

        void OnBombardClicked(UIButton b)
        {
            if (!TryGetNumBombersCanBomb(out Ship[] bomberList))
                return;

            var bombingNowList = bomberList.Filter(s => s.AI.State == AI.AIState.Bombard && s.AI.OrderQueue.Any(o => o.TargetPlanet == P));
            if (bombingNowList.Length > 0) // need to cancel bombing
            {
                Bombard.Style = ButtonStyle.DanButtonBlue;
                foreach (Ship bomber in bombingNowList)
                    bomber.AI.OrderToOrbit(P, true);
            }
            else
            {
                // Cancel bombardment 
                Bombard.Style = ButtonStyle.DanButtonRed;
                foreach (Ship bomber in bomberList)
                {
                    bomber.AI.OrderBombardPlanet(P);
                }
            }
        }

        bool TryGetNumBombersCanBomb(out Ship[] bombersList)
        {
            bombersList = P.ParentSystem.ShipList.Filter(s => s.loyalty == EmpireManager.Player
                                                         && s.BombBays.Count > 0
                                                         && s.Center.InRadius(P.Center, P.ObjectRadius + 15000f));

            return bombersList?.Length > 0;
        }

        void OnTroopItemDoubleClick(CombatScreenOrbitListItem item)
        {
            if (P.WeCanLandTroopsViaSpacePort(item.Troop.Loyalty))
                TryLandTroop(item);
            else
                TryLandTroopViaShip(item);
        }

        bool IsDraggingTroop;

        void OnTroopItemDrag(CombatScreenOrbitListItem item, DragEvent evt, bool outside)
        {
            if (evt == DragEvent.Begin)
            {
                IsDraggingTroop = true;
            }
            else if (evt == DragEvent.End)
            {
                IsDraggingTroop = false;
                if (outside)
                {
                    PlanetGridSquare toLand = P.FindTileUnderMouse(Input.CursorPosition);
                    if (P.WeCanLandTroopsViaSpacePort(item.Troop.Loyalty))
                        TryLandTroop(item, toLand);
                    else
                        TryLandTroopViaShip(item);
                }
                else
                {
                    GameAudio.NegativeClick();
                }
            }
        }

        void TryLandTroopViaShip(CombatScreenOrbitListItem item)
        {
            Ship ship = item.Troop.HostShip;
            if (ship != null && ship.Carrier.TryScrambleSingleAssaultShuttle(item.Troop, out Ship shuttle))
                shuttle.AI.OrderLandAllTroops(P);
        }

        void TryLandTroop(CombatScreenOrbitListItem item,
                          PlanetGridSquare where = null)
        {
            if (item.Troop.TryLandTroop(P, where))
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

            AssetsUI.HandleInput(input);
            if (Empire.Universe?.Debug == true && (input.SpawnRemnant || input.SpawnPlayerTroop))
            {
                Empire spawnFor = input.SpawnRemnant ? EmpireManager.Remnants : EmpireManager.Player;
                if (EmpireManager.Remnants == null)
                    Log.Warning("Remnant faction missing!");
                else
                {
                    Troop troop = ResourceManager.CreateTroop("Wyvern", spawnFor);
                    if (!troop.TryLandTroop(P))
                        return false; // eek-eek
                }
            }

            if (ActiveTile != null && TInfo.HandleInput(input))
                selectedSomethingThisFrame = true;

            HoveredSquare = null;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.ClickRect.HitTest(input.CursorPosition) && (pgs.TroopsHere.Count != 0 || pgs.Building != null))
                    HoveredSquare = pgs;
            }

            selectedSomethingThisFrame |= HandleInputPlanetGridSquares();
            
            if (ActiveTile != null && !selectedSomethingThisFrame && Input.LeftMouseClick && !SelectedItemRect.HitTest(input.CursorPosition))
                ActiveTile = null;

            if (ActiveTile != null)
                TInfo.Tile = ActiveTile;

            DetermineAttackAndMove(); 
            HInfo.SetTile(HoveredSquare);

            return base.HandleInput(input);
        }

        bool HandleInputPlanetGridSquares()
        {
            bool capturedInput = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (!pgs.ClickRect.HitTest(Input.CursorPosition))
                    pgs.Highlighted = false;
                else
                {
                    if (!pgs.Highlighted)
                        GameAudio.ButtonMouseOver();

                    pgs.Highlighted = true;
                }

                if (pgs.BuildingOnTile && pgs.ClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                {
                    ActiveTile = pgs;
                    TInfo.SetTile(pgs);
                    capturedInput = true;
                }

                if (pgs.TroopsAreOnTile)
                {
                    using (pgs.TroopsHere.AcquireReadLock())
                    {
                        for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                        {
                            Troop troop = pgs.TroopsHere[i];
                            if (troop.ClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                            {
                                if (P.Owner != EmpireManager.Player)
                                {
                                    ActiveTile = pgs;
                                    TInfo.SetTile(pgs, troop);
                                    capturedInput = true;
                                }
                                else
                                {
                                    foreach (PlanetGridSquare p1 in P.TilesList)
                                    {
                                        p1.ShowAttackHover = false;
                                    }

                                    ActiveTile = pgs;
                                    TInfo.SetTile(pgs, troop);
                                    capturedInput = true;
                                }
                            }
                        }
                    }
                }

                if (ActiveTile == null) 
                    continue;

                if (Input.LeftMouseClick && pgs.ClickRect.HitTest(Input.CursorPosition))
                {
                    if (ActiveTile.CombatBuildingOnTile 
                        && ActiveTile.Building.CanAttack  // Attacking building
                        && pgs.LockOnEnemyTroop(EmpireManager.Player, out Troop enemy))
                    {
                        ActiveTile.Building.UpdateAttackActions(-1);
                        ActiveTile.Building.ResetAttackTimer();
                        StartCombat(ActiveTile.Building, enemy, pgs, P);
                    }
                    else if (ActiveTile.LockOnPlayerTroop(out Troop ourTroop)) // Attacking troops
                    {
                        if (AttackTiles.Contains(pgs))
                        {
                            if (pgs.CombatBuildingOnTile) // Defending building
                            {
                                StartCombat(ourTroop, pgs.Building, pgs, P);
                                capturedInput = true;
                            }
                            else if (pgs.LockOnEnemyTroop(EmpireManager.Player, out Troop enemyTroop))
                            {
                                ourTroop.UpdateAttackActions(-1);
                                ourTroop.ResetAttackTimer();
                                ourTroop.UpdateMoveActions(-1);
                                ourTroop.ResetMoveTimer();
                                StartCombat(ourTroop, enemyTroop, pgs, P);
                                capturedInput = true;
                            }
                        }

                        if (MovementTiles.Contains(pgs))
                        {
                            ourTroop.facingRight = pgs.x > ActiveTile.x;
                            pgs.AddTroop(ourTroop);
                            ourTroop.UpdateMoveActions(-1);
                            ourTroop.ResetMoveTimer();
                            ourTroop.MovingTimer = 0.75f;
                            P.SetInGroundCombat(ourTroop.Loyalty);
                            ourTroop.SetFromRect(ourTroop.ClickRect);
                            GameAudio.PlaySfxAsync(ourTroop.MovementCue);
                            ActiveTile.TroopsHere.Remove(ourTroop);
                            ActiveTile = pgs;
                            MovementTiles.Remove(pgs);
                            capturedInput = true;
                        }
                    }
                }
            }
            
            return capturedInput;
        }

        public static void StartCombat(Troop attacker, Troop defender, PlanetGridSquare defenseTile, Planet planet)
        {
            Combat c = new Combat(attacker, defender, defenseTile, planet);
            attacker.DoAttack();
            planet.ActiveCombats.Add(c);
        }

        public static void StartCombat(Troop attacker, Building defender, PlanetGridSquare defenseTile, Planet planet)
        {
            Combat c = new Combat(attacker, defender, defenseTile, planet);
            attacker.DoAttack();
            planet.ActiveCombats.Add(c);
        }

        public static void StartCombat(Building attacker, Troop defender, PlanetGridSquare defenseTile, Planet planet)
        {
            Combat c = new Combat(attacker, defender, defenseTile, planet);
            planet.ActiveCombats.Add(c);
        }

        public override void Update(float elapsedTime)
        {
            if (ResetNextFrame)
            {
                OrbitalAssetsTimer = 2;
                ResetNextFrame     = false;
            }

            OrbitSL.Visible = OrbitSL.NumEntries > 0;
            UpdateOrbitalAssets(elapsedTime);

            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.TroopsAreOnTile)
                    using (pgs.TroopsHere.AcquireReadLock())
                        for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                            pgs.TroopsHere[i].Update(elapsedTime);
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
            P.ActiveCombats.ApplyPendingRemovals();

            base.Update(elapsedTime);
        }

        void UpdateOrbitalAssets(float elapsedTime)
        {
            OrbitalAssetsTimer -= elapsedTime;
            if (OrbitalAssetsTimer > 0f)
                return;

            OrbitalAssetsTimer = 1;

            Array<Troop> orbitingTroops = GetOrbitingTroops(EmpireManager.Player);

            OrbitSL.RemoveFirstIf(item => !orbitingTroops.ContainsRef(item.Troop));
            Troop[] toAdd = orbitingTroops.Filter(troop => !OrbitSL.Any(item => item.Troop == troop));

            foreach (Troop troop in toAdd)
                OrbitSL.AddItem(new CombatScreenOrbitListItem(troop));

            UpdateLaunchAllButton(P.TroopsHere.Count(t => t.Loyalty == Empire.Universe.player && t.CanMove));
            UpdateLandAllButton(OrbitSL.NumEntries);
            UpdateBombersButton();
        }

        Array<Troop> GetOrbitingTroops(Empire owner)
        {
            // get our friendly ships
            GameplayObject[] orbitingShips = UniverseScreen.Spatial.FindNearby(GameObjectType.Ship,
                                                P.Center, P.ObjectRadius+1500f, maxResults:128, onlyLoyalty:owner);

            // get a list of all the troops on those ships
            var troops = new Array<Troop>();
            foreach (GameplayObject go in orbitingShips)
            {
                var ship = (Ship)go;
                if (ship.shipData.Role != ShipData.RoleName.troop)
                {
                    if (ship.HasOurTroops && (ship.Carrier.HasActiveTroopBays || ship.Carrier.HasTransporters || P.HasSpacePort && P.Owner == ship.loyalty))  // fbedard
                    {
                        int landingLimit = LandingLimit(ship);
                        if (landingLimit > 0)
                            troops.AddRange(ship.GetOurTroops(landingLimit));
                    }
                }
                else if (ship.AI.State != AI.AIState.Rebase
                         && ship.AI.State != AI.AIState.RebaseToShip
                         && ship.AI.State != AI.AIState.AssaultPlanet)
                {
                    // this the default 1 troop ship or assault shuttle
                    if (ship.GetOurFirstTroop(out Troop first))
                        troops.Add(first);
                }
            }
            return troops;
        }


        int LandingLimit(Ship ship)
        {
            int landingLimit;
            if (P.WeCanLandTroopsViaSpacePort(ship.loyalty))
            {
                // fbedard: Allows to unload all troops if there is a space port
                landingLimit = ship.TroopCount;
            }
            else
            {
                landingLimit  = ship.Carrier.AllActiveTroopBays.Count(bay => bay.hangarTimer <= 0);
                landingLimit += ship.Carrier.AllTransporters.Where(module => module.TransporterTimer <= 1).Sum(m => m.TransporterTroopLanding);
            }
            return landingLimit;
        }

        void UpdateLandAllButton(int numTroops)
        {
            if (numTroops > 0)
            {
                LandAll.Enabled = true;
                LandAll.Text    = $"Land All ({Math.Min(OrbitSL.NumEntries, P.GetFreeTiles(EmpireManager.Player))})";
            }
            else
            {
                LandAll.Enabled = false;
                LandAll.Text     = "Land All";
            }

        }

        void UpdateLaunchAllButton(int numTroopsCanLaunch)
        {
            if (numTroopsCanLaunch > 0)
            {
                LaunchAll.Enabled = true;
                LaunchAll.Text    = $"Launch All ({numTroopsCanLaunch})";
            }
            else
            {
                LaunchAll.Enabled = false;
                LaunchAll.Text    = "Launch All";
            }
        }

        void UpdateBombersButton()
        {
            if (P.Owner == null || P.Owner == EmpireManager.Player)
            {
                Bombard.Enabled = false;
                return;
            }

            if (TryGetNumBombersCanBomb(out Ship[] bomberList))
            {
                Bombard.Enabled = true;
                Bombard.Text = $"{BombardDefaultText} ({bomberList.Length})";
            }
            else
            {
                Bombard.Enabled = false;
                Bombard.Text    = BombardDefaultText;
            }
        }

        public bool TryLaunchTroopFromActiveTile()
        {
            PlanetGridSquare tile = ActiveTile;
            if (tile == null || tile.TroopsHere.Count < 1)
                return false;

            Ship launched = tile.TroopsHere[0].Launch(tile);
            if (launched == null)
                return false;
            
            ActiveTile = null; // TODO: Handle ActiveTile in a better way?
            return true;
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
    }
}