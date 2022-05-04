﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public class ResponseListItem : ScrollListItem<ResponseListItem>
    {
        readonly UIButton Btn;
        public ResponseListItem(Response response, Action<Response> onClicked)
        {
            string text = response.LocalizedText.NotEmpty()
                        ? Localizer.Token(response.LocalizedText)
                        : response.Text ?? Localizer.Token(GameText.No);
            
            var customStyle = new UIButton.StyleTextures();
            Btn = Add(new UIButton(customStyle, new Vector2(120, 24), text));
            Btn.OnClick = b => onClicked(response);
            Btn.Font = Fonts.Arial14Bold;
        }

        public override void PerformLayout()
        {
            Btn.Rect = Rect;
            base.PerformLayout();
        }
    }
}
