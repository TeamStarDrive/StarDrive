using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.GameScreens.NewGame;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class SelectOpponentListItem : ScrollListItem<SelectOpponentListItem>
    {
        public SelectOpponnetsScreen Screen;
        public IEmpireData EmpireData;
        public SubTexture Portrait;

        public SelectOpponentListItem(SelectOpponnetsScreen screen, IEmpireData empireData)
        {
            Screen = screen;
            EmpireData = empireData;
            Portrait = ResourceManager.Texture("Portraits/" + empireData.VideoPath);
        }

        /*
        public override int ItemHeight
        {
            get
            {
                int width = (int)(List.Width * 0.8f);
                return (int)Portrait.GetHeightFromWidthAspect(width);
            }
        }*/

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            int height = (int)Height;
            int width = (int)Portrait.GetWidthFromHeightAspect(height);
            var portrait = new Rectangle((int)X +10, (int)Y, width, height);
            batch.Draw(Portrait, portrait, Color.White);

            if (Screen.Params.SelectedOpponents.Contains(EmpireData))
            {
                batch.DrawRectangle(portrait, Color.White);
            }
        }
    }
}