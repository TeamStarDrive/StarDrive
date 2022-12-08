using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.UI
{
    /// <summary>
    /// Nine-Slice Sprite container
    /// https://gamemaker.io/en/blog/slick-interfaces-with-9-slice
    /// </summary>
    public class NineSliceSprite
    {
        // top bar
        public RectF TL, TR, Top;
        public RectF VL, VR, ClientArea /* the center rect */;
        public RectF BL, BR, Bot;

        // width for top/bottom horizontal bars and left/right vertical bars
        public int BorderWidth;

        // textures for TL, TR, BL, BR corners and for Horizontal/Vertical Bars
        public SubTexture TTL, TTR, TBL, TBR, THB, TVB;

        public NineSliceSprite()
        {
        }

        /// <summary>
        /// Initializes the 4 corners of the Nine Slice Sprite
        ///
        /// The corners are allowed to be different dimensions,
        /// to allow non-rectangular 9-slice panels
        /// </summary>
        public void Update(in RectF r, 
                           SubTexture tl, SubTexture tr,
                           SubTexture bl, SubTexture br,
                           SubTexture hb, SubTexture vb,
                           int borderWidth)
        {
            BorderWidth = borderWidth;
            TTL = tl; TTR = tr;
            TBL = bl; TBR = br;
            THB = hb; TVB = vb;

            // top corners
            TL = new(r.Pos, tl.SizeF);
            TR = new(r.Right-tr.Width, r.Y, tr.SizeF);
            // bottom corners
            BL = new(r.X, r.Bottom-bl.Height, bl.SizeF);
            BR = new(r.Right-br.Width, r.Bottom-br.Height, br.SizeF);

            // define the central ClientArea for later use
            float clientX = r.X + TL.W;
            float clientY = r.Y + TR.H;
            float clientW = r.W - (TL.W + TR.W); // use top bar for the width
            float clientH = r.H - (TL.H + BL.H); // left bar for the height
            ClientArea = new(clientX, clientY, clientW, clientH);

            // top and bottom horizontal bars
            Top = new(r.X + TL.W, TL.Y, clientW, borderWidth);
            Bot = new(r.X + BL.W, r.Bottom - borderWidth, r.W - (BL.W + BR.W), borderWidth);

            // left and right vertical bars
            VL = new(r.X, clientY, borderWidth, clientH);
            VR = new(r.Right - borderWidth, clientY, borderWidth, clientH);
        }

        public void DrawBorders(SpriteBatch batch)
        {
            // draw horizontal bars first
            batch.Draw(THB, Top, Color.White);
            batch.Draw(THB, Bot, Color.White);
            // draw vertical bars
            batch.Draw(TVB, VL, Color.White);
            batch.Draw(TVB, VR, Color.White);

            // draw corners
            batch.Draw(TTL, TL, Color.White);
            batch.Draw(TTR, TR, Color.White);
            batch.Draw(TBL, BL, Color.White);
            batch.Draw(TBR, BR, Color.White);
        }

        public void DrawTopBar(SpriteBatch batch, float y)
        {
            // draw horizontal bar first
            batch.Draw(THB, new RectF(Top.X, y, Top.W, Top.H), Color.White);

            batch.Draw(TTL, new RectF(TL.X, y, TL.W, TL.H), Color.White);
            batch.Draw(TTR, new RectF(TR.X, y, TR.W, TR.H), Color.White);
        }

        public void DrawVerticalBars(SpriteBatch batch, float y)
        {
            // vertical left & right |
            float height = (BL.Y - y) - TL.H;
            batch.Draw(TVB, new RectF(VR.X, y+TL.H, VR.W, height), Color.White);
            batch.Draw(TVB, new RectF(VL.X, y+TL.H, VL.W, height), Color.White);
        }

        public void DrawBottomBar(SpriteBatch batch)
        {
            // bottom corners L
            batch.Draw(TBR, BR, Color.White);
            batch.Draw(TBL, BL, Color.White);
            // bottom horizontal --- 
            batch.Draw(THB, Bot, Color.White);
        }

        public void DrawDebug(SpriteBatch batch)
        {
            // top bar
            batch.DrawRectangle(TL, Color.Red);
            batch.DrawRectangle(TR, Color.Red);
            batch.DrawRectangle(Top, Color.Yellow);

            // bottom bar
            batch.DrawRectangle(BL, Color.Red);
            batch.DrawRectangle(BR, Color.Red);
            batch.DrawRectangle(Bot, Color.Yellow);
            
            // sides and central client area
            batch.DrawRectangle(VL, Color.Yellow);
            batch.DrawRectangle(VR, Color.Yellow);
            batch.DrawRectangle(ClientArea, Color.Yellow.Alpha(0.5f));
        }
    }
}
