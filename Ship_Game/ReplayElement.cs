using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDUtils;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    internal sealed class ReplayElement
    {
        UniverseScreen Universe;
        UniverseState UState;
        private GenericButton ShipCount;

        private GenericButton MilStrength;

        private GenericButton Population;

        private Array<GenericButton> Buttons = new();

        public Rectangle ElementRect;

        public Rectangle TextRect;

        private bool ShowMilitaryStrength;

        private bool ShowShipCount;

        private bool ShowPopulation;

        private int TurnsRepresented;

        private int MaxShips;

        private float MaxStrength;

        private float MaxPop;

        public Array<string> TextMessages = new();

        public float StarDate = 1000.1f;

        private bool Grid = true;

        private State state;

        private int FrameCount;
        Rectangle MapRect;

        public ReplayElement(UniverseScreen u, Rectangle r)
        {
            Universe = u;
            UState = u.UState;

            ElementRect = r;
            TextRect = new Rectangle(r.X, r.Y + r.Height, r.Width, 128);

            GenericButton AddButton(float x, float y, string title)
            {
                GenericButton button = new(new(x, y), title, Fonts.Pirulen16, Fonts.Pirulen12)
                {
                    ToggleOnColor = Color.DarkOrange,
                    ButtonStyle = GenericButton.Style.Shadow
                };
                Buttons.Add(button);
                return button;
            }
            
            ShipCount = AddButton(ElementRect.X - 10, ElementRect.Y + 40, "Ship Count");
            MilStrength = AddButton(ElementRect.X - 10, ShipCount.Y + Fonts.Pirulen16.LineSpacing + 4, "Military Strength");
            Population = AddButton(ElementRect.X - 10, MilStrength.Y + Fonts.Pirulen16.LineSpacing + 4, "Population");

            TurnsRepresented = UState.Stats.NumRecordedTurns;
            foreach (Map<int, Snapshot> snapshots in UState.Stats.Snapshots)
            {
                foreach (Snapshot shot in snapshots.Values)
                {
                    if (shot.ShipCount > MaxShips)
                    {
                        MaxShips = shot.ShipCount;
                    }
                    if (shot.MilitaryStrength > MaxStrength)
                    {
                        MaxStrength = shot.MilitaryStrength;
                    }
                    if (shot.Population > MaxPop)
                    {
                        MaxPop = shot.Population;
                    }
                }
            }
        }

        public void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            MapRect = new Rectangle(ElementRect.X + 30, ElementRect.Y + 30, ElementRect.Width - 60, ElementRect.Height - 60);
            batch.Draw(ResourceManager.Texture("EndGameScreen/ReplayHousing"), ElementRect, Color.White);
            float scale = (ElementRect.Width - 60) / (Universe.UState.Size * 2);        //Correction for negative map values -Gretman
            if (Grid)
            {
                for (int x = 0; x < 21; x++)
                {
                    Vector2 origin = new Vector2(x * MapRect.Width / 20f, 0f) + new Vector2(MapRect.X, MapRect.Y);
                    Vector2 end    = new Vector2(x * MapRect.Width / 20f, MapRect.Height) + new Vector2(MapRect.X, MapRect.Y);
                    batch.DrawLine(origin, end, new Color(100, 100, 100, 70));
                }
                for (int y = 0; y < 21; y++)
                {
                    Vector2 origin = new Vector2(0f, y * MapRect.Height / 20f) + new Vector2(MapRect.X, MapRect.Y);
                    Vector2 end    = new Vector2(MapRect.Width, y * MapRect.Height / 20f) + new Vector2(MapRect.X, MapRect.Y);
                    batch.DrawLine(origin, end, new Color(100, 100, 100, 40));
                }
            }
            batch.Draw(ResourceManager.Texture("EndGameScreen/TextBox"), TextRect, Color.White);

            DrawSolarSystemStats(batch, scale);
            
            string starDateStr = StarDate.StarDateString();
            if (!UState.Stats.GetAllSnapshotsFor(StarDate, out Map<int, Snapshot> allSnapshots))
            {
                TextMessages.Clear();
            }
            else
            {
                foreach (KeyValuePair<int, Snapshot> entry in allSnapshots)
                {
                    foreach (string message in entry.Value.Events)
                    {
                        string newMessage = $"StarDate {starDateStr}: {message}";
                        if (TextMessages.Count == 0 || TextMessages[0] != newMessage)
                        {
                            TextMessages.Insert(0, newMessage);
                            if (TextMessages.Count > 5)
                            {
                                TextMessages.RemoveAt(5);
                            }
                        }
                    }
                }

                if (FrameCount != 0)
                {
                    FrameCount += 1;
                    if (FrameCount == 15)
                        FrameCount = 0;
                }
                else if (state == State.Playing && UState.Stats.ContainsDate(StarDate + 0.1f))
                {
                    StarDate += 0.1f;
                }
            }

            float XInterval = MapRect.Width / (TurnsRepresented + 0.01f);
            if (ShowShipCount)
            {
                float YPerShip = MapRect.Height / (MaxShips + 0.1f);
                IterateSnapshotPairs((turn, empire, curr, next) =>
                {
                    DrawSnapshotLine(batch, empire, XInterval, YPerShip,
                                     x1:turn, x2:turn+1, y1:curr.ShipCount, y2:next.ShipCount);
                });
            }
            if (ShowMilitaryStrength)
            {
                float YPerStr = MapRect.Height / (MaxStrength + 0.1f);
                IterateSnapshotPairs((turn, empire, curr, next) =>
                {
                    DrawSnapshotLine(batch, empire, XInterval, YPerStr,
                                     x1:turn, x2:turn+1, y1:curr.MilitaryStrength, y2:next.MilitaryStrength);
                });
            }
            if (ShowPopulation)
            {
                float YPerPop = MapRect.Height / (MaxPop + 0.1f);
                IterateSnapshotPairs((turn, empire, curr, next) =>
                {
                    DrawSnapshotLine(batch, empire, XInterval, YPerPop,
                                     x1:turn, x2:turn+1, y1:curr.Population, y2:next.Population);
                });
            }
            for (int i = 0; i < 5 && TextMessages.Count > i; i++)
            {
                var TextPos = new Vector2(TextRect.X + 25, TextRect.Y + TextRect.Height - 30 - i * (Fonts.Arial20Bold.LineSpacing + 2));
                batch.DrawDropShadowText(TextMessages[i], TextPos, Fonts.Arial20Bold);
            }
            var StarDatePos = new Vector2(ElementRect.X + 10, ElementRect.Y + ElementRect.Height - Fonts.Tahoma11.LineSpacing - 5);
            batch.DrawString(Fonts.Tahoma11, "StarDate: ", StarDatePos, Color.White);
            StarDatePos.X += Fonts.Tahoma11.MeasureString("StarDate: ").X;
            batch.DrawString(Fonts.Tahoma11, starDateStr, StarDatePos, Color.White);
            var ControlsPos = new Vector2(ElementRect.X + ElementRect.Width / 2 - Fonts.Tahoma11.MeasureString("Press [Space] to Pause / Unpause").X / 2f, StarDatePos.Y);
            batch.DrawString(Fonts.Tahoma11, "Press [Space] to Pause / Unpause", ControlsPos, Color.White);
            var PlusMinus = new Vector2(ElementRect.X + ElementRect.Width - 10 - Fonts.Tahoma11.MeasureString("Left/Right Arrows").X, StarDatePos.Y);
            batch.DrawString(Fonts.Tahoma11, "Left/Right Arrows", PlusMinus, Color.White);
            foreach (GenericButton button in Buttons)
            {
                button.Draw(batch, elapsed);
            }
        }

        void DrawSolarSystemStats(SpriteBatch batch, float scale)
        {
            foreach (SolarSystem star in Universe.UState.Systems)
            {
                Vector2 starPos = (star.Position * scale) + MapRect.PosVec();
                starPos.X += (MapRect.Width / 2f); // Correction for negative map values -Gretman
                starPos.Y += (MapRect.Height / 2f);
                var starRect = new Rectangle((int) starPos.X - 3, (int) starPos.Y - 3, 6, 6);
                star.Sun.DrawIcon(batch, starRect);
            }

            foreach (Map<int, Snapshot> snap in UState.Stats.Snapshots)
            {
                foreach (KeyValuePair<int, Snapshot> entry in snap)
                {
                    Snapshot snapshot = entry.Value;
                    if (StarDate >= entry.Value.StarDate)
                    {
                        foreach (NRO nro in snapshot.EmpireNodes)
                        {
                            Vector2 starPos = (nro.Node * scale) + new Vector2(MapRect.X, MapRect.Y);
                            starPos.X += (MapRect.Width * 0.5f); //Correction for negative map values -Gretman
                            starPos.Y += (MapRect.Height * 0.5f);
                            var starRect = new Rectangle((int) starPos.X - (int) (nro.Radius * scale),
                                (int) starPos.Y - (int) (nro.Radius * scale),
                                (int) (nro.Radius * scale * 2f),
                                (int) (nro.Radius * scale * 2f));
                            var empire = UState.Empires[entry.Key];
                            batch.Draw(ResourceManager.Texture("UI/node"), starRect, new Color(empire.EmpireColor, 128));
                        }
                    }
                }
            }
        }

        void DrawSnapshotLine(SpriteBatch batch, Empire empire, float xInterval, float yInterval, int x1, int x2, float y1, float y2)
        {
            var start = new Vector2(MapRect.X + (int)(xInterval * x1), MapRect.Y + MapRect.Height - (int)(yInterval * y1));
            var end   = new Vector2(MapRect.X + (int)(xInterval * x2), MapRect.Y + MapRect.Height - (int)(yInterval * y2));
            batch.DrawLine(start, end, empire.EmpireColor);
        }

        void IterateSnapshotPairs(Action<int, Empire, Snapshot, Snapshot> onSnapshotPair)
        {
            int turn = 0;
            for (float currStarDate = 1000.1f; currStarDate < StarDate; currStarDate += 0.1f, ++turn)
            {
                float nextStarDate = currStarDate + 0.1f;

                if (UState.Stats.GetAllSnapshotsFor(currStarDate, out Map<int, Snapshot> currSnapshots) &&
                    UState.Stats.GetAllSnapshotsFor(nextStarDate, out Map<int, Snapshot> nextSnapshots))
                {
                    foreach (KeyValuePair<int, Snapshot> currEntry in currSnapshots)
                    {
                        Snapshot currShot = currEntry.Value;
                        foreach (KeyValuePair<int, Snapshot> nextEntry in nextSnapshots)
                        {
                            Snapshot nextShot = nextEntry.Value;
                            if (nextEntry.Key == currEntry.Key)
                            {
                                Empire empire = UState.Empires[currEntry.Key];
                                onSnapshotPair(turn, empire, currShot, nextShot);
                            }
                        }
                    }
                }
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
                if (UState.Stats.ContainsDate((StarDate + 0.1f)))
                {
                    StarDate = StarDate + 0.1f;
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