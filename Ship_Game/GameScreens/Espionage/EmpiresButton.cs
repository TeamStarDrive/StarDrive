using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.GameScreens.Espionage
{
    public class EmpireButton : UIElementV2
    {
        public readonly Empire Empire;
        readonly EspionageScreen Screen;
        readonly Action<EmpireButton> OnClick;

        public EmpireButton(EspionageScreen screen, Empire e, in Rectangle rect, Action<EmpireButton> onClick)
            : base(rect)
        {
            Empire = e;
            Screen = screen;
            OnClick = onClick;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.InGameSelect && Rect.HitTest(input.CursorPosition))
            {
                GameAudio.EchoAffirmative();
                OnClick(this);
                return true;
            }
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Empire player = Empire.Universe.Player;

            // red background:
            if (player != Empire && player.IsAtWarWith(Empire) && !Empire.IsDefeated)
            {
                batch.FillRectangle(Rect.Bevel(2), Color.Red);
            }

            void DrawRacePortrait()
            {
                batch.Draw(ResourceManager.Texture("Portraits/" + Empire.data.PortraitName), Rect, Color.White);
                batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), Rect, Color.White);

                Vector2 size = Fonts.Arial12Bold.MeasureString(Empire.data.Traits.Name);
                var nameCursor = new Vector2(Rect.X + 62 - size.X / 2f, Rect.Y + 148 + 8);
                batch.DrawString(Fonts.Arial12Bold, Empire.data.Traits.Name, nameCursor, Color.White);
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
            else if (player == Empire || player.IsKnown(Empire))
            {
                DrawRacePortrait();

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
            else if (player != Empire)
            {
                batch.Draw(ResourceManager.Texture("Portraits/unknown"), Rect, Color.White);
            }

            if (Empire == Screen.SelectedEmpire)
                batch.DrawRectangle(Rect, Color.Orange);
        }
    }
}
