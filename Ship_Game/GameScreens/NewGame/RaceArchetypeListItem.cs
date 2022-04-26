using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class RaceArchetypeListItem : ScrollListItem<RaceArchetypeListItem>
    {
        public RaceDesignScreen Screen;
        public IEmpireData EmpireData;
        public SubTexture Portrait;

        public RaceArchetypeListItem(RaceDesignScreen screen, IEmpireData empireData)
        {
            Screen = screen;
            EmpireData = empireData;
            Portrait = ResourceManager.Texture("Races/" + empireData.VideoPath);
        }

        public override int ItemHeight
        {
            get
            {
                int width = (int)(List.Width * 0.8f);
                return (int)Portrait.GetHeightFromWidthAspect(width);
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            int height = (int)Height;
            int width = (int)Portrait.GetWidthFromHeightAspect(height);
            var portrait = new Rectangle((int)CenterX - width/2, (int)Y, width, height);
            batch.Draw(Portrait, portrait, Color.White);

            if (Screen.SelectedData == EmpireData)
            {
                batch.DrawRectangle(portrait, Color.BurlyWood);
            }
        }
    }
}
