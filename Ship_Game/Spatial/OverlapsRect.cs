namespace Ship_Game.Spatial;

/// <summary>
/// Utility which quickly figures out which QuadTree sub-node is being overlapped
/// </summary>
public struct OverlapsRect
{
    public readonly byte NW, NE, SE, SW;
    public OverlapsRect(in AABoundingBox2D quad, in AABoundingBox2D rect)
    {
        float midX = (quad.X1 + quad.X2) * 0.5f;
        float midY = (quad.Y1 + quad.Y2) * 0.5f;
        // +---------+   The target rectangle overlaps Left quadrants (NW, SW)
        // | x--|    |
        // |-|--+----|
        // | x--|    |
        // +---------+
        byte overlaps_Left = (rect.X1 < midX)?(byte)1:(byte)0;
        // +---------+   The target rectangle overlaps Right quadrants (NE, SE)
        // |    |--x |
        // |----+--|-|
        // |    |--x |
        // +---------+
        byte overlaps_Right = (rect.X2 >= midX)?(byte)1:(byte)0;
        // +---------+   The target rectangle overlaps Top quadrants (NW, NE)
        // | x--|-x  |
        // |----+----|
        // |    |    |
        // +---------+
        byte overlaps_Top = (rect.Y1 < midY)?(byte)1:(byte)0;
        // +---------+   The target rectangle overlaps Bottom quadrants (SW, SE)
        // |    |    |
        // |----+----|
        // | x--|-x  |
        // +---------+
        byte overlaps_Bottom = (rect.Y2 >= midY)?(byte)1:(byte)0;

        // bitwise combine to get which quadrants we overlap: NW, NE, SE, SW
        NW = (byte)(overlaps_Top & overlaps_Left);
        NE = (byte)(overlaps_Top & overlaps_Right);
        SE = (byte)(overlaps_Bottom & overlaps_Right);
        SW = (byte)(overlaps_Bottom & overlaps_Left);
    }
}