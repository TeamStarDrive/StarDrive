using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.AI;

namespace Ship_Game
{
	public sealed class DeepSpaceBuildingWindow : IDisposable
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

            //The Doctor: Ensure Subspace Projector is always the first entry on the DSBW list so that the player never has to scroll to find it.
		    var buildables = EmpireManager.Player.structuresWeCanBuild;
            foreach (string s in buildables)
            {
                if (s != "Subspace Projector") continue;
                SL.AddItem(ResourceManager.ShipsDict[s], 0, 0);
                break;
            }
			foreach (string s in buildables)
			{
                if (s != "Subspace Projector")
                    SL.AddItem(ResourceManager.ShipsDict[s], 0, 0);
            }
			this.TextPos = new Vector2((float)(this.win.X + this.win.Width / 2) - Fonts.Arial12Bold.MeasureString("Deep Space Construction").X / 2f, (float)(this.win.Y + 25));
		}

       public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

       ~DeepSpaceBuildingWindow() { Dispose(false); }

       private void Dispose(bool disposing)
       {
            SL?.Dispose(ref SL);
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
                    if ((e.item as Ship).Name == "Subspace Projector")
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/subspace_projector"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    else
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    

					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    string name = (e.item as Ship).Name;
                    SpriteFont nameFont = Fonts.Arial10;
					this.ScreenManager.SpriteBatch.DrawString(nameFont, name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as Ship).shipData.GetRole(), tCursor, Color.Orange);

                    // Costs and Upkeeps for the deep space build menu - The Doctor
                    
                    string cost = (e.item as Ship).GetCost(EmpireManager.Player).ToString();

                    string upkeep = "Doctor rocks";
					if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        upkeep = (e.item as Ship).GetMaintCostRealism(EmpireManager.Player).ToString("F2");
                    }
                    else
                    {
                        upkeep = (e.item as Ship).GetMaintCost(EmpireManager.Player).ToString("F2");
                    }

                    Rectangle prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], prodiconRect, Color.White);

                    tCursor = new Vector2((float)(prodiconRect.X - 60), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
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
                    if ((e.item as Ship).Name == "Subspace Projector")
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/subspace_projector"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    else
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    string name = (e.item as Ship).Name;
                    SpriteFont nameFont = Fonts.Arial10;
                    this.ScreenManager.SpriteBatch.DrawString(nameFont, name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as Ship).shipData.GetRole(), tCursor, Color.Orange);

                    // Costs and Upkeeps for the deep space build menu - The Doctor

                    string cost = (e.item as Ship).GetCost(EmpireManager.Player).ToString();

                    string upkeep = "Doctor rocks";
					if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        upkeep = (e.item as Ship).GetMaintCostRealism(EmpireManager.Player).ToString("F2");
                    }
                    else
                    {
                        upkeep = (e.item as Ship).GetMaintCost(EmpireManager.Player).ToString("F2");
                    }

                    Rectangle prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], prodiconRect, Color.White);

                    tCursor = new Vector2((float)(prodiconRect.X - 60), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
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
				Vector3 nearPoint = screen.Viewport.Unproject(new Vector3(MousePos.X, MousePos.Y, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
				Vector3 farPoint = screen.Viewport.Unproject(new Vector3(MousePos.X, MousePos.Y, 1f), this.screen.projection, this.screen.view, Matrix.Identity);
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
						if (Vector2.Distance(p.planetToClick.Center, pp) > (2500f * p.planetToClick.scale))
						{
							continue;
						}
						this.TetherOffset = pp - p.planetToClick.Center;
						this.TargetPlanet = p.planetToClick.guid;
						this.ScreenManager.SpriteBatch.DrawLine(p.ScreenPos, MousePos, new Color(255, 165, 0, 150), 3f);
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Will Orbit ", p.planetToClick.Name), new Vector2(MousePos.X, MousePos.Y + 34f), Color.White);
					}
				}
				Rectangle? nullable = null;
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_platform"], MousePos, nullable, new Color(0, 255, 0, 100), 0f, IconOrigin, scale, SpriteEffects.None, 1f);
			}
		}

		public bool HandleInput(InputState input)
		{
			this.selector = null;
			this.SL.HandleInput(input);
			Vector2 MousePos = input.CursorPosition;
			for (int i = 0; i < this.SL.Entries.Count; i++)
			{
				ScrollList.Entry e = this.SL.Entries[i];
				if (!e.clickRect.HitTest(MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					this.selector = new Selector(this.ScreenManager, e.clickRect);
					if (e.clickRectHover == 0)
					{
						GameAudio.PlaySfxAsync("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
					{
						this.itemToBuild = e.item as Ship;
						return true;
					}
				}
			}
			if (this.itemToBuild == null || this.win.HitTest(MousePos) || input.CurrentMouseState.LeftButton != ButtonState.Pressed || input.LastMouseState.LeftButton != ButtonState.Released)
			{
				if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
				{
					this.itemToBuild = null;
				}
				if (!this.ConstructionSubMenu.Menu.HitTest(input.CursorPosition) || !input.RightMouseClick)
				{
					return false;
				}
				this.screen.showingDSBW = false;
				return true;
			}
			Vector3 nearPoint = screen.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
			Vector3 farPoint = screen.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 1f), this.screen.projection, this.screen.view, Matrix.Identity);
			Vector3 direction = farPoint - nearPoint;
			direction.Normalize();
			Ray pickRay = new Ray(nearPoint, direction);
			float k = -pickRay.Position.Z / pickRay.Direction.Z;
			Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
			Goal buildstuff = new Goal(new Vector2(pickedPosition.X, pickedPosition.Y), this.itemToBuild.Name, EmpireManager.Player);
			if (this.TargetPlanet != Guid.Empty)
			{
				buildstuff.TetherOffset = this.TetherOffset;
				buildstuff.TetherTarget = this.TargetPlanet;
			}
			EmpireManager.Player.GetGSAI().Goals.Add(buildstuff);
			GameAudio.PlaySfxAsync("echo_affirm");
			lock (GlobalStats.ClickableItemLocker)
			{
				this.screen.UpdateClickableItems();
			}
			if (!input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && (!input.CurrentKeyboardState.IsKeyDown(Keys.RightShift)))
			{
				this.itemToBuild = null;
			}
			return true;
		}
	}
}