using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.GameScreens.Espionage;
using Ship_Game.Gameplay;
using Ship_Game.UI;

namespace Ship_Game.GameScreens
{
    public class EmpireButton : UIElementV2
    {
        public readonly Empire Empire;
        readonly EspionageScreen Screen;
        readonly InfiltrationScreen InfiltrationScreen;
        readonly Action<EmpireButton> OnClick;
        readonly Ship_Game.Espionage Espionage;
        readonly Empire Player;
        FloatSlider InfiltrationDefense;

        bool UsingLegacyEspionage => Screen != null;

        public EmpireButton(EspionageScreen screen, Empire e, in Rectangle rect, Action<EmpireButton> onClick)
            : base(rect)
        {
            Empire = e;
            Screen = screen;
            OnClick = onClick;
            Player = Empire.Universe.Player;
        }

        public EmpireButton(InfiltrationScreen screen, Empire e, in Rectangle rect, Action<EmpireButton> onClick)
            : base(rect)
        {
            Empire = e;
            InfiltrationScreen = screen;
            OnClick = onClick;
            Player = Empire.Universe.Player;
            if (!Empire.isPlayer)
            {
                Espionage = Player.GetRelations(Empire).Espionage;
            }
        }

        public override void PerformLayout()
        {
            var weightRect = new Rectangle(Rect.Left, Rect.Y + 250, 140, 40);
            if (Empire == Player) 
            {
                InfiltrationDefense = new FloatSlider(weightRect, GameText.SpyDefense, min: 0, max: 50, value: Player.EspionageDefenseWeight);
                InfiltrationDefense.OnChange = (s) =>
                {
                    Player.SetEspionageDefenseWeight(s.AbsoluteValue.RoundUpTo(1));
                };
            }
            else if (Player.IsKnown(Empire))
            {
                InfiltrationDefense = new FloatSlider(weightRect, GameText.SpyDefense, min: 0, max: 10, value: Espionage.GetWeight());
                InfiltrationDefense.OnChange = (s) =>
                {
                    Espionage.SetWeight(s.AbsoluteValue.RoundUpTo(1));
                    if (Empire == InfiltrationScreen.SelectedEmpire)
                        InfiltrationScreen.RefreshInfiltrationLevelStatus(Espionage);
                };
            }

            base.PerformLayout();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.InGameSelect && Rect.HitTest(input.CursorPosition))
            {
                GameAudio.EchoAffirmative();
                OnClick(this);
                return true;
            }

            if (InfiltrationDefense != null && InfiltrationDefense.HandleInput(input))
                return true;

            return false;
        }


        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            // red background:
            if (Player != Empire && Player.IsAtWarWith(Empire) && !Empire.IsDefeated)
            {
                batch.FillRectangle(Rect.Bevel(2), Color.Red);
            }

            if (Empire.IsDefeated)
            {
                DrawRacePortrait();

                if (Empire.data.AbsorbedBy == null)
                {
                    batch.Draw(ResourceManager.ErrorTexture, Rect, Color.White);
                }
                else
                {
                    var r = new Rectangle(Rect.X, Rect.Y, 124, 124);
                    var e = Empire.Universe.GetEmpireByName(Empire.data.AbsorbedBy);
                    batch.Draw(ResourceManager.Flag(e.data.Traits.FlagIndex), r, e.EmpireColor);
                }
            }
            else if (Player == Empire || Player.IsKnown(Empire))
            {
                if (!UsingLegacyEspionage)
                    DrawDefenseRatio();

                DrawRacePortrait();
                if (UsingLegacyEspionage)
                    DrawLegacySpyDefense();
                else
                    DrawInfiltration();

                if (Player == Empire || Player.IsKnown(Empire))
                    DrawDefenseSlider();
            }
            else if (Player != Empire)
            {
                batch.Draw(ResourceManager.Texture("Portraits/unknown"), Rect, Color.White);
            }

            if (UsingLegacyEspionage && Empire == Screen.SelectedEmpire
                || !UsingLegacyEspionage && Empire == InfiltrationScreen.SelectedEmpire)
            {
                batch.DrawRectangle(Rect, Color.Orange);
            }
            else if (Player.IsKnown(Empire) &&
                (UsingLegacyEspionage && Rect.HitTest(Screen.Input.CursorPosition)
                || !UsingLegacyEspionage && Rect.HitTest(InfiltrationScreen.Input.CursorPosition)))
            {
                batch.DrawRectangle(Rect, Color.White);
            }

