using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            public Rectangle R;
            public string From;
            public FreeSpot(int x, int y, int w, int h, string from)
            {
                R = new Rectangle(x,y,w,h);
                From = from;
            }
            public int Bottom => R.Bottom;
            public int Area => R.Width * R.Height;
        }

        readonly Array<FreeSpot> FreeSpots = new Array<FreeSpot>();
        readonly Array<Rectangle> FreeSpotFills = new Array<Rectangle>();
        readonly Array<Rectangle> OverlapErrors = new Array<Rectangle>();
        readonly Array<Rectangle> FSOverlapErrors = new Array<Rectangle>();

        TextureInfo[] Textures;
        readonly string CachePath;
        int Iteration = 0;
        bool AllSameSize = false; // special case

        public TexturePacker(string cachePath)
        {
            CachePath = cachePath;
        }

        // @return True if size is close to target size
        static bool IsNearToPadSize(int size, int target)
        {
            return (target - Padding) <= size && size <= target;
        }

        static int TextureSorter(TextureInfo a, TextureInfo b)
        {
            // We want to have Super wide textures at the top:
            // ---------------------
            // Then vertical |
            // And finally square textures [ ]
            int aMaxEdge = Math.Max(a.Width, a.Height);
            int bMaxEdge = Math.Max(b.Width, b.Height);
            int diff = bMaxEdge - aMaxEdge;
            if (diff != 0)
                return diff;

            int aMinEdge = Math.Min(a.Width, a.Height);
            int bMinEdge = Math.Min(b.Width, b.Height);
            int diff2 = bMinEdge - aMinEdge;
            if (diff2 != 0)
                return diff2;
            
            // if w+h is equal, use name, so identical atlas anims are sequential
            return string.CompareOrdinal(a.Name, b.Name);
        }

        void PrepareToPack(TextureInfo[] textures)
        {
            Array.Sort(textures, TextureSorter);
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
            
            Textures = textures.Filter(t => t.NoPack == false);
            if (Textures.Length > 0)
            {
                AllSameSize = true;
                int w0 = Textures[0].Width;
                int h0 = Textures[0].Height;
                for (int i = 1; i < Textures.Length; ++i)
                {
                    if (Textures[i].Width != w0 || Textures[i].Height != h0)
                    {
                        AllSameSize = false;
                        break;
                    }
                }
            }

            // always set the Width/Height to minimum
            // to achieve maximum packing
            // pre-calculating the size is detrimental to efficient packing
            Width = 128;
            Height = 128;
        }

        // @note PERF: This is fast enough
        // @return Number of textures that were packed. Big textures are excluded from packing.
        public int PackTextures(TextureInfo[] textures)
        {
            PrepareToPack(textures);

            if (Textures.Length > 0)
            {
                if (AllSameSize)
                {
                    PackAllSameSize(Textures);
                }
                else
                {
                    PackByLinearWalk(Textures);
                }
            }

            int packed = FinalizePack();
            return packed;
        }

        int FinalizePack()
        {
            int packed = Textures.Length;
            if (packed == 0) Width = Height = 0;
            else CheckInRange();
            Textures = null;
            return packed;
        }

        void CheckInRange()
        {
            foreach (TextureInfo t in Textures)
            {
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
                    OverlapErrors.Add(intersection);
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
                var ra = FreeSpots[i].R;
                for (int j = i + 1; j < FreeSpots.Count; ++j)
                {
                    var rb = FreeSpots[j].R;
                    if (ra.GetIntersectingRect(rb, out Rectangle intersection))
                    {
                        FSOverlapErrors.Add(intersection);
                        Log.Warning(ConsoleColor.Red, $"FreeSpot {ra} overlaps with FreeSpot: {rb}  intersection: {intersection}");
                    }
                }
            }
        }

        public void DrawDebug(Color[] atlas, int width, int height)
        {
            if (TextureAtlas.DebugDrawFreeSpots)
            {
                foreach (FreeSpot fs in FreeSpots) // DEBUG only!
                {
                    ImageUtils.DrawRectangle(atlas, Width, Height, fs.R, Color.AliceBlue);
                }
            }

            if (TextureAtlas.DebugDrawFreeSpotFills)
            {
                foreach (Rectangle r in FreeSpotFills)
                    ImageUtils.DrawRectangle(atlas, Width, Height, r, Color.BlueViolet);
            }

            if (TextureAtlas.DebugCheckOverlap)
            {
                foreach (Rectangle r in OverlapErrors)
                    ImageUtils.DrawRectangle(atlas, Width, Height, r, Color.Red);
                foreach (Rectangle r in FSOverlapErrors)
                    ImageUtils.DrawRectangle(atlas, Width, Height, r, Color.Magenta);
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
                ImageUtils.DrawRectangle(data, Width, Height, fs.R, Color.AliceBlue);

            foreach (FreeSpot f in FreeSpots)
                ImageUtils.DrawRectangle(data, Width, Height, f.R, Color.AliceBlue);

            ImageUtils.ConvertToDDS($"{CachePath}.{Iteration}.dds", Width, Height, data, DDSFlags.Dxt5BGRA); // save compressed!
        }

        void ResetState(TextureInfo[] textures, int width, int height)
        {
            if (TextureAtlas.DebugPackerExpansion)
                SaveFreeSpotsDebug(textures);

            for (int i = 0; i < textures.Length; ++i)
                textures[i].X = textures[i].Y = -1;

            Width  = width;
            Height = height;
            FreeSpots.Clear();
            FreeSpotFills.Clear();
            OverlapErrors.Clear();
            FSOverlapErrors.Clear();
            ++Iteration;
        }
        
        void PackAllSameSize(TextureInfo[] textures)
        {
            int fillX = (textures[0].Width  + Padding);
            int fillY = (textures[0].Height + Padding);
            float ratio = (float)fillY / fillX;
            float sqrt = (float)Math.Sqrt(textures.Length);
            int cols = (int)Math.Ceiling(sqrt * ratio);
            int cursorX;
            int cursorY;
            int i;
            void ResetPack()
            {
                ResetState(textures, (fillX * cols).RoundUpToMultipleOf(16), fillY);
                cursorX = cursorY = 0;
                i = -1;
            }

            ResetPack();
            for (i = 0; i < textures.Length; ++i)
            {
                if ((cursorX+fillX) > Width)
                {
                    cursorX = 0;
                    cursorY += fillY;
                    Height += fillY;
                }
                if (Height >= Width * 2)
                {
                    cols += 1;
                    ResetPack();
                    continue;
                }
                TextureInfo t = textures[i];
                t.X = cursorX;
                t.Y = cursorY;
                cursorX += fillX;
            }
            Width  = Width.RoundUpToMultipleOf(16);
            Height = Height.RoundUpToMultipleOf(16);
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
                if (FillFreeSpot(i, t))
                    continue;

                if (t.Width > Width)
                {
                    while (t.Width > Width) Width += 64;
                    ResetPack(Width, 128);
                    continue;
                }

                // Remaining X is not enough, so we create a new free spot on the right side
                // ___________________
                // | prev | freespot |
                // | prev | freespot |
                // | prev | freespot |
                int remainingX = Width - cursorX;
                if (remainingX < t.Width)
                {
                    int remainingY = (bottomY - cursorY);
                    if (remainingX >= MinFreeSpotSize && remainingY >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new FreeSpot(cursorX, cursorY, remainingX, remainingY, "endOfX"));
                    }
                    cursorX = 0;
                    cursorY = bottomY + Padding; // next "line"
                }

                int newBottomY = cursorY + t.Height;
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
                // |  t.Width ||
                // |==========||
                // | freespot ||
                if (t.Width >= MinFreeSpotSize)
                {
                    int freeSpotY = (cursorY + fillY);
                    int freeSpotH = (bottomY - freeSpotY) + Padding;
                    if (freeSpotH >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new FreeSpot(t.X, freeSpotY, t.Width, freeSpotH, "belowFill"));
                    }
                }

                // With new sorting rules, this new fill may be bigger than previous,
                // thus leaving a free spot (because cursorY moved)
                // ___________________
                // |  prev    | this |
                // |----------| this |
                // | freespot | this |
                //if (t.X != 0 && i > 0)
                //{
                //    TextureInfo prev = textures[i - 1];
                //    int freeSpotY = prev.Bottom + Padding;
                //    int freeSpotH = (t.Bottom - freeSpotY) + Padding;
                //    int freeSpotW = t.X - prev.X;
                //    if (freeSpotH >= MinFreeSpotSize && freeSpotW >= MinFreeSpotSize)
                //    {
                //        FreeSpots.Add(new FreeSpot(prev.X, freeSpotY, freeSpotW, freeSpotH, "prevFill"));
                //    }
                //}
                
                if (TextureAtlas.DebugCheckOverlap)
                    CheckFreeSpotOverlap();
            }
        }

        bool FillFreeSpot(int tIndex, TextureInfo t)
        {
            SortFreeSpots();
            MergeAdjacentFreeSpots();

            for (int i = 0; i < FreeSpots.Count; ++i)
            {
                FreeSpot fs = FreeSpots[i];
                Rectangle r = fs.R;
                int fillX = t.Width + Padding;
                int fillY = t.Height + Padding;
                if (fillX > r.Width || fillY > r.Height)
                    continue;

                t.X = r.X;
                t.Y = r.Y;

                if (TextureAtlas.DebugCheckOverlap)
                    CheckForOverlap(tIndex, t);

                if (TextureAtlas.DebugDrawFreeSpotFills)
                    FreeSpotFills.Add(new Rectangle(t.X, t.Y, t.Width, t.Height));

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

        void SortFreeSpots()
        {
            // sort free spots, Ascending, so that we always use the smallest possible free spot
            FreeSpots.Sort((a, b) =>
            {
                int diff = a.Area - b.Area;
                if (diff == 0) // prefer wide spots first
                    return a.R.Width - b.R.Width;
                return diff;
            });
        }

        void MergeAdjacentFreeSpots()
        {
            for (int i = 0; i < FreeSpots.Count; ++i)
            {
                Rectangle a = FreeSpots[i].R;
                for (int j = i + 1; j < FreeSpots.Count; ++j)
                {
                    Rectangle b = FreeSpots[j].R;

                    // merge horizontally aligned adjacent rectangles:
                    // [aaa][bbb] or [bbb][aaa]
                    if (a.Y == b.Y)
                    {
                        if ((a.X < b.X && (b.X - a.Right) < MinFreeSpotSize) ||
                            (b.X < a.X && (a.X - b.Right) < MinFreeSpotSize))
                        {
                            int x = Math.Min(a.X, b.X);
                            int y = a.Y;
                            int w = Math.Max(a.Right, b.Right) - x;
                            int h = Math.Max(a.Bottom, b.Bottom) - y;
                            FreeSpots[i] = new FreeSpot(x, y, w, h, "merge");
                            FreeSpots.RemoveAtSwapLast(j--);
                            continue;
                        }
                    }
                }
            }
        }
    }
}
