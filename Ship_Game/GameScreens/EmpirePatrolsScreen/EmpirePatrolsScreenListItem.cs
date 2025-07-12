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

            PatrolNameRect = NextRect(w * 0.1f);
            NumWaypointsRect = NextRect(200);
            NumFleetsRect = NextRect(200);
            FleetsRect = NextRect(500);


            ShipIconRect = new Rectangle(PatrolNameRect.X + 5, PatrolNameRect.Y + 5, 50, 50);
            PlanetNameEntry.Text = FleetPatrol.Name;
            PlanetNameEntry.SetPos(ShipIconRect.Right + 10, y);

            var btn = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px");
            RenamePatrol.Rect = new Rectangle(FleetsRect.X + 10, FleetsRect.Y + FleetsRect.Height / 2 - btn.Height / 2, btn.Width, btn.Height);
            DeletePatrol.Rect = new RectF(FleetsRect.X + RenamePatrol.Width + 10, RenamePatrol.Y, RenamePatrol.Width, RenamePatrol.Height);

            AddPatrolName();
            base.PerformLayout();
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        void AddPatrolName()
        {
            string patrolName = FleetPatrol.Name;
            Graphics.Font patrolFont = NormalFont.MeasureString(patrolName).X <= PatrolNameRect.Width ? NormalFont : SmallFont;
            var sysNameCursor = new Vector2(PatrolNameRect.X + PatrolNameRect.Width / 2 - patrolFont.MeasureString(patrolName).X / 2f,
                                        2 + PatrolNameRect.Y + PatrolNameRect.Height / 2 - patrolFont.LineSpacing / 2);

            Label(sysNameCursor, patrolName, patrolFont, Player.EmpireColor);
        }

        void OnDeletePatrolClicked(UIButton b)
        {
        }

        void OnRenamePatrolClicked(UIButton b)
        {
        }
    }
}