            void DrawRacePortrait()
            {
                batch.Draw(ResourceManager.Texture("Portraits/" + Empire.data.PortraitName), Rect, Color.White);
                batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), Rect, Color.White);

                Vector2 size = Fonts.Arial12Bold.MeasureString(Empire.data.Traits.Name);
                var nameCursor = new Vector2(Rect.X + 62 - size.X / 2f, Rect.Y + 148 + 8);
                batch.DrawString(Fonts.Arial12Bold, Empire.data.Traits.Name, nameCursor, Empire.EmpireColor);
            }

            void DrawLegacySpyDefense()
            {
                SubTexture shield = ResourceManager.Texture("UI/icon_shield");
                // Added by McShooterz: Display Spy Defense value
                var defenseIcon = new Rectangle(Rect.Center.X - shield.Width, Rect.Y + Fonts.Arial12.LineSpacing + 164, shield.Width, shield.Height);
                batch.Draw(shield, defenseIcon, Color.White);

                float espionageDefense = Empire.GetSpyDefense();
                var defPos = new Vector2(defenseIcon.Right + 2, defenseIcon.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial12Bold, espionageDefense.String(1), defPos, Color.White);

                if (defenseIcon.HitTest(Screen.Input.CursorPosition))
                    ToolTip.CreateTooltip(Localizer.Token(GameText.IndicatesTheCounterespionageStrengthOf));
            }

            void DrawDefenseRatio()
            {
                SubTexture shield = ResourceManager.Texture("UI/icon_shield");
                var defenseIcon = new Rectangle(Rect.Center.X - shield.Width, Rect.Y - Fonts.Arial12.LineSpacing -10, shield.Width, shield.Height);
                batch.Draw(shield, defenseIcon, Color.White);
                string espionageDefense = Empire.isPlayer || Espionage.CanViewDefenseRatio
                    ? $"{((int)(Empire.EspionageDefenseRatio * 100)).String()}%"
                    : "?";

                var defPos = new Vector2(defenseIcon.Right + 2, defenseIcon.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial12Bold, espionageDefense, defPos, Color.White);
                if (defenseIcon.HitTest(InfiltrationScreen.Input.CursorPosition))
                    ToolTip.CreateTooltip(Localizer.Token(GameText.InfiltrationDefesneTip));
            }

            void DrawInfiltration()
            {
                if (Empire.isPlayer)
                    return;

                SubTexture spy = ResourceManager.Texture("UI/icon_spy");
                var spyPos = new Vector2(Rect.Right - 160, Rect.Y + Fonts.Arial12.LineSpacing + 164);
                for (byte i = 1; i <= Ship_Game.Espionage.MaxLevel; i++)
                {
                    var r = new Rectangle((int)spyPos.X + 27 * i, (int)spyPos.Y, 20, 21);
                    batch.Draw(spy, r, Espionage.Level >= i ? Player.EmpireColor : new Color(30,30,30));
                }

                var progressPos = new Vector2(Rect.X + 2, spyPos.Y + 30);
                batch.DrawString(Fonts.Arial12Bold, "Progress:", progressPos, Empire.EmpireColor);
                string percentProgress = $"{Espionage.ProgressPercent.String(1)}%";
                var percentPos = new Vector2(Rect.Right - (int)Fonts.Arial12Bold.MeasureString(percentProgress).X, spyPos.Y + 30);
                batch.DrawString(Fonts.Arial12Bold, percentProgress, percentPos, Color.Wheat);
                var infiltrationPos = new Vector2(Rect.X + 2, spyPos.Y + 50);
                batch.DrawString(Fonts.Arial12Bold, "Points/Turn:", infiltrationPos, Empire.EmpireColor);
                string pointsPerTurn = Espionage.GetProgressToIncrease(Player.Research.TaxedResearch, Player.CalcTotalEspionageWeight()).String(3);
                var pointsValuePos = new Vector2(Rect.Right - (int)Fonts.Arial12Bold.MeasureString(pointsPerTurn).X, spyPos.Y + 50);
                batch.DrawString(Fonts.Arial12Bold, pointsPerTurn, pointsValuePos, Color.Wheat);
            }

            void DrawDefenseSlider()
            {
                InfiltrationDefense.Draw(batch, elapsed);
            }
        }
    }
}
