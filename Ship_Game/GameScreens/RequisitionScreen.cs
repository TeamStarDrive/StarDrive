using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using Ship_Game.AI;

namespace Ship_Game
{
	public sealed class RequisitionScreen : GameScreen
	{
		private Vector2 Cursor = Vector2.Zero;

		private Fleet f;

		private FleetDesignScreen fds;

		private BlueButton AssignNow;

		private BlueButton BuildNow;

		private int numBeingBuilt;

		private Rectangle FleetStatsRect;

		private Array<Ship> AvailableShips = new Array<Ship>();

		private int numThatFit;

		private MouseState currentMouse;

		private MouseState previousMouse;

		public RequisitionScreen(FleetDesignScreen fds) : base(fds)
		{
			this.fds = fds;
			this.f = fds.fleet;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		private void AssignAvailableShips()
		{
			foreach (Ship ship in this.AvailableShips)
			{
				foreach (FleetDataNode node in this.f.DataNodes)
				{
					if (node.ShipName != ship.Name || node.Ship!= null)
					{
						continue;
					}
					node.Ship = ship;
					ship.RelativeFleetOffset = node.FleetOffset;
					ship.fleet = f;
					f.AddShip(ship);

                    foreach (Array<Fleet.Squad> flank in f.AllFlanks)
                    {
                        foreach (Fleet.Squad squad in flank)
                        {
                            foreach (FleetDataNode sqnode in squad.DataNodes)
                            {
                                if (sqnode.Ship != null || sqnode.ShipName != ship.Name)
                                    continue;
                                sqnode.Ship = ship;
                            }
                        }
                    }
				}
			}
			foreach (Ship ship in this.f.Ships)
			{
				ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, -1000000f));
			}
			this.f.Owner.GetFleetsDict()[this.fds.FleetToEdit] = this.f;
			this.fds.ChangeFleet(this.fds.FleetToEdit);
			this.UpdateRequisitionStatus();
		}

		private void CreateShipGoals()
		{
			foreach (FleetDataNode node in this.f.DataNodes)
			{
				if (node.Ship!= null)
				{
					continue;
				}
				Goal g = new Goal(node.ShipName, "FleetRequisition", this.f.Owner);
				g.SetFleet(this.f);
				node.GoalGUID = g.guid;
				this.f.Owner.GetGSAI().Goals.Add(g);
			}
		}

