using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System.Linq;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Graphics;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Fleets;

namespace Ship_Game
{
    public sealed class EmpirePatrolsScreenListItem : ScrollListItem<EmpirePatrolsScreenListItem> // Moved to UI V2
    {
        public readonly FleetPatrol FleetPatrol;
        public Rectangle PatrolNameRect;
        public Rectangle NumWaypointsRect;
        public Rectangle NumFleetsRect;
        public Rectangle FleetsRect;

        readonly Color Cream = Colors.Cream;
        readonly Graphics.Font NormalFont = Fonts.Arial20Bold;
        readonly Graphics.Font SmallFont = Fonts.Arial12Bold;

        Rectangle ShipIconRect;
        readonly UITextEntry PlanetNameEntry = new UITextEntry();
        UIButton RenamePatrol;
        UIButton DeletePatrol;
        readonly EmpirePatrolsScreen Screen;
        readonly Empire Player;



        public EmpirePatrolsScreenListItem(EmpirePatrolsScreen screen, FleetPatrol fleetPatrol, Empire player)
        {
            Screen = screen;
            FleetPatrol = fleetPatrol;
            Player = player;
        }

        public override void PerformLayout()
        {
            int x = (int)X;
            int y = (int)Y;
            int w = (int)Width;
            int h = (int)Height;
            RemoveAll();

            RenamePatrol = Button(ButtonStyle.Default, GameText.RenamePatrol, OnRenamePatrolClicked);
            DeletePatrol = Button(ButtonStyle.Military, GameText.DeletePatrol, OnDeletePatrolClicked);
            int nextX = x;
            Rectangle NextRect(float width)
            {
                int next = nextX;
                nextX += (int)width;
                return new Rectangle(next, y, (int)width, h);
            }

            PatrolNameRect = NextRect(w * 0.15f);
            NumWaypointsRect = NextRect(w * 0.08f);
            NumFleetsRect = NextRect(w * 0.08f);
            FleetsRect = NextRect(w * 0.4f);


            ShipIconRect = new Rectangle(PatrolNameRect.X + 5, PatrolNameRect.Y + 5, 50, 50);
            PlanetNameEntry.Text = FleetPatrol.Name;
            PlanetNameEntry.SetPos(ShipIconRect.Right + 10, y);

            var btn = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px");
            RenamePatrol.Rect = new Rectangle(FleetsRect.X + FleetsRect.Width + 10, FleetsRect.Y + FleetsRect.Height / 2 - btn.Height / 2, btn.Width, btn.Height);
            DeletePatrol.Rect = new RectF(FleetsRect.X + FleetsRect.Width +  RenamePatrol.Width + 10, RenamePatrol.Y, RenamePatrol.Width, RenamePatrol.Height);

            Array<string> fleetsAssigned = GetFleetsAssignedText();
            Color color = fleetsAssigned.Count == 0 ? Color.Gray : Player.EmpireColor;
            AddLabels(PatrolNameRect, FleetPatrol.Name, color);
            AddLabels(NumWaypointsRect, FleetPatrol.WayPoints.Count.ToString(), color);
            AddLabels(NumFleetsRect, fleetsAssigned.Count.ToString(), color);
            AddLabels(FleetsRect, AssignedFleetNames(fleetsAssigned), color, centered: false);
            base.PerformLayout();
        }

        Array<string> GetFleetsAssignedText()
        {
            Array<string> fleets = new();
            foreach (Fleet fleet in Player.AllFleets)
            {
                if (fleet.HasPatrolPlan && fleet.Patrol.Name == FleetPatrol.Name)
                    fleets.Add(fleet.Name);
            }

            return fleets;
        }

        string AssignedFleetNames(Array<string> fleets)
        {
            if (fleets.Count == 0)
                return "";

            string text = "";
            foreach (string fleet in fleets)
                text = text == "" ? $"{fleet}" : $"{text}, {fleet}";

            return text;
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        void AddLabels(Rectangle rect, string text, Color color, bool centered = true)
        {
            Graphics.Font font = NormalFont.MeasureString(text).X < rect.Width 
                                ? NormalFont 
                                : SmallFont.MeasureString(text).X < rect.Width ? SmallFont
                                                                               : Fonts.Arial8Bold;
                        
            var cursor = centered ? new Vector2(rect.X + rect.Width / 2 - font.MeasureString(text).X / 2f,
                                        2 + rect.Y + rect.Height / 2 - font.LineSpacing / 2)
                                  : new Vector2(rect.X +5,
                                        2 + rect.Y + rect.Height / 2 - font.LineSpacing / 2);

            Label(cursor, text, font, color);
        }

        void OnDeletePatrolClicked(UIButton b)
        {
            GameAudio.EchoAffirmative();
            Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen, "This will permanently remove the Patrol Plan from your Empire's database and from any fleets assigned to it as well.")
            {
                Accepted = () => Screen.DeletePatrol(FleetPatrol)
            }); 
        }

        void OnRenamePatrolClicked(UIButton b)
        {
            GameAudio.EchoAffirmative();
            Screen.ScreenManager.AddScreen(new RenamePatrolPlanScreen(Screen, FleetPatrol));
        }
    }
}
