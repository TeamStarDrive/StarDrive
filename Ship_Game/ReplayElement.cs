using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	internal class ReplayElement
	{
		private GenericButton ShipCount;

		private GenericButton MilStrength;

		//private GenericButton TroopStrength;

		//private GenericButton TaxRate;

		private GenericButton Population;

		//private GenericButton ShipKills;

		private List<GenericButton> Buttons = new List<GenericButton>();

		public Rectangle ElementRect;

		public Rectangle TextRect;

		//private bool ShipDeaths;

		private bool ShowMilitaryStrength;

		private bool ShowShipCount;

		private bool ShowPopulation;

		private int TurnsRepresented;

		private int MaxShips;

		private float MaxStrength;

		private float MaxPop;

		public List<string> TextMessages = new List<string>();

		public float StarDate = 1000.1f;

		private bool Grid = true;

		//private bool Fleets = true;

		//private bool Ships = true;

		private ReplayElement.State state;

		private int FrameCount;

		public ReplayElement(Rectangle r)
		{
			this.ElementRect = r;
			this.TextRect = new Rectangle(r.X, r.Y + r.Height, r.Width, 128);
			this.ShipCount = new GenericButton(new Vector2((float)(this.ElementRect.X - 10), (float)(this.ElementRect.Y + 40)), "Ship Count", Fonts.Pirulen16, Fonts.Pirulen12);
			this.Buttons.Add(this.ShipCount);
			this.MilStrength = new GenericButton(new Vector2((float)(this.ElementRect.X - 10), (float)(this.ShipCount.R.Y + Fonts.Pirulen16.LineSpacing + 4)), "Military Strength", Fonts.Pirulen16, Fonts.Pirulen12);
			this.Buttons.Add(this.MilStrength);
			this.Population = new GenericButton(new Vector2((float)(this.ElementRect.X - 10), (float)(this.MilStrength.R.Y + Fonts.Pirulen16.LineSpacing + 4)), "Population", Fonts.Pirulen16, Fonts.Pirulen12);
			this.Buttons.Add(this.Population);
			this.TurnsRepresented = StatTracker.SnapshotsDict.Count;
			foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> Entry in StatTracker.SnapshotsDict)
			{
				foreach (KeyValuePair<int, Snapshot> shot in Entry.Value)
				{
					if (shot.Value.ShipCount > this.MaxShips)
					{
						this.MaxShips = shot.Value.ShipCount;
					}
					if (shot.Value.MilitaryStrength > this.MaxStrength)
					{
						this.MaxStrength = shot.Value.MilitaryStrength;
					}
					if (shot.Value.Population <= this.MaxPop)
					{
						continue;
					}
					this.MaxPop = shot.Value.Population;
				}
			}
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			float single;
			Rectangle MapRect = new Rectangle(this.ElementRect.X + 30, this.ElementRect.Y + 30, this.ElementRect.Width - 60, this.ElementRect.Height - 60);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EndGameScreen/ReplayHousing"], this.ElementRect, Color.White);
			float scale = (float)(this.ElementRect.Width - 60) / Ship.universeScreen.Size.X;
			if (this.Grid)
			{
				for (int x = 0; x < 21; x++)
				{
					Vector2 Origin = new Vector2((float)(x * MapRect.Width / 20), 0f) + new Vector2((float)MapRect.X, (float)MapRect.Y);
					Vector2 End = new Vector2((float)(x * MapRect.Width / 20), (float)MapRect.Height) + new Vector2((float)MapRect.X, (float)MapRect.Y);
					Primitives2D.DrawLine(ScreenManager.SpriteBatch, Origin, End, new Color(100, 100, 100, 70));
				}
				for (int y = 0; y < 21; y++)
				{
					Vector2 Origin = new Vector2(0f, (float)(y * MapRect.Height / 20)) + new Vector2((float)MapRect.X, (float)MapRect.Y);
					Vector2 End = new Vector2((float)MapRect.Width, (float)(y * MapRect.Height / 20)) + new Vector2((float)MapRect.X, (float)MapRect.Y);
					Primitives2D.DrawLine(ScreenManager.SpriteBatch, Origin, End, new Color(100, 100, 100, 40));
				}
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["EndGameScreen/TextBox"], this.TextRect, Color.White);
			foreach (SolarSystem star in UniverseScreen.SolarSystemList)
			{
				Vector2 starPos = (star.Position * scale) + new Vector2((float)MapRect.X, (float)MapRect.Y);
				Rectangle StarRect = new Rectangle((int)starPos.X - 3, (int)starPos.Y - 3, 6, 6);
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Suns/", star.SunPath)], StarRect, Color.White);
			}
			string date = this.StarDate.ToString("#.0");
			foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> shotdict in StatTracker.SnapshotsDict)
			{
				foreach (KeyValuePair<int, Snapshot> entry in shotdict.Value)
				{
					Snapshot snapshot = entry.Value;
					if (this.StarDate < entry.Value.StarDate)
					{
						continue;
					}
					foreach (NRO nro in snapshot.EmpireNodes)
					{
						Vector2 starPos = (nro.Node * scale) + new Vector2((float)MapRect.X, (float)MapRect.Y);
						Rectangle StarRect = new Rectangle((int)starPos.X - (int)(nro.Radius * scale), (int)starPos.Y - (int)(nro.Radius * scale), (int)(nro.Radius * scale * 2f), (int)(nro.Radius * scale * 2f));
						ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node"], StarRect, new Color(EmpireManager.EmpireList[entry.Key].EmpireColor, 128));
					}
				}
			}
			if (!StatTracker.SnapshotsDict.ContainsKey(date))
			{
				this.TextMessages.Clear();
			}
			else
			{
				foreach (KeyValuePair<int, Snapshot> entry in StatTracker.SnapshotsDict[date])
				{
					foreach (string message in entry.Value.Events)
					{
						if (this.TextMessages.Count > 0 && this.TextMessages[0] == string.Concat("StarDate ", date, ": ", message))
						{
							continue;
						}
						this.TextMessages.Insert(0, string.Concat("StarDate ", date, ": ", message));
						if (this.TextMessages.Count <= 5)
						{
							continue;
						}
						this.TextMessages.RemoveAt(5);
					}
				}
				if (this.FrameCount != 0)
				{
					ReplayElement frameCount = this;
					frameCount.FrameCount = frameCount.FrameCount + 1;
					if (this.FrameCount == 15)
					{
						this.FrameCount = 0;
					}
				}
				else if (this.state == ReplayElement.State.Playing && StatTracker.SnapshotsDict.ContainsKey((this.StarDate + 0.1f).ToString("#.0")))
				{
					ReplayElement starDate = this;
					starDate.StarDate = starDate.StarDate + 0.1f;
				}
			}
			float XInterval = (float)MapRect.Width / ((float)this.TurnsRepresented + 0.01f);
			if (this.ShowShipCount)
			{
				float YPerShip = (float)MapRect.Height / ((float)this.MaxShips + 0.1f);
				int turn = 0;
				for (float i = 1000.1f; i < this.StarDate; i = i + 0.1f)
				{
					if (StatTracker.SnapshotsDict.ContainsKey(i.ToString("#.0")))
					{
						foreach (KeyValuePair<int, Snapshot> entry in StatTracker.SnapshotsDict[i.ToString("#.0")])
						{
							Snapshot shot = entry.Value;
							if (!StatTracker.SnapshotsDict.ContainsKey((i + 0.1f).ToString("#.0")))
							{
								continue;
							}
							single = i + 0.1f;
							foreach (KeyValuePair<int, Snapshot> nextEntry in StatTracker.SnapshotsDict[single.ToString("#.0")])
							{
								Snapshot nextShot = nextEntry.Value;
								if (nextEntry.Key != entry.Key)
								{
									continue;
								}
								Vector2 Start = new Vector2((float)(MapRect.X + (int)(XInterval * (float)turn)), (float)(MapRect.Y + MapRect.Height - (int)(YPerShip * (float)shot.ShipCount)));
								Vector2 End = new Vector2((float)(MapRect.X + (int)(XInterval * (float)(1 + turn))), (float)(MapRect.Y + MapRect.Height - (int)(YPerShip * (float)nextShot.ShipCount)));
								Primitives2D.DrawLine(ScreenManager.SpriteBatch, Start, End, EmpireManager.EmpireList[entry.Key].EmpireColor);
							}
						}
					}
					turn++;
				}
			}
			if (this.ShowMilitaryStrength)
			{
				float YPerStr = (float)MapRect.Height / (this.MaxStrength + 0.1f);
				int turn = 0;
				for (float i = 1000.1f; i < this.StarDate; i = i + 0.1f)
				{
					if (StatTracker.SnapshotsDict.ContainsKey(i.ToString("#.0")))
					{
						foreach (KeyValuePair<int, Snapshot> entry in StatTracker.SnapshotsDict[i.ToString("#.0")])
						{
							Snapshot shot = entry.Value;
							if (!StatTracker.SnapshotsDict.ContainsKey((i + 0.1f).ToString("#.0")))
							{
								continue;
							}
							single = i + 0.1f;
							foreach (KeyValuePair<int, Snapshot> nextEntry in StatTracker.SnapshotsDict[single.ToString("#.0")])
							{
								Snapshot nextShot = nextEntry.Value;
								if (nextEntry.Key != entry.Key)
								{
									continue;
								}
								Vector2 Start = new Vector2((float)(MapRect.X + (int)(XInterval * (float)turn)), (float)(MapRect.Y + MapRect.Height - (int)(YPerStr * shot.MilitaryStrength)));
								Vector2 End = new Vector2((float)(MapRect.X + (int)(XInterval * (float)(1 + turn))), (float)(MapRect.Y + MapRect.Height - (int)(YPerStr * nextShot.MilitaryStrength)));
								Primitives2D.DrawLine(ScreenManager.SpriteBatch, Start, End, EmpireManager.EmpireList[entry.Key].EmpireColor);
							}
						}
					}
					turn++;
				}
			}
			if (this.ShowPopulation)
			{
				float YPerStr = (float)MapRect.Height / (this.MaxPop + 0.1f);
				int turn = 0;
				for (float i = 1000.1f; i < this.StarDate; i = i + 0.1f)
				{
					if (StatTracker.SnapshotsDict.ContainsKey(i.ToString("#.0")))
					{
						foreach (KeyValuePair<int, Snapshot> entry in StatTracker.SnapshotsDict[i.ToString("#.0")])
						{
							Snapshot shot = entry.Value;
							if (!StatTracker.SnapshotsDict.ContainsKey((i + 0.1f).ToString("#.0")))
							{
								continue;
							}
							single = i + 0.1f;
							foreach (KeyValuePair<int, Snapshot> nextEntry in StatTracker.SnapshotsDict[single.ToString("#.0")])
							{
								Snapshot nextShot = nextEntry.Value;
								if (nextEntry.Key != entry.Key)
								{
									continue;
								}
								Vector2 Start = new Vector2((float)(MapRect.X + (int)(XInterval * (float)turn)), (float)(MapRect.Y + MapRect.Height - (int)(YPerStr * shot.Population)));
								Vector2 End = new Vector2((float)(MapRect.X + (int)(XInterval * (float)(1 + turn))), (float)(MapRect.Y + MapRect.Height - (int)(YPerStr * nextShot.Population)));
								Primitives2D.DrawLine(ScreenManager.SpriteBatch, Start, End, EmpireManager.EmpireList[entry.Key].EmpireColor);
							}
						}
					}
					turn++;
				}
			}
			for (int i = 0; i < 5 && this.TextMessages.Count > i; i++)
			{
				Vector2 TextPos = new Vector2((float)(this.TextRect.X + 25), (float)(this.TextRect.Y + this.TextRect.Height - 30 - i * (Fonts.Arial20Bold.LineSpacing + 2)));
				HelperFunctions.DrawDropShadowText(ScreenManager, this.TextMessages[i], TextPos, Fonts.Arial20Bold);
			}
			Vector2 StarDatePos = new Vector2((float)(this.ElementRect.X + 10), (float)(this.ElementRect.Y + this.ElementRect.Height - Fonts.Tahoma11.LineSpacing - 5));
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, "StarDate: ", StarDatePos, Color.White);
			StarDatePos.X = StarDatePos.X + Fonts.Tahoma11.MeasureString("StarDate: ").X;
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, date, StarDatePos, Color.White);
			Vector2 ControlsPos = new Vector2((float)(this.ElementRect.X + this.ElementRect.Width / 2) - Fonts.Tahoma11.MeasureString("Press [Space] to Pause / Unpause").X / 2f, StarDatePos.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, "Press [Space] to Pause / Unpause", ControlsPos, Color.White);
			Vector2 PlusMinus = new Vector2((float)(this.ElementRect.X + this.ElementRect.Width - 10) - Fonts.Tahoma11.MeasureString("Left/Right Arrows").X, StarDatePos.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, "Left/Right Arrows", PlusMinus, Color.White);
			foreach (GenericButton button in this.Buttons)
			{
				button.DrawWithShadowCaps(ScreenManager);
			}
		}

		public void HandleInput(InputState input)
		{
			if (this.ShipCount.HandleInput(input))
			{
				this.ShipCount.ToggleOn = !this.ShipCount.ToggleOn;
				if (!this.ShipCount.ToggleOn)
				{
					this.ShowShipCount = false;
				}
				else
				{
					this.ShowShipCount = true;
				}
			}
			if (this.MilStrength.HandleInput(input))
			{
				this.MilStrength.ToggleOn = !this.MilStrength.ToggleOn;
				if (!this.MilStrength.ToggleOn)
				{
					this.ShowMilitaryStrength = false;
				}
				else
				{
					this.ShowMilitaryStrength = true;
				}
			}
			if (this.Population.HandleInput(input))
			{
				this.Population.ToggleOn = !this.Population.ToggleOn;
				if (!this.Population.ToggleOn)
				{
					this.ShowPopulation = false;
				}
				else
				{
					this.ShowPopulation = true;
				}
			}
			if (input.CurrentKeyboardState.IsKeyDown(Keys.Right))
			{
				this.state = ReplayElement.State.Paused;
				if (StatTracker.SnapshotsDict.ContainsKey((this.StarDate + 0.1f).ToString("#.0")))
				{
					ReplayElement starDate = this;
					starDate.StarDate = starDate.StarDate + 0.1f;
				}
			}
			if (input.CurrentKeyboardState.IsKeyDown(Keys.Left))
			{
				this.state = ReplayElement.State.Paused;
				ReplayElement replayElement = this;
				replayElement.StarDate = replayElement.StarDate - 0.1f;
				if ((double)this.StarDate < 1000.1)
				{
					this.StarDate = 1000.1f;
				}
			}
			if (input.CurrentKeyboardState.IsKeyDown(Keys.Space) && input.LastKeyboardState.IsKeyUp(Keys.Space))
			{
				if (this.state == ReplayElement.State.Playing)
				{
					this.state = ReplayElement.State.Paused;
					return;
				}
				this.state = ReplayElement.State.Playing;
			}
		}

		public enum State
		{
			Playing,
			Paused
		}
	}
}