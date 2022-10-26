using SDGraphics;
using SDUtils;
namespace Ship_Game.Spatial;

class DebugQtreeFind
{
    public AABoundingBox2D SearchArea;
    public Vector2 FilterOrigin;
    public float RadialFilter;
    public Array<AABoundingBox2D> FindCells = new();
    public Array<SpatialObjectBase> SearchResults = new();

    public void Draw(GameScreen screen, VisualizerOptions opt)
    {
        if (!SearchArea.IsEmpty)
            screen.DrawRectProjected(SearchArea, Qtree.Yellow);

        if (RadialFilter > 0)
            screen.DrawCircleProjected(FilterOrigin, RadialFilter, Qtree.Yellow);

        foreach (AABoundingBox2D r in FindCells)
            screen.DrawRectProjected(r, Qtree.Blue);

        if (opt.SearchResults)
        {
            foreach (SpatialObjectBase go in SearchResults)
            {
                screen.DrawRectProjected(new AABoundingBox2D(go), Qtree.YellowBright);
            }
        }
    }
}

class QtreeDebug
{
    readonly Map<int, DebugQtreeFind> FindNearbyDbg = new();

    public DebugQtreeFind GetFindDebug(in SearchOptions opt)
    {
        if (opt.DebugId == 0)
            return null;
        var dfn = new DebugQtreeFind
        {
            SearchArea = opt.SearchRect,
            FilterOrigin = opt.FilterOrigin,
            RadialFilter = opt.FilterRadius
        };
        FindNearbyDbg[opt.DebugId] = dfn;
        return dfn;
    }

    public void Draw(GameScreen screen, VisualizerOptions opt)
    {
        foreach (var kv in FindNearbyDbg)
        {
            kv.Value.Draw(screen, opt);
        }
    }
}
