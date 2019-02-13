using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

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
            f = fds.SelectedFleet;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        private void AssignAvailableShips()
        {
            foreach (Ship ship in AvailableShips)
            {
                if (ship.fleet != null)
                    continue;
                foreach (FleetDataNode node in f.DataNodes)
                {
                    if (node.ShipName != ship.Name || node.Ship!= null)
                    {
                        continue;
                    }
                    //node.Ship = ship;
                    //ship.RelativeFleetOffset = node.FleetOffset;
                    //ship.fleet = f;
                    
                    f.AddExistingShip(ship);

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
                    break;
                }
            }
            foreach (Ship ship in f.Ships)
            {
                ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, -1000000f));
            }                        
            f.Owner.GetFleetsDict()[fds.FleetToEdit] = f;
            fds.ChangeFleet(fds.FleetToEdit);
            UpdateRequisitionStatus();
        }

        private void CreateFleetRequisitionGoals()
        {
            foreach (FleetDataNode node in f.DataNodes)
            {
                if (node.Ship != null || node.GoalGUID != Guid.Empty)
                    continue;
                var g = new FleetRequisition(node.ShipName, f.Owner) {Fleet = f};
                node.GoalGUID = g.guid;
                f.Owner.GetEmpireAI().Goals.Add(g);
                g.Evaluate();
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            string text;
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();
            Color c = new Color(255, 239, 208);
            Selector fleetstats = new Selector(FleetStatsRect, new Color(0, 0, 0, 180));
            fleetstats.Draw(ScreenManager.SpriteBatch);
            Cursor = new Vector2(FleetStatsRect.X + 25, FleetStatsRect.Y + 25);
            ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Fleet Statistics", Cursor, c);
            Cursor.Y = Cursor.Y + (Fonts.Pirulen16.LineSpacing + 8);
            DrawStat("# Ships in Design:", f.DataNodes.Count, ref Cursor);
            int actualnumber = 0;
            foreach (FleetDataNode node in f.DataNodes)
            {
                if (node.Ship== null)
                {
                    continue;
                }
                actualnumber++;
            }
            DrawStat("# Ships in Fleet:", actualnumber, ref Cursor);
            DrawStat("# Ships being Built:", numBeingBuilt, ref Cursor);
            int tofill = f.DataNodes.Count - actualnumber - numBeingBuilt;
            DrawStat("# Slots To Fill:", tofill, ref Cursor, Color.LightPink);
            float cost = 0f;
            foreach (FleetDataNode node in f.DataNodes)
            {
                cost = (node.Ship== null ? cost + ResourceManager.ShipsDict[node.ShipName].GetCost(f.Owner) : cost + node.Ship.GetCost(f.Owner));
            }
            DrawStat("Total Production Cost:", (int)cost, ref Cursor);
            Cursor.Y = Cursor.Y + 20f;
            int numships = 0;
            foreach (Ship s in f.Owner.GetShips())
            {
                if (s.fleet != null)
                {
                    continue;
                }
                numships++;
            }
            if (tofill != 0)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Owned Ships", Cursor, c);
                Cursor.Y = Cursor.Y + (Fonts.Pirulen16.LineSpacing + 8);
                if (numThatFit <= 0)
                {
                    text = "There are no ships in your empire that are not already assigned to a fleet that can fit any of the roles required by this fleet's design.";
                    text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
                    AssignNow.ToggleOn = false;
                }
                else
                {
                    string[] str = { "Of the ", numships.ToString(), " ships in your empire that are not assigned to fleets, ", numThatFit.ToString(), " of them can be assigned to fill in this fleet" };
                    text = string.Concat(str);
                    text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
                    AssignNow.Draw(ScreenManager);
                }
                Cursor.Y = AssignNow.Button.Y + 70;
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Build New Ships", Cursor, c);
                Cursor.Y = Cursor.Y + (Fonts.Pirulen16.LineSpacing + 8);
                if (tofill > 0)
                {
                    text = string.Concat("Order ", tofill.ToString(), " new ships to be built at your best available shipyards");
                    text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
                }
                BuildNow.Draw(ScreenManager);
            }
            else
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "No Requisition Needed", Cursor, c);
                Cursor.Y = Cursor.Y + (Fonts.Pirulen16.LineSpacing + 8);
                text = "This fleet is at full strength, or has build orders in place to bring it to full strength, and does not require further requisitions";
                text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
            }
            ScreenManager.SpriteBatch.End();
        }

        private void DrawStat(string text, int value, ref Vector2 Cursor)
        {
            Color c = new Color(255, 239, 208);
            float column1 = Cursor.X;
            float column2 = Cursor.X + 175f;
            Cursor.X = column1;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
            Cursor.X = column2;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, value.ToString(), Cursor, c);
            Cursor.Y = Cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            Cursor.X = column1;
        }

        private void DrawStat(string text, int value, ref Vector2 Cursor, Color statcolor)
        {
            Color c = new Color(255, 239, 208);
            float column1 = Cursor.X;
            float column2 = Cursor.X + 175f;
            Cursor.X = column1;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
            Cursor.X = column2;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, value.ToString(), Cursor, statcolor);
            Cursor.Y = Cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            Cursor.X = column1;
        }


        public override bool HandleInput(InputState input)
        {
            currentMouse = input.MouseCurr;
            if (numThatFit > 0 && AssignNow.HandleInput(input))
            {
                AssignAvailableShips();
                UpdateRequisitionStatus();
            }
            if (BuildNow.HandleInput(input))
            {
                CreateFleetRequisitionGoals();
                UpdateRequisitionStatus();
            }
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            previousMouse = input.MousePrev;
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            FleetStatsRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 172, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 345, 600);
            AssignNow = new BlueButton(new Vector2(FleetStatsRect.X + 85, FleetStatsRect.Y + 225), "Assign Now")
            {
                ToggleOn = true
            };
            BuildNow = new BlueButton(new Vector2(FleetStatsRect.X + 85, FleetStatsRect.Y + 365), "Build Now")
            {
                ToggleOn = true
            };
            UpdateRequisitionStatus();
            foreach (FleetDataNode node in f.DataNodes)
            {
                foreach (Goal g in f.Owner.GetEmpireAI().Goals)
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
            numThatFit = 0;
            AvailableShips.Clear();
            foreach (Ship ship in f.Owner.GetShips())
            {
                if (ship.fleet != null)
                    continue;
                AvailableShips.Add(ship);
            }
            foreach (Ship ship in AvailableShips)
            {
                foreach (FleetDataNode node in f.DataNodes)
                {
                    if (node.ShipName != ship.Name || node.Ship != null)
                        continue;
                    RequisitionScreen requisitionScreen = this;
                    requisitionScreen.numThatFit = requisitionScreen.numThatFit + 1;
                    break;
                }
            }
            numBeingBuilt = 0;
            foreach (FleetDataNode node in f.DataNodes)
            {
                foreach (Goal g in f.Owner.GetEmpireAI().Goals)
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