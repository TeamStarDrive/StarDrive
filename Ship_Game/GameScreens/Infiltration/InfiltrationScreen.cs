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
            var levelRect = new Rectangle(ourRect.X + 35, ourRect.Y + 400, 250, 250);
            Level1 = Add(new EspionageLevelPanel(this, Player, levelRect, 1));
            /*
            Level1 = Add(new UIPanel(levelRect, PanelBackground));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level2 = Add(new UIPanel(levelRect, PanelBackground));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level3 = Add(new UIPanel(levelRect, PanelBackground));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level4 = Add(new UIPanel(levelRect, PanelBackground));
            levelRect = new Rectangle(levelRect.Right + 20, levelRect.Y, 250, 250);
            Level5 = Add(new UIPanel(levelRect, PanelBackground));
            
            UIPanel AddInfiltrationLevel(Rectangle rect, int level, out ProgressBar progress)
            {
                string title = $"Infiltration Level {level}";
                Vector2 pos = new Vector2(rect.X, rect.Y+5);
                Add(new UILabel(pos, title, Fonts.Arial12Bold, Color.White));
                var levelRect = new Rectangle(rect.X + 15 + 20*level, rect.Y + 320, 250, 250);
                progress = Add(new ProgressBar())
                return Add(new UIPanel(levelRect, PanelBackground));
            }

            
            
            Add(new AgentsPanel(this, agentsRect));
            Add(new DossierPanel(this, dossierRect));
            Add(new OperationsPanel(this, operationsRect));
            */
            /*
            RectF agentComponentRect = new(agentsRect.X + 20, agentsRect.Y + 35, agentsRect.Width - 40, agentsRect.Height - 95);
            Agents = Add(new AgentComponent(this, agentComponentRect, operationsRect));
            */
            Add(new InfiltrationPanel(this, Universe.Player, ourRect));
            RefreshSelectedEmpire(Player);
            GameAudio.MuteRacialMusic();
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            InfiltrationTitle.Pos = new Vector2(HelperFunctions.GetMiddlePosForTitle(InfiltrationTitle.Text.Text, Fonts.Arial20Bold, Width, 0), Level1.Y - 20);
            RefreshSelectedEmpire(Player);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            if (SelectedEmpire != Player)
            {
                //Level1Progress.Draw(batch);
            }
            batch.DrawLine(new Vector2(Level1.X, Level1.Y - 30), new Vector2(Level1.X + Level1.Width*5, Level1.Y - 30), SeperatorColor, 2);
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
            return base.HandleInput(input);
        }

        public void RefreshSelectedEmpire(Empire selectedEmpire)
        {
            SeperatorColor = selectedEmpire.isPlayer || !Player.IsKnown(selectedEmpire) ? Player.EmpireColor : selectedEmpire.EmpireColor;
            InfiltrationTitle.Color = SeperatorColor;
            SelectedEmpire = selectedEmpire;
            Level1.Visible = SelectedEmpire != Player;


            if (Level1.Visible)
                Level1.RefreshEmpire();
        }

        void SetLevelsVisibility(bool value)
        {

        }

        void SetLevel1PanelVisibility(bool value)
        {
            //Level1.Visible = 
            
        }
    }
}
