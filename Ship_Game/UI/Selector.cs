using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.UI;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class Selector : UIElementV2
    {
        NineSliceSprite S;
        readonly Color Fill;
        readonly Color EdgeColor;

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
            Initialize(new(Rect));
        }

        class ElementTextures
        {
            public SubTexture CornerTL, CornerTR, CornerBL, CornerBR;
            public SubTexture RoundTL, RoundTR, RoundBL, RoundBR;
            public SubTexture HoriVert;
        }

        static int ContentId;
        static ElementTextures Tex;

        void Initialize(in RectF r)
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

            S ??= new();
            S.Update(r, Tex.CornerTL, Tex.CornerTR, Tex.CornerBL, Tex.CornerBR,
                        Tex.HoriVert, Tex.HoriVert, borderWidth:2);
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
                float h2 = S.BL.Top-S.TL.Bottom;
                if (h2 > 0)
                {
                    var left = new RectF(x, S.TL.Bottom, 8, h2);
                    var right = new RectF(x+w-8, S.TL.Bottom, 8, h2);
                    batch.FillRectangle(left, Fill);
                    batch.FillRectangle(right, Fill);
                }

                // draw rounded TL, TR, BL, BR
                //    ______
                // (TL|    |TR)
                // |          |
                // (BL|____|BR)
                batch.Draw(Tex.RoundTL, S.TL, Fill);
                batch.Draw(Tex.RoundTR, S.TR, Fill);
                batch.Draw(Tex.RoundBL, S.BL, Fill);
                batch.Draw(Tex.RoundBR, S.BR, Fill);
            }

            batch.Draw(Tex.HoriVert, S.Top, EdgeColor);
            batch.Draw(Tex.HoriVert, S.Bot, EdgeColor);
            batch.Draw(Tex.HoriVert, S.VL, EdgeColor);
            batch.Draw(Tex.HoriVert, S.VR, EdgeColor);
            batch.Draw(Tex.CornerTL, S.TL, EdgeColor);
            batch.Draw(Tex.CornerTR, S.TR, EdgeColor);
            batch.Draw(Tex.CornerBL, S.BL, EdgeColor);
            batch.Draw(Tex.CornerBR, S.BR, EdgeColor);
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }
    }
}