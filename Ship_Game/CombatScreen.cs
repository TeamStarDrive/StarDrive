using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
	public class CombatScreen : PlanetScreen, IDisposable
	{
		public static UniverseScreen universeScreen;

		private Ship_Game.ScreenManager ScreenManager;

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

		private List<CombatScreen.PointSet> CenterPoints = new List<CombatScreen.PointSet>();

		private List<CombatScreen.PointSet> pointsList = new List<CombatScreen.PointSet>();

		private bool ResetNextFrame;

		public PlanetGridSquare ActiveTroop;

		private Selector selector;

		private MouseState currentMouse;

		private MouseState previousMouse;

		private ScrollList.Entry draggedTroop;

		private List<PlanetGridSquare> ReversedList = new List<PlanetGridSquare>();

		public BatchRemovalCollection<CombatScreen.SmallExplosion> Explosions = new BatchRemovalCollection<CombatScreen.SmallExplosion>();

		private float[] anglesByColumn = new float[] { (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0), (float)Math.Atan(0) };

		private float[] distancesByRow = new float[] { 437f, 379f, 311f, 229f, 128f, 0f };

		private float[] widthByRow = new float[] { 110f, 120f, 132f, 144f, 162f, 183f };

		private float[] startXByRow = new float[] { 254f, 222f, 181f, 133f, 74f, 0f };

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public CombatScreen(Ship_Game.ScreenManager sm, Planet p)
		{
			this.p = p;
			this.ScreenManager = sm;
			int screenWidth = this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			this.GridRect = new Rectangle(screenWidth / 2 - 639, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 490, 1278, 437);
			Rectangle titleRect = new Rectangle(screenWidth / 2 - 250, 44, 500, 80);
			this.TitleBar = new Menu2(this.ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString("Ground Combat").X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			this.SelectedItemRect = new Rectangle(screenWidth - 240, 48, 225, 205);
			this.AssetsRect = new Rectangle(10, 48, 225, 200);
			this.HoveredItemRect = new Rectangle(10, 248, 225, 200);
			this.assetsUI = new OrbitalAssetsUIElement(this.AssetsRect, this.ScreenManager, PlanetScreen.screen, p);
			this.tInfo = new TroopInfoUIElement(this.SelectedItemRect, this.ScreenManager, PlanetScreen.screen);
			this.hInfo = new TroopInfoUIElement(this.HoveredItemRect, this.ScreenManager, PlanetScreen.screen);
			Rectangle ColonyGrid = new Rectangle(screenWidth / 2 - screenWidth * 2 / 3 / 2, 130, screenWidth * 2 / 3, screenWidth * 2 / 3 * 5 / 7);
			this.CombatField = new Menu2(sm, ColonyGrid);
			Rectangle OrbitalRect = new Rectangle(5, ColonyGrid.Y, (screenWidth - ColonyGrid.Width) / 2 - 20, ColonyGrid.Height+20);
			this.OrbitalResources = new Menu1(this.ScreenManager, OrbitalRect);
			Rectangle psubRect = new Rectangle(this.AssetsRect.X + 225, this.AssetsRect.Y+23, 185, this.AssetsRect.Height);
			this.orbitalResourcesSub = new Submenu(this.ScreenManager, psubRect);
			this.orbitalResourcesSub.AddTab("In Orbit");
			this.OrbitSL = new ScrollList(this.orbitalResourcesSub);
			this.LandAll = new UIButton()
			{
				Rect = new Rectangle(this.orbitalResourcesSub.Menu.X + 20, this.orbitalResourcesSub.Menu.Y - 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Launches = "Land",
				Text = "Land All"
			};
            this.LaunchAll = new UIButton()
            {
                Rect = new Rectangle(this.orbitalResourcesSub.Menu.X + 20, this.LandAll.Rect.Y -2- this.LandAll.Rect.Height, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
                Launches = "LaunchAll",
                Text = "Launch All"
            };
			foreach (Ship ship in CombatScreen.universeScreen.MasterShipList)			
            {
				if (Vector2.Distance(p.Position, ship.Center) >= 4000f || ship.loyalty != EmpireManager.GetEmpireByName(CombatScreen.universeScreen.PlayerLoyalty))
				{
					continue;
				}
				if (ship.Role != "troop")
				{
					if (ship.TroopList.Count <= 0 || !ship.HasTroopBay && !ship.hasTransporter)
						continue;
                    int LandingLimit = ship.GetHangars().Where(ready => ready.IsTroopBay && ready.hangarTimer <= 0).Count();
                    foreach (ShipModule module in ship.Transporters.Where(module => module.TransporterTimer <= 1))
                        LandingLimit += module.TransporterTroopLanding;
                    for (int i = 0; i < ship.TroopList.Count() && LandingLimit > 0; i++)
                    {
                        if (ship.TroopList[i] != null && ship.TroopList[i].GetOwner() == ship.loyalty)
                        {
                            this.OrbitSL.AddItem(ship.TroopList[i]);
                            LandingLimit--;
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
			}
			for (int row = 0; row < 6; row++)
			{
				for (int i = 0; i < 7; i++)
				{
					CombatScreen.PointSet ps = new CombatScreen.PointSet()
					{
						point = new Vector2((float)this.GridRect.X + (float)i * this.widthByRow[row] + this.widthByRow[row] / 2f + this.startXByRow[row], (float)(this.GridRect.Y + this.GridRect.Height) - this.distancesByRow[row]),
						row = row,
						column = i
					};
					this.pointsList.Add(ps);
				}
			}
			foreach (CombatScreen.PointSet ps in this.pointsList)
			{
				foreach (CombatScreen.PointSet toCheck in this.pointsList)
				{
					if (ps.column != toCheck.column || ps.row != toCheck.row - 1)
					{
						continue;
					}
					float Distance = Vector2.Distance(ps.point, toCheck.point);
					Vector2 vtt = toCheck.point - ps.point;
					vtt = Vector2.Normalize(vtt);
					Vector2 cPoint = ps.point + ((vtt * Distance) / 2f);
					CombatScreen.PointSet cp = new CombatScreen.PointSet()
					{
						point = cPoint,
						row = ps.row,
						column = ps.column
					};
					this.CenterPoints.Add(cp);
				}
			}
			foreach (PlanetGridSquare pgs in p.TilesList)
			{
				foreach (CombatScreen.PointSet ps in this.CenterPoints)
				{
					if (pgs.x != ps.column || pgs.y != ps.row)
					{
						continue;
					}
					pgs.ClickRect = new Rectangle((int)ps.point.X - 32, (int)ps.point.Y - 32, 64, 64);
				}
			}
			foreach (PlanetGridSquare pgs in p.TilesList)
			{
				this.ReversedList.Add(pgs);
			}
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

		public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
		{
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("PlanetTiles/", this.p.GetTile(), "_tilt")], this.GridRect, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/grid"], this.GridRect, Color.White);
			if (this.assetsUI.LandTroops.Toggled)
			{
				this.OrbitSL.Draw(this.ScreenManager.SpriteBatch);
				Vector2 bCursor = new Vector2((float)(this.orbitalResourcesSub.Menu.X + 25), 350f);
				for (int i = this.OrbitSL.indexAtTop; i < this.OrbitSL.Copied.Count && i < this.OrbitSL.indexAtTop + this.OrbitSL.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.OrbitSL.Copied[i];
					if (e.item is Ship)
					{
						if ((e.item as Ship).TroopList.Count == 0)
						{
							//goto Label0;  //jumping to the end of a for loop by goto is bad mojo
                            continue;
						}
						Troop t = (e.item as Ship).TroopList[0];
						if (e.clickRectHover != 0)
						{
							bCursor.Y = (float)e.clickRect.Y;
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", t.TexturePath)], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
							Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.White);
							tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Strength: ", t.Strength.ToString("0.")), tCursor, Color.Orange);
						}
						else
						{
							bCursor.Y = (float)e.clickRect.Y;
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", t.TexturePath)], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
							Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.LightGray);
							tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Strength: ", t.Strength.ToString("0.")), tCursor, Color.LightGray);
						}
					}
					else if (e.item is Troop)
					{
						Troop t = e.item as Troop;
						if (e.clickRectHover != 0)
						{
							bCursor.Y = (float)e.clickRect.Y;
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", t.TexturePath)], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
							Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.White);
							tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Strength: ", t.Strength.ToString("0.")), tCursor, Color.Orange);
						}
						else
						{
							bCursor.Y = (float)e.clickRect.Y;
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", t.TexturePath)], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
							Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.LightGray);
							tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Strength: ", t.Strength.ToString("0.")), tCursor, Color.LightGray);
						}
					}
					if (HelperFunctions.CheckIntersection(e.clickRect, new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y)))
					{
						e.clickRectHover = 1;
					}
                //Label0:
                 //   continue;
				}
				if (this.OrbitSL.Entries.Count > 0)
				{
					this.LandAll.Draw(this.ScreenManager.SpriteBatch);
                    
				}
                if (p.TroopsHere.Where(mytroops => mytroops.GetOwner() == universeScreen.player && mytroops.Launchtimer<=0).Count() > 0)
                {
                    this.LaunchAll.Draw(this.ScreenManager.SpriteBatch);
                }
			}
			foreach (PlanetGridSquare pgs in this.ReversedList)
			{
				if (pgs.building == null)
				{
					continue;
				}
				Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", pgs.building.Icon, "_64x64")], bRect, Color.White);
			}
			foreach (PlanetGridSquare pgs in this.ReversedList)
			{
				this.DrawPGSIcons(pgs);
				this.DrawCombatInfo(pgs);
			}
			if (this.ActiveTroop != null)
			{
				this.tInfo.Draw(gameTime);
			}
			PlanetGridSquare hoveredSquare = this.HoveredSquare;
			this.assetsUI.Draw(gameTime);
			if (this.draggedTroop != null)
			{
				foreach (PlanetGridSquare pgs in this.ReversedList)
				{
					if ((pgs.building != null || pgs.TroopsHere.Count != 0) && (pgs.building == null || pgs.building.CombatStrength != 0 || pgs.TroopsHere.Count != 0))
					{
						continue;
					}
					Vector2 center = new Vector2((float)(pgs.ClickRect.X + pgs.ClickRect.Width / 2), (float)(pgs.ClickRect.Y + pgs.ClickRect.Height / 2));
					Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, 5f, 50, Color.White, 5f);
					Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, 5f, 50, Color.Black);
				}
				if (!(this.draggedTroop.item is Ship))
				{
					Vector2 Origin = new Vector2((float)(ResourceManager.TextureDict[string.Concat("Troops/", (this.draggedTroop.item as Troop).TexturePath)].Width / 2), (float)(ResourceManager.TextureDict[string.Concat("Troops/", (this.draggedTroop.item as Troop).TexturePath)].Height / 2));
					Rectangle? nullable = null;
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", (this.draggedTroop.item as Troop).TexturePath)], new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y), nullable, Color.White, 0f, Origin, 0.65f, SpriteEffects.None, 1f);
				}
				else if ((this.draggedTroop.item as Ship).TroopList.Count > 0)
				{
					Vector2 Origin = new Vector2((float)(ResourceManager.TextureDict[string.Concat("Troops/", (this.draggedTroop.item as Ship).TroopList[0].TexturePath)].Width / 2), (float)(ResourceManager.TextureDict[string.Concat("Troops/", (this.draggedTroop.item as Ship).TroopList[0].TexturePath)].Height / 2));
					Rectangle? nullable1 = null;
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", (this.draggedTroop.item as Ship).TroopList[0].TexturePath)], new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y), nullable1, Color.White, 0f, Origin, 0.65f, SpriteEffects.None, 1f);
				}
			}
			if (CombatScreen.universeScreen.IsActive)
			{
				ToolTip.Draw(this.ScreenManager);
			}
			this.ScreenManager.SpriteBatch.End();
			this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
			lock (GlobalStats.ExplosionLocker)
			{
				foreach (CombatScreen.SmallExplosion exp in this.Explosions)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[exp.AnimationTexture], exp.grid, Color.White);
				}
			}
			this.ScreenManager.SpriteBatch.End();
			this.ScreenManager.SpriteBatch.Begin();
		}

		private void DrawCombatInfo(PlanetGridSquare pgs)
		{
			if (this.ActiveTroop != null && this.ActiveTroop == pgs || pgs.building != null && pgs.building.CombatStrength > 0 && this.ActiveTroop != null && this.ActiveTroop == pgs)
			{
				Rectangle ActiveSelectionRect = new Rectangle(pgs.TroopClickRect.X - 5, pgs.TroopClickRect.Y - 5, pgs.TroopClickRect.Width + 10, pgs.TroopClickRect.Height + 10);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/GC_Square Selection"], ActiveSelectionRect, Color.White);
				foreach (PlanetGridSquare nearby in this.ReversedList)
				{
					if (nearby == pgs || !nearby.ShowAttackHover)
					{
						continue;
					}
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/GC_Attack_Confirm"], nearby.TroopClickRect, Color.White);
				}
			}
		}

		public void DrawOld(SpriteBatch spriteBatch, GameTime gameTime)
		{
			this.TitleBar.Draw();
			this.CombatField.Draw();
			this.OrbitalResources.Draw();
			this.orbitalResourcesSub.Draw();
			this.OrbitSL.Draw(this.ScreenManager.SpriteBatch);
			Vector2 bCursor = new Vector2((float)(this.orbitalResourcesSub.Menu.X + 20), (float)(this.orbitalResourcesSub.Menu.Y + 45));
			for (int i = this.OrbitSL.indexAtTop; i < this.OrbitSL.Entries.Count && i < this.OrbitSL.indexAtTop + this.OrbitSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.OrbitSL.Entries[i];
				Troop t = (e.item as Ship).TroopList[0];
				if (e.clickRectHover != 0)
				{
					bCursor.Y = (float)e.clickRect.Y;
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", t.TexturePath)], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Strength: ", t.Strength.ToString("0.")), tCursor, Color.Orange);
				}
				else
				{
					bCursor.Y = (float)e.clickRect.Y;
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", t.TexturePath)], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Strength: ", t.Strength.ToString("0.")), tCursor, Color.Orange);
				}
				if (HelperFunctions.CheckIntersection(e.clickRect, new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y)))
				{
					e.clickRectHover = 1;
				}
			}
			if (this.selector != null)
			{
				this.selector.Draw();
			}
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, "Ground Combat", this.TitlePos, Color.LightPink);
			Rectangle tilebg = new Rectangle(this.gridPos.X, this.gridPos.Y + 1, this.gridPos.Width - 4, this.gridPos.Height - 3);
			if (this.p.Type != "Terran")
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetTiles/tiles_barren_1"], tilebg, Color.White);
			}
			else
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetTiles/tiles_terran_1"], tilebg, Color.White);
			}
			foreach (PlanetGridSquare pgs in this.ReversedList)
			{
				if (!pgs.Habitable)
				{
					Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, pgs.ClickRect, new Color(0, 0, 0, 200));
				}
				Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, pgs.ClickRect, new Color(211, 211, 211, 70), 2f);
				if (pgs.building == null || pgs.building.CombatStrength <= 0)
				{
					continue;
				}
				Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", pgs.building.Icon, "_64x64")], bRect, Color.White);
			}
			this.DetermineAttackAndMove();
			foreach (PlanetGridSquare pgs in this.ReversedList)
			{
				this.DrawPGSIcons(pgs);
				this.DrawCombatInfo(pgs);
			}
			foreach (PlanetGridSquare pgs in this.ReversedList)
			{
				if (!pgs.highlighted)
				{
					continue;
				}
				Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, pgs.ClickRect, Color.White, 2f);
			}
			if (this.draggedTroop != null)
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", (this.draggedTroop.item as Ship).TroopList[0].TexturePath)], new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y), Color.White);
			}
			this.ScreenManager.SpriteBatch.End();
			this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
			lock (GlobalStats.ExplosionLocker)
			{
				foreach (CombatScreen.SmallExplosion exp in this.Explosions)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[exp.AnimationTexture], exp.grid, Color.White);
				}
			}
			this.ScreenManager.SpriteBatch.End();
			this.ScreenManager.SpriteBatch.Begin();
		}

		private void DrawPGSIcons(PlanetGridSquare pgs)
		{
			float width = (float)(pgs.y * 15 + 64);
			if (width > 128f)
			{
				width = 128f;
			}
			if (pgs.building != null && pgs.building.CombatStrength > 0)
			{
				width = 64f;
			}
			pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - (int)width / 2, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - (int)width / 2, (int)width, (int)width);
			if (pgs.TroopsHere.Count > 0)
			{
				Rectangle TroopClickRect = pgs.TroopClickRect;
				if (pgs.TroopsHere[0].MovingTimer > 0f)
				{
					float amount = 1f - pgs.TroopsHere[0].MovingTimer;
					TroopClickRect.X = (int)MathHelper.Lerp((float)pgs.TroopsHere[0].GetFromRect().X, (float)pgs.TroopClickRect.X, amount);
					TroopClickRect.Y = (int)MathHelper.Lerp((float)pgs.TroopsHere[0].GetFromRect().Y, (float)pgs.TroopClickRect.Y, amount);
					TroopClickRect.Width = (int)MathHelper.Lerp((float)pgs.TroopsHere[0].GetFromRect().Width, (float)pgs.TroopClickRect.Width, amount);
					TroopClickRect.Height = (int)MathHelper.Lerp((float)pgs.TroopsHere[0].GetFromRect().Height, (float)pgs.TroopClickRect.Height, amount);
				}
				pgs.TroopsHere[0].Draw(this.ScreenManager.SpriteBatch, TroopClickRect);
				Rectangle MoveRect = new Rectangle(TroopClickRect.X + TroopClickRect.Width + 2, TroopClickRect.Y + 38, 12, 12);
				if (pgs.TroopsHere[0].AvailableMoveActions <= 0)
				{
					int moveTimer = (int)pgs.TroopsHere[0].MoveTimer + 1;
					HelperFunctions.DrawDropShadowText1(this.ScreenManager, moveTimer.ToString(), new Vector2((float)(MoveRect.X + 4), (float)MoveRect.Y), Fonts.Arial12, Color.White);
				}
				else
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Move"], MoveRect, Color.White);
				}
				Rectangle AttackRect = new Rectangle(TroopClickRect.X + TroopClickRect.Width + 2, TroopClickRect.Y + 23, 12, 12);
				if (pgs.TroopsHere[0].AvailableAttackActions <= 0)
				{
					int attackTimer = (int)pgs.TroopsHere[0].AttackTimer + 1;
					HelperFunctions.DrawDropShadowText1(this.ScreenManager, attackTimer.ToString(), new Vector2((float)(AttackRect.X + 4), (float)AttackRect.Y), Fonts.Arial12, Color.White);
				}
				else
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], AttackRect, Color.White);
				}
				Rectangle StrengthRect = new Rectangle(TroopClickRect.X + TroopClickRect.Width + 2, TroopClickRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
				Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, StrengthRect, new Color(0, 0, 0, 200));
				Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, StrengthRect, pgs.TroopsHere[0].GetOwner().EmpireColor);
                Vector2 cursor = new Vector2((float)(StrengthRect.X + StrengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.TroopsHere[0].Strength.ToString("0.")).X / 2f, (float)(1 + StrengthRect.Y + StrengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, pgs.TroopsHere[0].Strength.ToString("0."), cursor, Color.White);
				if (this.ActiveTroop != null && this.ActiveTroop == pgs)
				{
					if (this.ActiveTroop.TroopsHere[0].AvailableAttackActions > 0)
					{
						foreach (PlanetGridSquare nearby in this.p.TilesList)
						{
							if (nearby == pgs || !nearby.CanAttack)
							{
								continue;
							}
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/GC_Potential_Attack"], nearby.ClickRect, Color.White);
						}
					}
					if (this.ActiveTroop.TroopsHere[0].AvailableMoveActions > 0)
					{
						foreach (PlanetGridSquare nearby in this.p.TilesList)
						{
							if (nearby == pgs || !nearby.CanMoveTo)
							{
								continue;
							}
							Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, nearby.ClickRect, new Color(255, 255, 255, 30));
							Vector2 center = new Vector2((float)(nearby.ClickRect.X + nearby.ClickRect.Width / 2), (float)(nearby.ClickRect.Y + nearby.ClickRect.Height / 2));
							Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, 5f, 50, Color.White, 5f);
							Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, 5f, 50, Color.Black);
						}
					}
				}
			}
			else if (pgs.building != null)
			{
				if (pgs.building.CombatStrength <= 0)
				{
					Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
					Rectangle StrengthRect = new Rectangle(bRect.X + bRect.Width + 2, bRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
					Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, StrengthRect, new Color(0, 0, 0, 200));
					Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, StrengthRect, (this.p.Owner != null ? this.p.Owner.EmpireColor : Color.Gray));
					Vector2 cursor = new Vector2((float)(StrengthRect.X + StrengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.building.Strength.ToString()).X / 2f, (float)(1 + StrengthRect.Y + StrengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, pgs.building.Strength.ToString(), cursor, Color.White);
				}
				else
				{
					Rectangle AttackRect = new Rectangle(pgs.TroopClickRect.X + pgs.TroopClickRect.Width + 2, pgs.TroopClickRect.Y + 23, 12, 12);
					if (pgs.building.AvailableAttackActions <= 0)
					{
						int num = (int)pgs.building.AttackTimer + 1;
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, num.ToString(), new Vector2((float)(AttackRect.X + 4), (float)AttackRect.Y), Color.White);
					}
					else
					{
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], AttackRect, Color.White);
					}
					Rectangle StrengthRect = new Rectangle(pgs.TroopClickRect.X + pgs.TroopClickRect.Width + 2, pgs.TroopClickRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
					Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, StrengthRect, new Color(0, 0, 0, 200));
					Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, StrengthRect, (this.p.Owner != null ? this.p.Owner.EmpireColor : Color.LightGray));
					Vector2 cursor = new Vector2((float)(StrengthRect.X + StrengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.building.CombatStrength.ToString()).X / 2f, (float)(1 + StrengthRect.Y + StrengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, pgs.building.CombatStrength.ToString(), cursor, Color.White);
				}
				if (this.ActiveTroop != null && this.ActiveTroop == pgs && this.ActiveTroop.building.AvailableAttackActions > 0)
				{
					foreach (PlanetGridSquare nearby in this.p.TilesList)
					{
						if (nearby == pgs || !nearby.CanAttack)
						{
							continue;
						}
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/GC_Potential_Attack"], nearby.ClickRect, Color.White);
					}
				}
			}
		}

		public override void HandleInput(InputState input)
		{
			this.currentMouse = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			bool SelectedSomethingThisFrame = false;
			this.assetsUI.HandleInput(input);
			if (this.ActiveTroop != null && this.tInfo.HandleInput(input))
			{
				SelectedSomethingThisFrame = true;
			}
			this.selector = null;
			this.HoveredSquare = null;
			foreach (PlanetGridSquare pgs in this.p.TilesList)
			{
				if (!HelperFunctions.CheckIntersection(pgs.ClickRect, MousePos) || pgs.TroopsHere.Count == 0 && pgs.building == null)
				{
					continue;
				}
				this.HoveredSquare = pgs;
			}
			if (this.OrbitSL.Entries.Count > 0)
			{
				if (!HelperFunctions.CheckIntersection(this.LandAll.Rect, input.CursorPosition))
				{
					this.LandAll.State = UIButton.PressState.Normal;
				}
				else
				{
					this.LandAll.State = UIButton.PressState.Hover;
					if (input.InGameSelect)
					{
						AudioManager.PlayCue("sd_troop_land");
						for (int i = 0; i < this.OrbitSL.Entries.Count; i++)
						{
							ScrollList.Entry e = this.OrbitSL.Entries[i];
							if (e.item is Ship)
							{
								(e.item as Ship).GetAI().OrderLandAllTroops(this.p);
							}
							else if (e.item is Troop)
							{
								(e.item as Troop).GetShip().TroopList.Remove(e.item as Troop);
								this.p.AssignTroopToTile(e.item as Troop);
							}
						}
                        this.OrbitSL.Entries.Clear();
					}
                    
				}
			}
            if (p.TroopsHere.Where(mytroops => mytroops.GetOwner() == universeScreen.player).Count() > 0)
            {
                if (!HelperFunctions.CheckIntersection(this.LaunchAll.Rect, input.CursorPosition))
                {
                    this.LaunchAll.State = UIButton.PressState.Normal;
                }
                else
                {
                    this.LaunchAll.State = UIButton.PressState.Hover;
                    if (input.InGameSelect)
                    {
                        bool play = false;
                        foreach (PlanetGridSquare pgs in this.p.TilesList)
                        {
                            if ( pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() !=universeScreen.player || pgs.TroopsHere[0].Launchtimer >= 0)
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
                                ResourceManager.CreateTroopShipAtPoint((pgs.TroopsHere[0].GetOwner().data.DefaultTroopShip != null) ? pgs.TroopsHere[0].GetOwner().data.DefaultTroopShip : pgs.TroopsHere[0].GetOwner().data.DefaultSmallTransport, pgs.TroopsHere[0].GetOwner(), this.p.Position, pgs.TroopsHere[0]);
                                this.p.TroopsHere.Remove(pgs.TroopsHere[0]);
                                pgs.TroopsHere[0].SetPlanet(null);
                                pgs.TroopsHere.Clear();
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine("Troop Launch Crash");
                            }
                        }
                        if (play)
                        {
                            AudioManager.PlayCue("sd_troop_takeoff");
                            this.ResetNextFrame = true;

                        }

                        
                    }
                }
            }
			this.OrbitSL.HandleInput(input);
			foreach (ScrollList.Entry e in this.OrbitSL.Copied)
			{
				if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					this.selector = new Selector(this.ScreenManager, e.clickRect);
					if (this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
					{
						continue;
					}
					this.draggedTroop = e;
				}
			}
			if (this.draggedTroop != null && this.currentMouse.LeftButton == ButtonState.Released && this.previousMouse.LeftButton == ButtonState.Pressed)
			{
				bool foundPlace = false;
				foreach (PlanetGridSquare pgs in this.p.TilesList)
				{
					if (!HelperFunctions.CheckIntersection(pgs.ClickRect, MousePos))
					{
						continue;
					}
					if (!(this.draggedTroop.item is Ship) || (this.draggedTroop.item as Ship).TroopList.Count <= 0)
					{
						if (!(this.draggedTroop.item is Troop) || (pgs.building != null || pgs.TroopsHere.Count != 0) && (pgs.building == null || pgs.building.CombatStrength != 0 || pgs.TroopsHere.Count != 0))
						{
							continue;
						}
                        try
                        {
                            AudioManager.PlayCue("sd_troop_land");
                            pgs.TroopsHere.Add(this.draggedTroop.item as Troop);
                            pgs.TroopsHere[0].AvailableAttackActions = 0;
                            pgs.TroopsHere[0].AvailableMoveActions = 0;
                            pgs.TroopsHere[0].Launchtimer = pgs.TroopsHere[0].MoveTimerBase;
                            pgs.TroopsHere[0].AttackTimer = (float)pgs.TroopsHere[0].AttackTimerBase;
                            pgs.TroopsHere[0].MoveTimer = (float)pgs.TroopsHere[0].MoveTimerBase;

                            this.p.TroopsHere.Add(this.draggedTroop.item as Troop);
                            (this.draggedTroop.item as Troop).SetPlanet(this.p);
                            this.OrbitSL.Entries.Remove(this.draggedTroop);
                            (this.draggedTroop.item as Troop).GetShip().TroopList.Remove(this.draggedTroop.item as Troop);
                            foundPlace = true;
                            this.draggedTroop = null;
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine("Troop Launch Crash");
                        }
					}
					else
					{
						if ((pgs.building != null || pgs.TroopsHere.Count != 0) && (pgs.building == null || pgs.building.CombatStrength != 0 || pgs.TroopsHere.Count != 0))
						{
							continue;
						}
                        try
                        {
                            AudioManager.PlayCue("sd_troop_land");
                            pgs.TroopsHere.Add((this.draggedTroop.item as Ship).TroopList[0]);
                            pgs.TroopsHere[0].AvailableAttackActions = 0;
                            pgs.TroopsHere[0].AvailableMoveActions = 0;
                            pgs.TroopsHere[0].Launchtimer = pgs.TroopsHere[0].MoveTimerBase;
                            pgs.TroopsHere[0].AttackTimer = (float)pgs.TroopsHere[0].AttackTimerBase;
                            pgs.TroopsHere[0].MoveTimer = (float)pgs.TroopsHere[0].MoveTimerBase;
                            this.p.TroopsHere.Add((this.draggedTroop.item as Ship).TroopList[0]);
                            (this.draggedTroop.item as Ship).TroopList[0].SetPlanet(this.p);
                            if (pgs.building != null && pgs.building.EventTriggerUID != "" && pgs.TroopsHere.Count > 0 && !pgs.TroopsHere[0].GetOwner().isFaction && !pgs.TroopsHere[0].GetOwner().MinorRace)
                            {
                                ResourceManager.EventsDict[pgs.building.EventTriggerUID].TriggerPlanetEvent(this.p, pgs.TroopsHere[0].GetOwner(), pgs, EmpireManager.GetEmpireByName(CombatScreen.universeScreen.PlayerLoyalty), CombatScreen.universeScreen);
                            }
                            this.OrbitSL.Entries.Remove(this.draggedTroop);
                            this.OrbitSL.Copied.Remove(this.draggedTroop);
                            (this.draggedTroop.item as Ship).QueueTotalRemoval();
                            foundPlace = true;
                            this.draggedTroop = null;
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine("Troop Launch Crash");
                        }
					}
				}
				if (!foundPlace)
				{
					this.draggedTroop = null;
					AudioManager.PlayCue("UI_Misc20");
				}
			}
			foreach (PlanetGridSquare pgs in this.p.TilesList)
			{
				if (!HelperFunctions.CheckIntersection(pgs.ClickRect, MousePos))
				{
					pgs.highlighted = false;
				}
				else
				{
					if (!pgs.highlighted)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					pgs.highlighted = true;
				}
				if (pgs.CanAttack)
				{
					if (!pgs.CanAttack || this.ActiveTroop == null)
					{
						continue;
					}
					if (!HelperFunctions.CheckIntersection(pgs.TroopClickRect, MousePos))
					{
						pgs.ShowAttackHover = false;
					}
					else if (this.ActiveTroop.TroopsHere.Count <= 0)
					{
						if (this.ActiveTroop.building == null || this.ActiveTroop.building.CombatStrength <= 0 || this.ActiveTroop.building.AvailableAttackActions <= 0 || this.p.Owner == null || this.p.Owner != EmpireManager.GetEmpireByName(PlanetScreen.screen.PlayerLoyalty))
						{
							continue;
						}
						if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
						{
							Building activeTroop = this.ActiveTroop.building;
							activeTroop.AvailableAttackActions = activeTroop.AvailableAttackActions - 1;
							this.ActiveTroop.building.AttackTimer = 10f;
							this.StartCombat(this.ActiveTroop, pgs);
						}
						pgs.ShowAttackHover = true;
					}
					else
					{
						if (this.ActiveTroop.TroopsHere[0].AvailableAttackActions <= 0 || this.ActiveTroop.TroopsHere[0].GetOwner() != EmpireManager.GetEmpireByName(PlanetScreen.screen.PlayerLoyalty))
						{
							continue;
						}
						if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
						{
							if (pgs.x > this.ActiveTroop.x)
							{
								this.ActiveTroop.TroopsHere[0].facingRight = true;
							}
							else if (pgs.x < this.ActiveTroop.x)
							{
								this.ActiveTroop.TroopsHere[0].facingRight = false;
							}
							Troop item = this.ActiveTroop.TroopsHere[0];
							item.AvailableAttackActions = item.AvailableAttackActions - 1;
							this.ActiveTroop.TroopsHere[0].AttackTimer = (float)this.ActiveTroop.TroopsHere[0].AttackTimerBase;
							Troop availableMoveActions = this.ActiveTroop.TroopsHere[0];
							availableMoveActions.AvailableMoveActions = availableMoveActions.AvailableMoveActions - 1;
							this.ActiveTroop.TroopsHere[0].MoveTimer = (float)this.ActiveTroop.TroopsHere[0].MoveTimerBase;
							this.StartCombat(this.ActiveTroop, pgs);
						}
						pgs.ShowAttackHover = true;
					}
				}
				else
				{
					if (pgs.TroopsHere.Count > 0)
					{
						if (HelperFunctions.CheckIntersection(pgs.TroopClickRect, MousePos) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
						{
							if (pgs.TroopsHere[0].GetOwner() != EmpireManager.GetEmpireByName(CombatScreen.universeScreen.PlayerLoyalty))
							{
								this.ActiveTroop = pgs;
								this.tInfo.SetPGS(pgs);
								SelectedSomethingThisFrame = true;
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
								SelectedSomethingThisFrame = true;
							}
						}
					}
					else if (pgs.building != null && !pgs.CanMoveTo && HelperFunctions.CheckIntersection(pgs.TroopClickRect, MousePos) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
					{
						if (this.p.Owner != EmpireManager.GetEmpireByName(CombatScreen.universeScreen.PlayerLoyalty))
						{
							this.ActiveTroop = pgs;
							this.tInfo.SetPGS(pgs);
							SelectedSomethingThisFrame = true;
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
							SelectedSomethingThisFrame = true;
						}
					}
					if (this.ActiveTroop == null || !pgs.CanMoveTo || this.ActiveTroop.TroopsHere.Count == 0 || !HelperFunctions.CheckIntersection(pgs.ClickRect, MousePos) || this.ActiveTroop.TroopsHere[0].GetOwner() != EmpireManager.GetEmpireByName(PlanetScreen.screen.PlayerLoyalty) || this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released || this.ActiveTroop.TroopsHere[0].AvailableMoveActions <= 0)
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
					AudioManager.PlayCue(pgs.TroopsHere[0].MovementCue);
					this.ActiveTroop.TroopsHere.Clear();
					this.ActiveTroop = null;
					this.ActiveTroop = pgs;
					pgs.CanMoveTo = false;
					SelectedSomethingThisFrame = true;
				}
			}
			if (this.ActiveTroop != null && !SelectedSomethingThisFrame && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released && !HelperFunctions.CheckIntersection(this.SelectedItemRect, input.CursorPosition))
			{
				this.ActiveTroop = null;
			}
			if (this.ActiveTroop != null)
			{
				this.tInfo.pgs = this.ActiveTroop;
			}
			this.DetermineAttackAndMove();
			this.hInfo.SetPGS(this.HoveredSquare);
			this.previousMouse = this.currentMouse;
		}
        
		private void ResetTroopList()
		{
			this.OrbitSL.Entries.Clear();
			this.OrbitSL.Copied.Clear();
			this.OrbitSL.indexAtTop = 0;
            for (int i = 0; i < this.p.ParentSystem.ShipList.Count; i++)
			{
                Ship ship = this.p.ParentSystem.ShipList[i];
				if (Vector2.Distance(this.p.Position, ship.Center) < 15000f && ship.loyalty == EmpireManager.GetEmpireByName(CombatScreen.universeScreen.PlayerLoyalty))
				{
					if (ship.Role == "troop" && !CombatScreen.universeScreen.MasterShipList.pendingRemovals.Contains(ship))
					{
						this.OrbitSL.AddItem(ship);
					}
                    else if (ship.HasTroopBay || ship.hasTransporter)
                    {
                        int LandingLimit = ship.GetHangars().Where(ready => ready.IsTroopBay && ready.hangarTimer <= 0).Count();
                        foreach (ShipModule module in ship.Transporters.Where(module => module.TransporterTimer <= 1f))
                            LandingLimit += module.TransporterTroopLanding;
                        for (int x = 0; x < ship.TroopList.Count && LandingLimit > 0; x++)
                        {
                            if (ship.TroopList[x].GetOwner() == ship.loyalty)
                            {
                                this.OrbitSL.AddItem(ship.TroopList[x]);
                                LandingLimit--;
                            }
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
				AudioManager.PlayCue("sd_weapon_bigcannon_01");
				AudioManager.PlayCue("uzi_loop");
			}
			else
			{
				Attacker.TroopsHere[0].DoAttack();
				AudioManager.PlayCue(Attacker.TroopsHere[0].sound_attack);
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
                        if (ActiveTroop == planetGridSquare1)
                        {
                            foreach (PlanetGridSquare planetGridSquare2 in p.TilesList)
                            {
                                if (planetGridSquare2 != ActiveTroop && planetGridSquare2 == squareToAttack)
                                {
                                    //Added by McShooterz: Prevent troops from firing on own buildings
                                    if (planetGridSquare2.TroopsHere.Count == 0 && 
                                        (planetGridSquare2.building == null || 
                                        (planetGridSquare2.building != null && 
                                        planetGridSquare2.building.CombatStrength == 0) || 
                                        p.Owner == ActiveTroop.TroopsHere[0].GetOwner()))
                                        return false;
                                    int num1 = Math.Abs(planetGridSquare1.x - planetGridSquare2.x);
                                    int num2 = Math.Abs(planetGridSquare1.y - planetGridSquare2.y);
                                    if (planetGridSquare2.TroopsHere.Count > 0)
                                    {
                                        if (planetGridSquare1.TroopsHere.Count != 0 && 
                                            (double)num1 <= (double)planetGridSquare1.TroopsHere[0].Range && 
                                            ((double)num2 <= (double)planetGridSquare1.TroopsHere[0].Range && 
                                            planetGridSquare2.TroopsHere[0].GetOwner() != ActiveTroop.TroopsHere[0].GetOwner()))
                                            return true;
                                    }
                                    else if (planetGridSquare2.building != null && 
                                        planetGridSquare2.building.CombatStrength > 0 && 
                                        ((double)num1 <= (double)planetGridSquare1.TroopsHere[0].Range && 
                                        (double)num2 <= (double)planetGridSquare1.TroopsHere[0].Range))
                                    {
                                        if (p.Owner == null)
                                            return false;
                                        if (p.Owner != ActiveTroop.TroopsHere[0].GetOwner())
                                            return true;
                                    }
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
                                            planetGridSquare2.TroopsHere[0].GetOwner() != p.Owner)
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.Explosions != null)
                        this.Explosions.Dispose();
                    if (this.OrbitSL != null)
                        this.OrbitSL.Dispose();

                }
                this.Explosions = null;
                this.OrbitSL = null;
                this.disposed = true;
            }
        }
	}
}