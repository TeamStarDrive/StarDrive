using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
	public sealed class OrbitalAssetsUIElement : UIElement
	{
		private Rectangle SliderRect;

		private Rectangle clickRect;

		private UniverseScreen screen;

		private Rectangle LeftRect;

		private Rectangle RightRect;

		private Rectangle flagRect;

		private Rectangle DefenseRect;

		private Rectangle SoftAttackRect;

		private Rectangle HardAttackRect;

		private Rectangle ItemDisplayRect;

		private Selector sel;

		private Planet p;

		public DanButton BombardButton;

		public DanButton LandTroops;

		private Array<TippedItem> ToolTipItems = new Array<TippedItem>();

		new private Color tColor = new Color(255, 239, 208);

		//private string fmt = "0.#";

		public OrbitalAssetsUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen, Planet p)
		{
			this.p = p;
			this.screen = screen;
			ScreenManager = sm;
			ElementRect = r;
			sel = new Selector(r, Color.Black);
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
			SliderRect = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
			clickRect = new Rectangle(ElementRect.X + ElementRect.Width - 16, ElementRect.Y + ElementRect.Height / 2 - 11, 11, 22);
			LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
			RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
			BombardButton = new DanButton(new Vector2(LeftRect.X + 20, LeftRect.Y + 25), Localizer.Token(1431))
			{
				IsToggle = true,
				ToggledText = Localizer.Token(1426)
			};
			LandTroops = new DanButton(new Vector2(LeftRect.X + 20, LeftRect.Y + 75), Localizer.Token(1432))
			{
				IsToggle = true,
				ToggledText = Localizer.Token(1433)
			};
			flagRect = new Rectangle(r.X + r.Width - 31, r.Y + 22 - 13, 26, 26);
			DefenseRect = new Rectangle(LeftRect.X + 12, LeftRect.Y + 18, 22, 22);
			SoftAttackRect = new Rectangle(LeftRect.X + 12, DefenseRect.Y + 22 + 5, 16, 16);
			HardAttackRect = new Rectangle(LeftRect.X + 12, SoftAttackRect.Y + 16 + 5, 16, 16);
			DefenseRect.X = DefenseRect.X - 3;
			ItemDisplayRect = new Rectangle(LeftRect.X + 85, LeftRect.Y + 5, 85, 85);
			TippedItem bomb = new TippedItem
			{
				r = BombardButton.r,
				TIP_ID = 32
			};
			ToolTipItems.Add(bomb);
			bomb = new TippedItem
			{
				r = LandTroops.r,
				TIP_ID = 36
			};
			ToolTipItems.Add(bomb);
		}

		public override void Draw(GameTime gameTime)
		{
			MathHelper.SmoothStep(0f, 1f, TransitionPosition);
			ScreenManager.SpriteBatch.FillRectangle(sel.Rect, Color.Black);
			float x = Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 vector2 = new Vector2(x, state.Y);
			Header slant = new Header(new Rectangle(sel.Rect.X, sel.Rect.Y, sel.Rect.Width, 41), "Orbital Assets");
			Body body = new Body(new Rectangle(slant.leftRect.X, sel.Rect.Y + 44, sel.Rect.Width, sel.Rect.Height - 44));
			slant.Draw(ScreenManager);
			body.Draw(ScreenManager);
			BombardButton.DrawBlue(ScreenManager);
			LandTroops.DrawBlue(ScreenManager);
		}

		public override bool HandleInput(InputState input)
		{
			if (BombardButton.HandleInput(input))
			{
				if (!BombardButton.Toggled)
				{
					foreach (Ship ship in p.ParentSystem.ShipList)
					{
						if (ship.loyalty != EmpireManager.Player || ship.AI.State != AIState.Bombard)
						{
							continue;
						}
						ship.AI.OrderQueue.Clear();
						ship.AI.State = AIState.AwaitingOrders;
					}
				}
				else
				{
					foreach (Ship ship in p.ParentSystem.ShipList)
					{
						if (ship.loyalty != EmpireManager.Player || ship.BombBays.Count <= 0 || Vector2.Distance(ship.Center, p.Center) >= 15000f)
						{
							continue;
						}
						ship.AI.OrderBombardPlanet(p);
					}
				}
			}
			LandTroops.HandleInput(input);
			foreach (TippedItem ti in ToolTipItems)
			{
				if (!ti.r.HitTest(input.CursorPosition))
				{
					continue;
				}
				ToolTip.CreateTooltip(ti.TIP_ID);
			}
			return false;
		}

		private struct TippedItem
		{
			public Rectangle r;

			public int TIP_ID;
		}
	}
}