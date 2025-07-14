using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.UI;
using System;
using Ship_Game.PathFinder;

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
        Submenu SubmenuRename;
        public RenamePatrolPlanScreen(EmpirePatrolsScreen screen, FleetPatrol fleetPatrol)
            : base(screen, toPause: null)
        {
            Screen = screen;
            FleetPatrol = fleetPatrol;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void LoadContent()
        {
            Menu = Add(new Menu1((int)Screen.Width / 2 - 200, (int)Screen.Height / 2 - 100, 400, 200));
            RectF subRect = new (Menu.X + 15, Menu.Y + 15, Menu.Width - 30, Menu.Height - 30);
            SubmenuRename = Add(new Submenu(subRect, "Change Patrol Plan Name"));
            PatrolNameEntry = Add(new UITextEntry());
            PatrolNameEntry.AutoCaptureOnHover = true;
            PatrolNameEntry.AutoCaptureOnKeys = true;
            PatrolNameEntry.MaxCharacters = 40;
            Add(new CloseButton(Menu.Right - 40, Menu.Y + 15));
            RenameButton = ButtonMedium(Menu.X + 30, Menu.Bottom - 50, GameText.RenamePatrol, OnRenameClicked);
            CancelButton = ButtonBigDip(Menu.X + 180, Menu.Bottom - 50, GameText.RenamePatrol, OnCancelClicked);
            base.LoadContent();
        }

        public override void PerformLayout()
        {
            PatrolNameEntry.Text = FleetPatrol.Name;
            PatrolNameEntry.SetPos(Menu.X+20, Menu.Y+50);
            RenameButton.Text = "Rename";
            CancelButton.Text = "Cancel";
        }

        
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        void OnRenameClicked(UIButton b)
        {
            /*
            FleetPatrol.Name = PatrolNameEntry.Text;
            Screen.UpdateList();
            ExitScreen();*/
        }

        void OnCancelClicked(UIButton b)
        {
            ExitScreen();
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);
        }
    }
}
