using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class RaceArchetypeListItem : ScrollList<RaceArchetypeListItem>.Entry
    {
        public RaceDesignScreen Screen;
        public IEmpireData EmpireData;
        public RaceArchetypeListItem(RaceDesignScreen screen, IEmpireData empireData)
        {
            Screen = screen;
            EmpireData = empireData;
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            var portrait = new Rectangle((int)CenterX - 176, (int)Y, 352, 128);
            batch.Draw(Screen.TextureDict[EmpireData], portrait, Color.White);

            if (Screen.SelectedData == EmpireData)
            {
                batch.DrawRectangle(portrait, Color.BurlyWood);
            }
        }
    }
}
