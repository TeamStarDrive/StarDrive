using System;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

namespace Ship_Game.Ships.Legacy
{
    public struct LegacyShipGridInfo
    {
        public Point Size; // slot dimensions of the grid, for example 4x4 for Vulcan Scout
        public Vector2 VirtualOrigin; // where is the TopLeft of the grid? in the virtual coordinate space
        public Vector2 Span; // actual size of the grid in world coordinate space (64.0 x 64.0 for vulcan scout)
        public int SurfaceArea;
        public Vector2 MeshOffset; // offset of the mesh from Mesh object center, for grid to match model
        
        public Point GridCenter; // offset from grid TopLeft to the Center slot

        public override string ToString() => $"surface={SurfaceArea} size={Size} Vorigin={VirtualOrigin} span={Span}";
        
        public const float ModuleSlotOffset = 264f;

        public LegacyShipGridInfo(string name, LegacyModuleSlotData[] templateSlots, bool isHull, LegacyShipData baseHull)
        {
            SurfaceArea = 0;
            var min = new Vector2(+4096, +4096);
            var max = new Vector2(-4096, -4096);

            isHull = isHull || LegacyShipData.IsAllDummySlots(templateSlots);

            if (isHull)
            {
                // hulls are simple
                for (int i = 0; i < templateSlots.Length; ++i)
                {
                    LegacyModuleSlotData slot = templateSlots[i];
                    if (slot.ModuleUID != null)
                        throw new Exception($"A ShipHull cannot have ModuleUID! uid={slot.ModuleUID}");

                    var topLeft = slot.Position - new Vector2(ModuleSlotOffset);
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

                var slotsMap = new Map<Point, LegacyModuleSlotData>();

                // insert BaseHull slots, this is required for some broken designs
                // where BaseHull has added Slots to the Top, but design has not been updated
                // leading to a mismatched ModuleGrid
                for (int i = 0; i < baseHull.ModuleSlots.Length; ++i)
                {
                    LegacyModuleSlotData designSlot = baseHull.ModuleSlots[i];
                    slotsMap[designSlot.PosAsPoint] = designSlot;
                }

                // insert dummy modules first
                for (int i = 0; i < templateSlots.Length; ++i)
                {
                    LegacyModuleSlotData designSlot = templateSlots[i];
                    if (designSlot.IsDummy)
                    {
                        slotsMap[designSlot.PosAsPoint] = designSlot;
                    }
                }

                // now place non-dummy modules as XSIZE*YSIZE grids
                for (int i = 0; i < templateSlots.Length; ++i)
                {
                    LegacyModuleSlotData designSlot = templateSlots[i];
                    if (!designSlot.IsDummy)
                    {
                        Point position = designSlot.PosAsPoint;
                        slotsMap[position] = designSlot;

                        ShipModule m = designSlot.ModuleOrNull;
                        if (m == null)
                            throw new Exception($"Module {designSlot.ModuleUID} does not exist! This design is invalid.");

                        Point size = m.GetOrientedSize(designSlot.Orientation);
                        for (int x = 0; x < size.X; ++x)
                        for (int y = 0; y < size.Y; ++y)
                        {
                            if (x == 0 && y == 0) continue;

                            var pos = new Point(position.X + x*16, position.Y + y*16);
                            if (!slotsMap.ContainsKey(pos))
                            {
                                slotsMap[pos] = new LegacyModuleSlotData(new Vector2(pos.X, pos.Y), designSlot.Restrictions);
                            }
                        }
                    }
                }

                // Now we should have a list of unique slots, normalized to 1x1
                foreach (LegacyModuleSlotData slot in slotsMap.Values)
                {
                    var topLeft = slot.Position - new Vector2(ModuleSlotOffset);
                    var botRight = new Vector2(topLeft.X + 16f, topLeft.Y + 16f);
                    if (topLeft.X  < min.X) min.X = topLeft.X;
                    if (topLeft.Y  < min.Y) min.Y = topLeft.Y;
                    if (botRight.X > max.X) max.X = botRight.X;
                    if (botRight.Y > max.Y) max.Y = botRight.Y;
                    ++SurfaceArea;
                }
            }

            VirtualOrigin = new Vector2(min.X, min.Y);
            Span = new Vector2(max.X - min.X, max.Y - min.Y);
            Size = new Point((int)Span.X / 16, (int)Span.Y / 16);

            Vector2 offset = -(VirtualOrigin + Span*0.5f);
            MeshOffset = offset;

            // Center of the design
            GridCenter = new Point((int)(-VirtualOrigin.X / 16f),
                                   (int)(-VirtualOrigin.Y / 16f));

            // Make sure it doesn't go out of bounds
            if (GridCenter.Y > Size.Y-1)
                GridCenter.Y = Size.Y-1;
        }
    }
}