using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class Selector : IElement
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

	    public Rectangle Rect {get; private set;}


		public Selector(Rectangle theMenu)
		{
			theMenu.X = theMenu.X - 15;
			theMenu.Y = theMenu.Y - 5;
			theMenu.Width = theMenu.Width + 12;
            Initialize(theMenu);
		}

		public Selector(Rectangle theMenu, bool useRealRect)
		{
		    Initialize(theMenu);
		}

		public Selector(Rectangle theMenu, Color fillColor)
		{
			Fill = fillColor;
		    Initialize(theMenu);
		}

	    private class ElementTextures
	    {
	        public Texture2D CornerTL, CornerTR, CornerBL, CornerBR;
            public Texture2D RoundTL, RoundTR, RoundBL, RoundBR;
            public Texture2D HoriVert;
	    }
	    private static ElementTextures Tex;

        private void Initialize(Rectangle theMenu)
        {
            if (Tex == null)
            {
                Tex = new ElementTextures
                {
                    CornerTL = ResourceManager.Texture("NewUI/submenu_corner_TL"),
                    CornerTR = ResourceManager.Texture("NewUI/submenu_corner_TR"),
                    CornerBL = ResourceManager.Texture("NewUI/submenu_corner_BL"),
                    CornerBR = ResourceManager.Texture("NewUI/submenu_corner_BR"),
                    RoundTL  = ResourceManager.Texture("NewUI/rounded_upperLeft"),
                    RoundTR  = ResourceManager.Texture("NewUI/rounded_upperRight"),
                    RoundBL  = ResourceManager.Texture("NewUI/rounded_lowerLeft"),
                    RoundBR  = ResourceManager.Texture("NewUI/rounded_lowerRight"),
                    HoriVert = ResourceManager.Texture("NewUI/submenu_horiz_vert")
                };
            }

            Rect = theMenu;
            int x = theMenu.X,     y = theMenu.Y;
            int w = theMenu.Width, h = theMenu.Height;

            TL = new Rectangle(x, y - 2, Tex.CornerTL.Width, Tex.CornerTL.Height);
            TR = new Rectangle(x + w - Tex.CornerTR.Width, y - 2, Tex.CornerTR.Width, Tex.CornerTR.Height);
            BL = new Rectangle(x, y + h - Tex.CornerBL.Height + 2, Tex.CornerBL.Width, Tex.CornerBL.Height);
            BR = new Rectangle(x + w - Tex.CornerBR.Width, y + h + 2 - Tex.CornerBR.Height, Tex.CornerBR.Width, Tex.CornerBR.Height);
            HT = new Rectangle(x + Tex.CornerTL.Width, y - 2, w - TR.Width - Tex.CornerTL.Width, 2);
            HB = new Rectangle(x + BL.Width, y + h, w - BL.Width - BR.Width, 2);
            VL = new Rectangle(x, y + TR.Height - 2, 2, h - BL.Height - 2);
            VR = new Rectangle(x + w - 2, y + TR.Height - 2, 2, h - BR.Height - 2);
        }


	    public void Layout(Vector2 pos)
	    {
	        throw new NotImplementedException();
	    }

	    public void Draw(SpriteBatch spriteBatch)
		{
            int x = Rect.X,     y = Rect.Y;
            int w = Rect.Width, h = Rect.Height;
			var upperleft  = new Rectangle(x, y, 24, 24);
		    var upperRight = new Rectangle(x + w - 24, y, 24, 24);
		    var lowerLeft  = new Rectangle(x, y + h - 24, 24, 24);
		    var lowerRight = new Rectangle(x + w - 24, y + h - 24, 24, 24);
		    var top        = new Rectangle(x + 24, y, w - 48, 24);
		    var bottom     = new Rectangle(x + 24, y + h - 24, w - 48, 24);
		    var right      = new Rectangle(x + w - 24, y + 24, 24, h - 48);
		    var left       = new Rectangle(x, y + 24, 24, h - 48);
		    var middle     = new Rectangle(x + 24, y + 24, w - 48, h - 48);

		    spriteBatch.Draw(Tex.RoundTL, upperleft,  Fill);
		    spriteBatch.Draw(Tex.RoundTR, upperRight, Fill);
		    spriteBatch.Draw(Tex.RoundBL, lowerLeft,  Fill);
		    spriteBatch.Draw(Tex.RoundBR, lowerRight, Fill);
            spriteBatch.FillRectangle(top,    Fill);
		    spriteBatch.FillRectangle(bottom, Fill);
		    spriteBatch.FillRectangle(right,  Fill);
		    spriteBatch.FillRectangle(left,   Fill);
		    spriteBatch.FillRectangle(middle, Fill);

		    spriteBatch.Draw(Tex.HoriVert, HT, Color.White);
		    spriteBatch.Draw(Tex.HoriVert, HB, Color.White);
		    spriteBatch.Draw(Tex.HoriVert, VR, Color.White);
		    spriteBatch.Draw(Tex.HoriVert, VL, Color.White);
		    spriteBatch.Draw(Tex.CornerTL, TL, Color.White);
		    spriteBatch.Draw(Tex.CornerTR, TR, Color.White);
		    spriteBatch.Draw(Tex.CornerBR, BR, Color.White);
		    spriteBatch.Draw(Tex.CornerBL, BL, Color.White);
		}

	    public bool HandleInput(InputState input)
	    {
	        return false;
	    }
	}
}