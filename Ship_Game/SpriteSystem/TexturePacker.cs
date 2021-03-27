using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Texture;

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
        public Array<Rectangle> DebugFreeSpotOverlapError = new Array<Rectangle>();

        TextureInfo[] Textures;
        string CachePath;
        int Iteration = 0;
        GameContentManager Content;

        public TexturePacker(GameContentManager content, string cachePath)
        {
            CachePath = cachePath;
            Content = content;
        }

        // @return True if size is close to target size
        static bool IsNearToPadSize(int size, int target)
        {
            return (target - Padding) <= size && size <= target;
        }

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

            // filter out textures which shouldn't be packed
            foreach (TextureInfo t in textures)
            {
                t.NoPack |= (t.Width + t.Height) >= MaxWidthHeightSum;
                // for perfectly square 512x512, packing will be waste of texture memory
                if (IsNearToPadSize(t.Width, 512) &&
                    IsNearToPadSize(t.Height, 512))
                {
                    t.NoPack = true;
                }
            }

            // always set the Width/Height to minimum
            // to achieve maximum packing
            // pre-calculating the size is detrimental to efficient packing
            Width = 128;
            Height = 128;
        }

        void SortFreeSpots()
        {
            // sort free spots, Ascending, so that we always use the smallest possible free spot
            FreeSpots.Sort((a, b) =>
            {
                int diff = a.r.Area() - b.r.Area();
                if (diff == 0) // prefer wide spots first
                    return a.r.Width - b.r.Width;
                return diff;
            });
        }

        void ResetTextureCoords(TextureInfo[] textures)
        {
            for (int i = 0; i < textures.Length; ++i)
            {
                TextureInfo t = textures[i];
                t.X = t.Y = -1;
            }
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
            const int p1 = Padding / 2;
            const int p2 = Padding;
            var ra = new Rectangle(t.X - p1, t.Y - p1, t.Width + p2, t.Height + p2);
            for (int i = 0; i < tIndex; ++i)
            {
                TextureInfo u = Textures[i];
                if (u.NoPack) continue;
                var rb = new Rectangle(u.X - p1, u.Y - p1, u.Width + p2, u.Height + p2);
                if (ra.GetIntersectingRect(rb, out Rectangle intersection))
                {
                    DebugOverlapError.Add(intersection);
                    Log.Warning(ConsoleColor.Red, $"{t} overlaps with existing tex: {u}  intersection: {intersection}");
                    return true;
                }
            }
            return false;
        }

        void CheckFreeSpotOverlap()
        {
            for (int i = 0; i < FreeSpots.Count; ++i)
            {
                var ra = FreeSpots[i].r;
                for (int j = i + 1; j < FreeSpots.Count; ++j)
                {
                    var rb = FreeSpots[j].r;
                    if (ra.GetIntersectingRect(rb, out Rectangle intersection))
                    {
                        DebugFreeSpotOverlapError.Add(intersection);
                        Log.Warning(ConsoleColor.Red, $"FreeSpot {ra} overlaps with FreeSpot: {rb}  intersection: {intersection}");
                    }
                }
            }
        }

        void SaveFreeSpotsDebug(TextureInfo[] textures)
        {
            var data = new Color[Width * Height];
            ImageUtils.FillPixels(data, Width, Height, 0, 0, Color.Black, Width, Height);

            foreach (TextureInfo t in textures)
            {
                if (t.X != -1)
                    ImageUtils.DrawRectangle(data, Width, Height, new Rectangle(t.X, t.Y, t.Width, t.Height), Color.YellowGreen);
            }

            foreach (FreeSpot fs in FreeSpots)
                ImageUtils.DrawRectangle(data, Width, Height, fs.r, Color.AliceBlue);

            foreach (FreeSpot f in FreeSpots)
                ImageUtils.DrawRectangle(data, Width, Height, f.r, Color.AliceBlue);

            ImageUtils.ConvertToRGBA(Width, Height, data);
            ImageUtils.SaveAsDds($"{CachePath}.{Iteration}.dds", Width, Height, data); // save compressed!
        }

        void ResetState(TextureInfo[] textures, int width, int height)
        {
            if (TextureAtlas.DebugPackerExpansion)
                SaveFreeSpotsDebug(textures);
            ResetTextureCoords(textures);
            Width  = width;
            Height = height;
            DebugFreeSpotFills.Clear();
            FreeSpots.Clear();
            ++Iteration;
        }

        void PackByLinearWalk(TextureInfo[] textures)
        {
            int cursorX = 0;
            int cursorY = 0;
            int bottomY = 0;
            int i;

            void ResetPack(int width, int height)
            {
                ResetState(textures, width, height);
                cursorX = cursorY = bottomY = 0;
                i = -1;
            }

            for (i = 0; i < textures.Length; ++i)
            {
                TextureInfo t = textures[i];
                if (t.NoPack || FillFreeSpot(i, t))
                    continue;

                if (t.Width > Width)
                {
                    while (t.Width > Width) Width += 64;
                    ResetPack(Width, 128);
                    continue;
                }

                int remainingX = Width - cursorX;
                if (remainingX < t.Width)
                {
                    int remainingY = (bottomY - cursorY);
                    if (remainingX >= MinFreeSpotSize && remainingY >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new FreeSpot(cursorX, cursorY, remainingX, remainingY, "endOfX"));
                        if (TextureAtlas.DebugCheckOverlap)
                            CheckFreeSpotOverlap();
                    }
                    cursorX = 0;
                    cursorY = bottomY + Padding;
                }

                int newBottomY = cursorY + (t.Height);
                if (newBottomY > bottomY)
                {
                    bottomY = newBottomY;
                    while (bottomY > Height) { Height += 64; }
                    if (Height >= Width * 2) // reset everything if Height is double of Width
                    {
                        ResetPack(Width + 128, 128);
                        continue;
                    }
                }

                t.X = cursorX;
                t.Y = cursorY;
                if (TextureAtlas.DebugCheckOverlap)
                    CheckForOverlap(i, t);

                int fillX = t.Width + Padding;
                int fillY = t.Height + Padding;
                cursorX += fillX;

                // After filling our spot, there is a potential free spot.
                // We know this because we fill our objects in size descending order.
                // ____________
                // |  t.Width || (padding)
                // |==========||
                // | freespot ||
                if (t.Width >= MinFreeSpotSize)
                {
                    int freeSpotY = (cursorY + fillY);
                    int freeSpotH = (bottomY - freeSpotY - Padding);
                    if (freeSpotH >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new FreeSpot(t.X, freeSpotY, t.Width, freeSpotH, "belowFill"));
                        if (TextureAtlas.DebugCheckOverlap)
                            CheckFreeSpotOverlap();
                    }
                }
            }
        }

        // @note PERF: This is fast enough
        // @return Number of textures that were packed. Big textures are excluded from packing.
        public int PackTextures(TextureInfo[] textures)
        {
            PrepareToPack(textures);
            PackByLinearWalk(textures);
            
            int packed = FinalizePack();
            return packed;
        }


        bool FillFreeSpot(int tIndex, TextureInfo t)
        {
            for (int i = 0; i < FreeSpots.Count; ++i)
            {
                FreeSpot fs = FreeSpots[i];
                Rectangle r = fs.r;
                int fillX = t.Width + Padding;
                int fillY = t.Height + Padding;
                if (fillX > r.Width || fillY > r.Height)
                    continue;

                t.X = r.X;
                t.Y = r.Y;

                if (TextureAtlas.DebugCheckOverlap)
                    CheckForOverlap(tIndex, t);

                if (TextureAtlas.DebugDrawFreeSpotFills)
                    DebugFreeSpotFills.Add(new Rectangle(t.X, t.Y, t.Width, t.Height));

                FreeSpots.RemoveAt(i);
                int remX = r.Width  - fillX;
                int remY = r.Height - fillY;
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

                if (TextureAtlas.DebugCheckOverlap)
                    CheckFreeSpotOverlap();
                return true; // success! we filled the free spot
            }
            return false;
        }
    }
}
