using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.UI;
using System;

namespace Ship_Game
{
    public sealed class ChoosePatrolPlan : GameScreen
    {
        readonly ShipListScreen Screen;
        public readonly Fleet Fleet;
        Empire Player => Fleet.Owner;
        SubmenuScrollList<ChoosePatrolListItem> SubAvailablePatrols;
        ScrollList<ChoosePatrolListItem> AvailablePatrols;
        UIButton LoadPatrol;
        FleetPatrol SelectedPatrol;
        DanButton ConfirmPatrol;

        public ChoosePatrolPlan(UniverseScreen parent, Fleet fleet) : base(parent, toPause: parent)
        {
            Fleet = fleet;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class ChoosePatrolListItem : ScrollListItem<ChoosePatrolListItem>
        {
            readonly ChoosePatrolPlan Screen;
            public readonly FleetPatrol FleetPatrol;
            readonly int TurnsToComplete;
            readonly Empire Player;

            public ChoosePatrolListItem(ChoosePatrolPlan screen, FleetPatrol patrolPlan, float fleetSpeedFTL, 
                float fleetSpeedSTL, float fleetWarpOutDistance, Empire player)
            {
                Screen = screen;
                FleetPatrol = patrolPlan;
                Player = player;
                TurnsToComplete = CalculateTurnsToComplete(fleetSpeedFTL, fleetSpeedSTL, fleetWarpOutDistance, player.Universe.P.TurnTimer);
            }

            int CalculateTurnsToComplete(float fleetSpeedFTL, float fleetSpeedSTL, float fleetWarpOutDistance, int secondsPerTurn)
            {
                Vector2[] positions = FleetPatrol.WayPoints.ToArray().Select(wp => wp.Position);
                float totalDistance = 0f;
                for (int i = 0; i < positions.Length-1; i++)
                {
                    Vector2 pos = positions[i];
                    totalDistance += positions[i].Distance(positions[i+1]);
                }

                totalDistance += positions[positions.Length - 1].Distance(positions[0]); // Close the loop

                int totalFTLTime = (int)Math.Ceiling(totalDistance / fleetSpeedFTL);
                int totalSTLTime = (int)Math.Ceiling(positions.Length * fleetWarpOutDistance / fleetSpeedSTL);
                return (totalFTLTime + totalSTLTime) / secondsPerTurn;
            }
            
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                bool active = Screen.Fleet.HasPatrolPlan && FleetPatrol.Name == Screen.Fleet.Patrol.Name;
                batch.Draw(ResourceManager.Texture("UI/icon_shield"), new Rectangle((int)X, (int)Y, 29, 30), active ? Screen.ApplyCurrentAlphaToColor(Color.White) : Color.White);
                var tCursor = new Vector2(X + 40f, Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, FleetPatrol.Name, tCursor, Color.White);

                if (Screen.SubAvailablePatrols.SelectedIndex == 0)
                {
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    string CompletionText = $"{TurnsToComplete} Turns to Complete";
                    if (active)
                        CompletionText = $"{CompletionText} (active)";

                    batch.DrawString(Fonts.Arial12Bold, CompletionText, tCursor, active ? Player.EmpireColor : Color.Gray);
                }

                var waypointRect = new Rectangle((int)X + 285, (int)Y+7, 21, 20);
                var waypointText = new Vector2((waypointRect.X + 25), (waypointRect.Y + 2));
                batch.Draw(ResourceManager.Texture("NewUI/icon_waypoints"), waypointRect, active ? Player.EmpireColor : Color.Gray);
                batch.DrawString(Fonts.Arial12Bold, $"{FleetPatrol.WayPoints.Count}", waypointText, Color.White);
            }
        }
        
        public override void LoadContent()
        {
            RectF shipDesignsRect = new(ScreenWidth / 2 - 200, 200, 400, 500);
            SubAvailablePatrols = Add(new SubmenuScrollList<ChoosePatrolListItem>(shipDesignsRect, "Available Patrol Plans"));
            SubAvailablePatrols.SetBackground(Colors.TransparentBlackFill);
            
            AvailablePatrols = SubAvailablePatrols.List;
            AvailablePatrols.EnableItemHighlight = true;
            AvailablePatrols.OnClick = OnRefitShipItemClicked;

            foreach (FleetPatrol patrolPlan in Player.FleetPatrols)
            {
                AvailablePatrols.AddItem(new ChoosePatrolListItem(this, patrolPlan, Fleet.GetMinFleetSpeedFTL(), 
                    Fleet.GetMinFleetSpeedSTL(), Fleet.GetAverageWarpOutDistance(), Player));
            }

            ConfirmPatrol = new DanButton(new Vector2(shipDesignsRect.X, (shipDesignsRect.Y + 485)), "Load Patrol");
            LoadPatrol = ButtonMedium(shipDesignsRect.X + 10, shipDesignsRect.Y + 505, text: GameText.LoadPatrol, click: OnLoadPatrolClicked);
            LoadPatrol.Tooltip = GameText.LoadPatrolTip;

            base.LoadContent();
            LoadPatrol.Visible = false;
        }

        void OnRefitShipItemClicked(ChoosePatrolListItem item)
        {
            SelectedPatrol = item.FleetPatrol;
            LoadPatrol.Visible = SelectedPatrol != null;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            if (SelectedPatrol != null)
            {
                var cursor = new Vector2(ConfirmPatrol.r.X, (ConfirmPatrol.r.Y + 60));
                string text = Fonts.Arial14Bold.ParseText($"Load Patrol '{SelectedPatrol.Name}' to {Fleet.Name}", 270f);
                batch.DrawString(Fonts.Arial14Bold, text, cursor, Color.White);
            }
            batch.SafeEnd();
        }

        public override void ExitScreen()
        {
            Screen?.ResetStatus();
            base.ExitScreen();
        }

        void OnLoadPatrolClicked(UIButton b)
        {
            Fleet.LoadPatrol(SelectedPatrol);
            GameAudio.EchoAffirmative();
            ExitScreen();
        }
    }
}
