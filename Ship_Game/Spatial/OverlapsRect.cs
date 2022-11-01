namespace Ship_Game.Spatial;

public readonly struct OverlapsRect
{
    /// <summary>
    /// Utility which quickly figures out which QuadTree sub-node is being overlapped
    /// </summary>
    public static (bool NW, bool NE, bool SE, bool SW, int NumOverlaps) GetWithCount(in AABoundingBox2D quad, in AABoundingBox2D rect)
    {
        float midX = (quad.X1 + quad.X2) * 0.5f;
        float midY = (quad.Y1 + quad.Y2) * 0.5f;
        // +---------+   The target rectangle overlaps Left quadrants (NW, SW)
        // | x--|    |
        // |-|--+----|
        // | x--|    |
        // +---------+
        bool overlaps_Left = (rect.X1 < midX);
        // +---------+   The target rectangle overlaps Right quadrants (NE, SE)
        // |    |--x |
        // |----+--|-|
        // |    |--x |
        // +---------+
        bool overlaps_Right = (rect.X2 >= midX);
        // +---------+   The target rectangle overlaps Top quadrants (NW, NE)
        // | x--|-x  |
        // |----+----|
        // |    |    |
        // +---------+
        bool overlaps_Top = (rect.Y1 < midY);
        // +---------+   The target rectangle overlaps Bottom quadrants (SW, SE)
        // |    |    |
        // |----+----|
        // | x--|-x  |
        // +---------+
        bool overlaps_Bottom = (rect.Y2 >= midY);

        // combine to get which quadrants we overlap: NW, NE, SE, SW
        bool NW = overlaps_Top && overlaps_Left;
        bool NE = overlaps_Top && overlaps_Right;
        bool SE = overlaps_Bottom && overlaps_Right;
        bool SW = overlaps_Bottom && overlaps_Left;

        int numOverlaps = NW ? 1 : 0;
        numOverlaps += NE ? 1 : 0;
        numOverlaps += SE ? 1 : 0;
        numOverlaps += SW ? 1 : 0;
        return (NW, NE, SE, SW, numOverlaps);
    }
}
