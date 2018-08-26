using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
	internal sealed class ReplayElement
	{
		private GenericButton ShipCount;

		private GenericButton MilStrength;

		//private GenericButton TroopStrength;

		//private GenericButton TaxRate;

		private GenericButton Population;

		//private GenericButton ShipKills;

		private Array<GenericButton> Buttons = new Array<GenericButton>();

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

		public Array<string> TextMessages = new Array<string>();

		public float StarDate = 1000.1f;

		private bool Grid = true;

		//private bool Fleets = true;

		//private bool Ships = true;

		private State state;

		private int FrameCount;

		public ReplayElement(Rectangle r)
		{
			ElementRect = r;
			TextRect = new Rectangle(r.X, r.Y + r.Height, r.Width, 128);
			ShipCount = new GenericButton(new Vector2(ElementRect.X - 10, ElementRect.Y + 40), "Ship Count", Fonts.Pirulen16, Fonts.Pirulen12);
			Buttons.Add(ShipCount);
			MilStrength = new GenericButton(new Vector2(ElementRect.X - 10, ShipCount.R.Y + Fonts.Pirulen16.LineSpacing + 4), "Military Strength", Fonts.Pirulen16, Fonts.Pirulen12);
			Buttons.Add(MilStrength);
			Population = new GenericButton(new Vector2(ElementRect.X - 10, MilStrength.R.Y + Fonts.Pirulen16.LineSpacing + 4), "Population", Fonts.Pirulen16, Fonts.Pirulen12);
			Buttons.Add(Population);
			TurnsRepresented = StatTracker.SnapshotsDict.Count;
			foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> Entry in StatTracker.SnapshotsDict)
			{
				foreach (KeyValuePair<int, Snapshot> shot in Entry.Value)
				{
					if (shot.Value.ShipCount > MaxShips)
					{
						MaxShips = shot.Value.ShipCount;
					}
					if (shot.Value.MilitaryStrength > MaxStrength)
					{
						MaxStrength = shot.Value.MilitaryStrength;
					}
					if (shot.Value.Population <= MaxPop)
					{
						continue;
					}
					MaxPop = shot.Value.Population;
				}
			}
		}

		public void Draw(ScreenManager ScreenManager)
		{
			float single;
			Rectangle MapRect = new Rectangle(ElementRect.X + 30, ElementRect.Y + 30, ElementRect.Width - 60, ElementRect.Height - 60);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("EndGameScreen/ReplayHousing"), ElementRect, Color.White);
			float scale = (ElementRect.Width - 60) / (Empire.Universe.UniverseSize * 2);        //Correction for negative map values -Gretman
            if (Grid)
			{
				for (int x = 0; x < 21; x++)
				{
					Vector2 Origin = new Vector2(x * MapRect.Width / 20, 0f) + new Vector2(MapRect.X, MapRect.Y);
					Vector2 End = new Vector2(x * MapRect.Width / 20, MapRect.Height) + new Vector2(MapRect.X, MapRect.Y);
					ScreenManager.SpriteBatch.DrawLine(Origin, End, new Color(100, 100, 100, 70));
				}
				for (int y = 0; y < 21; y++)
				{
					Vector2 Origin = new Vector2(0f, y * MapRect.Height / 20) + new Vector2(MapRect.X, MapRect.Y);
					Vector2 End = new Vector2(MapRect.Width, y * MapRect.Height / 20) + new Vector2(MapRect.X, MapRect.Y);
					ScreenManager.SpriteBatch.DrawLine(Origin, End, new Color(100, 100, 100, 40));
				}
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("EndGameScreen/TextBox"), TextRect, Color.White);
			foreach (SolarSystem star in UniverseScreen.SolarSystemList)
			{
				Vector2 starPos = (star.Position * scale) + new Vector2(MapRect.X, MapRect.Y);
                starPos.X = starPos.X + (MapRect.Width / 2);        //Correction for negative map values -Gretman
                starPos.Y = starPos.Y + (MapRect.Height / 2);
                Rectangle StarRect = new Rectangle((int)starPos.X - 3, (int)starPos.Y - 3, 6, 6);
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Suns/", star.SunPath)), StarRect, Color.White);
			}
			string date = StarDate.ToString("#.0");
			foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> shotdict in StatTracker.SnapshotsDict)
			{
				foreach (KeyValuePair<int, Snapshot> entry in shotdict.Value)
				{
					Snapshot snapshot = entry.Value;
					if (StarDate < entry.Value.StarDate)
					{
						continue;
					}
					foreach (NRO nro in snapshot.EmpireNodes)
					{
						Vector2 starPos = (nro.Node * scale) + new Vector2(MapRect.X, MapRect.Y);
                        starPos.X = starPos.X + (MapRect.Width / 2);        //Correction for negative map values -Gretman
                        starPos.Y = starPos.Y + (MapRect.Height / 2);
                        Rectangle StarRect = new Rectangle((int)starPos.X - (int)(nro.Radius * scale), (int)starPos.Y - (int)(nro.Radius * scale), (int)(nro.Radius * scale * 2f), (int)(nro.Radius * scale * 2f));
						ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/node"), StarRect, new Color(EmpireManager.Empires[entry.Key].EmpireColor, 128));
					}
				}
			}
			if (!StatTracker.SnapshotsDict.ContainsKey(date))
			{
				TextMessages.Clear();
			}
			else
			{
				foreach (KeyValuePair<int, Snapshot> entry in StatTracker.SnapshotsDict[date])
				{
					foreach (string message in entry.Value.Events)
					{
						if (TextMessages.Count > 0 && TextMessages[0] == string.Concat("StarDate ", date, ": ", message))
						{
							continue;
						}
						TextMessages.Insert(0, string.Concat("StarDate ", date, ": ", message));
						if (TextMessages.Count <= 5)
						{
							continue;
						}
						TextMessages.RemoveAt(5);
					}
				}
				if (FrameCount != 0)
				{
					ReplayElement frameCount = this;
					frameCount.FrameCount = frameCount.FrameCount + 1;
					if (FrameCount == 15)
					{
						FrameCount = 0;
					}
				}
				else if (state == State.Playing && StatTracker.SnapshotsDict.ContainsKey((StarDate + 0.1f).ToString("#.0")))
				{
					ReplayElement starDate = this;
					starDate.StarDate = starDate.StarDate + 0.1f;
				}
			}
			float XInterval = MapRect.Width / (TurnsRepresented + 0.01f);
			if (ShowShipCount)
			{
				float YPerShip = MapRect.Height / (MaxShips + 0.1f);
				int turn = 0;
				for (float i = 1000.1f; i < StarDate; i = i + 0.1f)
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
								Vector2 Start = new Vector2(MapRect.X + (int)(XInterval * turn), MapRect.Y + MapRect.Height - (int)(YPerShip * shot.ShipCount));
								Vector2 End = new Vector2(MapRect.X + (int)(XInterval * (1 + turn)), MapRect.Y + MapRect.Height - (int)(YPerShip * nextShot.ShipCount));
								ScreenManager.SpriteBatch.DrawLine(Start, End, EmpireManager.Empires[entry.Key].EmpireColor);
							}
						}
					}
					turn++;
				}
			}
			if (ShowMilitaryStrength)
			{
				float YPerStr = MapRect.Height / (MaxStrength + 0.1f);
				int turn = 0;
				for (float i = 1000.1f; i < StarDate; i = i + 0.1f)
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
								Vector2 Start = new Vector2(MapRect.X + (int)(XInterval * turn), MapRect.Y + MapRect.Height - (int)(YPerStr * shot.MilitaryStrength));
								Vector2 End = new Vector2(MapRect.X + (int)(XInterval * (1 + turn)), MapRect.Y + MapRect.Height - (int)(YPerStr * nextShot.MilitaryStrength));
								ScreenManager.SpriteBatch.DrawLine(Start, End, EmpireManager.Empires[entry.Key].EmpireColor);
							}
						}
					}
					turn++;
				}
			}
			if (ShowPopulation)
			{
				float YPerStr = MapRect.Height / (MaxPop + 0.1f);
				int turn = 0;
				for (float i = 1000.1f; i < StarDate; i = i + 0.1f)
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
								Vector2 Start = new Vector2(MapRect.X + (int)(XInterval * turn), MapRect.Y + MapRect.Height - (int)(YPerStr * shot.Population));
								Vector2 End = new Vector2(MapRect.X + (int)(XInterval * (1 + turn)), MapRect.Y + MapRect.Height - (int)(YPerStr * nextShot.Population));
								ScreenManager.SpriteBatch.DrawLine(Start, End, EmpireManager.Empires[entry.Key].EmpireColor);
							}
						}
					}
					turn++;
				}
			}
			for (int i = 0; i < 5 && TextMessages.Count > i; i++)
			{
				Vector2 TextPos = new Vector2(TextRect.X + 25, TextRect.Y + TextRect.Height - 30 - i * (Fonts.Arial20Bold.LineSpacing + 2));
				HelperFunctions.DrawDropShadowText(ScreenManager, TextMessages[i], TextPos, Fonts.Arial20Bold);
			}
			Vector2 StarDatePos = new Vector2(ElementRect.X + 10, ElementRect.Y + ElementRect.Height - Fonts.Tahoma11.LineSpacing - 5);
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, "StarDate: ", StarDatePos, Color.White);
			StarDatePos.X = StarDatePos.X + Fonts.Tahoma11.MeasureString("StarDate: ").X;
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, date, StarDatePos, Color.White);
			Vector2 ControlsPos = new Vector2(ElementRect.X + ElementRect.Width / 2 - Fonts.Tahoma11.MeasureString("Press [Space] to Pause / Unpause").X / 2f, StarDatePos.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, "Press [Space] to Pause / Unpause", ControlsPos, Color.White);
			Vector2 PlusMinus = new Vector2(ElementRect.X + ElementRect.Width - 10 - Fonts.Tahoma11.MeasureString("Left/Right Arrows").X, StarDatePos.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma11, "Left/Right Arrows", PlusMinus, Color.White);
			foreach (GenericButton button in Buttons)
			{
				button.DrawWithShadowCaps(ScreenManager);
			}
		}

		public void HandleInput(InputState input)
		{
			if (ShipCount.HandleInput(input))
			{
				ShipCount.ToggleOn = !ShipCount.ToggleOn;
				if (!ShipCount.ToggleOn)
				{
					ShowShipCount = false;
				}
				else
				{
					ShowShipCount = true;
				}
			}
			if (MilStrength.HandleInput(input))
			{
				MilStrength.ToggleOn = !MilStrength.ToggleOn;
				if (!MilStrength.ToggleOn)
				{
					ShowMilitaryStrength = false;
				}
				else
				{
					ShowMilitaryStrength = true;
				}
			}
			if (Population.HandleInput(input))
			{
				Population.ToggleOn = !Population.ToggleOn;
				if (!Population.ToggleOn)
				{
					ShowPopulation = false;
				}
				else
				{
					ShowPopulation = true;
				}
			}
			if (input.KeysCurr.IsKeyDown(Keys.Right))
			{
				state = State.Paused;
				if (StatTracker.SnapshotsDict.ContainsKey((StarDate + 0.1f).ToString("#.0")))
				{
					ReplayElement starDate = this;
					starDate.StarDate = starDate.StarDate + 0.1f;
				}
			}
			if (input.KeysCurr.IsKeyDown(Keys.Left))
			{
				state = State.Paused;
				ReplayElement replayElement = this;
				replayElement.StarDate = replayElement.StarDate - 0.1f;
				if (StarDate < 1000.1)
				{
					StarDate = 1000.1f;
				}
			}
			if (input.KeysCurr.IsKeyDown(Keys.Space) && input.KeysPrev.IsKeyUp(Keys.Space))
			{
				if (state == State.Playing)
				{
					state = State.Paused;
					return;
				}
				state = State.Playing;
			}
		}

		public enum State
		{
			Playing,
			Paused
		}
	}
}