		public override void Draw(GameTime gameTime)
		{
			string text;
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			Color c = new Color(255, 239, 208);
			Selector fleetstats = new Selector(this.FleetStatsRect, new Color(0, 0, 0, 180));
			fleetstats.Draw(ScreenManager.SpriteBatch);
			this.Cursor = new Vector2((float)(this.FleetStatsRect.X + 25), (float)(this.FleetStatsRect.Y + 25));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Fleet Statistics", this.Cursor, c);
			this.Cursor.Y = this.Cursor.Y + (float)(Fonts.Pirulen16.LineSpacing + 8);
			this.DrawStat("# Ships in Design:", this.f.DataNodes.Count, ref this.Cursor);
			int actualnumber = 0;
			foreach (FleetDataNode node in this.f.DataNodes)
			{
				if (node.Ship== null)
				{
					continue;
				}
				actualnumber++;
			}
			this.DrawStat("# Ships in Fleet:", actualnumber, ref this.Cursor);
			this.DrawStat("# Ships being Built:", this.numBeingBuilt, ref this.Cursor);
			int tofill = this.f.DataNodes.Count - actualnumber - this.numBeingBuilt;
			this.DrawStat("# Slots To Fill:", tofill, ref this.Cursor, Color.LightPink);
			float cost = 0f;
			foreach (FleetDataNode node in this.f.DataNodes)
			{
				cost = (node.Ship== null ? cost + ResourceManager.ShipsDict[node.ShipName].GetCost(this.f.Owner) : cost + node.Ship.GetCost(this.f.Owner));
			}
			this.DrawStat("Total Production Cost:", (int)cost, ref this.Cursor);
			this.Cursor.Y = this.Cursor.Y + 20f;
			int numships = 0;
			foreach (Ship s in this.f.Owner.GetShips())
			{
				if (s.fleet != null)
				{
					continue;
				}
				numships++;
			}
			if (tofill != 0)
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Owned Ships", this.Cursor, c);
				this.Cursor.Y = this.Cursor.Y + (float)(Fonts.Pirulen16.LineSpacing + 8);
				if (this.numThatFit <= 0)
				{
					text = "There are no ships in your empire that are not already assigned to a fleet that can fit any of the roles required by this fleet's design.";
					text = HelperFunctions.ParseText(Fonts.Arial12Bold, text, (float)(this.FleetStatsRect.Width - 40));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, this.Cursor, c);
					this.AssignNow.ToggleOn = false;
				}
				else
				{
					string[] str = new string[] { "Of the ", numships.ToString(), " ships in your empire that are not assigned to fleets, ", this.numThatFit.ToString(), " of them can be assigned to fill in this fleet" };
					text = string.Concat(str);
					text = HelperFunctions.ParseText(Fonts.Arial12Bold, text, (float)(this.FleetStatsRect.Width - 40));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, this.Cursor, c);
					this.AssignNow.Draw(base.ScreenManager);
				}
				this.Cursor.Y = (float)(this.AssignNow.Button.Y + 70);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Build New Ships", this.Cursor, c);
				this.Cursor.Y = this.Cursor.Y + (float)(Fonts.Pirulen16.LineSpacing + 8);
				if (tofill > 0)
				{
					text = string.Concat("Order ", tofill.ToString(), " new ships to be built at your best available shipyards");
					text = HelperFunctions.ParseText(Fonts.Arial12Bold, text, (float)(this.FleetStatsRect.Width - 40));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, this.Cursor, c);
				}
				this.BuildNow.Draw(base.ScreenManager);
			}
			else
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "No Requisition Needed", this.Cursor, c);
				this.Cursor.Y = this.Cursor.Y + (float)(Fonts.Pirulen16.LineSpacing + 8);
				text = "This fleet is at full strength, or has build orders in place to bring it to full strength, and does not require further requisitions";
				text = HelperFunctions.ParseText(Fonts.Arial12Bold, text, (float)(this.FleetStatsRect.Width - 40));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, this.Cursor, c);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		private void DrawStat(string text, int value, ref Vector2 Cursor)
		{
			Color c = new Color(255, 239, 208);
			float column1 = Cursor.X;
			float column2 = Cursor.X + 175f;
			Cursor.X = column1;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
			Cursor.X = column2;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, value.ToString(), Cursor, c);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			Cursor.X = column1;
		}

		private void DrawStat(string text, int value, ref Vector2 Cursor, Color statcolor)
		{
			Color c = new Color(255, 239, 208);
			float column1 = Cursor.X;
			float column2 = Cursor.X + 175f;
			Cursor.X = column1;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
			Cursor.X = column2;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, value.ToString(), Cursor, statcolor);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			Cursor.X = column1;
		}


		public override bool HandleInput(InputState input)
		{
			this.currentMouse = input.MouseCurr;
			if (this.numThatFit > 0 && this.AssignNow.HandleInput(input))
			{
				this.AssignAvailableShips();
				this.UpdateRequisitionStatus();
			}
			if (this.BuildNow.HandleInput(input))
			{
				this.CreateShipGoals();
				this.UpdateRequisitionStatus();
			}
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
                return true;
			}
			this.previousMouse = input.MousePrev;
			return base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.FleetStatsRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 172, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 345, 600);
			this.AssignNow = new BlueButton(new Vector2((float)(this.FleetStatsRect.X + 85), (float)(this.FleetStatsRect.Y + 225)), "Assign Now")
			{
				ToggleOn = true
			};
			this.BuildNow = new BlueButton(new Vector2((float)(this.FleetStatsRect.X + 85), (float)(this.FleetStatsRect.Y + 365)), "Build Now")
			{
				ToggleOn = true
			};
			this.UpdateRequisitionStatus();
			foreach (FleetDataNode node in this.f.DataNodes)
			{
				foreach (Goal g in this.f.Owner.GetGSAI().Goals)
				{
					if (g.guid != node.GoalGUID)
					{
						continue;
					}
					RequisitionScreen requisitionScreen = this;
					requisitionScreen.numBeingBuilt = requisitionScreen.numBeingBuilt + 1;
				}
			}
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		private void UpdateRequisitionStatus()
		{
			this.numThatFit = 0;
			this.AvailableShips.Clear();
			foreach (Ship ship in this.f.Owner.GetShips())
			{
				if (ship.fleet != null)
				{
					continue;
				}
				this.AvailableShips.Add(ship);
			}
			foreach (Ship ship in this.AvailableShips)
			{
				foreach (FleetDataNode node in this.f.DataNodes)
				{
					if (!(node.ShipName == ship.Name) || node.Ship!= null)
					{
						continue;
					}
					RequisitionScreen requisitionScreen = this;
					requisitionScreen.numThatFit = requisitionScreen.numThatFit + 1;
					break;
				}
			}
			this.numBeingBuilt = 0;
			foreach (FleetDataNode node in this.f.DataNodes)
			{
				foreach (Goal g in this.f.Owner.GetGSAI().Goals)
				{
					if (g.guid != node.GoalGUID)
					{
						continue;
					}
					RequisitionScreen requisitionScreen1 = this;
					requisitionScreen1.numBeingBuilt = requisitionScreen1.numBeingBuilt + 1;
				}
			}
		}
	}
}