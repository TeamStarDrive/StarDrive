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
		private Rectangle LeftRect;
		private Rectangle DefenseRect;
		private Rectangle SoftAttackRect;

		private Selector sel;

		private Planet p;

		public DanButton BombardButton;
		public DanButton LandTroops;
		private Array<TippedItem> ToolTipItems = new Array<TippedItem>();

		public OrbitalAssetsUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen, Planet p)
		{
			this.p = p;
			ScreenManager = sm;
			ElementRect = r;
			sel = new Selector(r, Color.Black);
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
			LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
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
			DefenseRect = new Rectangle(LeftRect.X + 12, LeftRect.Y + 18, 22, 22);
			SoftAttackRect = new Rectangle(LeftRect.X + 12, DefenseRect.Y + 22 + 5, 16, 16);
			DefenseRect.X = DefenseRect.X - 3;
			var bomb = new TippedItem
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

		public override void Draw(SpriteBatch batch, DrawTimes elapsed)
		{
			MathHelper.SmoothStep(0f, 1f, TransitionPosition);
			batch.FillRectangle(sel.Rect, Color.Black);
			var slant = new Header(new Rectangle(sel.Rect.X, sel.Rect.Y, sel.Rect.Width, 41), "Orbital Assets");
			var body = new Body(new Rectangle(slant.leftRect.X, sel.Rect.Y + 44, sel.Rect.Width, sel.Rect.Height - 44));
			slant.Draw(batch, elapsed);
			body.Draw(batch, elapsed);
			BombardButton.DrawBlue(batch);
			LandTroops.DrawBlue(batch);
		}

		public override bool HandleInput(InputState input)
		{
			if (BombardButton.HandleInput(input))
			{
				if (!BombardButton.Toggled)
				{
					foreach (Ship ship in p.ParentSystem.ShipList)
                    {
                        if (ship.loyalty == EmpireManager.Player && ship.AI.State == AIState.Bombard)
                            ship.AI.ClearOrders();
                    }
				}
				else
				{
					foreach (Ship ship in p.ParentSystem.ShipList)
                    {
                        if (ship.loyalty == EmpireManager.Player && ship.BombBays.Count > 0 &&
                            ship.Center.InRadius(p.Center, 15000f))
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