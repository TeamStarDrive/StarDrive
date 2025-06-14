using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.GameScreens.NewGame;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class SelectOpponentListItem : ScrollListItem<SelectOpponentListItem>
    {
        public SelectOpponentsScreen Screen;
        public IEmpireData EmpireData;
        public SubTexture Portrait;

        public SelectOpponentListItem(SelectOpponentsScreen screen, IEmpireData empireData)
        {
            Screen = screen;
            EmpireData = empireData;
            Portrait = ResourceManager.Texture("Races/" + empireData.VideoPath);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            int height = (int)Height;
            int width = (int)Portrait.GetWidthFromHeightAspect(height);
            var portrait = new Rectangle((int)X +10, (int)Y, width, height);
            bool selected = Screen.Params.SelectedOpponents.Contains(EmpireData);
            float alpha = selected ? 1f : 0.3f;
            batch.Draw(Portrait, portrait, Color.White.Alpha(alpha));
            if (selected)
                batch.DrawRectangle(portrait, EmpireData.Traits.Color, thickness: 2);
        }
    }
}