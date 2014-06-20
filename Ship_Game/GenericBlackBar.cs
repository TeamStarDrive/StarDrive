using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class GenericBlackBar : GameScreen, IDisposable
	{
		private Matrix worldMatrix = Matrix.Identity;

		private Matrix view;

		//private Matrix projection;

		public Camera2d camera;

		public ShipData ActiveHull;

		//private Menu1 ModuleSelectionMenu;

		//private Model ActiveModel;

		//private SceneObject shipSO;

		private Vector3 cameraPosition = new Vector3(0f, 0f, 1300f);

		public List<SlotStruct> Slots = new List<SlotStruct>();

		private List<UIButton> Buttons = new List<UIButton>();

		private Rectangle SearchBar;

		private Rectangle bottom_sep;

		private Rectangle HullSelectionRect;

		private Submenu hullSelectionSub;

		private Rectangle BlackBar;

		//private Rectangle SideBar;

		private DanButton Fleets;

		private DanButton ShipList;

		private DanButton Shipyard;

		private MouseState mouseStateCurrent;

		private MouseState mouseStatePrevious;

		//private ShipModule HighlightedModule;

		private Vector2 cameraVelocity = Vector2.Zero;

		//private Vector2 StartDragPos = new Vector2();

		//private bool ShowAllArcs;

		//private ShipModule HoveredModule;

		//private Selector selector;

		public GenericBlackBar()
		{
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
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

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.SpriteBatch.Begin();
			this.DrawUI(gameTime);
			base.ScreenManager.SpriteBatch.End();
		}

		private void DrawUI(GameTime gameTime)
		{
			float transitionOffset = (float)Math.Pow((double)base.TransitionPosition, 2);
			Rectangle r = this.BlackBar;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, Color.Black);
			r = this.bottom_sep;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, new Color(77, 55, 25));
			r = this.SearchBar;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, new Color(54, 54, 54));
			Vector2 vector2 = new Vector2((float)(this.SearchBar.X + 3), (float)(r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2));
			r = this.Fleets.r;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.Fleets.Draw(base.ScreenManager, r);
			r = this.ShipList.r;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.ShipList.Draw(base.ScreenManager, r);
			r = this.Shipyard.r;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.Shipyard.Draw(base.ScreenManager, r);
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		public override void HandleInput(InputState input)
		{
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, input.CursorPosition))
				{
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					b.State = UIButton.PressState.Hover;
					if (this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.mouseStateCurrent.LeftButton != ButtonState.Released || this.mouseStatePrevious.LeftButton != ButtonState.Pressed)
					{
						continue;
					}
					string text = b.Text;
				}
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.BlackBar = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70, 2000, 70);
			this.bottom_sep = new Rectangle(this.BlackBar.X, this.BlackBar.Y, this.BlackBar.Width, 1);
			this.HullSelectionRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 285, 100, 280, 400);
			this.hullSelectionSub = new Submenu(base.ScreenManager, this.HullSelectionRect, true);
			this.Fleets = new DanButton(new Vector2(21f, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47)), "Fleets")
			{
				IsToggle = true,
				Toggled = true,
				ToggledText = "Fleets"
			};
			this.ShipList = new DanButton(new Vector2((float)(66 + this.Fleets.r.Width), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47)), "Unlocks")
			{
				IsToggle = true,
				Toggled = true,
				ToggledText = "Ships List"
			};
			this.Shipyard = new DanButton(new Vector2((float)(66 + this.Fleets.r.Width + 45 + this.Fleets.r.Width), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47)), "Shipyard")
			{
				IsToggle = true,
				ToggledText = "Shipyard"
			};
			this.SearchBar = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 585, this.Fleets.r.Y, 320, 25);
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}