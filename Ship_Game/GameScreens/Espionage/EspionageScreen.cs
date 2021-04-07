using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game.GameScreens.Espionage
{
    public sealed class EspionageScreen : GameScreen
    {
        public Empire SelectedEmpire;
        public AgentComponent Agents { get; private set; }
        public static readonly Color PanelBackground = new Color(23, 20, 14);

        public EspionageScreen(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
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


            var ourRect = new Rectangle(ScreenWidth / 2 - 700, (ScreenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1400, 660);
            Add(new Menu2(ourRect));

            CloseButton(ourRect.Right - 40, ourRect.Y + 20);

            var agentsRect     = new Rectangle(ourRect.X + 60,         ourRect.Y + 250, 368, 376);
            var dossierRect    = new Rectangle(agentsRect.Right + 30,  agentsRect.Y,    368, 376);
            var operationsRect = new Rectangle(dossierRect.Right + 30, agentsRect.Y,    368, 376);

            Add(new EmpiresPanel(this, ourRect, operationsRect));
            Add(new AgentsPanel(this, agentsRect));
            Add(new DossierPanel(this, dossierRect));
            Add(new OperationsPanel(this, operationsRect));

            var agentComponentRect = new Rectangle(agentsRect.X + 20, agentsRect.Y + 35, agentsRect.Width - 40, agentsRect.Height - 95);
            Agents = Add(new AgentComponent(this, agentComponentRect, operationsRect));
            
            GameAudio.MuteRacialMusic();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();

            base.Draw(batch, elapsed);

            batch.End();
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
    }
}
