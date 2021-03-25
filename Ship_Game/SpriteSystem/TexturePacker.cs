using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.SpriteSystem
{
    class TexturePacker
    {
        // Atlas texture padding, must be at least 2px, otherwise pixel padding
        // will cause overlap errors
        const int Padding = 2; 
        const int MinFreeSpotSize = 16; // Minimum width/height for recycled free spots
        const int MaxWidthHeightSum = 1024; // tex width+height > this is excluded from packing

        public int Width  { get; private set; }
        public int Height { get; private set; }

        public struct FreeSpot
        {
            public Rectangle r;
            public string from;
            public FreeSpot(int x, int y, int w, int h, string from)
            {
                r = new Rectangle(x,y,w,h);
                this.from = from;
            }
        }

        public Array<FreeSpot> FreeSpots = new Array<FreeSpot>();
        public Array<Rectangle> DebugFreeSpotFills = new Array<Rectangle>();
        public Array<Rectangle> DebugOverlapError = new Array<Rectangle>();

        int CursorX, CursorY, BottomY;
        TextureInfo[] Textures;

        void PrepareToPack(TextureInfo[] textures)
        {
            // Sort textures by AREA, DESCENDING
            Textures = textures;
            Array.Sort(textures, (a, b) =>
            {
                int diff = (b.Width * b.Height) - (a.Width * a.Height);
                if (diff == 0) // if w+h is equal, use name, so identical atlas anims are sequential
                    return string.CompareOrdinal(a.Name, b.Name);
                return diff;
            });
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

        int FinalizePack()
        {
            int packed = 0;
            foreach (TextureInfo t in Textures) if (!t.NoPack) ++packed;
            if (packed == 0) Width = Height = 0;
            else CheckInRange();
            Textures = null;
            return packed;
        }

        static bool IsExcludedFromPack(TextureInfo t)
        {
            if (t.NoPack) return true; // NoPack already set by someone
            t.NoPack = (t.Width + t.Height) > MaxWidthHeightSum;
            return t.NoPack;
        }

        void CheckInRange()
        {
            foreach (TextureInfo t in Textures)
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

        // checks for 1px padded overlap
        bool CheckForOverlap(int tIndex, TextureInfo t)
        {
            var ra = new Rectangle(t.X - 1, t.Y - 1, t.Width + 2, t.Height + 2);
            for (int i = 0; i < tIndex; ++i)
            {
                TextureInfo u = Textures[i];
                if (u.NoPack) continue;
                var rb = new Rectangle(u.X - 1, u.Y - 1, u.Width + 2, u.Height + 2);
                if (ra.GetIntersectingRect(rb, out Rectangle intersection))
                {
                    DebugOverlapError.Add(intersection);
                    Log.Warning(ConsoleColor.Red, $"{t} overlaps with existing tex: {u}  intersection: {intersection}");
                    return true;
                }
            }
            return false;
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
                if (IsExcludedFromPack(t) || FillFreeSpot(i, t))
                    continue;

                if (t.Width > Width)
                {
                    while (t.Width > Width) Width *= 2;
                    i = -1; ResetPack(Width, 128); continue;
                }

                int remainingX = Width - CursorX;
                if (remainingX < t.Width)
                {
                    int remainingY = (BottomY - CursorY);
                    if (remainingX >= MinFreeSpotSize && remainingY >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new FreeSpot(CursorX, CursorY, remainingX, remainingY, "endOfX"));
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
                if (TextureAtlas.DebugCheckOverlap)
                    CheckForOverlap(i, t);
                CursorX += (t.Width + Padding);

                // After filling our spot, there is a potential free spot.
                // We know this because we fill our objects in size descending order.
                // ____________
                // |  t.Width || (padding)
                // |==========||
                // | freespot ||
                if (t.Width >= MinFreeSpotSize)
                {
                    int freeSpotY = (t.Y + t.Height + Padding);
                    int freeSpotH = (BottomY - freeSpotY - Padding);
                    if (freeSpotH >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new FreeSpot(t.X, freeSpotY, t.Width, freeSpotH, "belowFill"));
                    }
                }
            }
            return FinalizePack();
        }


        bool FillFreeSpot(int tIndex, TextureInfo t)
        {
            for (int i = 0; i < FreeSpots.Count; ++i)
            {
                FreeSpot fs = FreeSpots[i];
                Rectangle r = fs.r;
                if (t.Width > r.Width || t.Height > r.Height)
                    continue;

                t.X = r.X;
                t.Y = r.Y;

                if (TextureAtlas.DebugCheckOverlap)
                    CheckForOverlap(tIndex, t);

                if (TextureAtlas.DebugDrawFreeSpotFills)
                    DebugFreeSpotFills.Add(new Rectangle(t.X, t.Y, t.Width, t.Height));

                FreeSpots.RemoveAt(i);
                int fillX = t.Width  + Padding;
                int fillY = t.Height + Padding;
                int remX = r.Width  - fillX - Padding;
                int remY = r.Height - fillY - Padding;
                // We have remaining sections A, B that could be recycled
                // So split it up if >= MinFreeSpotSize and insert to freeSpots
                // _____________
                // |fill |  A  |
                // |_____|  A  |
                // |__B__|__A__|
                if (remX >= MinFreeSpotSize) // A
                {
                    FreeSpots.Insert(i, new FreeSpot(r.X + fillX, r.Y, remX, r.Height, "A"));
                    if (remY >= MinFreeSpotSize) // B?
                        FreeSpots.Insert(i, new FreeSpot(r.X, r.Y + fillY, fillX, remY, "B?"));
                }
                // _________
                // |fill | |
                // |_____|_|
                // |B B B B|
                // |B_B_B_B|
                else if (remY >= MinFreeSpotSize && (fillX + remX) >= MinFreeSpotSize)
                {
                    FreeSpots.Insert(i, new FreeSpot(r.X, r.Y + fillY, fillX + remX, remY, "BB"));
                }
                return true; // success! we filled the free spot
            }
            return false;
        }
    }
}
