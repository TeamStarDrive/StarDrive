using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class RequisitionScreen : GameScreen
    {
        private Vector2 Cursor = Vector2.Zero;
        private readonly Fleet F;
        private readonly FleetDesignScreen Fds;
        private BlueButton AssignNow;
        private BlueButton BuildNow;
        private BlueButton BuildNowRush;
        private int NumBeingBuilt;
        private Rectangle FleetStatsRect;
        private readonly Array<Ship> AvailableShips = new Array<Ship>();
        private int NumThatFit;
        private UICheckBox AutoRequisition;
        Rectangle AutoRequisitionRect;
        public RequisitionScreen(FleetDesignScreen fds) : base(fds)
        {
            Fds               = fds;
            F                 = fds.SelectedFleet;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }

        private void AssignAvailableShips()
        {
            foreach (Ship ship in AvailableShips)
            {
                if (ship.fleet != null)
                    continue;

                foreach (FleetDataNode node in F.DataNodes)
                {
                    if (node.ShipName != ship.Name || node.Ship!= null)
                        continue;

                    F.AddExistingShip(ship, node);

                    foreach (Array<Fleet.Squad> flank in F.AllFlanks)
                    {
                        foreach (Fleet.Squad squad in flank)
                        {
                            foreach (FleetDataNode sqNode in squad.DataNodes)
                            {
                                if (sqNode.Ship != null || sqNode.ShipName != ship.Name)
                                    continue;
                                sqNode.Ship = ship;
                            }
                        }
                    }
                    break;
                }
            }
            foreach (Ship ship in F.Ships)
            {
                ship.ShowSceneObjectAt(new Vector3(ship.RelativeFleetOffset, -1000000f));
            }                       
            F.Owner.GetFleetsDict()[Fds.FleetToEdit] = F;
            Fds.ChangeFleet(Fds.FleetToEdit);
            UpdateRequisitionStatus();
        }

        private void CreateFleetRequisitionGoals(bool rush = false)
        {
            foreach (FleetDataNode node in F.DataNodes)
            {
                if (node.Ship != null || node.GoalGUID != Guid.Empty)
                    continue;

                var g = new FleetRequisition(node.ShipName, F.Owner, rush) {Fleet = F};
                node.GoalGUID = g.guid;
                F.Owner.GetEmpireAI().Goals.Add(g);
                g.Evaluate();
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            string text;
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();
            Color c = Colors.Cream;
            Selector fleetStats = new Selector(FleetStatsRect, new Color(0, 0, 0, 180));
            fleetStats.Draw(ScreenManager.SpriteBatch, elapsed);
            Cursor = new Vector2(FleetStatsRect.X + 25, FleetStatsRect.Y + 25);
            ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Fleet Statistics", Cursor, c);
            Cursor.Y += (Fonts.Pirulen16.LineSpacing + 8);
            DrawStat("# Ships in Design:", F.DataNodes.Count, ref Cursor);
            int actualNumber = 0;
            foreach (FleetDataNode node in F.DataNodes)
            {
                if (node.Ship== null)
                {
                    continue;
                }
                actualNumber++;
            }
            DrawStat("# Ships in Fleet:", actualNumber, ref Cursor);
            DrawStat("# Ships being Built:", NumBeingBuilt, ref Cursor);
            int toFill = F.DataNodes.Count - actualNumber - NumBeingBuilt;
            DrawStat("# Slots To Fill:", toFill, ref Cursor, Color.LightPink);
            float cost = 0f;
            foreach (FleetDataNode node in F.DataNodes)
            {
                if (node.Ship != null)
                    cost += node.Ship.GetCost(F.Owner);
                else if (ResourceManager.GetShipTemplate(node.ShipName, out Ship ship))
                    cost += ship.GetCost(F.Owner);
            }
            DrawStat("Total Production Cost:", (int)cost, ref Cursor);
            Cursor.Y += 20f;
            int numShips = 0;
            foreach (Ship s in F.Owner.GetShips())
            {
                if (s.fleet != null)
                {
                    continue;
                }
                numShips++;
            }
            if (toFill != 0)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Owned Ships", Cursor, c);
                Cursor.Y = Cursor.Y + (Fonts.Pirulen16.LineSpacing + 8);
                if (NumThatFit <= 0)
                {
                    text = "There are no ships in your empire that are not already assigned to a fleet that can fit any of the roles required by this fleet's design.";
                    text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
                    AssignNow.ToggleOn = false;
                }
                else
                {
                    string[] str = { "Of the ", numShips.ToString(), " ships in your empire that are not assigned to fleets, ", NumThatFit.ToString(), " of them can be assigned to fill in this fleet" };
                    text = string.Concat(str);
                    text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
                    AssignNow.Draw(ScreenManager);
                }
                Cursor.Y = AssignNow.Button.Y + 70;
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Build New Ships", Cursor, c);
                Cursor.Y = Cursor.Y + (Fonts.Pirulen16.LineSpacing + 8);
                if (toFill > 0)
                {
                    text = string.Concat("Order ", toFill.ToString(), " new ships to be built at your best available shipyards");
                    text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
                }

                BuildNow.Draw(ScreenManager);
                BuildNowRush.Draw(ScreenManager);
            }
            else
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "No Requisition Needed", Cursor, c);
                Cursor.Y += (Fonts.Pirulen16.LineSpacing + 8);
                text = "This fleet is at full strength, or has build orders in place to bring it to full strength, and does not require further requisitions";
                text = Fonts.Arial12Bold.ParseText(text, FleetStatsRect.Width - 40);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, c);
            }

            AutoRequisition.Draw(batch, elapsed);
            if (F.AutoRequisition)
                batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), AutoRequisitionRect, ApplyCurrentAlphaToColor(EmpireManager.Player.EmpireColor));

            ScreenManager.SpriteBatch.End();
        }

        private void DrawStat(string text, int value, ref Vector2 cursor)
        {
            Color c = Colors.Cream;
            float column1 = cursor.X;
            float column2 = cursor.X + 175f;
            cursor.X = column1;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, c);
            cursor.X = column2;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, value.ToString(), cursor, c);
            cursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            cursor.X = column1;
        }

        private void DrawStat(string text, int value, ref Vector2 cursor, Color statColor)
        {
            Color c = Colors.Cream;
            float column1 = cursor.X;
            float column2 = cursor.X + 175f;
            cursor.X = column1;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, c);
            cursor.X = column2;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, value.ToString(), cursor, statColor);
            cursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            cursor.X = column1;
        }


        public override bool HandleInput(InputState input)
        {
            if (NumThatFit > 0 && AssignNow.HandleInput(input))
            {
                AssignAvailableShips();
                UpdateRequisitionStatus();
            }
            if (BuildNow.HandleInput(input))
            {
                CreateFleetRequisitionGoals();
                UpdateRequisitionStatus();
            }
            if (BuildNowRush.HandleInput(input))
            {
                CreateFleetRequisitionGoals(true);
                UpdateRequisitionStatus();
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            FleetStatsRect = new Rectangle(ScreenWidth / 2 - 172, ScreenHeight / 2 - 300, 345, 600);
            AssignNow = new BlueButton(new Vector2(FleetStatsRect.X + 85, FleetStatsRect.Y + 225), "Assign Now")
            {
                ToggleOn = true
            };

            BuildNow = new BlueButton(new Vector2(FleetStatsRect.X + 85, FleetStatsRect.Y + 365), "Build Now")
            {
                ToggleOn = true
            };

            BuildNowRush = new BlueButton(new Vector2(FleetStatsRect.X + 85, FleetStatsRect.Y + 415), "Rush Now")
            {
                ToggleOn = true,
                Tooltip   = GameText.BuildAllShipsNowPrioritize
            };

            AutoRequisition = Add(new UICheckBox(() => F.AutoRequisition, Fonts.Arial12Bold, title: 1833, tooltip: GameText.IfCheckedEveryTimeA));
            AutoRequisition.Pos = new Vector2(FleetStatsRect.X + 85, FleetStatsRect.Y + 480);
            AutoRequisitionRect = new Rectangle((int)AutoRequisition.Pos.X - 40, (int)AutoRequisition.Pos.Y - 14, 30, 40);
            UpdateRequisitionStatus();
        }

        private void UpdateRequisitionStatus()
        {
            NumThatFit = 0;
            AvailableShips.Clear();
            foreach (Ship ship in F.Owner.GetShips())
            {
                if (ship.fleet != null)
                    continue;

                AvailableShips.Add(ship);
            }
            foreach (Ship ship in AvailableShips)
            {
                foreach (FleetDataNode node in F.DataNodes)
                {
                    if (node.ShipName != ship.Name || node.Ship != null || ship.IsHomeDefense || ship.IsHangarShip)
                        continue;

                    NumThatFit++;
                    break;
                }
            }

            NumBeingBuilt = 0;
            foreach (Goal g in F.Owner.GetEmpireAI().Goals)
            {
                if (F.GoalGuidExists(g.guid))
                    NumBeingBuilt++;
            }
        }
    }
}