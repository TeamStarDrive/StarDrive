using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class DeepSpaceBuildingWindow
	{
		private Ship_Game.ScreenManager ScreenManager;

		private ScrollList SL;

		private Submenu ConstructionSubMenu;

		private UniverseScreen screen;

		private Rectangle win;

		private Vector2 TextPos;

		public Ship itemToBuild;

		private Selector selector;

		private Vector2 TetherOffset = new Vector2();

		private Guid TargetPlanet = Guid.Empty;

		public DeepSpaceBuildingWindow(Ship_Game.ScreenManager ScreenManager, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			int WindowWidth = 320;
			this.win = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 5 - WindowWidth, 260, WindowWidth, 225);
			Rectangle rectangle = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 5 - WindowWidth + 20, 225, WindowWidth - 40, 455);
			this.ConstructionSubMenu = new Submenu(ScreenManager, this.win, true);
			this.ConstructionSubMenu.AddTab("Build Menu");
			this.SL = new ScrollList(this.ConstructionSubMenu, 40);
			foreach (string s in EmpireManager.GetEmpireByName(screen.PlayerLoyalty).structuresWeCanBuild)
			{
				this.SL.AddItem(ResourceManager.ShipsDict[s], 0, 0);
			}
			this.TextPos = new Vector2((float)(this.win.X + this.win.Width / 2) - Fonts.Arial12Bold.MeasureString("Deep Space Construction").X / 2f, (float)(this.win.Y + 25));
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public void Draw(GameTime gameTime)
		{
			Rectangle r = this.ConstructionSubMenu.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(this.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			this.ConstructionSubMenu.Draw();
			this.SL.Draw(this.ScreenManager.SpriteBatch);
			Vector2 bCursor = new Vector2((float)(this.ConstructionSubMenu.Menu.X + 20), (float)(this.ConstructionSubMenu.Menu.Y + 45));
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			for (int i = this.SL.indexAtTop; i < this.SL.Entries.Count && i < this.SL.indexAtTop + this.SL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.SL.Entries[i];
				bCursor.Y = (float)e.clickRect.Y;
                bCursor.X = (float)e.clickRect.X - 9;
				if (e.clickRectHover != 0)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as Ship).Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as Ship).Role, tCursor, Color.Orange);
                    
                    string cost = (e.item as Ship).GetCost(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)).ToString();
                    string upkeep = (e.item as Ship).GetMaintCost(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)).ToString();

                    Rectangle prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], prodiconRect, Color.White);

                    tCursor = new Vector2((float)(prodiconRect.X - 50), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), tCursor, Color.Salmon);

                    tCursor = new Vector2((float)(prodiconRect.X + 26), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, cost, tCursor, Color.White);


					if (e.Plus != 0)
					{
						if (e.PlusHover != 0)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], e.addRect, Color.White);
						}
						else
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover1"], e.addRect, Color.White);
						}
					}
					if (e.Edit != 0)
					{
						if (e.EditHover != 0)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], e.editRect, Color.White);
						}
						else
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover1"], e.editRect, Color.White);
						}
					}
					if (e.clickRect.Y == 0)
					{
					}
				}
				else
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as Ship).Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as Ship).Role, tCursor, Color.Orange);

                    string cost = (e.item as Ship).GetCost(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)).ToString();
                    string upkeep = (e.item as Ship).GetMaintCost(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)).ToString();

                    Rectangle prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], prodiconRect, Color.White);

                    tCursor = new Vector2((float)(prodiconRect.X - 50), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), tCursor, Color.Salmon);

                    tCursor = new Vector2((float)(prodiconRect.X + 26), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, cost, tCursor, Color.White);

					if (e.Plus != 0)
					{
						if (e.PlusHover != 0)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], e.addRect, Color.White);
						}
						else
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add"], e.addRect, Color.White);
						}
					}
					if (e.Edit != 0)
					{
						if (e.EditHover != 0)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], e.editRect, Color.White);
						}
						else
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit"], e.editRect, Color.White);
						}
					}
					if (e.clickRect.Y == 0)
					{
					}
				}
			}
			if (this.selector != null)
			{
				this.selector.Draw();
			}
			if (this.itemToBuild != null)
			{
				float scale = (float)((float)this.itemToBuild.Size) / (float)ResourceManager.TextureDict["TacticalIcons/symbol_platform"].Width;
				Vector2 IconOrigin = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_platform"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_platform"].Width / 2));
				scale = scale * 4000f / this.screen.camHeight;
				if (scale > 1f)
				{
					scale = 1f;
				}
				if (scale < 0.15f)
				{
					scale = 0.15f;
				}
				Viewport viewport = this.ScreenManager.GraphicsDevice.Viewport;
				Vector3 nearPoint = viewport.Unproject(new Vector3(MousePos.X, MousePos.Y, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
				Viewport viewport1 = this.ScreenManager.GraphicsDevice.Viewport;
				Vector3 farPoint = viewport1.Unproject(new Vector3(MousePos.X, MousePos.Y, 1f), this.screen.projection, this.screen.view, Matrix.Identity);
				Vector3 direction = farPoint - nearPoint;
				direction.Normalize();
				Ray pickRay = new Ray(nearPoint, direction);
				float k = -pickRay.Position.Z / pickRay.Direction.Z;
				Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
				Vector2 pp = new Vector2(pickedPosition.X, pickedPosition.Y);
				this.TargetPlanet = Guid.Empty;
				this.TetherOffset = Vector2.Zero;
				lock (GlobalStats.ClickableSystemsLock)
				{
					foreach (UniverseScreen.ClickablePlanets p in this.screen.ClickPlanetList)
					{
						if (Vector2.Distance(p.planetToClick.Position, pp) > 2500f)
						{
							continue;
						}
						this.TetherOffset = pp - p.planetToClick.Position;
						this.TargetPlanet = p.planetToClick.guid;
						Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, p.ScreenPos, MousePos, new Color(255, 165, 0, 150), 3f);
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Will Orbit ", p.planetToClick.Name), new Vector2(MousePos.X, MousePos.Y + 34f), Color.White);
					}
				}
				Rectangle? nullable = null;
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_platform"], MousePos, nullable, new Color(0, 255, 0, 100), 0f, IconOrigin, scale, SpriteEffects.None, 1f);
			}
		}

		~DeepSpaceBuildingWindow()
		{
			this.Dispose(false);
		}

		public bool HandleInput(InputState input)
		{
			this.selector = null;
			this.SL.HandleInput(input);
			Vector2 MousePos = input.CursorPosition;
			for (int i = 0; i < this.SL.Entries.Count; i++)
			{
				ScrollList.Entry e = this.SL.Entries[i];
				if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					this.selector = new Selector(this.ScreenManager, e.clickRect);
					if (e.clickRectHover == 0)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
					{
						this.itemToBuild = e.item as Ship;
						return true;
					}
				}
			}
			if (this.itemToBuild == null || HelperFunctions.CheckIntersection(this.win, MousePos) || input.CurrentMouseState.LeftButton != ButtonState.Pressed || input.LastMouseState.LeftButton != ButtonState.Released)
			{
				if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
				{
					this.itemToBuild = null;
				}
				if (!HelperFunctions.CheckIntersection(this.ConstructionSubMenu.Menu, input.CursorPosition) || !input.RightMouseClick)
				{
					return false;
				}
				this.screen.showingDSBW = false;
				return true;
			}
			Viewport viewport = this.ScreenManager.GraphicsDevice.Viewport;
			Vector3 nearPoint = viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
			Viewport viewport1 = this.ScreenManager.GraphicsDevice.Viewport;
			Vector3 farPoint = viewport1.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 1f), this.screen.projection, this.screen.view, Matrix.Identity);
			Vector3 direction = farPoint - nearPoint;
			direction.Normalize();
			Ray pickRay = new Ray(nearPoint, direction);
			float k = -pickRay.Position.Z / pickRay.Direction.Z;
			Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
			Goal buildstuff = new Goal(new Vector2(pickedPosition.X, pickedPosition.Y), this.itemToBuild.Name, EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty));
			if (this.TargetPlanet != Guid.Empty)
			{
				buildstuff.TetherOffset = this.TetherOffset;
				buildstuff.TetherTarget = this.TargetPlanet;
			}
			EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetGSAI().Goals.Add(buildstuff);
			AudioManager.PlayCue("echo_affirm");
			lock (GlobalStats.ClickableItemLocker)
			{
				this.screen.UpdateClickableItems();
			}
			if (!input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
			{
				this.itemToBuild = null;
			}
			return true;
		}
	}
}