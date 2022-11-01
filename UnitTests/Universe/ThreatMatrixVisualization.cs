using Ship_Game;
using Ship_Game.Spatial;
using SDGraphics;
using SDUtils;

namespace UnitTests.Universe;

// Debug & Test visualizer for GenericQtree
internal class ThreatMatrixVisualization : CommonVisualization
{
    readonly GenericQtree Tree;

    float FindOneTime;
    float FindMultiTime;
    float FindLinearTime;
    SpatialObjectBase FoundOne;
    SpatialObjectBase[] FoundLinear = Empty<SpatialObjectBase>.Array;

    // run more iterations to get some actual stats
    int Iterations = 1000;
    
    protected override float FullSize => Tree.FullSize;
    protected override float WorldSize => Tree.WorldSize;

    public ThreatMatrixVisualization(Empire owner)
        : base(owner.Threats.ClustersMap.FullSize)
    {
        Tree = owner.Threats.ClustersMap;
        AllObjects = owner.Threats.OurClusters.Concat(owner.Threats.RivalClusters);
    }
    
    protected override void Search(in AABoundingBox2D searchArea)
    {
        var opt = new SearchOptions(SearchArea) { MaxResults = 1000, DebugId = 1, };

        var t1 = new PerfTimer();
        for (int i = 0; i < Iterations; ++i)
            FoundOne = Tree.FindOne(opt);
        FindOneTime = t1.Elapsed;

        var t2 = new PerfTimer();
        for (int i = 0; i < Iterations; ++i)
            Found = Tree.Find(opt);
        FindMultiTime = t2.Elapsed;

        var t3 = new PerfTimer();
        for (int i = 0; i < Iterations; ++i)
            FoundLinear = Tree.FindLinear(opt, AllObjects);
        FindLinearTime = t3.Elapsed;
    }

    protected override void UpdateSim(float fixedDeltaTime)
    {
    }

    protected override void DrawTree()
    {
        Tree.DebugVisualize(this, VisOpt);
    }

    protected override void DrawStats()
    {
        var cursor = new Vector2(20, 20);
        DrawText(ref cursor, "Press ESC to quit");
        DrawText(ref cursor, $"Camera: {Camera}");
        DrawText(ref cursor, $"NumObjects: {AllObjects.Length}");
        DrawText(ref cursor, $"SearchArea: {SearchArea.Width}x{SearchArea.Height}");
        DrawText(ref cursor, $"FindOneTime {Iterations}x:  {(FindOneTime*1000).String(4)}ms");
        DrawText(ref cursor, $"FindLinearTime {Iterations}x: {(FindLinearTime*1000).String(4)}ms");
        DrawText(ref cursor, $"FindMultiTime {Iterations}x: {(FindMultiTime*1000).String(4)}ms");
        DrawText(ref cursor, $"FindOne:  {FoundOne?.ToString() ?? "<none>"}");
        DrawText(ref cursor, $"FindLinear: {FoundLinear.Length}");
        DrawText(ref cursor, $"FindMulti: {Found.Length}");
        for (int i = 0; i < Found.Length && i < 10; ++i)
        {
            DrawText(ref cursor, $"  + {Found[i]}");
        }
    }
}
