using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public class DialogOptionListItem : ScrollListItem<DialogOptionListItem>
    {
        public readonly DialogOption Option;
        public DialogOptionListItem(DialogOption option)
        {
            Height = 15;
            Option = option;
        }
        public override void Draw(SpriteBatch batch)
        {
            Color color = (Hovered ? Color.White : new Color(255, 255, 255, 220));
            batch.DrawDropShadowText(Option.Number+". "+Option.Words, Pos, Fonts.Consolas18, color);
            
        }
        
    }
}
