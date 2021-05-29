using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public struct ShipGridInfo
    {
        public Point Size; // slot dimensions of the grid, for example 4x4 for Vulcan Scout
        public Vector2 Origin; // where is the TopLeft of the grid? in the virtual coordinate space
        public Vector2 Span; // actual size of the grid in world coordinate space (64.0 x 64.0 for vulcan scout)
        public int SurfaceArea;
        
        void SetSpanAndSize(Vector2 min, Vector2 max)
        {
            Origin = new Vector2(min.X, min.Y);
            Span = new Vector2(max.X - min.X, max.Y - min.Y);
            Size = new Point((int)Span.X / 16, (int)Span.Y / 16);
        }

        public override string ToString() => $"surface={SurfaceArea} size={Size} origin={Origin} span={Span}";

        public ShipGridInfo(ShipModule[] modules)
        {
            Size = Point.Zero;
            Origin = Vector2.Zero;
            Span = Vector2.Zero;
            SurfaceArea = 0;

            int surface = 0;
            foreach (ShipModule m in modules)
                surface += m.Area;

            var min = new Vector2(+4096, +4096);
            var max = new Vector2(-4096, -4096);
            for (int i = 0; i < modules.Length; ++i)
            {
                ShipModule module = modules[i];
                Vector2 topLeft = module.Position;
                var botRight = new Vector2(topLeft.X + module.XSIZE * 16.0f,
                                           topLeft.Y + module.YSIZE * 16.0f);
                SurfaceArea += (module.XSIZE * module.YSIZE);
                if (topLeft.X  < min.X) min.X = topLeft.X;
                if (topLeft.Y  < min.Y) min.Y = topLeft.Y;
                if (botRight.X > max.X) max.X = botRight.X;
                if (botRight.Y > max.Y) max.Y = botRight.Y;
            }

            SetSpanAndSize(min, max);
        }

        public ShipGridInfo(ModuleSlotData[] templateSlots, bool isHull = false)
        {
            Size = Point.Zero;
            Origin = Vector2.Zero;
            Span = Vector2.Zero;
            SurfaceArea = 0;
            
            var min = new Vector2(+4096, +4096);
            var max = new Vector2(-4096, -4096);

            if (isHull || ShipData.IsAllDummySlots(templateSlots))
            {
                // hulls are simple
                for (int i = 0; i < templateSlots.Length; ++i)
                {
                    ModuleSlotData slot = templateSlots[i];
                    if (slot.ModuleUID != null)
                        throw new Exception($"A ShipHull cannot have ModuleUID! uid={slot.ModuleUID}");

                    var topLeft = slot.Position - new Vector2(ShipModule.ModuleSlotOffset);
                    var botRight = new Vector2(topLeft.X + 16f, topLeft.Y + 16f);
                    if (topLeft.X  < min.X) min.X = topLeft.X;
                    if (topLeft.Y  < min.Y) min.Y = topLeft.Y;
                    if (botRight.X > max.X) max.X = botRight.X;
                    if (botRight.Y > max.Y) max.Y = botRight.Y;
                    ++SurfaceArea;
                }
            }
            else
            {
                // This is the worst case, we need to support any crazy designs out there
                // including designs which don't even match BaseHull and have a mix of Dummy and Placed modules
                // Only way is to create a Map of unique coordinates

                var slotsMap = new Map<Point, ModuleSlotData>();

                // insert dummy modules first
                for (int i = 0; i < templateSlots.Length; ++i)
                {
                    ModuleSlotData designSlot = templateSlots[i];
                    if (designSlot.IsDummy)
                        slotsMap[designSlot.PosAsPoint] = designSlot;
                }

                // now place non-dummy modules as XSIZE*YSIZE grids
                for (int i = 0; i < templateSlots.Length; ++i)
                {
                    ModuleSlotData designSlot = templateSlots[i];
                    if (!designSlot.IsDummy)
                    {
                        Point position = designSlot.PosAsPoint;
                        slotsMap[position] = designSlot;

                        ShipModule m = designSlot.ModuleOrNull;
                        if (m == null)
                            throw new Exception($"Module {designSlot.ModuleUID} does not exist! This design is invalid.");

                        Point size = m.GetOrientedSize(designSlot);
                        for (int x = 0; x < size.X; ++x)
                        for (int y = 0; y < size.Y; ++y)
                        {
                            if (x == 0 && y == 0) continue;

                            var pos = new Point(position.X + x*16, position.Y + y*16);
                            if (!slotsMap.ContainsKey(pos))
                            {
                                slotsMap[pos] = new ModuleSlotData(new Vector2(pos.X, pos.Y), designSlot.Restrictions);
                            }
                        }
                    }
                }

                // Now we should have a list of unique slots, normalized to 1x1
                foreach (ModuleSlotData slot in slotsMap.Values)
                {
                    var topLeft = slot.Position - new Vector2(ShipModule.ModuleSlotOffset);
                    var botRight = new Vector2(topLeft.X + 16f, topLeft.Y + 16f);
                    if (topLeft.X  < min.X) min.X = topLeft.X;
                    if (topLeft.Y  < min.Y) min.Y = topLeft.Y;
                    if (botRight.X > max.X) max.X = botRight.X;
                    if (botRight.Y > max.Y) max.Y = botRight.Y;
                    ++SurfaceArea;
                }
            }

            SetSpanAndSize(min, max);
        }
    }
}