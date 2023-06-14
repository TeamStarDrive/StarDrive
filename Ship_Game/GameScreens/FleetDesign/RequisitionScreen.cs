using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using System;

namespace Ship_Game
{
    public sealed class RequisitionScreen : GameScreen
    {
        readonly Fleet F;
        readonly FleetDesignScreen Fds;

        Submenu Background;
        BlueButton AssignNow;
        UIList OwnedShips;
        UIList BuildNewShips;
        UIList NoRequisitionNeeded;

        public RequisitionScreen(FleetDesignScreen fds) : base(fds, toPause: null)
        {
            Fds = fds;
            F = fds.SelectedFleet;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;

            RectF = new RectF(fds.CenterX - 172, fds.CenterY - 300, 345, 600);
        }

        public override void LoadContent()
        {
            Background = Add(new Submenu(RectF));
            Background.SetBackground(new Color(0, 0, 0, 180));

            var main = Background.Add(new UIList(Background.ClientArea, Color.TransparentBlack));
            main.Padding = new(4, 8);

            var stats = main.Add(new UIList(new(main.Width, 20), ListLayoutStyle.ResizeList));
            void AddStatLabel(string title, Func<UILabel, string> getText)
            {
                var s = stats.AddSplit(new UILabel(title), new UILabel(getText));
                s.Split = Background.ClientArea.W / 2;
            }
            stats.Add(new UILabel("Fleet Statistics", Fonts.Pirulen16));
            AddStatLabel("# Ships in Design:", _ => F.DataNodes.Count.ToString());
            AddStatLabel("# Active Ships:", _ => GetNumActiveShips().ToString());
            AddStatLabel("# Building Ships:", _ => GetNumBeingBuilt().ToString());
            AddStatLabel("# Empty Slots:", _ => GetSlotsToFill().ToString());
            AddStatLabel("Total Production Cost:", _ => GetTotalProductionCost().String(0));

            // OwnedShips: Only visible when we have slots to fill
            OwnedShips = main.Add(new UIList(new(main.Width, 20), ListLayoutStyle.ResizeList));
            OwnedShips.Add(new UILabel("Owned Ships", Fonts.Pirulen16));
            OwnedShips.Add(new UILabel(_ => GetNumThatFitText()));
            AssignNow = OwnedShips.Add(new BlueButton("Assign Now")
            {
                OnClick = (_) => AssignAvailableShips()
            });

            BuildNewShips = main.Add(new UIList(new(main.Width, 20), ListLayoutStyle.ResizeList));
            BuildNewShips.Add(new UILabel("Build New Ships", Fonts.Pirulen16));
            BuildNewShips.Add(new UILabel(_ => GetSlotsToFillText()));
            BuildNewShips.Add(new BlueButton("Build Now")
            {
                OnClick = (_) => CreateFleetRequisitionGoals()
            });
            BuildNewShips.Add(new BlueButton("Rush Now")
            {
                Tooltip = GameText.BuildAllShipsNowPrioritize,
                OnClick = (_) => CreateFleetRequisitionGoals(true)
            });

            // NoRequisitionNeeded: Only visible when all slots are filled
            NoRequisitionNeeded = main.Add(new UIList(new(main.Width, 20), ListLayoutStyle.ResizeList));
            NoRequisitionNeeded.Add(new UILabel("No Requisition Needed", Fonts.Pirulen16, Colors.Cream));
            NoRequisitionNeeded.Add(new UILabel(_ => GetFullStrengthText()) { Color = Colors.Cream });

            main.Add(new UICheckBox(() => F.AutoRequisition, Fonts.Arial12Bold,
                                    title: GameText.AutomaticRequisition,
                                    tooltip: GameText.IfCheckedEveryTimeA));
        }

        string GetNumThatFitText()
        {
            Ship[] available = GetAvailableShips();
            int numThatFit = GetNumThatFit(available);

            // TODO: implement dynamic text in GameText.yaml
            string s = numThatFit > 0
                ? $"Of the {available.Length} ships in your empire that are not assigned to fleets, {numThatFit} of them can be assigned to fill in this fleet"
                : "There are no ships in your empire that are not already assigned to a fleet that can fit any of the roles required by this fleet's design.";

            return Fonts.Arial12Bold.ParseText(s, Background.ClientArea.W - 40);
        }

        string GetSlotsToFillText()
        {
            string s = $"Order {GetSlotsToFill()} new ships to be built at your best available shipyards";
            return Fonts.Arial12Bold.ParseText(s, Background.ClientArea.W - 40);
        }

        string GetFullStrengthText()
        {
            // TODO: Add auto-parse support for Labels
            string s = "This fleet is at full strength, or has build orders in place to bring it to full strength, and does not require further requisitions";
            return Fonts.Arial12Bold.ParseText(s, Background.ClientArea.W - 40);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();

            int slotsToFill = GetSlotsToFill();
            AssignNow.Visible = slotsToFill > 0 && GetNumThatFit(GetAvailableShips()) > 0;
            OwnedShips.Visible = slotsToFill > 0;
            BuildNewShips.Visible = slotsToFill > 0;
            NoRequisitionNeeded.Visible = slotsToFill <= 0;

            base.Draw(batch, elapsed);

            batch.SafeEnd();
        }

        void AssignAvailableShips()
        {
            Ship[] available = GetAvailableShips();

            foreach (Ship ship in available)
            {
                foreach (FleetDataNode node in F.DataNodes)
                {
                    if (node.ShipName != ship.Name || node.Ship != null)
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
                ship.ShowSceneObjectAt(ship.RelativeFleetOffset, -1000000f);
            }

            F.Owner.SetFleet(Fds.SelectedFleet.Key, F);
            Fds.ChangeFleet(Fds.SelectedFleet.Key);
        }

        void CreateFleetRequisitionGoals(bool rush = false)
        {
            foreach (FleetDataNode node in F.DataNodes)
            {
                if (node.Ship == null
                    && node.Goal == null
                    && ResourceManager.Ships.GetDesign(node.ShipName, out IShipDesign ship)
                    && F.Owner.CanBuildShip(ship))
                {
                    var g = new FleetRequisition(node.ShipName, F.Owner, F, rush);
                    node.Goal = g;
                    F.Owner.AI.AddGoalAndEvaluate(g);
                }
            }
        }

        int GetNumActiveShips()
        {
            return F.DataNodes.Count(n => n.Ship != null);
        }

        int GetNumBeingBuilt()
        {
            return F.DataNodes.Count(n => n.Ship == null && n.Goal is FleetRequisition);
        }

        int GetSlotsToFill()
        {
            return F.DataNodes.Count(n => n.Ship == null && n.Goal == null);
        }

        Ship[] GetAvailableShips() => F.Owner.OwnedShips.Filter(s => s.Fleet == null);

        int GetNumThatFit(Ship[] available)
        {
            int numThatFit = 0;
            foreach (Ship ship in available)
            {
                foreach (FleetDataNode node in F.DataNodes)
                {
                    if (node.ShipName == ship.Name && node.Ship == null &&
                        !ship.IsHomeDefense && !ship.IsHangarShip)
                    {
                        ++numThatFit;
                        break;
                    }
                }
            }
            return numThatFit;
        }

        float GetTotalProductionCost()
        {
            float cost = 0f;
            foreach (FleetDataNode node in F.DataNodes)
            {
                if (node.Ship != null)
                    cost += node.Ship.GetCost(F.Owner);
                else if (ResourceManager.Ships.GetDesign(node.ShipName, out IShipDesign ship))
                    cost += ship.GetCost(F.Owner);
            }
            return cost;
        }

    }
}