using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class EmpireUIOverlay
	{
		public Empire empire;

		private Rectangle res1;

		private Rectangle res2;

		private Rectangle res3;

		private Rectangle res4;

		private Rectangle res5;

		private Array<ToolTip> ToolTips = new Array<ToolTip>();

		private Array<Button> Buttons = new Array<Button>();

		private bool LowRes;

		private MouseState currentMouse;

		private MouseState previousMouse;

		//private float TipTimer = 0.35f;

		//private bool FirstRun = true;

		public EmpireUIOverlay(Empire playerEmpire, GraphicsDevice device)
		{
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
				Cursor.X = (float)(Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"].Width);
				this.res5 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"].Height);
				Button r1 = new Button();
				
					r1.Rect = this.res1;
					r1.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1"];
					r1.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1_hover"];
					r1.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res1_press"];
                    r1.launches = "Research";
				
				this.Buttons.Add(r1);
				Button r2 = new Button();
				
					r2.Rect = this.res2;
					r2.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"];
					r2.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"];
					r2.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2"];
                    r2.launches = "Research";
				
				this.Buttons.Add(r2);
				Button r3 = new Button();
				
                    r3.Rect = this.res3;
                    r3.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3"];
                    r3.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3_hover"];
                    r3.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res3_press"];
                    r3.launches = "Budget";
				
				this.Buttons.Add(r3);
				Button r4 = new Button();
				
					r4.Rect = this.res4;
                    r4.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"];
                    r4.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"];
                    r4.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res4"];
                    r4.launches = "Budget";
				
				this.Buttons.Add(r4);
				Button r5 = new Button();
				
					r5.Rect = this.res5;
					r5.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"];
					r5.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"];
                    r5.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res5"];
				
				this.Buttons.Add(r5);
				float rangeforbuttons = (float)(r5.Rect.X - (r4.Rect.X + r4.Rect.Width));
				float roomoneitherside = (rangeforbuttons - 734f) / 2f;
                //Added by McShooterz: Shifted buttons to add new ones, added dummy espionage button
				Cursor.X = (float)(r4.Rect.X + r4.Rect.Width) + roomoneitherside;


                if (Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth >= 1920)
                {
                    float saveY = Cursor.Y;
                    
                    Cursor.X -= 220f;
                    //saveY = Cursor.Y + 5;

                    //Cursor.Y += ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height;

                    Button ShipList = new Button();

                    ShipList.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                    ShipList.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
                    ShipList.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
                    ShipList.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
                    ShipList.Text = Localizer.Token(104);
                    ShipList.launches = "ShipList";

                    this.Buttons.Add(ShipList);
                    Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
                    Button Fleets = new Button();

                    Fleets.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                    Fleets.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
                    Fleets.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
                    Fleets.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
                    Fleets.Text = Localizer.Token(103);
                    Fleets.launches = "Fleets";

                    this.Buttons.Add(Fleets);
                    Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
                    Cursor.Y = saveY;
                    
                }
                else
                {
                    Cursor.X -= 50f;                    
                   
                }
				Button Shipyard = new Button();

                Shipyard.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				Shipyard.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
				Shipyard.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
				Shipyard.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
				Shipyard.Text = Localizer.Token(98);
				Shipyard.launches = "Shipyard";
				
				this.Buttons.Add(Shipyard);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 40;
				Button empire = new Button();
                empire.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                empire.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
                empire.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
                empire.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
                empire.launches = "Empire";
                empire.Text = Localizer.Token(99);
				
				this.Buttons.Add(empire);
				Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 40;
                Button Espionage = new Button();

                Espionage.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                Espionage.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"];
                Espionage.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"];
                Espionage.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"];
                Espionage.Text = Localizer.Token(6088);
                Espionage.launches = "Espionage";

                this.Buttons.Add(Espionage);
                Cursor.X = Cursor.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
				Button Diplomacy = new Button();

                Diplomacy.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				Diplomacy.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip"];
				Diplomacy.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_hover"];
				Diplomacy.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_dip_pressed"];
				Diplomacy.launches = "Diplomacy";
				Diplomacy.Text = Localizer.Token(100);
				
				this.Buttons.Add(Diplomacy);
				Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 7);
				Button MainMenu = new Button();

                MainMenu.Rect = new Rectangle(this.res5.X + 52, 39, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height);
					MainMenu.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu"];
					MainMenu.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_hover"];
					MainMenu.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_pressed"];
					MainMenu.launches = "Main Menu";
					MainMenu.Text = Localizer.Token(101);
				
				this.Buttons.Add(MainMenu);
				Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"].Width + 5);
				Button Help = new Button();

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
			Cursor0.X = (float)(Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width);
			this.res5 = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Height);
			Button r1n = new Button()
			{
				Rect = this.res1,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_press"]
			};
			this.Buttons.Add(r1n);
			r1n.launches = "Research";
			Button r2n = new Button()
			{
				Rect = this.res2,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"]
			};
			this.Buttons.Add(r2n);
			r2n.launches = "Research";
			Button r3n = new Button()
			{
				Rect = this.res3,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_press"],
				launches = "Budget"
			};
			this.Buttons.Add(r3n);
			Button r4n = new Button()
			{
				Rect = this.res4,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				launches = "Budget"
			};
			this.Buttons.Add(r4n);
			Button r5n = new Button()
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
			Button Shipyard0 = new Button()
			{
				Rect = new Rectangle((int)Cursor0.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				Text = Localizer.Token(98),
				launches = "Shipyard"
			};
			this.Buttons.Add(Shipyard0);
            {
                float saveY = Cursor0.Y;
                float saveX = Cursor0.X;
                saveY = Cursor0.Y + 5;

                Cursor0.Y += ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height;

                Button ShipList = new Button();

                ShipList.Rect = new Rectangle((int)Cursor0.X, (int)Cursor0.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                ShipList.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
                ShipList.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
                ShipList.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
                ShipList.Text = Localizer.Token(104);
                ShipList.launches = "ShipList";

                this.Buttons.Add(ShipList);
                Cursor0.X = Cursor0.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
                Button Fleets = new Button();

                Fleets.Rect = new Rectangle((int)Cursor0.X, (int)Cursor0.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
                Fleets.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military"];
                Fleets.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_hover"];
                Fleets.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_military_pressed"];
                Fleets.Text = Localizer.Token(103);
                Fleets.launches = "Fleets";

                this.Buttons.Add(Fleets);
                Cursor0.X = Cursor0.X + (float)ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"].Width + 5;
                Cursor0.Y = saveY;
                Cursor0.X = saveX;
            }
			Cursor0.X = Cursor0.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 18);
			Button Empire0 = new Button()
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
            Button Espionage0 = new Button()
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
			Button Diplomacy0 = new Button()
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
			Button MainMenu0 = new Button()
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
			Button Help0 = new Button()
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
			if (Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366 && !this.LowRes)
			{
				this.Buttons.Clear();
				this.ResetLowRes();
				this.LowRes = true;
				return;
			}
			Vector2 textCursor = new Vector2();
			foreach (Button b in this.Buttons)
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
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in EmpireManager.Player.AllRelations)
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
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> r in this.empire.AllRelations)
			{
				EspionageBudget = EspionageBudget + r.Value.IntelligenceBudget;
			}
			EspionageBudget = EspionageBudget + EmpireManager.Player.data.CounterIntelligenceBudget;
			plusMoney = plusMoney - (this.empire.GetTotalBuildingMaintenance() + this.empire.GetTotalShipMaintenance() + EspionageBudget);
			float damoney = Empire.Universe.player.EstimateIncomeAtTaxRate(Empire.Universe.player.data.TaxRate);
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
			spriteBatch.DrawString(Fonts.Arial12Bold, (this.LowRes ? Empire.Universe.StarDate.ToString(Empire.Universe.StarDateFmt) : string.Concat("StarDate: ", Empire.Universe.StarDate.ToString(Empire.Universe.StarDateFmt))), StarDateCursor, new Color(255, 240, 189));
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
				float percentResearch = this.empire.GetTDict()[this.empire.ResearchTopic].Progress / this.empire.GetTDict()[this.empire.ResearchTopic].TechCost* UniverseScreen.GamePaceStatic;
				int xOffset = (int)(percentResearch * (float)this.res2.Width);
				Rectangle gradientSourceRect = this.res2;
				gradientSourceRect.X = 159 - xOffset;
                Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_gradient"], new Rectangle(this.res2.X, this.res2.Y, this.res2.Width, this.res2.Height), new Rectangle?(gradientSourceRect), Color.White);
                Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_over"], this.res2, Color.White);
				int research = (int)this.empire.GetTDict()[this.empire.ResearchTopic].Progress;
				float plusRes = this.empire.GetProjectedResearchNextTurn();
				float x = (float)(this.res2.X + this.res2.Width - 30);
				SpriteFont arial12Bold = Fonts.Arial12Bold;
				object[] str = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].TechCost* UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
				textCursor.X = x - arial12Bold.MeasureString(string.Concat(str)).X;
				textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
				SpriteFont spriteFont = Fonts.Arial12Bold;
				object[] objArray = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].TechCost* UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
				spriteBatch.DrawString(spriteFont, string.Concat(objArray), textCursor, new Color(255, 240, 189));
				return;
			}
			if (this.LowRes)
			{
				if (!string.IsNullOrEmpty(this.empire.ResearchTopic))
				{
					float percentResearch = this.empire.GetTDict()[this.empire.ResearchTopic].Progress / this.empire.GetTDict()[this.empire.ResearchTopic].TechCost* UniverseScreen.GamePaceStatic;
					int xOffset = (int)(percentResearch * (float)this.res2.Width);
					Rectangle gradientSourceRect = this.res2;
					gradientSourceRect.X = 159 - xOffset;
                    Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_gradient"], new Rectangle(this.res2.X, this.res2.Y, this.res2.Width, this.res2.Height), new Rectangle?(gradientSourceRect), Color.White);
                    Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_res2_over"], this.res2, Color.White);
					int research = (int)this.empire.GetTDict()[this.empire.ResearchTopic].Progress;
					float plusRes = this.empire.GetProjectedResearchNextTurn();
					float single = (float)(this.res2.X + this.res2.Width - 20);
					SpriteFont arial12Bold1 = Fonts.Arial12Bold;
					object[] str1 = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].TechCost* UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
					textCursor.X = single - arial12Bold1.MeasureString(string.Concat(str1)).X;
					textCursor.Y = (float)(this.res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
					object[] objArray1 = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].TechCost* UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
					string text = string.Concat(objArray1);
					if (Fonts.Arial12Bold.MeasureString(text).X <= 75f)
					{
						spriteBatch.DrawString(Fonts.Arial12Bold, text, textCursor, new Color(255, 240, 189));
						return;
					}
					float x1 = (float)(this.res2.X + this.res2.Width - 20);
					SpriteFont tahoma10 = Fonts.Tahoma10;
					object[] str2 = new object[] { research.ToString(), "/", this.empire.GetTDict()[this.empire.ResearchTopic].TechCost* UniverseScreen.GamePaceStatic, " (+", plusRes.ToString("#.0"), ")" };
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
            if (input.KeysCurr.IsKeyDown(Keys.R) && !input.KeysPrev.IsKeyDown(Keys.R) && !GlobalStats.TakingInput)
			{
				GameAudio.PlaySfxAsync("echo_affirm");
                Empire.Universe.ScreenManager.AddScreen(new ResearchScreenNew(Empire.Universe, this));
			}
            if (input.KeysCurr.IsKeyDown(Keys.T) && !input.KeysPrev.IsKeyDown(Keys.T) && !GlobalStats.TakingInput)
			{
				GameAudio.PlaySfxAsync("echo_affirm");
                Empire.Universe.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
			}
            if (input.KeysCurr.IsKeyDown(Keys.Y) && !input.KeysPrev.IsKeyDown(Keys.Y) && !GlobalStats.TakingInput)
			{
				GameAudio.PlaySfxAsync("echo_affirm");
                Empire.Universe.ScreenManager.AddScreen(new ShipDesignScreen(Empire.Universe, this));
			}
            if (input.KeysCurr.IsKeyDown(Keys.U) && !input.KeysPrev.IsKeyDown(Keys.U) && !GlobalStats.TakingInput)
			{
				GameAudio.PlaySfxAsync("echo_affirm");
                Empire.Universe.ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, this));
			}
            if (input.KeysCurr.IsKeyDown(Keys.I) && !input.KeysPrev.IsKeyDown(Keys.I) && !GlobalStats.TakingInput)
			{
				GameAudio.PlaySfxAsync("echo_affirm");
                Empire.Universe.ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
			}
            if (input.KeysCurr.IsKeyDown(Keys.O) && !input.KeysPrev.IsKeyDown(Keys.O) && !GlobalStats.TakingInput)
			{
				GameAudio.PlaySfxAsync("echo_affirm");
                Empire.Universe.ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
			}
            if (input.KeysCurr.IsKeyDown(Keys.E) && !input.KeysPrev.IsKeyDown(Keys.E) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                Empire.Universe.ScreenManager.AddScreen(new EspionageScreen(Empire.Universe));
            }
			if (input.KeysCurr.IsKeyDown(Keys.P) && !input.KeysPrev.IsKeyDown(Keys.P) && !GlobalStats.TakingInput)
			{
				GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
				InGameWiki wiki = new InGameWiki(Empire.Universe)
				{
					TitleText = Localizer.Token(2304),
					MiddleText = Localizer.Token(2303)
				};
                Empire.Universe.ScreenManager.AddScreen(wiki);
			}
			foreach (Button b in this.Buttons)
			{
				if (!b.Rect.HitTest(MousePos))
				{
					b.State = PressState.Normal;
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
								string res = (ResourceManager.TechTree.ContainsKey(Empire.Universe.player.ResearchTopic) ? Localizer.Token(ResourceManager.TechTree[Empire.Universe.player.ResearchTopic].NameIndex) : Localizer.Token(341));
								string[] strArrays = { Localizer.Token(2306), "\n\n", Localizer.Token(1405), ": ", res };
								ToolTip.CreateTooltip(string.Concat(strArrays), "R");
								break;
							}
							case "Budget":
							{
								ToolTip.CreateTooltip(Localizer.Token(2305), "T");
								break;
							}
							case "Main Menu":
							{
								ToolTip.CreateTooltip(Localizer.Token(2301), "O");
								break;
							}
							case "Shipyard":
							{
								ToolTip.CreateTooltip(Localizer.Token(2297), "Y");
								break;
							}
							case "Empire":
							{
								ToolTip.CreateTooltip(Localizer.Token(2298), "U");
								break;
							}
							case "Diplomacy":
							{
								ToolTip.CreateTooltip(Localizer.Token(2299), "I");
								break;
							}
                            case "Espionage":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(7043), "E");
                                break;
                            }
                            case "ShipList":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(7044), "K");
                                break;
                            }
                            case "Fleets":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(7045), "J");
                                break;
                            }
							case "?":
							{
								ToolTip.CreateTooltip(Localizer.Token(2302), "P");
								break;
							}
						}
					}
					if (b.State != EmpireUIOverlay.PressState.Hover && b.State != EmpireUIOverlay.PressState.Pressed)
					{
						GameAudio.PlaySfxAsync("mouse_over4");
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
							GameAudio.PlaySfxAsync("echo_affirm");
                            Empire.Universe.ScreenManager.AddScreen(new ResearchScreenNew(Empire.Universe, this));
						}
						else if (str3 == "Budget")
						{
							GameAudio.PlaySfxAsync("echo_affirm");
                            Empire.Universe.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
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
						GameAudio.PlaySfxAsync("echo_affirm");
						Empire.Universe.ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
					}
					else if (str5 == "Shipyard")
					{
						GameAudio.PlaySfxAsync("echo_affirm");
                        Empire.Universe.ScreenManager.AddScreen(new ShipDesignScreen(Empire.Universe, this));
					}
                    else if (str5 == "Fleets")
                    {
                        GameAudio.PlaySfxAsync("echo_affirm");
                        Empire.Universe.ScreenManager.AddScreen(new FleetDesignScreen(Empire.Universe, this));
                    }
                    else if (str5 == "ShipList")
                    {
                        GameAudio.PlaySfxAsync("echo_affirm");
                        Empire.Universe.ScreenManager.AddScreen(new ShipListScreen(Empire.Universe, this));
                    }
					else if (str5 == "Empire")
					{
                        Empire.Universe.ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, this));
						GameAudio.PlaySfxAsync("echo_affirm");
					}
					else if (str5 == "Diplomacy")
					{
                        Empire.Universe.ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
						GameAudio.PlaySfxAsync("echo_affirm");
					}
                    else if (str5 == "Espionage")
                    {
                        Empire.Universe.ScreenManager.AddScreen(new EspionageScreen(Empire.Universe));
                        GameAudio.PlaySfxAsync("echo_affirm");
                    }
					else if (str5 == "?")
					{
						GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
						InGameWiki wiki = new InGameWiki(Empire.Universe)
						{
							TitleText = Localizer.Token(2304),
							MiddleText = Localizer.Token(2303)
						};
                        Empire.Universe.ScreenManager.AddScreen(wiki);
					}
				}
			}
			this.previousMouse = Mouse.GetState();
		}

		public void HandleInput(InputState input, GameScreen caller)
		{
			this.currentMouse = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			foreach (Button b in this.Buttons)
			{
				if (!b.Rect.HitTest(MousePos))
				{
					b.State = EmpireUIOverlay.PressState.Normal;
				}
				else
				{
                    
                    if (b.State != EmpireUIOverlay.PressState.Hover && b.State != EmpireUIOverlay.PressState.Pressed)
					{
						GameAudio.PlaySfxAsync("mouse_over4");
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
                            (caller as ShipDesignScreen)//.ExitScreen();
                                .ExitToMenu(b.launches);
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
							GameAudio.PlaySfxAsync("echo_affirm");
							if (!(caller is ResearchScreenNew))
							{
                                Empire.Universe.ScreenManager.AddScreen(new ResearchScreenNew(Empire.Universe, this));
							}
						}
						else if (str1 == "Budget")
						{
							GameAudio.PlaySfxAsync("echo_affirm");
							if (!(caller is BudgetScreen))
							{
                                Empire.Universe.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
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
						GameAudio.PlaySfxAsync("echo_affirm");
                        Empire.Universe.ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe, caller));
					}
					else if (str3 == "Shipyard")
					{
						if (caller is ShipDesignScreen)
						{
							continue;
						}
						GameAudio.PlaySfxAsync("echo_affirm");
                        Empire.Universe.ScreenManager.AddScreen(new ShipDesignScreen(Empire.Universe, this));
					}
                    else if (str3 == "Fleets")
                    {
                        if (caller is FleetDesignScreen)
                        {
                            continue;
                        }
                        GameAudio.PlaySfxAsync("echo_affirm");
                        Empire.Universe.ScreenManager.AddScreen(new FleetDesignScreen(Empire.Universe, this));
                    }
					else if (str3 == "Empire")
					{
                        Empire.Universe.ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, this));
						GameAudio.PlaySfxAsync("echo_affirm");
					}
					else if (str3 == "Diplomacy")
					{
                        Empire.Universe.ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
						GameAudio.PlaySfxAsync("echo_affirm");
					}
					else if (str3 == "?")
					{
						GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
						InGameWiki wiki = new InGameWiki(Empire.Universe)
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
			Cursor.X = (float)(Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width);
			this.res5 = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res5"].Height);
			Button r1 = new Button()
			{
				Rect = this.res1,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res1_press"]
			};
			this.Buttons.Add(r1);
			r1.launches = "Research";
			Button r2 = new Button()
			{
				Rect = this.res2,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res2"]
			};
			this.Buttons.Add(r2);
			r2.launches = "Research";
			Button r3 = new Button()
			{
				Rect = this.res3,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res3_press"],
				launches = "Budget"
			};
			this.Buttons.Add(r3);
			Button r4 = new Button()
			{
				Rect = this.res4,
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_res4"]
			};
			this.Buttons.Add(r4);
			Button r5 = new Button()
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
			Button Shipyard = new Button()
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
			Button empire = new Button()
			{
				Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_pressed"],
				launches = "Empire",
				Text = Localizer.Token(99)
			};
			this.Buttons.Add(empire);
            Cursor.X = Cursor.X + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_124px_hover"].Width + 18);
            Button Espionage = new Button()
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
			Button Diplomacy = new Button()
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
			Button MainMenu = new Button()
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
			Button Help = new Button()
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
		}

		public class Button
		{
			public Rectangle Rect;
			public PressState State;
			public Texture2D NormalTexture;
			public Texture2D HoverTexture;
			public Texture2D PressedTexture;
			public string Text = "";
			public string launches;
		}

		public enum PressState
		{
			Normal,
			Hover,
			Pressed
		}
	}
}