using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.UI;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class Selector : UIElementV2
    {
        private Rectangle TR; // top-right corner
        private Rectangle BL;
        private Rectangle BR;
        private Rectangle TL;
        
        private Rectangle HT; // horizontal top bar
        private Rectangle HB;
        private Rectangle VL; // vertical left bar
        private Rectangle VR;

        private readonly Color Fill;
        private readonly Color EdgeColor;

        public Selector(UIElementContainer parent, LocalPos pos, RelSize size, Color fillColor)
            : base(pos, size)
        {
            Fill = fillColor;
            EdgeColor = Color.White;

            parent.Add(this);
            PerformLayout();
        }

        public Selector(UIElementContainer parent, LocalPos pos, Vector2 size, Color fillColor)
            : base(pos, size)
        {
            Fill = fillColor;
            EdgeColor = Color.White;

            parent.Add(this);
            PerformLayout();
        }

        public Selector(Rectangle theMenu) : base(theMenu)
        {
            EdgeColor = Color.White;
            PerformLayout();
        }

        public Selector(LocalPos pos, Vector2 size, Color fillColor) : base(new RectF(pos.X, pos.Y, size))
        {
            Fill = fillColor;
            EdgeColor = Color.White;
            PerformLayout();
        }

        public Selector(Rectangle theMenu, Color fillColor) : base(theMenu)
        {
            Fill = fillColor;
            EdgeColor = Color.White;
            PerformLayout();
        }

        public Selector(Rectangle theMenu, Color fillColor, float textureAlpha) : base(theMenu)
        {
            Fill = fillColor;
            EdgeColor = new(Color.White, (byte)textureAlpha);
            PerformLayout();
        }

        public Selector(Rectangle theMenu, Color fillColor, Color edgeColor) : base(theMenu)
        {
            Fill = fillColor;
            EdgeColor = edgeColor;
            PerformLayout();
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            Height = Math.Max(12, Height); // height must be at least 12px
            Initialize((int)X, (int)Y, (int)Width, (int)Height);
        }

        class ElementTextures
        {
            public SubTexture CornerTL, CornerTR, CornerBL, CornerBR;
            public SubTexture RoundTL, RoundTR, RoundBL, RoundBR;
            public SubTexture HoriVert;
        }

        static int ContentId;
        static ElementTextures Tex;

        void Initialize(int x, int y, int w, int h)
        {
            if (Tex == null || ContentId != ResourceManager.ContentId)
            {
                ContentId = ResourceManager.ContentId;
                Tex = new ElementTextures
                {
                    CornerTL = ResourceManager.Texture("NewUI/submenu_corner_TL"),
                    CornerTR = ResourceManager.Texture("NewUI/submenu_corner_TR"),
                    CornerBL = ResourceManager.Texture("NewUI/submenu_corner_BL"),
                    CornerBR = ResourceManager.Texture("NewUI/submenu_corner_BR"),
                    RoundTL  = ResourceManager.Texture("NewUI/rounded_TL"),
                    RoundTR  = ResourceManager.Texture("NewUI/rounded_TR"),
                    RoundBL  = ResourceManager.Texture("NewUI/rounded_BL"),
                    RoundBR  = ResourceManager.Texture("NewUI/rounded_BR"),
                    HoriVert = ResourceManager.Texture("NewUI/submenu_horiz_vert")
                };
            }

            TL = new(x, y - 2, Tex.CornerTL.Width, Tex.CornerTL.Height);
            TR = new(x + w - Tex.CornerTR.Width, y - 2, Tex.CornerTR.Width, Tex.CornerTR.Height);
            BL = new(x, y + h - Tex.CornerBL.Height + 2, Tex.CornerBL.Width, Tex.CornerBL.Height);
            BR = new(x + w - Tex.CornerBR.Width, y + h + 2 - Tex.CornerBR.Height, Tex.CornerBR.Width, Tex.CornerBR.Height);
            HT = new(x + Tex.CornerTL.Width, y - 2, w - TR.Width - Tex.CornerTL.Width, 2);
            HB = new(x + BL.Width, y + h, w - BL.Width - BR.Width, 2);
            VL = new(x, y + TR.Height - 2, 2, h - BL.Height - 2);
            VR = new(x + w - 2, y + TR.Height - 2, 2, h - BR.Height - 2);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Fill.A > 0)
            {
                int x = (int)X,     y = (int)Y;
                int w = (int)Width, h = (int)Height;

                // fill Left, Middle, Right
                //  ______________
                // |__|        |__|
                // |le|        |ri|
                // |ft| middle |gt|
                // |__|        |__|
                // |__|________|__|
                var middle = new Rectangle(x+8, y, w-16, h);
                batch.FillRectangle(middle, Fill);
                int h2 = BL.Top-TL.Bottom;
                if (h2 > 0)
                {
                    var left   = new Rectangle(x,     TL.Bottom, 8, h2);
                    var right  = new Rectangle(x+w-8, TL.Bottom, 8, h2);
                    batch.FillRectangle(left, Fill);
                    batch.FillRectangle(right, Fill);
                }

                // draw rounded TL, TR, BL, BR
                //    ______
                // (TL|    |TR)
                // |          |
                // (BL|____|BR)
                batch.Draw(Tex.RoundTL, TL, Fill);
                batch.Draw(Tex.RoundTR, TR, Fill);
                batch.Draw(Tex.RoundBL, BL, Fill);
                batch.Draw(Tex.RoundBR, BR, Fill);
            }

            batch.Draw(Tex.HoriVert, HT, EdgeColor);
            batch.Draw(Tex.HoriVert, HB, EdgeColor);
            batch.Draw(Tex.HoriVert, VR, EdgeColor);
            batch.Draw(Tex.HoriVert, VL, EdgeColor);
            batch.Draw(Tex.CornerTL, TL, EdgeColor);
            batch.Draw(Tex.CornerTR, TR, EdgeColor);
            batch.Draw(Tex.CornerBR, BR, EdgeColor);
            batch.Draw(Tex.CornerBL, BL, EdgeColor);
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }
    }
}