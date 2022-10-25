using Ship_Game;
using Ship_Game.Spatial;
using SDGraphics;
using SDUtils;

namespace UnitTests.Universe;

// Debug & Test visualizer for GenericQtree
internal class GenericQtreeVisualization : CommonVisualization
{
    readonly GenericQtree Tree;
    readonly SpatialObjectBase[] AllObjects;

    float FindOneTime;
    float FindMultiTime;

    public SpatialObjectBase FoundOne = null;
    public Array<SpatialObjectBase> Found = new();
    public VisualizerOptions VisOpt = new();
    
    protected override float FullSize => Tree.FullSize;
    protected override float WorldSize => Tree.WorldSize;

    public GenericQtreeVisualization(SpatialObjectBase[] allObjects, GenericQtree tree)
        : base(tree.FullSize)
    {
        Tree = tree;
        AllObjects = allObjects;
    }

    
    protected override void Search(in AABoundingBox2D searchArea)
    {
        var t1 = new PerfTimer();
        FoundOne = Tree.FindOne(searchArea);
        FindOneTime = t1.Elapsed;

        var t2 = new PerfTimer();
        Found = Tree.Find(searchArea);
        FindMultiTime = t2.Elapsed;
    }

    protected override void UpdateSim(float fixedDeltaTime)
    {
    }

    protected override void DrawTree()
    {
        Tree.DebugVisualize(this, VisOpt);
    }

    protected override void DrawObjects()
    {
        AABoundingBox2D visibleWorldRect = VisibleWorldRect;
        foreach (SpatialObjectBase go in AllObjects)
        {
            // TODO: draw more details?
        }
    }

    protected override void DrawStats()
    {
        var cursor = new Vector2(20, 20);
        DrawText(ref cursor, "Press ESC to quit");
        DrawText(ref cursor, $"Camera: {Camera}");
        DrawText(ref cursor, $"NumObjects: {AllObjects.Length}");
        DrawText(ref cursor, $"FindOne:   {FoundOne?.ToString() ?? "<none>"}");
        DrawText(ref cursor, $"FindMulti: {Found.Count}");
        DrawText(ref cursor, $"SearchArea: {SearchArea.Width}x{SearchArea.Height}");
        DrawText(ref cursor, $"FindOneTime:   {(FindOneTime*1000).String(4)}ms");
        DrawText(ref cursor, $"FindMultiTime: {(FindMultiTime*1000).String(4)}ms");
    }
}