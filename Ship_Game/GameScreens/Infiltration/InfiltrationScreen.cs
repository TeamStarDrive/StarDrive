using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Input;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.GameScreens.Espionage;
using Ship_Game.GameScreens.EspionageNew;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Graphics.Color;

namespace Ship_Game.GameScreens
{
    public sealed class InfiltrationScreen : GameScreen
    {
        public readonly UniverseScreen Universe;
        public Empire SelectedEmpire;
        readonly Empire Player;
        public static readonly Color PanelBackground = new Color(23, 20, 14);
        EspionageLevelPanel Level1, Level2, Level3, Level4, Level5;
        UILabel InfiltrationTitle;
        Color SeperatorColor;

        public InfiltrationScreen(UniverseScreen parent) : base(parent, toPause: parent)
        {
            Universe = parent;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            Player = Universe.Player;
            SelectedEmpire = Player;
        }

        public override void LoadContent()
        {
            var titleRect = new Rectangle(ScreenWidth / 2 - 200, 44, 400, 80);
            Add(new Menu2(titleRect));

            if (ScreenHeight > 766)
            {
                Add(new Menu2(titleRect));

                // "Espionage"
                string espionage = Localizer.Token(GameText.EspionageOverview);
                var titlePos = new Vector2(titleRect.Center.X - Fonts.Laserian14.MeasureString(espionage).X / 2f,
                                           titleRect.Center.Y - Fonts.Laserian14.LineSpacing / 2);
                Label(titlePos, espionage, Fonts.Laserian14, Colors.Cream);
            }


            var ourRect = new Rectangle(ScreenWidth / 2 - 700, (ScreenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1400, 700);
            Add(new Menu2(ourRect));

            CloseButton(ourRect.Right - 40, ourRect.Y + 20);

            InfiltrationTitle = Add(new UILabel("INFILTRATION LEVELS", Fonts.Arial20Bold, Color.Wheat));
            var levelRect = new Rectangle(ourRect.X + 35, ourRect.Y + 430, 250, 250);
            Level1 = Add(new EspionageLevelPanel(this, Player, levelRect, 1));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level2 = Add(new EspionageLevelPanel(this, Player, levelRect, 2));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level3 = Add(new EspionageLevelPanel(this, Player, levelRect, 3));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level4 = Add(new EspionageLevelPanel(this, Player, levelRect, 4));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level5 = Add(new EspionageLevelPanel(this, Player, levelRect, 5));
            Add(new InfiltrationPanel(this, Universe.Player, ourRect));
            RefreshSelectedEmpire(Player);
            GameAudio.MuteRacialMusic();
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            InfiltrationTitle.Pos = new Vector2(HelperFunctions.GetMiddlePosForTitle(InfiltrationTitle.Text.Text, Fonts.Arial20Bold, Width, 0), Level1.Y - 45);
            RefreshSelectedEmpire(Player);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            if (!SelectedEmpire.isPlayer)
            {
                batch.DrawLine(new Vector2(Level1.X, Level1.Y - 20), new Vector2(Level1.X + 80 + Level1.Width * 5, Level1.Y - 20), SeperatorColor, 2);
                batch.DrawLine(new Vector2(Level1.X, Level1.Y - 50), new Vector2(Level1.X + 80 + Level1.Width * 5, Level1.Y - 50), SeperatorColor, 2);
            }

            batch.SafeEnd();
        }

        public override void Update(float fixedDeltaTime)
        {

            base.Update(fixedDeltaTime);

        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.E) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            if (Player.Universe.Debug && !SelectedEmpire.isPlayer && HandleDebugInput(input))
                return true;


            return base.HandleInput(input);
        }

        bool HandleDebugInput(InputState input)
        {
            Keys[] keys = [Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5];
            for (byte i = 0; i < keys.Length; i++) 
            {
                if (input.KeyPressed(keys[i]))
                {
                    Player.GetEspionage(SelectedEmpire).SetInfiltrationLevelTo(i);
                    RefreshSelectedEmpire(SelectedEmpire);
                    return true;
                }
            }

            return false;
        }

        public void RefreshSelectedEmpire(Empire selectedEmpire)
        {
            SelectedEmpire = selectedEmpire;
            SeperatorColor = SelectedEmpire.isPlayer || !Player.IsKnown(SelectedEmpire) ? Player.EmpireColor : SelectedEmpire.EmpireColor;
            InfiltrationTitle.Color = SeperatorColor;
            // need to change that to only inprogress level is shown
            Level1.Visible = !SelectedEmpire.isPlayer;
            Level2.Visible = !SelectedEmpire.isPlayer;
            Level3.Visible = !SelectedEmpire.isPlayer;
            Level4.Visible = !SelectedEmpire.isPlayer;
            Level5.Visible = !SelectedEmpire.isPlayer;
            InfiltrationTitle.Visible = !SelectedEmpire.isPlayer;

            if (Level1.Visible) Level1.RefreshEmpire();
            if (Level2.Visible) Level2.RefreshEmpire();
            if (Level3.Visible) Level3.RefreshEmpire();
            if (Level4.Visible) Level4.RefreshEmpire();
            if (Level5.Visible) Level5.RefreshEmpire();
        }

        public void RefreshInfiltrationLevelStatus(Ship_Game.Espionage espionage)
        {
            if (Level1.Visible) Level1.RefreshStatus(espionage);
            if (Level2.Visible) Level2.RefreshStatus(espionage);
            if (Level3.Visible) Level3.RefreshStatus(espionage);
            if (Level4.Visible) Level4.RefreshStatus(espionage);
            if (Level5.Visible) Level5.RefreshStatus(espionage);
        }
    }
}
