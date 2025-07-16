using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Fleets;

namespace Ship_Game
{
    public sealed class RenamePatrolPlanScreen : GameScreen
    {
        readonly EmpirePatrolsScreen Screen;
        readonly FleetPatrol FleetPatrol;
        UITextEntry PatrolNameEntry;
        UIButton RenameButton;
        UIButton CancelButton;
        Menu1 Menu;
        UILabel NameAlreadyExistsLabel;

        public RenamePatrolPlanScreen(EmpirePatrolsScreen screen, FleetPatrol fleetPatrol)
            : base(screen, toPause: null)
        {
            Screen = screen;
            FleetPatrol = fleetPatrol;
            IsPopup = true;
            TransitionOnTime = 0.25f;
        }

        public override void LoadContent()
        {
            Menu = Add(new Menu1((int)Screen.Width / 2 - 250, (int)Screen.Height / 2 - 100, 500, 200));
            RectF subRect = new (Menu.X + 15, Menu.Y + 15, Menu.Width - 30, Menu.Height - 30);
            Add(new Submenu(subRect, "Change Patrol Plan Name"));
            PatrolNameEntry = Add(new UITextEntry(Menu.X + 30, Menu.Y + 50, 200, Fonts.Arial20Bold, FleetPatrol.Name));
            PatrolNameEntry.AutoCaptureOnHover = true;
            PatrolNameEntry.AutoCaptureOnKeys = true;
            PatrolNameEntry.MaxCharacters = 40;
            PatrolNameEntry.OnTextChanged = OnPatrolNameTextChanged;
            PatrolNameEntry.Background = new Submenu(new RectF(PatrolNameEntry.X-10, PatrolNameEntry.Y-3, PatrolNameEntry.Width+220, PatrolNameEntry.Height+6));
            NameAlreadyExistsLabel = Add(new UILabel(Menu.X + 30, Menu.Y + 90, GameText.PatrolNameAlreadyExists));
            NameAlreadyExistsLabel.Color = Color.Red;
            Add(new CloseButton(Menu.Right - 40, Menu.Y + 15));
            RenameButton = ButtonMedium(Menu.X + 30, Menu.Bottom - 50, GameText.RenamePatrol, OnRenameClicked);
            CancelButton = ButtonBigDip(Menu.X + 180, Menu.Bottom - 50, GameText.RenamePatrol, OnCancelClicked);
            base.LoadContent();
        }

        public override void PerformLayout()
        {
            RenameButton.Enabled = false;
            NameAlreadyExistsLabel.Visible = false;
            RenameButton.Text = "Rename";
            CancelButton.Text = "Cancel";
            base.PerformLayout();
        }

        
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        void OnPatrolNameTextChanged(string newName)
        {
            if (FleetPatrol.Name == newName)
            {
                RenameButton.Enabled = false;
                return;
            }

            if (Screen.Player.FleetPatrols.Any(p => p.Name == newName))
            {
                NameAlreadyExistsLabel.Visible = true;
                RenameButton.Enabled = false;
            }
            else
            {
                NameAlreadyExistsLabel.Visible = false;
                RenameButton.Enabled = true;
            }
        }

        void OnRenameClicked(UIButton b)
        {
            Screen.RenamePatrol(FleetPatrol, PatrolNameEntry.Text);
            ExitScreen();
        }

        void OnCancelClicked(UIButton b)
        {
            ExitScreen();
        }
    }
}
