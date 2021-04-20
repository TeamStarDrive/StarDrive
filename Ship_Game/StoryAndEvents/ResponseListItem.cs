using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class ResponseListItem : ScrollListItem<ResponseListItem>
    {
        public Response Response;
        public ResponseListItem(Response response)
        {
            Response = response;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            string text = Response.LocalizedText.NotEmpty()
                        ? Localizer.Token(Response.LocalizedText)
                        : Response.Text;
            batch.DrawString(Fonts.Arial12Bold,
                $"{ItemIndex+1}. {text}", Pos, (Hovered ? Color.LightGray : Color.White));
        }
    }
}
