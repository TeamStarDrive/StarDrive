using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.SpriteSystem
{
    class TexturePacker
    {
        const int Padding = 2; // Atlas texture padding
        const int MinFreeSpotSize = 16; // Minimum width/height for recycled free spots
        const int MaxWidthHeightSum = 920; // tex width+height > this is excluded from packing

        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Array<Rectangle> FreeSpots = new Array<Rectangle>();

        int CursorX, CursorY, BottomY;

        void PrepareToPack(TextureInfo[] textures)
        {
            // Sort textures by AREA, DESCENDING
            Array.Sort(textures, (a, b) => (b.Width * b.Height) - (a.Width * a.Height));
            FreeSpots.Clear();
            Width = 128;
            Height = 128;
            CursorX = CursorY = BottomY = 0;
        }

        void ResetPack(int width, int height)
        {
            FreeSpots.Clear();
            Width  = width;
            Height = height;
            CursorX = CursorY = BottomY = 0;
        }

        int FinalizePack(TextureInfo[] textures)
        {
            int packed = 0;
            foreach (TextureInfo t in textures) if (!t.NoPack) ++packed;
            if (packed == 0) Width = Height = 0;
            else CheckInRange(textures);
            return packed;
        }

        static bool IsTextureTooBig(TextureInfo t)
        {
            t.NoPack = (t.Width + t.Height) > MaxWidthHeightSum;
            return t.NoPack;
        }

        void CheckInRange(TextureInfo[] textures)
        {
            foreach (TextureInfo t in textures)
            {
                if (t.NoPack) continue;
                if ((t.X + t.Width) > Width)
                {
                    Log.Error($"{t} X-axis out of atlas width:{Width}");
                }
                if ((t.Y + t.Height) > Height)
                {
                    Log.Error($"{t} Y-axis out of atlas height:{Height}");
                }
            }
        }

        // @note PERF: This is fast enough
        // @return Number of textures that were packed. Big textures are excluded from packing.
        public int PackTextures(TextureInfo[] textures)
        {
            //Stopwatch s = Stopwatch.StartNew();
            PrepareToPack(textures);

            for (int i = 0; i < textures.Length; ++i)
            {
                TextureInfo t = textures[i];
                if (IsTextureTooBig(t) || FillFreeSpot(t))
                    continue;

                if (t.Width > Width)
                {
                    while (t.Width > Width) Width *= 2;
                    i = -1; ResetPack(Width, 128); continue;
                }

                int remainingX = Width - CursorX;
                if (remainingX < t.Width)
                {
                    int remainingY = BottomY - CursorY;
                    if (remainingX >= MinFreeSpotSize && remainingY >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new Rectangle(CursorX, CursorY, remainingX, remainingY));
                    }
                    CursorX = 0;
                    CursorY = BottomY + Padding;
                }
                int newBottomY = CursorY + (t.Height);
                if (newBottomY > BottomY)
                {
                    BottomY = newBottomY;
                    while (BottomY > Height) { Height += 64; }
                    if (Height >= Width * 2) // reset everything if Height is double of Width
                    {
                        i = -1; ResetPack(Width * 2, 128); continue;
                    }
                }

                t.X = CursorX;
                t.Y = CursorY;
                int fillX = (t.Width + Padding);
                CursorX += fillX;

                // After filling our spot, there is a potential free spot.
                // We know this because we fill our objects in descending order.
                // ____________
                // |  tdfill  |
                // |__________|
                // | freespot |
                if (fillX >= MinFreeSpotSize)
                {
                    int freeSpotY = (t.Y + t.Height + Padding);
                    int freeSpotH = (BottomY - freeSpotY);
                    if (freeSpotH >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new Rectangle(t.X, freeSpotY, fillX, freeSpotH));
                    }
                }
            }
            return FinalizePack(textures);
        }


        bool FillFreeSpot(TextureInfo td)
        {
            for (int j = 0; j < FreeSpots.Count; ++j)
            {
                Rectangle r = FreeSpots[j];
                if (td.Width > r.Width || td.Height > r.Height)
                    continue;
                td.X = r.X;
                td.Y = r.Y;
                FreeSpots.RemoveAt(j);
                int fillX = td.Width + Padding;
                int fillY = td.Height + Padding;
                int remX = r.Width - fillX;
                int remY = r.Height - fillY;
                // We have remaining sections A, B that could be recycled
                // So split it up if >= MinFreeSpotSize and insert to freeSpots
                // _____________
                // |fill |  A  |
                // |_____|  A  |
                // |__B__|__A__|
                if (remX >= MinFreeSpotSize) // A
                {
                    FreeSpots.Insert(j, new Rectangle(r.X + fillX, r.Y, remX, r.Height));
                    if (remY >= MinFreeSpotSize) // B?
                        FreeSpots.Insert(j, new Rectangle(r.X, r.Y + fillY, fillX, remY));
                }
                // _________
                // |fill | |
                // |_____|_|
                // |B B B B|
                // |B_B_B_B|
                else if (remY >= MinFreeSpotSize && (fillX + remX) >= MinFreeSpotSize)
                {
                    FreeSpots.Insert(j, new Rectangle(r.X, r.Y + fillY, fillX + remX, remY));
                }
                return true; // success! we filled the free spot
            }
            return false;
        }
    }
}
