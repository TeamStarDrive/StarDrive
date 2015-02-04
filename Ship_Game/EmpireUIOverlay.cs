using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EmpireUIOverlay
	{
		public Empire empire;

		private Rectangle res1;

		private Rectangle res2;

		private Rectangle res3;

		private Rectangle res4;

		private Rectangle res5;

		private List<ToolTip> ToolTips = new List<ToolTip>();

		private List<EmpireUIOverlay.Button> Buttons = new List<EmpireUIOverlay.Button>();

		public UniverseScreen screen;

		private bool LowRes;

		private MouseState currentMouse;

		private MouseState previousMouse;

		//private float TipTimer = 0.35f;

		//private bool FirstRun = true;

		public EmpireUIOverlay(Ship_Game.Empire playerEmpire, GraphicsDevice device, UniverseScreen screen)
		{
			this.screen = screen;
			this.empire = playerEmpire;
			if (device.PresentationParameters.BackBufferWidth > 1366)
			{
				Vector2 Cursor = Vector2.Zero;
				this.res1 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1"].Height);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1"].Width;
				this.res2 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"].Height);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"].Width;
				this.res3 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3"].Height);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3"].Width;
				this.res4 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"].Height);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"].Width;
				Cursor.X = (float)(screen.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"].Width);
				this.res5 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"].Height);
				EmpireUIOverlay.Button r1 = new EmpireUIOverlay.Button();
				
					r1.Rect = this.res1;
					r1.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1"];
					r1.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1_hover"];
					r1.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1_press"];
                    r1.launches = "Research";
				
				this.Buttons.Add(r1);
				EmpireUIOverlay.Button r2 = new EmpireUIOverlay.Button();
				
					r2.Rect = this.res2;
					r2.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"];
					r2.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"];
					r2.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"];
                    r2.launches = "Research";
				
				this.Buttons.Add(r2);
				EmpireUIOverlay.Button r3 = new EmpireUIOverlay.Button();
				
                    r3.Rect = this.res3;
                    r3.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3"];
                    r3.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3_hover"];
                    r3.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3_press"];
                    r3.launches = "Budget";
				
				this.Buttons.Add(r3);
				EmpireUIOverlay.Button r4 = new EmpireUIOverlay.Button();
				
					r4.Rect = this.res4;
                    r4.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"];
                    r4.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"];
                    r4.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"];
                    r4.launches = "Budget";
				
				this.Buttons.Add(r4);
				EmpireUIOverlay.Button r5 = new EmpireUIOverlay.Button();
				
					r5.Rect = this.res5;
					r5.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"];
					r5.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"];
                    r5.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"];
				
				this.Buttons.Add(r5);
				float rangeforbuttons = (float)(r5.Rect.X - (r4.Rect.X + r4.Rect.Width));
				float roomoneitherside = (rangeforbuttons - 734f) / 2f;
                //Added by McShooterz: Shifted buttons to add new ones, added dummy espionage button
				Cursor.X = (float)(r4.Rect.X + r4.Rect.Width) + roomoneitherside;
                if (this.screen.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth >= 1920)
                {
                    Cursor.X -= 220f;
                    EmpireUIOverlay.Button ShipList = new EmpireUIOverlay.Button();

                    ShipList.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                    ShipList.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
                    ShipList.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
                    ShipList.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
                    ShipList.Text = Localizer.Token(104);
                    ShipList.launches = "ShipList";

                    this.Buttons.Add(ShipList);
                    Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
                    EmpireUIOverlay.Button Fleets = new EmpireUIOverlay.Button();

                    Fleets.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                    Fleets.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
                    Fleets.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
                    Fleets.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
                    Fleets.Text = Localizer.Token(103);
                    Fleets.launches = "Fleets";

                    this.Buttons.Add(Fleets);
                    Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
                }
                else
                    Cursor.X -= 50f;
				EmpireUIOverlay.Button Shipyard = new EmpireUIOverlay.Button();

                Shipyard.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				Shipyard.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
				Shipyard.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
				Shipyard.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
				Shipyard.Text = Localizer.Token(98);
				Shipyard.launches = "Shipyard";
				
				this.Buttons.Add(Shipyard);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 40;
				EmpireUIOverlay.Button Empire = new EmpireUIOverlay.Button();

                Empire.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				Empire.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
				Empire.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
				Empire.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
				Empire.launches = "Empire";
				Empire.Text = Localizer.Token(99);
				
				this.Buttons.Add(Empire);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 40;
                EmpireUIOverlay.Button Espionage = new EmpireUIOverlay.Button();

                Espionage.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                Espionage.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"];
                Espionage.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"];
                Espionage.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"];
                Espionage.Text = Localizer.Token(6088);
                Espionage.launches = "Espionage";

                this.Buttons.Add(Espionage);
                Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
				EmpireUIOverlay.Button Diplomacy = new EmpireUIOverlay.Button();

                Diplomacy.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				Diplomacy.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"];
				Diplomacy.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"];
				Diplomacy.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"];
				Diplomacy.launches = "Diplomacy";
				Diplomacy.Text = Localizer.Token(100);
				
				this.Buttons.Add(Diplomacy);
				Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 7);
				EmpireUIOverlay.Button MainMenu = new EmpireUIOverlay.Button();

                MainMenu.Rect = new Rectangle(this.res5.X + 52, 39, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height);
					MainMenu.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu"];
					MainMenu.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_hover"];
					MainMenu.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_pressed"];
					MainMenu.launches = "Main Menu";
					MainMenu.Text = Localizer.Token(101);
				
				this.Buttons.Add(MainMenu);
				Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"].Width + 5);
				EmpireUIOverlay.Button Help = new EmpireUIOverlay.Button();

                Help.Rect = new Rectangle(this.res5.X + 72, 64, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height);
					Help.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_menu"];
					Help.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_menu_hover"];
					Help.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_menu_pressed"];
					Help.Text = "Help";
					Help.launches = "?";
				
				this.Buttons.Add(Help);
				return;
			}
			this.LowRes = true;
			Vector2 Cursor0 = Vector2.Zero;
			this.res1 = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"].Height);
			Cursor0.X = Cursor0.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"].Width;
			this.res2 = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"].Height);
			Cursor0.X = Cursor0.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"].Width;
			this.res3 = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"].Height);
			Cursor0.X = Cursor0.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"].Width;
			this.res4 = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"].Height);
			Cursor0.X = Cursor0.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"].Width;
			Cursor0.X = (float)(screen.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width);
			this.res5 = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Height);
			EmpireUIOverlay.Button r1n = new EmpireUIOverlay.Button()
			{
				Rect = this.res1,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_press"]
			};
			this.Buttons.Add(r1n);
			r1n.launches = "Research";
			EmpireUIOverlay.Button r2n = new EmpireUIOverlay.Button()
			{
				Rect = this.res2,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"]
			};
			this.Buttons.Add(r2n);
			r2n.launches = "Research";
			EmpireUIOverlay.Button r3n = new EmpireUIOverlay.Button()
			{
				Rect = this.res3,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_press"],
				launches = "Budget"
			};
			this.Buttons.Add(r3n);
			EmpireUIOverlay.Button r4n = new EmpireUIOverlay.Button()
			{
				Rect = this.res4,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				launches = "Budget"
			};
			this.Buttons.Add(r4n);
			EmpireUIOverlay.Button r5n = new EmpireUIOverlay.Button()
			{
				Rect = this.res5,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"]
			};
			this.Buttons.Add(r5n);
			float rangeforbuttons0 = (float)(r5n.Rect.X - (r4n.Rect.X + r4n.Rect.Width));
			float roomoneitherside0 = (rangeforbuttons0 - 607f) / 2f;
			Cursor0.X = (float)(r4n.Rect.X + r4n.Rect.Width) + roomoneitherside0 - 50f;
			EmpireUIOverlay.Button Shipyard0 = new EmpireUIOverlay.Button()
			{
				Rect = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				Text = Localizer.Token(98),
				launches = "Shipyard"
			};
			this.Buttons.Add(Shipyard0);
			Cursor0.X = Cursor0.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 18);
			EmpireUIOverlay.Button Empire0 = new EmpireUIOverlay.Button()
			{
				Rect = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				launches = "Empire",
				Text = Localizer.Token(99)
			};
			this.Buttons.Add(Empire0);
            Cursor0.X = Cursor0.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 18);
            EmpireUIOverlay.Button Espionage0 = new EmpireUIOverlay.Button()
            {
                Rect = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
                launches = "Espionage",
                Text = Localizer.Token(6088)
            };
            this.Buttons.Add(Espionage0);
			Cursor0.X = Cursor0.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 6);
			EmpireUIOverlay.Button Diplomacy0 = new EmpireUIOverlay.Button()
			{
				Rect = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				launches = "Diplomacy",
				Text = Localizer.Token(100)
			};
			this.Buttons.Add(Diplomacy0);
			Cursor0.X = Cursor0.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 6);
			EmpireUIOverlay.Button MainMenu0 = new EmpireUIOverlay.Button()
			{
                Rect = new Rectangle(this.res5.X + 52, 39, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_pressed"],
				launches = "Main Menu",
				Text = Localizer.Token(101)
			};
			this.Buttons.Add(MainMenu0);
			Cursor0.X = Cursor0.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_hover"].Width + 5);
			EmpireUIOverlay.Button Help0 = new EmpireUIOverlay.Button()
			{
                Rect = new Rectangle(this.res5.X + 72, 64, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px_pressed"],
				Text = "Help",
				launches = "?"
			};
			this.Buttons.Add(Help0);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (this.screen.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366 && !this.LowRes)
			{
				this.Buttons.Clear();
				this.ResetLowRes();
				this.LowRes = true;
				return;
			}
			Vector2 textCursor = new Vector2();
			foreach (EmpireUIOverlay.Button b in this.Buttons)
			{
                //make sure b.Text!=null
                //System.Diagnostics.Debug.Write(b.launches);
                //System.Diagnostics.Debug.Assert(b != null);
                //System.Diagnostics.Debug.Assert(b.Text != null);
                 
				if (!string.IsNullOrEmpty(b.Text))//&& b.Text != null)
				{
					textCursor.X = (float)(b.Rect.X + b.Rect.Width / 2) - Fonts.Arial12Bold.MeasureString(b.Text).X / 2f;
					textCursor.Y = (float)(b.Rect.Y + b.Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2 - (this.LowRes ? 1 : 0));
				}
				if (b.State == EmpireUIOverlay.PressState.Normal)
				{
					spriteBatch.Draw(b.NormalTexture, b.Rect, Color.White);
					if (string.IsNullOrEmpty(b.Text))
					{
						continue;
					}
					spriteBatch.DrawString(Fonts.Arial12Bold, b.Text, textCursor, new Color(255, 240, 189));
				}
				else if (b.State != EmpireUIOverlay.PressState.Hover)
				{
					if (b.State != EmpireUIOverlay.PressState.Pressed)
					{
						continue;
					}
					spriteBatch.Draw(b.PressedTexture, b.Rect, Color.White);
					if (string.IsNullOrEmpty(b.Text))
					{
						continue;
					}
					textCursor.Y = textCursor.Y + 1f;
					spriteBatch.DrawString(Fonts.Arial12Bold, b.Text, textCursor, new Color(255, 240, 189));
				}
				else
				{
					spriteBatch.Draw(b.HoverTexture, b.Rect, Color.White);
					if (string.IsNullOrEmpty(b.Text))
					{
						continue;
					}
					spriteBatch.DrawString(Fonts.Arial12Bold, b.Text, textCursor, new Color(255, 240, 189));
				}
			}
			int Money = (int)this.empire.Money;
			float plusMoney = 0f;
			foreach (Planet p in this.empire.GetPlanets())
			{
				plusMoney = plusMoney + (p.GrossMoneyPT + this.empire.data.Traits.TaxMod * p.GrossMoneyPT);
			}
			float TotalTradeIncome = 0f;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations())
			{
				if (!Relationship.Value.Treaty_Trade)
				{
					continue;
				}
				float TradeValue = -3f + 0.25f * (float)Relationship.Value.Treaty_Trade_TurnsExisted;
				if (TradeValue > 3f)
				{
					TradeValue = 3f;
				}
				TotalTradeIncome = TotalTradeIncome + TradeValue;
			}
			plusMoney = plusMoney + TotalTradeIncome;
			plusMoney = plusMoney + this.empire.data.FlatMoneyBonus;
			float EspionageBudget = 0f;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> r in this.empire.GetRelations())
			{
				EspionageBudget = EspionageBudget + r.Value.IntelligenceBudget;
			}
			EspionageBudget = EspionageBudget + EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CounterIntelligenceBudget;
			plusMoney = plusMoney - (this.empire.GetTotalBuildingMaintenance() + this.empire.GetTotalShipMaintenance() + EspionageBudget);
			float damoney = this.screen.player.EstimateIncomeAtTaxRate(this.screen.player.data.TaxRate);
			if (damoney <= 0f)
			{
				textCursor.X = (float)(this.res4.X + this.res2.Width - 30) - Fonts.Arial12Bold.MeasureString(string.Concat(Money.ToString(), " (", damoney.ToString("#.0"), ")")).X;
				textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
				spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Money.ToString(), " (", damoney.ToString("#.0"), ")"), textCursor, new Color(255, 240, 189));
			}
			else
			{
				textCursor.X = (float)(this.res4.X + this.res2.Width - 30) - Fonts.Arial12Bold.MeasureString(string.Concat(Money.ToString(), " (+", damoney.ToString("#.0"), ")")).X;
				textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
				spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Money.ToString(), " (+", damoney.ToString("#.0"), ")"), textCursor, new Color(255, 240, 189));
			}
			Vector2 StarDateCursor = new Vector2((float)(this.res5.X + 75), textCursor.Y);
			spriteBatch.DrawString(Fonts.Arial12Bold, (this.LowRes ? this.screen.StarDate.ToString(this.screen.StarDateFmt) : string.Concat("StarDate: ", this.screen.StarDate.ToString(this.screen.StarDateFmt))), StarDateCursor, new Color(255, 240, 189));
			//this.FirstRun = false;
			if (!this.LowRes)
			{
				if (string.IsNullOrEmpty(this.empire.ResearchTopic))
				{
					textCursor.X = (float)(this.res2.X + this.res2.Width - 30) - Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(102), "...")).X;
					textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
					spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(102), "..."), textCursor, new Color(255, 240, 189));
					return;
				}
				float percentResearch = this.empire.GetTDict()[this.empire.ResearchTopic].Progress / this.empire.GetTDict()[this.empire.ResearchTopic].GetTechCost() * UniverseScreen.GamePaceStatic;
				int xOffset = (int)(percentResearch * (float)this.res2.Width);
				Rectangle gradientSourceRect = this.res2;
				gradientSourceRect.X = 159 - xOffset;
				this.screen.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_gradient"], new Rectangle(this.res2.X, this.res2.Y, this.res2.Width, this.res2.Height), new Rectangle?(gradientSourceRect), Color.White);
				this.screen.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_over"], this.res2, Color.White);
				int research = (int)this.empire.GetTDict()[this.empire.ResearchTopic].Progress;
				float plusRes = this.empire.GetProjectedResearchNextTurn();
				float x = (float)(this.res2.X + this.res2.Width - 30);
				SpriteFont arial12Bold = Fonts.Arial12Bold;
				object[] str = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].GetTechCost() * UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
				textCursor.X = x - arial12Bold.MeasureString(string.Concat(str)).X;
				textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
				SpriteFont spriteFont = Fonts.Arial12Bold;
				object[] objArray = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].GetTechCost() * UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
				spriteBatch.DrawString(spriteFont, string.Concat(objArray), textCursor, new Color(255, 240, 189));
				return;
			}
			if (this.LowRes)
			{
				if (!string.IsNullOrEmpty(this.empire.ResearchTopic))
				{
					float percentResearch = this.empire.GetTDict()[this.empire.ResearchTopic].Progress / this.empire.GetTDict()[this.empire.ResearchTopic].GetTechCost() * UniverseScreen.GamePaceStatic;
					int xOffset = (int)(percentResearch * (float)this.res2.Width);
					Rectangle gradientSourceRect = this.res2;
					gradientSourceRect.X = 159 - xOffset;
					this.screen.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_gradient"], new Rectangle(this.res2.X, this.res2.Y, this.res2.Width, this.res2.Height), new Rectangle?(gradientSourceRect), Color.White);
					this.screen.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_over"], this.res2, Color.White);
					int research = (int)this.empire.GetTDict()[this.empire.ResearchTopic].Progress;
					float plusRes = this.empire.GetProjectedResearchNextTurn();
					float single = (float)(this.res2.X + this.res2.Width - 20);
					SpriteFont arial12Bold1 = Fonts.Arial12Bold;
					object[] str1 = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].GetTechCost() * UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
					textCursor.X = single - arial12Bold1.MeasureString(string.Concat(str1)).X;
					textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
					object[] objArray1 = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].GetTechCost() * UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
					string text = string.Concat(objArray1);
					if (Fonts.Arial12Bold.MeasureString(text).X <= 75f)
					{
						spriteBatch.DrawString(Fonts.Arial12Bold, text, textCursor, new Color(255, 240, 189));
						return;
					}
					float x1 = (float)(this.res2.X + this.res2.Width - 20);
					SpriteFont tahoma10 = Fonts.Tahoma10;
					object[] str2 = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].GetTechCost() * UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
					textCursor.X = x1 - tahoma10.MeasureString(string.Concat(str2)).X;
					textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Tahoma10.LineSpacing / 2);
					textCursor.X = (float)((int)textCursor.X);
					textCursor.Y = (float)((int)textCursor.Y);
					spriteBatch.DrawString(Fonts.Tahoma10, text, textCursor, new Color(255, 240, 189));
					return;
				}
				textCursor.X = (float)(this.res2.X + this.res2.Width - 30) - Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(102), "...")).X;
				textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
				spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(102), "..."), textCursor, new Color(255, 240, 189));
			}
		}

		public void HandleInput(InputState input)
		{
			this.currentMouse = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            if (input.CurrentKeyboardState.IsKeyDown(Keys.R) && !input.LastKeyboardState.IsKeyDown(Keys.R) && !GlobalStats.TakingInput)
			{
				AudioManager.PlayCue("echo_affirm");
				this.screen.ScreenManager.AddScreen(new ResearchScreenNew(this));
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.T) && !input.LastKeyboardState.IsKeyDown(Keys.T) && !GlobalStats.TakingInput)
			{
				AudioManager.PlayCue("echo_affirm");
				this.screen.ScreenManager.AddScreen(new BudgetScreen(Ship.universeScreen));
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Y) && !input.LastKeyboardState.IsKeyDown(Keys.Y) && !GlobalStats.TakingInput)
			{
				AudioManager.PlayCue("echo_affirm");
				this.screen.ScreenManager.AddScreen(new ShipDesignScreen(this));
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.U) && !input.LastKeyboardState.IsKeyDown(Keys.U) && !GlobalStats.TakingInput)
			{
				AudioManager.PlayCue("echo_affirm");
				this.screen.ScreenManager.AddScreen(new EmpireScreen(Ship.universeScreen.ScreenManager, this));
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.I) && !input.LastKeyboardState.IsKeyDown(Keys.I) && !GlobalStats.TakingInput)
			{
				AudioManager.PlayCue("echo_affirm");
				this.screen.ScreenManager.AddScreen(new MainDiplomacyScreen(Ship.universeScreen));
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.O) && !input.LastKeyboardState.IsKeyDown(Keys.O) && !GlobalStats.TakingInput)
			{
				AudioManager.PlayCue("echo_affirm");
				this.screen.ScreenManager.AddScreen(new GameplayMMScreen(Ship.universeScreen));
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.E) && !input.LastKeyboardState.IsKeyDown(Keys.E) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.screen.ScreenManager.AddScreen(new EspionageScreen(Ship.universeScreen));
            }
			if (input.CurrentKeyboardState.IsKeyDown(Keys.P) && !input.LastKeyboardState.IsKeyDown(Keys.P) && !GlobalStats.TakingInput)
			{
				AudioManager.PlayCue("sd_ui_tactical_pause");
				InGameWiki wiki = new InGameWiki(new Rectangle(0, 0, 750, 600))
				{
					TitleText = Localizer.Token(2304),
					MiddleText = Localizer.Token(2303)
				};
				this.screen.ScreenManager.AddScreen(wiki);
			}
			foreach (EmpireUIOverlay.Button b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = EmpireUIOverlay.PressState.Normal;
				}
				else
				{
					string str = b.launches;
					string str1 = str;
					if (str != null)
					{
						switch (str1)
						{
							case "Research":
							{
								string res = (ResourceManager.TechTree.ContainsKey(Ship.universeScreen.player.ResearchTopic) ? Localizer.Token(ResourceManager.TechTree[Ship.universeScreen.player.ResearchTopic].NameIndex) : Localizer.Token(341));
								string[] strArrays = new string[] { Localizer.Token(2306), "\n\n", Localizer.Token(1405), ": ", res };
								ToolTip.CreateTooltip(string.Concat(strArrays), Ship.universeScreen.ScreenManager, "R");
								break;
							}
							case "Budget":
							{
								ToolTip.CreateTooltip(Localizer.Token(2305), Ship.universeScreen.ScreenManager, "T");
								break;
							}
							case "Main Menu":
							{
								ToolTip.CreateTooltip(Localizer.Token(2301), Ship.universeScreen.ScreenManager, "O");
								break;
							}
							case "Shipyard":
							{
								ToolTip.CreateTooltip(Localizer.Token(2297), Ship.universeScreen.ScreenManager, "Y");
								break;
							}
							case "Empire":
							{
								ToolTip.CreateTooltip(Localizer.Token(2298), Ship.universeScreen.ScreenManager, "U");
								break;
							}
							case "Diplomacy":
							{
								ToolTip.CreateTooltip(Localizer.Token(2299), Ship.universeScreen.ScreenManager, "I");
								break;
							}
                            case "Espionage":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(7043), Ship.universeScreen.ScreenManager, "E");
                                break;
                            }
                            case "ShipList":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(7044), Ship.universeScreen.ScreenManager, "K");
                                break;
                            }
                            case "Fleets":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(7045), Ship.universeScreen.ScreenManager, "J");
                                break;
                            }
							case "?":
							{
								ToolTip.CreateTooltip(Localizer.Token(2302), Ship.universeScreen.ScreenManager, "P");
								break;
							}
						}
					}
					if (b.State != EmpireUIOverlay.PressState.Hover && b.State != EmpireUIOverlay.PressState.Pressed)
					{
						AudioManager.PlayCue("mouse_over4");
					}
					b.State = EmpireUIOverlay.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = EmpireUIOverlay.PressState.Pressed;
					}
					if (!input.InGameSelect)
					{
						continue;
					}
					string str2 = b.launches;
					string str3 = str2;
					if (str2 != null)
					{
						if (str3 == "Research")
						{
							AudioManager.PlayCue("echo_affirm");
							this.screen.ScreenManager.AddScreen(new ResearchScreenNew(this));
						}
						else if (str3 == "Budget")
						{
							AudioManager.PlayCue("echo_affirm");
							this.screen.ScreenManager.AddScreen(new BudgetScreen(this.screen));
						}
					}
					string str4 = b.launches;
					string str5 = str4;
					if (str4 == null)
					{
						continue;
					}
					if (str5 == "Main Menu")
					{
						AudioManager.PlayCue("echo_affirm");
						this.screen.ScreenManager.AddScreen(new GameplayMMScreen(this.screen));
					}
					else if (str5 == "Shipyard")
					{
						AudioManager.PlayCue("echo_affirm");
						this.screen.ScreenManager.AddScreen(new ShipDesignScreen(this));
					}
                    else if (str5 == "Fleets")
                    {
                        AudioManager.PlayCue("echo_affirm");
                        this.screen.ScreenManager.AddScreen(new FleetDesignScreen(this));
                    }
                    else if (str5 == "ShipList")
                    {
                        AudioManager.PlayCue("echo_affirm");
                        this.screen.ScreenManager.AddScreen(new ShipListScreen(this.screen.ScreenManager, this));
                    }
					else if (str5 == "Empire")
					{
						this.screen.ScreenManager.AddScreen(new EmpireScreen(this.screen.ScreenManager, this));
						AudioManager.PlayCue("echo_affirm");
					}
					else if (str5 == "Diplomacy")
					{
						this.screen.ScreenManager.AddScreen(new MainDiplomacyScreen(this.screen));
						AudioManager.PlayCue("echo_affirm");
					}
                    else if (str5 == "Espionage")
                    {
                        this.screen.ScreenManager.AddScreen(new EspionageScreen(this.screen));
                        AudioManager.PlayCue("echo_affirm");
                    }
					else if (str5 == "?")
					{
						AudioManager.PlayCue("sd_ui_tactical_pause");
						InGameWiki wiki = new InGameWiki(new Rectangle(0, 0, 750, 600))
						{
							TitleText = Localizer.Token(2304),
							MiddleText = Localizer.Token(2303)
						};
						this.screen.ScreenManager.AddScreen(wiki);
					}
				}
			}
			this.previousMouse = Mouse.GetState();
		}

		public void HandleInput(InputState input, GameScreen caller)
		{
			this.currentMouse = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			foreach (EmpireUIOverlay.Button b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = EmpireUIOverlay.PressState.Normal;
				}
				else
				{
					if (b.State != EmpireUIOverlay.PressState.Hover && b.State != EmpireUIOverlay.PressState.Pressed)
					{
						AudioManager.PlayCue("mouse_over4");
					}
					b.State = EmpireUIOverlay.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = EmpireUIOverlay.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
					{
						continue;
					}
                    if (!(caller is ShipDesignScreen) && !(caller is FleetDesignScreen))
					{
						caller.ExitScreen();
					}
                    else if (b.launches != "Shipyard" && b.launches != "Fleets")
					{
                        if (caller is ShipDesignScreen)
                        {
                            (caller as ShipDesignScreen).ExitToMenu(b.launches);
                        }
                        else if (caller is FleetDesignScreen)
                        {
                            (caller as FleetDesignScreen).ExitScreen();
                        }
						return;
					}
                    else if (caller is FleetDesignScreen && b.launches != "Fleets")
                    {
                        (caller as FleetDesignScreen).ExitScreen();
                    }
					string str = b.launches;
					string str1 = str;
					if (str != null)
					{
						if (str1 == "Research")
						{
							AudioManager.PlayCue("echo_affirm");
							if (!(caller is ResearchScreenNew))
							{
								this.screen.ScreenManager.AddScreen(new ResearchScreenNew(this));
							}
						}
						else if (str1 == "Budget")
						{
							AudioManager.PlayCue("echo_affirm");
							if (!(caller is BudgetScreen))
							{
								this.screen.ScreenManager.AddScreen(new BudgetScreen(this.screen));
							}
						}
					}
					string str2 = b.launches;
					string str3 = str2;
					if (str2 == null)
					{
						continue;
					}
					if (str3 == "Main Menu")
					{
						AudioManager.PlayCue("echo_affirm");
						this.screen.ScreenManager.AddScreen(new GameplayMMScreen(this.screen, caller));
					}
					else if (str3 == "Shipyard")
					{
						if (caller is ShipDesignScreen)
						{
							continue;
						}
						AudioManager.PlayCue("echo_affirm");
						this.screen.ScreenManager.AddScreen(new ShipDesignScreen(this));
					}
                    else if (str3 == "Fleets")
                    {
                        if (caller is FleetDesignScreen)
                        {
                            continue;
                        }
                        AudioManager.PlayCue("echo_affirm");
                        this.screen.ScreenManager.AddScreen(new FleetDesignScreen(this));
                    }
					else if (str3 == "Empire")
					{
						this.screen.ScreenManager.AddScreen(new EmpireScreen(this.screen.ScreenManager, this));
						AudioManager.PlayCue("echo_affirm");
					}
					else if (str3 == "Diplomacy")
					{
						this.screen.ScreenManager.AddScreen(new MainDiplomacyScreen(this.screen));
						AudioManager.PlayCue("echo_affirm");
					}
					else if (str3 == "?")
					{
						AudioManager.PlayCue("sd_ui_tactical_pause");
						InGameWiki wiki = new InGameWiki(new Rectangle(0, 0, 750, 600))
						{
							TitleText = "StarDrive Help",
							MiddleText = "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
						};
					}
				}
			}
			this.previousMouse = Mouse.GetState();
		}

		private void ResetLowRes()
		{
			this.LowRes = true;
			Vector2 Cursor = Vector2.Zero;
			this.res1 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"].Height);
			Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"].Width;
			this.res2 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"].Height);
			Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"].Width;
			this.res3 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"].Height);
			Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"].Width;
			this.res4 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"].Height);
			Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"].Width;
			Cursor.X = (float)(this.screen.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width);
			this.res5 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Height);
			EmpireUIOverlay.Button r1 = new EmpireUIOverlay.Button()
			{
				Rect = this.res1,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_press"]
			};
			this.Buttons.Add(r1);
			r1.launches = "Research";
			EmpireUIOverlay.Button r2 = new EmpireUIOverlay.Button()
			{
				Rect = this.res2,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"]
			};
			this.Buttons.Add(r2);
			r2.launches = "Research";
			EmpireUIOverlay.Button r3 = new EmpireUIOverlay.Button()
			{
				Rect = this.res3,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_press"],
				launches = "Budget"
			};
			this.Buttons.Add(r3);
			EmpireUIOverlay.Button r4 = new EmpireUIOverlay.Button()
			{
				Rect = this.res4,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"]
			};
			this.Buttons.Add(r4);
			EmpireUIOverlay.Button r5 = new EmpireUIOverlay.Button()
			{
				Rect = this.res5,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"]
			};
			this.Buttons.Add(r5);
			float rangeforbuttons = (float)(r5.Rect.X - (r4.Rect.X + r4.Rect.Width));
			float roomoneitherside = (rangeforbuttons - 607f) / 2f;
			Cursor.X = (float)(r4.Rect.X + r4.Rect.Width) + roomoneitherside - 50f;
			EmpireUIOverlay.Button Shipyard = new EmpireUIOverlay.Button()
			{
				Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				Text = Localizer.Token(98),
				launches = "Shipyard"
			};
			this.Buttons.Add(Shipyard);
			Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 18);
			EmpireUIOverlay.Button Empire = new EmpireUIOverlay.Button()
			{
				Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				launches = "Empire",
				Text = Localizer.Token(99)
			};
			this.Buttons.Add(Empire);
            Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 18);
            EmpireUIOverlay.Button Espionage = new EmpireUIOverlay.Button()
            {
                Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
                launches = "Espionage",
                Text = Localizer.Token(6088)
            };
            this.Buttons.Add(Espionage);
			Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 6);
			EmpireUIOverlay.Button Diplomacy = new EmpireUIOverlay.Button()
			{
				Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				launches = "Diplomacy",
				Text = Localizer.Token(100)
			};
			this.Buttons.Add(Diplomacy);
			Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 6);
			EmpireUIOverlay.Button MainMenu = new EmpireUIOverlay.Button()
			{
                Rect = new Rectangle(this.res5.X+ 52, 39, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_pressed"],
				launches = "Main Menu",
				Text = Localizer.Token(101)
			};
			this.Buttons.Add(MainMenu);
			Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_hover"].Width + 5);
			EmpireUIOverlay.Button Help = new EmpireUIOverlay.Button()
			{
                Rect = new Rectangle(this.res5.X + 72, 64, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px_pressed"],
				Text = "Help",
				launches = "?"
			};
			this.Buttons.Add(Help);
		}

		public void Update(float elapsedTime)
		{
			Vector2 vector2 = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
		}

		public class Button
		{
			public Rectangle Rect;

			public EmpireUIOverlay.PressState State;

			public Texture2D NormalTexture;

			public Texture2D HoverTexture;

			public Texture2D PressedTexture;

			public string Text;

			public string launches;

			public Button()
			{
                Text = "";
			}
		}

		public enum PressState
		{
			Normal,
			Hover,
			Pressed
		}
	}
}