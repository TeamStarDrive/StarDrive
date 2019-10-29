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
        }

        public override void PerformLayout()
        {
            Portrait = Screen.TextureDict[EmpireData];

            int width = (int)(List.Width * 0.8f);
            int height = (int)(width / Portrait.AspectRatio);
            List.ItemHeight = height;
            base.PerformLayout();
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            int height = (int)Height - 8;
            int width = (int)(height * Portrait.AspectRatio);
            var portrait = new Rectangle((int)CenterX - width/2, (int)Y, width, height);
            batch.Draw(Portrait, portrait, Color.White);

            if (Screen.SelectedData == EmpireData)
            {
                batch.DrawRectangle(portrait, Color.BurlyWood);
            }
        }
    }
}
