using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game
{
	public sealed class PlanetListScreenEntry
	{
		public Planet planet;

		public Rectangle TotalEntrySize;

		public Rectangle SysNameRect;

		public Rectangle PlanetNameRect;

		public Rectangle FertRect;

		public Rectangle RichRect;

		public Rectangle PopRect;

		public Rectangle OwnerRect;

		public Rectangle OrdersRect;

		private Rectangle ShipIconRect;

		private UITextEntry ShipNameEntry = new UITextEntry();

		private UIButton Colonize;
        private UIButton SendTroops;

		public PlanetListScreen screen;

		private bool marked;

		//private string Status_Text;

		public PlanetListScreenEntry(Planet p, int x, int y, int width1, int height, PlanetListScreen caller)
		{
			this.screen = caller;
			this.planet = p;
			this.TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)this.TotalEntrySize.Width * 0.12f), height);
			this.PlanetNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.25f), height);
			this.FertRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width, y, 100, height);
			this.RichRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width, y, 120, height);
			this.PopRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width, y, 200, height);
			this.OwnerRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width, y, 100, height);
			this.OrdersRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width + this.OwnerRect.Width, y, 100, height);
			//this.Status_Text = "";
			this.ShipIconRect = new Rectangle(this.PlanetNameRect.X + 5, this.PlanetNameRect.Y + 5, 50, 50);
			string shipName = this.planet.Name;
			this.ShipNameEntry.ClickableArea = new Rectangle(this.ShipIconRect.X + this.ShipIconRect.Width + 10, 2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
			this.ShipNameEntry.Text = shipName;
			float width = (float)((int)((float)this.FertRect.Width * 0.8f));
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}

			Goal goal = new Goal();
			foreach (Goal g in Empire.Universe.player.GetGSAI().Goals)
			{
				if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != p)
				{
					continue;
				}
				this.marked = true;
			}

            if (this.marked)
                this.Colonize = new UIButton()
                {
                    Rect = new Rectangle(this.OrdersRect.X + 10, this.OrdersRect.Y + this.OrdersRect.Height / 2 - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height / 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
                    NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
                    HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
                    PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"]
                };
            else
                this.Colonize = new UIButton()
                {
                    Rect = new Rectangle(this.OrdersRect.X + 10, this.OrdersRect.Y + this.OrdersRect.Height / 2 - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height / 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
                    NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"],
                    HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"],
                    PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"]
                };
            if (!this.marked)
                this.Colonize.Text = Localizer.Token(1425);
            else
                this.Colonize.Text = "Cancel Colonize";
            this.Colonize.Launches = Localizer.Token(1425);

            this.SendTroops = new UIButton()
            {
                Rect = new Rectangle(this.OrdersRect.X + this.Colonize.Rect.Width + 10, this.Colonize.Rect.Y, this.Colonize.Rect.Width, this.Colonize.Rect.Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"]
            };
		}

        public void Draw(Ship_Game.ScreenManager ScreenManager, GameTime gameTime)
        {
            string singular;
            Color TextColor = new Color(255, 239, 208);
            string sysname = this.planet.system.Name;
            if (Fonts.Arial20Bold.MeasureString(sysname).X <= (float)this.SysNameRect.Width)
            {
                Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(sysname).X / 2f, (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, sysname, SysNameCursor, TextColor);
            }
            else
            {
                Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Arial12Bold.MeasureString(sysname).X / 2f, (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sysname, SysNameCursor, TextColor);
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            if (this.planet.system.DangerTimer > 0f)
            {
                TimeSpan totalGameTime = gameTime.TotalGameTime;
                float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
                f = Math.Abs(f) * 255f;
                Color flashColor = new Color(255, 255, 255, (byte)f);
                Rectangle flashRect = new Rectangle(this.SysNameRect.X + this.SysNameRect.Width - 40, this.SysNameRect.Y + 5, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/EnemyHere"], flashRect, flashColor);
                if (flashRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(123);
                }
            }
            Rectangle planetIconRect = new Rectangle(this.PlanetNameRect.X + 5, this.PlanetNameRect.Y + 5, this.PlanetNameRect.Height - 10, this.PlanetNameRect.Height - 10);
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.planet.planetType)], planetIconRect, Color.White);
            if (this.planet.Owner != null)
            {
                SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
                KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[this.planet.Owner.data.Traits.FlagIndex];
                spriteBatch.Draw(item.Value, planetIconRect, this.planet.Owner.EmpireColor);
            }
            int i = 0;
            Vector2 StatusIcons = new Vector2((float)(this.PlanetNameRect.X + this.PlanetNameRect.Width), (float)(planetIconRect.Y + 10));
            if (this.planet.RecentCombat)
            {
                Rectangle statusRect = new Rectangle((int)StatusIcons.X - 18, (int)StatusIcons.Y, 16, 16);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_fighting_small"], statusRect, Color.White);
                if (statusRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(119);
                }
                //i++;
            }
            if (EmpireManager.Player.data.MoleList.Count > 0)
            {
                foreach (Mole m in EmpireManager.Player.data.MoleList)
                {
                    if (m.PlanetGuid != this.planet.guid)
                    {
                        continue;
                    }
                    StatusIcons.X = StatusIcons.X - 20f;// (float)(18 * i);
                    Rectangle statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 16, 16);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_spy_small"], statusRect, Color.White);
                    //i++;
                    if (!statusRect.HitTest(MousePos))
                    {
                        break;
                    }
                    ToolTip.CreateTooltip(120);
                    break;
                }
            }
            //Building lastBuilding;
            foreach (Building b in this.planet.BuildingList)
            {
                if (string.IsNullOrEmpty(b.EventTriggerUID) || (this.planet.Owner != null && this.planet.Owner.GetBDict()[b.Name] == true))
                {
                    continue;
                }
                StatusIcons.X = StatusIcons.X - 20f;// (float)(18 * i);
                Rectangle statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 16, 16);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + b.Icon + "_48x48"], statusRect, Color.White);
                i++;
                if (!statusRect.HitTest(MousePos))
                {
                    continue;
                    //break; 
                }
                ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                continue;
                //break;

            }

            foreach (Building b in this.planet.BuildingList)
            {
                if (!b.IsCommodity)
                {
                    continue;
                }
                StatusIcons.X = StatusIcons.X - 20f;// (float)(18 * i);

                Rectangle statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 16, 16);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + b.Icon + "_48x48"], statusRect, Color.White);
                i++;
                if (!statusRect.HitTest(MousePos))
                {
                    continue;
                    //break;
                }
                ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                //break;
            }
            int troops = 0;
            using (planet.TroopsHere.AcquireReadLock())
            foreach (Troop troop in this.planet.TroopsHere)
            {
                if (troop.GetOwner().isPlayer)
                {
                    troops++;

                }
            }
            if (troops > 0)
            {
                //TimeSpan totalGameTime = gameTime.TotalGameTime;
                //float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
                //f = Math.Abs(f) * 255f;
                StatusIcons.X = StatusIcons.X - 20f;// (float)(18 * i);

                Rectangle statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 16, 16);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], statusRect, new Color(255, 255, 255, 255));//Color..White);
                //i++;
                if (statusRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(Localizer.Token(336) + ": " + troops.ToString());
                }

            }
            
            Vector2 rpos = new Vector2()
            {
                X = (float)this.ShipNameEntry.ClickableArea.X,
                Y = (float)(this.ShipNameEntry.ClickableArea.Y - 10)
            };
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.planet.Name, rpos, TextColor);
            rpos.Y = rpos.Y + (float)(Fonts.Arial20Bold.LineSpacing - 3);
            Vector2 FertilityCursor = new Vector2((float)(this.FertRect.X + 35), (float)(this.FertRect.Y + this.FertRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.planet.Fertility.ToString("#.0"), FertilityCursor, (this.planet.habitable ? Color.White : Color.LightPink));
            Vector2 RichCursor = new Vector2((float)(this.RichRect.X + 35), (float)(this.RichRect.Y + this.RichRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.planet.MineralRichness.ToString("#.0"), RichCursor, (this.planet.habitable ? Color.White : Color.LightPink));
            Vector2 PopCursor = new Vector2((float)(this.PopRect.X + 60), (float)(this.PopRect.Y + this.PopRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
            SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            float population = this.planet.Population / 1000f;
            string str = population.ToString("#.0");
            float maxPopulation = (this.planet.MaxPopulation + this.planet.MaxPopBonus) / 1000f;
            spriteBatch1.DrawString(arial12Bold, string.Concat(str, " / ", maxPopulation.ToString("#.0")), PopCursor, (this.planet.habitable ? Color.White : Color.LightPink));
            Vector2 OwnerCursor = new Vector2((float)(this.OwnerRect.X + 20), (float)(this.OwnerRect.Y + this.OwnerRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
            SpriteBatch spriteBatch2 = ScreenManager.SpriteBatch;
            SpriteFont spriteFont = Fonts.Arial12Bold;
            if (this.planet.Owner != null)
            {
                singular = this.planet.Owner.data.Traits.Singular;
            }
            else
            {
                singular = (this.planet.habitable ? Localizer.Token(2263) : Localizer.Token(2264));
            }
            spriteBatch2.DrawString(spriteFont, singular, OwnerCursor, (this.planet.Owner != null ? this.planet.Owner.EmpireColor : Color.Gray));
            string PlanetText = string.Concat(this.planet.GetTypeTranslation(), " ", this.planet.GetRichness());
            Vector2 vector2 = new Vector2((float)(this.FertRect.X + 10), (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2) - Fonts.Arial12Bold.MeasureString(PlanetText).Y / 2f);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, PlanetText, rpos, TextColor);
            if (this.planet.habitable && this.planet.Owner == null)
            {
                this.Colonize.Draw(ScreenManager.SpriteBatch);
            }

            //if (i > 0 && this.planet.Owner == null)
            if (this.planet.Owner ==null && this.planet.habitable)  //fbedard: can send troop anywhere
            {
                int troopsInvading = this.screen.EmpireUI.empire.GetShips()
                 .Where(troop => troop.TroopList.Count > 0)
                 .Where(ai => ai.AI.State != AIState.Resupply).Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == this.planet));
                if (troopsInvading > 0)
                {
                    this.SendTroops.Text = "Invading: " + troopsInvading;
                    this.SendTroops.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
                    this.SendTroops.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
                    this.SendTroops.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
                }
                else
                {
                    this.SendTroops.Text = "Send Troops";
                    this.SendTroops.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"];
                    this.SendTroops.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"];
                    this.SendTroops.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"];
                }
                this.SendTroops.Draw(ScreenManager.SpriteBatch);
            }
            //fbedard : Add Send Button for your planets
            if (this.planet.Owner == Empire.Universe.player)
            {
                int troopsInvading = this.screen.EmpireUI.empire.GetShips()
                 .Where(troop => troop.TroopList.Count > 0)
                 .Where(ai => ai.AI.State != AIState.Resupply).Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == this.planet));
                if (troopsInvading > 0)
                {
                    this.SendTroops.Text = "Landing: " + troopsInvading.ToString();
                    this.SendTroops.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
                    this.SendTroops.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
                    this.SendTroops.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
                }
                else
                {
                    this.SendTroops.Text = "Send Troops";
                    this.SendTroops.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"];
                    this.SendTroops.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"];
                    this.SendTroops.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"];
                }
                this.SendTroops.Draw(ScreenManager.SpriteBatch);
            }
        }

		public void HandleInput(InputState input)
		{
            if (!this.SendTroops.Rect.HitTest(input.CursorPosition))
            {
                this.SendTroops.State = UIButton.PressState.Default;              
            }
            else
            {
                this.SendTroops.State = UIButton.PressState.Hover;
                if (input.InGameSelect)
                {
                    Array<Ship> troopShips;
                    using (screen.EmpireUI.empire.GetShips().AcquireReadLock())
                    {
                        troopShips = new Array<Ship>(this.screen.EmpireUI.empire.GetShips()
                            .Where(troop => troop.TroopList.Count > 0
                                && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                                && troop.fleet == null && !troop.InCombat).OrderBy(distance => Vector2.Distance(distance.Center, planet.Center)));
                    }

                    var planetTroops = new Array<Planet>(screen.EmpireUI.empire.GetPlanets()
                        .Where(troops => troops.TroopsHere.Count > 1)
                        .OrderBy(distance => Vector2.Distance(distance.Center, planet.Center))
                        .Where(p => p.Name != planet.Name));

                    if (troopShips.Count > 0)
                    {
                        GameAudio.PlaySfxAsync("echo_affirm");
                        troopShips.First().AI.OrderAssaultPlanet(this.planet);
                    }
                    else
                        if (planetTroops.Count > 0)
                        {
                            {
                                Ship troop = planetTroops.First().TroopsHere.First().Launch();
                                if (troop != null)
                                {
                                    GameAudio.PlaySfxAsync("echo_affirm");
                                    troop.AI.OrderAssaultPlanet(this.planet);
                                }
                            }
                        }
                        else
                        {
                            GameAudio.PlaySfxAsync("blip_click");
                        }
                }
            }
            if (!this.Colonize.Rect.HitTest(input.CursorPosition))
			{
				this.Colonize.State = UIButton.PressState.Default;
			}
			else
			{
				this.Colonize.State = UIButton.PressState.Hover;
				if (input.InGameSelect)
				{
					if (!this.marked)
					{
						GameAudio.PlaySfxAsync("echo_affirm");
						Goal g = new Goal(this.planet, Empire.Universe.player);
						Empire.Universe.player.GetGSAI().Goals.Add(g);
						this.Colonize.Text = "Cancel Colonize";
                        this.Colonize.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
                        this.Colonize.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
                        this.Colonize.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
						this.marked = true;
						return;
					}
					foreach (Goal g in Empire.Universe.player.GetGSAI().Goals)
					{
						if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != this.planet)
						{
							continue;
						}
						GameAudio.PlaySfxAsync("echo_affirm");
						if (g.GetColonyShip() != null)
						{
							g.GetColonyShip().AI.OrderOrbitNearest(true);
						}
						Empire.Universe.player.GetGSAI().Goals.QueuePendingRemoval(g);
						this.marked = false;
						this.Colonize.Text = "Colonize";
                        this.Colonize.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"];
                        this.Colonize.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"];
                        this.Colonize.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"];
						break;
					}
					Empire.Universe.player.GetGSAI().Goals.ApplyPendingRemovals();
					return;
				}

			}
		}

		public void SetNewPos(int x, int y)
		{
			this.TotalEntrySize = new Rectangle(x, y, this.TotalEntrySize.Width, this.TotalEntrySize.Height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)this.TotalEntrySize.Width * 0.12f), this.TotalEntrySize.Height);
			this.PlanetNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.25f), this.TotalEntrySize.Height);
			this.FertRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width, y, 100, this.TotalEntrySize.Height);
			this.RichRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width, y, 120, this.TotalEntrySize.Height);
			this.PopRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width, y, 200, this.TotalEntrySize.Height);
			this.OwnerRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width, y, 100, this.TotalEntrySize.Height);
			this.OrdersRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width + this.OwnerRect.Width, y, 100, this.TotalEntrySize.Height);
			this.ShipIconRect = new Rectangle(this.PlanetNameRect.X + 5, this.PlanetNameRect.Y + 5, 50, 50);
			string shipName = this.planet.Name;
			this.ShipNameEntry.ClickableArea = new Rectangle(this.ShipIconRect.X + this.ShipIconRect.Width + 10, 2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
			this.Colonize.Rect = new Rectangle(this.OrdersRect.X + 10, this.OrdersRect.Y + this.OrdersRect.Height / 2 - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height / 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
            this.SendTroops.Rect = new Rectangle(this.OrdersRect.X  + this.Colonize.Rect.Width + 10, this.Colonize.Rect.Y, this.Colonize.Rect.Width, this.Colonize.Rect.Height);
			float width = (float)((int)((float)this.FertRect.Width * 0.8f));
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
		}
	}
}