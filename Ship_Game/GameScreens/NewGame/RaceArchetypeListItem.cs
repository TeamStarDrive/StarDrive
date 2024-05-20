using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.GameScreens.NewGame;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class RaceArchetypeListItem : ScrollListItem<RaceArchetypeListItem>
    {
        public GameScreen Screen;
        public IEmpireData EmpireData;
        public SubTexture Portrait;

        public RaceArchetypeListItem(RaceDesignScreen screen, IEmpireData empireData)
        {
            Screen = screen;
            EmpireData = empireData;
            Portrait = ResourceManager.Texture("Races/" + empireData.VideoPath);
        }

        public RaceArchetypeListItem(FoeSelectionScreen screen, IEmpireData empireData)
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
            var portrait = new Rectangle((int)CenterX - width / 2, (int)Y, width, height);
            batch.Draw(Portrait, portrait, Color.White);

            if ((Screen as RaceDesignScreen)?.SelectedData == EmpireData)
            {
                batch.DrawRectangle(portrait, Color.BurlyWood);
            }

            if (((Screen as FoeSelectionScreen)?.FoeList?.Contains(EmpireData)) ?? false)
            {
                batch.DrawRectangle(portrait, Color.BurlyWood);
            }

        }
    }
}
