using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SystemInfoUIElement : UIElement
	{
		public static SpriteFont SysFont;

		public static SpriteFont DataFont;

		private Rectangle SliderRect;

		private Rectangle clickRect;

		private UniverseScreen screen;

		private Rectangle LeftRect;

		private Rectangle RightRect;

		//private Rectangle PlanetIconRect;

		//private Rectangle flagRect;

		//private string PlanetTypeRichness;

		private Vector2 PlanetTypeCursor;

		public SolarSystem s;

		private Selector sel;

		private float ClickTimer;

		private float TimerDelay = 0.25f;

		public float SelectionTimer;

		private bool Hovering;

		private float HoverTimer;

		private List<SystemInfoUIElement.ClickMe> ClickList = new List<SystemInfoUIElement.ClickMe>();

		new private Color tColor = new Color(255, 239, 208);

		private string fmt = "0.#";

		public SystemInfoUIElement(Rectangle r, Ship_Game.ScreenManager sm, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = sm;
			this.ElementRect = r;
			this.sel = new Selector(this.ScreenManager, r, Color.Black);
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.SliderRect = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
			this.clickRect = new Rectangle(this.ElementRect.X + this.ElementRect.Width - 16, this.ElementRect.Y + this.ElementRect.Height / 2 - 11, 11, 22);
			this.LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
			this.RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
		}

		public override void Draw(GameTime gameTime)
		{
			this.DrawInPosition(gameTime);
		}

		public void DrawInPosition(GameTime gameTime)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			SystemInfoUIElement clickTimer = this;
			clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
			SystemInfoUIElement selectionTimer = this;
			selectionTimer.SelectionTimer = selectionTimer.SelectionTimer + elapsedTime;
			Viewport viewport = this.ScreenManager.GraphicsDevice.Viewport;
			Vector3 pScreenSpace = viewport.Project(new Vector3(this.s.Position, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
			Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
			Vector2 radialPos = new Vector2(this.s.Position.X + 4500f, this.s.Position.Y);
			Viewport viewport1 = this.ScreenManager.GraphicsDevice.Viewport;
			Vector3 insetRadialPos = viewport1.Project(new Vector3(radialPos, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
			Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
			float pRadius = Vector2.Distance(insetRadialSS, pPos);
			if (pRadius < 5f)
			{
				pRadius = 5f;
			}
			Rectangle rectangle = new Rectangle((int)pPos.X - (int)pRadius, (int)pPos.Y - (int)pRadius, (int)pRadius * 2, (int)pRadius * 2);
			Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, pPos, pRadius, Color.White);
			float count = 0.4f / (float)this.s.PlanetList.Count;
			if (this.SelectionTimer > 0.4f)
			{
				this.SelectionTimer = 0.4f;
			}
			this.Hovering = false;
			this.ClickList.Clear();
			float TransitionPosition = 1f - this.SelectionTimer / 0.4f;
			float transitionOffset = (float)Math.Pow((double)TransitionPosition, 2);
			if (this.s.ExploredDict[EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)])
			{
				for (int i = 0; i < this.s.PlanetList.Count; i++)
				{
					Vector2 planetPos = HelperFunctions.findPointFromAngleAndDistance(pPos, this.s.PlanetList[i].OrbitalAngle, (float)(40 + 40 * i));
					planetPos = planetPos - ((Vector2.Normalize(planetPos - pPos) * (float)(40 + 40 * i)) * transitionOffset);
					Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, pPos, Vector2.Distance(pPos, planetPos), 50, (this.s.PlanetList[i].Owner == null ? new Color(50, 50, 50, 90) : new Color(this.s.PlanetList[i].Owner.EmpireColor, 100)), 2f);
				}
				for (int i = 0; i < this.s.PlanetList.Count; i++)
				{
					Planet planet = this.s.PlanetList[i];
                    Vector2 planetPos = HelperFunctions.findPointFromAngleAndDistance(pPos, this.s.PlanetList[i].OrbitalAngle, (float)(40 + 40 * i));
					planetPos = planetPos - ((Vector2.Normalize(planetPos - pPos) * (float)(40 + 40 * i)) * transitionOffset);
                    float fIconScale = 1.0f + ((float)(Math.Log(this.s.PlanetList[i].scale)));
					Rectangle PlanetRect = new Rectangle((int)planetPos.X - (int)(16 * fIconScale / 2), (int)planetPos.Y - (int)(16 * fIconScale / 2), (int)(16 * fIconScale), (int)(16 * fIconScale));
					if (HelperFunctions.CheckIntersection(PlanetRect, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
					{
						this.Hovering = true;
						int widthplus = (int)(4f * (this.HoverTimer / 0.2f));
                        PlanetRect = new Rectangle((int)planetPos.X - ((int)(16 * fIconScale / 2) + widthplus), (int)planetPos.Y - ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus));
						SystemInfoUIElement.ClickMe cm = new SystemInfoUIElement.ClickMe()
						{
							p = this.s.PlanetList[i],
							r = PlanetRect
						};
						this.ClickList.Add(cm);
					}
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.s.PlanetList[i].planetType)], PlanetRect, Color.White);
            
					if (this.screen.SelectedPlanet == this.s.PlanetList[i])
					{
						Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, PlanetRect, (this.s.PlanetList[i].Owner != null ? this.s.PlanetList[i].Owner.EmpireColor : Color.Gray), 3);
					}
					Planet p = this.s.PlanetList[i];
					this.PlanetTypeCursor = new Vector2((float)(PlanetRect.X + PlanetRect.Width / 2) - SystemInfoUIElement.SysFont.MeasureString(p.Name).X / 2f, (float)(PlanetRect.Y + PlanetRect.Height + 4));
					HelperFunctions.ClampVectorToInt(ref this.PlanetTypeCursor);
					bool hasAnamoly = false;
                    bool hasCommodities = false;
                    bool hastroops =false;
                    bool hasEnemyTroop = false;
                    int playerTroops = 0;
                    int sideSpacing = 0;
					if (p.ExploredDict[EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)])
					{
						int j = 0;
                        #region replaced
                        //while (j < this.s.PlanetList[i].BuildingList.Count)
                        //{
                        //    if (this.s.PlanetList[i].BuildingList[j].EventTriggerUID == "")
                        //    {
                        //        j++;
                        //    }
                        //    else
                        //    {
                        //        hasAnamoly = true;
                        //        break;
                        //    }
                        //} 
                        #endregion

                        while (j < this.s.PlanetList[i].BuildingList.Count)
                        {
                            
                            Building building = this.s.PlanetList[i].BuildingList[j];
                            
                            if (!string.IsNullOrEmpty(building.EventTriggerUID))
                            {
                                hasAnamoly = true;
                            }
                            if (building.IsCommodity)
                            {
                                hasCommodities = true;
                            }
                            if (hasCommodities && hasAnamoly)
                                break;
                            j++;


                        }
                        j = 0;
                        if (planet.Owner != null && planet.Owner.isPlayer)
                        while (j < this.s.PlanetList[i].TroopsHere.Count)
                        {
                            if (!this.s.PlanetList[i].TroopsHere[j].GetOwner().isPlayer)
                            {
                                hasEnemyTroop = true;

                            }
                            else
                            {
                                hastroops = true;
                                playerTroops++;
                            }
                            j++;
                        } 
						if (hasAnamoly)
						{
                            sideSpacing += 4;
                            TimeSpan totalGameTime = gameTime.TotalGameTime;
							float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
							f = Math.Abs(f) * 255f;
							Color flashColor = new Color(255, 255, 255, (byte)f);
                            Rectangle flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_anomaly_small"], flashRect, flashColor);
							if (HelperFunctions.CheckIntersection(flashRect, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
							{
								ToolTip.CreateTooltip(121, this.ScreenManager);
							}
                            sideSpacing += flashRect.Width;
						}
                        if (hasCommodities)
                        {
                            
                                sideSpacing += 4;
                            TimeSpan totalGameTime = gameTime.TotalGameTime;
                            float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
                            f = Math.Abs(f) * 255f;
                            Color flashColor = new Color(255, 255, 255, (byte)f);
                            Rectangle flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/marketIcon"], flashRect, flashColor);
                            if (HelperFunctions.CheckIntersection(flashRect, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
                            {
                                ToolTip.CreateTooltip(121, this.ScreenManager);
                            }
                            sideSpacing += flashRect.Width;
                        }
                        if (hastroops)
                        {

                            sideSpacing += 4;
                            TimeSpan totalGameTime = gameTime.TotalGameTime;
                            float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
                            f = Math.Abs(f) * 255f;
                            Color flashColor = new Color(255, 255, 255, (byte)f);
                            Rectangle flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], flashRect, flashColor);
                            if (HelperFunctions.CheckIntersection(flashRect, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
                            {
                                ToolTip.CreateTooltip(121, this.ScreenManager);
                            }
                            sideSpacing += flashRect.Width;
                        }
						if (p.Owner == null)
						{
							HelperFunctions.DrawDropShadowText1(this.ScreenManager, p.Name, this.PlanetTypeCursor, SystemInfoUIElement.SysFont, (p.habitable ? this.tColor : Color.LightPink));
						}
						else
						{
							HelperFunctions.DrawDropShadowText1(this.ScreenManager, p.Name, this.PlanetTypeCursor, SystemInfoUIElement.SysFont, (p.habitable ? p.Owner.EmpireColor : Color.LightPink));
						}
						if (p.habitable)
						{
							int Spacing = SystemInfoUIElement.DataFont.LineSpacing;
							this.PlanetTypeCursor.Y = this.PlanetTypeCursor.Y + (float)(Spacing + 4);
							float population = p.Population / 1000f;
							string popString = population.ToString(this.fmt);
							float maxPopulation = p.MaxPopulation / 1000f + p.MaxPopBonus / 1000f;
							popString = string.Concat(popString, " / ", maxPopulation.ToString(this.fmt));
							this.PlanetTypeCursor.X = (float)(PlanetRect.X + PlanetRect.Width / 2) - SystemInfoUIElement.DataFont.MeasureString(popString).X / 2f;
							HelperFunctions.ClampVectorToInt(ref this.PlanetTypeCursor);
							this.ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.DataFont, popString, this.PlanetTypeCursor, this.tColor);
							Rectangle flagRect = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 10, PlanetRect.Y - 20, 20, 20);
							if (p.Owner != null)
							{
								Ship_Game.ScreenManager screenManager = this.ScreenManager;
								KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[p.Owner.data.Traits.FlagIndex];
								HelperFunctions.DrawDropShadowImage(screenManager, flagRect, item.Value, p.Owner.EmpireColor);
							}
							Rectangle fIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)this.PlanetTypeCursor.Y + Spacing, 10, 10);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.White);
							Rectangle pIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(this.PlanetTypeCursor.Y + (float)(2 * Spacing)), 10, 10);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], pIcon, Color.White);
							Rectangle rIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(this.PlanetTypeCursor.Y + (float)(3 * Spacing)), 10, 10);
                            Rectangle tIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(this.PlanetTypeCursor.Y + (float)(4 * Spacing)), 10, 10);
							if (p.Owner != null && p.Owner == EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
							{
								this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], rIcon, Color.White);

                                    
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], tIcon, Color.White);
                                

							}
							Vector2 ft = new Vector2((float)(fIcon.X + 12), (float)fIcon.Y);
							Vector2 pt = new Vector2((float)(pIcon.X + 12), (float)pIcon.Y);
							HelperFunctions.ClampVectorToInt(ref ft);
							HelperFunctions.ClampVectorToInt(ref pt);
							if (p.Owner == null || p.Owner != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
							{
								this.ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.DataFont, p.Fertility.ToString(this.fmt), ft, this.tColor);
								this.ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.DataFont, p.MineralRichness.ToString(this.fmt), pt, this.tColor);
							}
							else
							{
								p.UpdateIncomes();
								SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
								SpriteFont dataFont = SystemInfoUIElement.DataFont;
								float netFoodPerTurn = p.GetNetFoodPerTurn();
								spriteBatch.DrawString(dataFont, netFoodPerTurn.ToString(this.fmt), ft, this.tColor);
								SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
								SpriteFont spriteFont = SystemInfoUIElement.DataFont;
								float netProductionPerTurn = p.GetNetProductionPerTurn();
								spriteBatch1.DrawString(spriteFont, netProductionPerTurn.ToString(this.fmt), pt, this.tColor);
								Vector2 rt = new Vector2((float)(rIcon.X + 12), (float)rIcon.Y);
								HelperFunctions.ClampVectorToInt(ref rt);
								this.ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.DataFont, p.NetResearchPerTurn.ToString(this.fmt), rt, this.tColor);
                                Vector2 tt = new Vector2((float)(rIcon.X + 12), (float)tIcon.Y);
                                HelperFunctions.ClampVectorToInt(ref tt);
                                this.ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.DataFont, playerTroops.ToString(this.fmt), tt, this.tColor);
							}
						}
						float x = (float)Mouse.GetState().X;
						MouseState state = Mouse.GetState();
						Vector2 MousePos = new Vector2(x, (float)state.Y);
						foreach (Goal g in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetGSAI().Goals)
						{
							if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != p)
							{
								continue;
							}
							Rectangle Flag = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 6, PlanetRect.Y - 17, 13, 17);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/flagicon"], Flag, EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).EmpireColor);
							if (!HelperFunctions.CheckIntersection(Flag, MousePos))
							{
								continue;
							}
							ToolTip.CreateTooltip(26, this.ScreenManager);
						}
					}
				}
			}
			if (!this.Hovering)
			{
				this.HoverTimer = 0f;
			}
			else
			{
				SystemInfoUIElement hoverTimer = this;
				hoverTimer.HoverTimer = hoverTimer.HoverTimer + elapsedTime;
				if (this.HoverTimer > 0.1f)
				{
					this.HoverTimer = 0.1f;
					return;
				}
			}
		}

        public override bool HandleInput(InputState input)
        {
            if (this.s == null)
                return false;
            foreach (SystemInfoUIElement.ClickMe clickMe in this.ClickList)
            {
                if (HelperFunctions.CheckIntersection(clickMe.r, input.CursorPosition) && input.InGameSelect)
                {
                    if ((double)this.ClickTimer < (double)this.TimerDelay)
                    {
                        this.screen.SelectedPlanet = clickMe.p;
                        this.screen.pInfoUI.SetPlanet(clickMe.p);
                        this.screen.ViewPlanet((object)null);
                        return true;
                    }
                    else
                    {
                        AudioManager.PlayCue("mouse_over4");
                        this.screen.SelectedPlanet = clickMe.p;
                        this.screen.pInfoUI.SetPlanet(clickMe.p);
                        this.ClickTimer = 0.0f;
                        return true;
                    }
                }
            }
            return HelperFunctions.CheckIntersection(this.ElementRect, input.CursorPosition);
        }

		public void SetSystem(SolarSystem s)
		{
			if (this.s != s)
			{
				this.SelectionTimer = 0f;
			}
			this.s = s;
		}

		public struct ClickMe
		{
			public Rectangle r;

			public Planet p;
		}
	}
}