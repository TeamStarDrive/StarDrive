using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Universe;

namespace UnitTests.Universe;

[TestClass]
public class BorderFieldTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void CachedQueriesMatchDenseClusterReference()
    {
        var nodes = new BorderField.Node[32];
        var bridges = new BorderField.Bridge[nodes.Length - 1];
        for (int i = 0; i < nodes.Length; ++i)
        {
            nodes[i] = new(new Vector2(i % 8 * 70000, i / 8 * 70000), 80000, i * 0.73f, 0.5f);
            if (i > 0) bridges[i - 1] = new(nodes[i - 1], nodes[i]);
        }
        var watch = Stopwatch.StartNew();
        var field = new BorderField(nodes, bridges);
        double buildMs = watch.Elapsed.TotalMilliseconds;
        var points = new Vector2[20000];
        var expected = new float[points.Length];
        for (int i = 0; i < points.Length; ++i)
            points[i] = new Vector2((i * 7919 % 750000) - 100000, (i * 3571 % 450000) - 100000);
        watch.Restart();
        for (int i = 0; i < points.Length; ++i)
        {
            float sum = 0;
            foreach (BorderField.Node node in nodes) Empire.AccumulateGaussianInfluence(ref sum, node.Influence(points[i]));
            foreach (BorderField.Bridge bridge in bridges) Empire.AccumulateGaussianInfluence(ref sum, bridge.Influence(points[i]));
            expected[i] = Empire.CombineGaussianInfluence(sum) - Empire.GaussianBorderThreshold;
        }
        double referenceMs = watch.Elapsed.TotalMilliseconds;
        var actual = new float[points.Length];
        watch.Restart();
        for (int i = 0; i < points.Length; ++i) actual[i] = field.Strength(points[i], smooth: false);
        double cachedMs = watch.Elapsed.TotalMilliseconds;
        for (int i = 0; i < points.Length; ++i) Assert.AreEqual(expected[i], actual[i], 0.003f);
        TestContext.WriteLine($"32 nodes / 31 bridges / 20,000 queries: build {buildMs:F2} ms, analytic {referenceMs:F2} ms, cached {cachedMs:F2} ms");
    }

    [TestMethod]
    public void SmallHoleIsFilledWithoutChangingExistingClaims()
    {
        float[] raw = Square(11, 2, 8);
        raw[5 + 5 * 11] = -0.2f;
        float[] smooth = BorderField.CloseCavities(raw, 11, 11, 0, 4);
        Assert.IsTrue(smooth[5 + 5 * 11] > 0f);
        for (int i = 0; i < raw.Length; ++i)
            if (raw[i] >= 0f) Assert.AreEqual(raw[i], smooth[i]);
        Assert.AreEqual(-0.2f, raw[5 + 5 * 11], "Input must remain immutable");
        Assert.IsTrue(smooth[0] < 0f);
    }

    [TestMethod]
    public void LargeHoleAndDiagonalExteriorPassageArePreserved()
    {
        float[] raw = Square(15, 2, 12);
        for (int y = 5; y <= 9; ++y)
            for (int x = 5; x <= 9; ++x) raw[x + y * 15] = -0.2f;
        Assert.IsTrue(BorderField.CloseCavities(raw, 15, 15, 0, 4)[7 + 7 * 15] < 0f);
        raw = Square(11, 1, 9);
        for (int i = 0; i <= 5; ++i) raw[i + i * 11] = -0.2f;
        Assert.IsTrue(BorderField.CloseCavities(raw, 11, 11, 0, 64)[5 + 5 * 11] < 0f);
    }

    [TestMethod]
    public void ClosingRoundsNarrowNotchWithoutBridgingDistantIslands()
    {
        float[] raw = Square(15, 2, 12);
        for (int y = 2; y <= 6; ++y) raw[7 + y * 15] = -0.2f;
        float[] smooth = BorderField.CloseCavities(raw, 15, 15, 2, 0);
        Assert.IsTrue(smooth[7 + 6 * 15] >= 0f);
        Assert.IsTrue(smooth[7] < 0f);
        for (int y = 0; y < 15; ++y)
            for (int x = 5; x <= 9; ++x) raw[x + y * 15] = -0.2f;
        smooth = BorderField.CloseCavities(raw, 15, 15, 2, 0);
        Assert.IsTrue(smooth[7 + 7 * 15] < 0f);
    }

    [TestMethod]
    public void CachedFieldMatchesAnalyticNodeWithinSamplingTolerance()
    {
        var node = new BorderField.Node(new Vector2(12000, -34000), 100000f, 0.73f, 0.5f);
        var field = new BorderField(new[] { node }, System.Array.Empty<BorderField.Bridge>());
        for (int i = 0; i < 200; ++i)
        {
            float angle = i * 0.31f;
            Vector2 point = node.Position + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (i * 1200f);
            Assert.AreEqual(node.Influence(point) - Empire.GaussianBorderThreshold,
                field.Strength(point, smooth: false), 0.002f);
        }
        Assert.IsTrue(field.Strength(new Vector2(1e8f)) < 0f);
    }

    [TestMethod]
    public void GaussianRingClosesSmallCentralCavity()
    {
        const float radius = 100000f;
        var nodes = new BorderField.Node[4];
        for (int i = 0; i < 4; ++i)
        {
            float angle = i * (float)Math.PI / 2f;
            nodes[i] = new(new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (radius * 1.305f), radius, 0f, 1f);
        }
        var field = new BorderField(nodes, System.Array.Empty<BorderField.Bridge>());
        Assert.IsTrue(field.Strength(Vector2.Zero, smooth: false) < 0f);
        Assert.IsTrue(field.Strength(Vector2.Zero) >= 0f);
    }

    [TestMethod]
    public void SaddleConnectivityFollowsFieldDecision()
    {
        Vector2 top = new(1, 0), right = new(2, 1), bottom = new(1, 2), left = new(0, 1);
        foreach (int mask in new[] { 5, 10 })
        {
            var segments = new SDUtils.Array<Vector2>();
            BorderNodeCache.AddMarchingSegments(mask, top, right, bottom, left, segments, true);
            Assert.AreEqual(left, segments[0]); Assert.AreEqual(bottom, segments[1]);
            segments.Clear();
            BorderNodeCache.AddMarchingSegments(mask, top, right, bottom, left, segments, false);
            Assert.AreEqual(top, segments[0]); Assert.AreEqual(left, segments[1]);
        }
    }

    [TestMethod]
    public void ContourStitchingHandlesUnorderedAndReversedEdges()
    {
        var raw = new SDUtils.Array<Vector2>();
        foreach (Vector2 p in new[] { new Vector2(0, 0), new Vector2(10, 0),
            new Vector2(0, 10), new Vector2(10, 10), new Vector2(0, 0), new Vector2(0, 10),
            new Vector2(10, 10), new Vector2(10, 0) }) raw.Add(p);
        Vector2[] result = BorderNodeCache.SmoothContourSegments(raw, 0.01f);
        Assert.AreEqual(32, result.Length); // four edges, two Chaikin passes
        for (int i = 0; i < result.Length; i += 2)
            Assert.AreEqual(result[i + 1], result[(i + 2) % result.Length]);
    }

    [TestMethod]
    public void WorkerBudgetRejectsExtraWorkWithoutWaitingAndRecoversAfterFault()
    {
        using var release = new ManualResetEventSlim(false);
        var tasks = new List<Task<int>>();
        try
        {
            Task<int> task;
            while ((task = BorderWorker.TryRun(() => { release.Wait(); return 1; })) != null)
                tasks.Add(task);
            Assert.IsTrue(tasks.Count <= 2);
            Assert.IsNull(BorderWorker.TryRun(() => 2));
        }
        finally
        {
            release.Set();
            Task.WaitAll(tasks.ToArray());
        }
        Task<int> failure = null;
        Assert.IsTrue(SpinWait.SpinUntil(() => (failure = BorderWorker.TryRun<int>(() => throw new InvalidOperationException("test"))) != null, 5000));
        Assert.ThrowsExactly<AggregateException>(() => failure.Wait());
        Task<int> next = null;
        Assert.IsTrue(SpinWait.SpinUntil(() => (next = BorderWorker.TryRun(() => 42)) != null, 5000));
        Assert.AreEqual(42, next.Result);
    }

    static float[] Square(int size, int first, int last)
    {
        var field = new float[size * size];
        for (int y = 0; y < size; ++y)
            for (int x = 0; x < size; ++x)
                field[x + y * size] = x >= first && x <= last && y >= first && y <= last ? 0.3f : -0.2f;
        return field;
    }
}

[TestClass]
public class BorderSceneTests : StarDriveTest
{
    public BorderSceneTests()
    {
        CreateUniverseAndPlayerEmpire();
        Player.GetRelations(Enemy).AtWar = false;
        Enemy.GetRelations(Player).AtWar = false;
    }

    void SetBorder(Empire empire, Vector2 position, float radius)
    {
        Planet planet = AddDummyPlanetToEmpire(position, empire);
        var node = new Empire.InfluenceNode(planet.System, radius, true);
        empire.BorderNodes = new[] { node };
        var sample = new BorderField.Node(position, radius, planet.System.Id * 0.754877666f + empire.Id * 1.618033989f, 1f);
        empire.PreparedBorders = new BorderSnapshot(empire.BorderNodes, new[] { sample }, empire.GetProjectorRadius());
    }

    [TestMethod]
    public void RenderAndMovementAgreeAcrossPeacefulFrontier()
    {
        SetBorder(Player, new Vector2(-60000, 0), 100000);
        SetBorder(Enemy, new Vector2(60000, 0), 100000);
        var scene = new BorderScene(new[] { Player, Enemy });
        for (int y = -130000; y <= 130000; y += 11000)
            for (int x = -170000; x <= 170000; x += 11000)
            {
                Vector2 point = new(x, y);
                Assert.AreEqual(Player.IsInBorderTerritory(point), scene.Territory(0, point, out _) >= 0f);
                Assert.AreEqual(Enemy.IsInBorderTerritory(point), scene.Territory(1, point, out _) >= 0f);
            }
    }

    [TestMethod]
    public void RivalEnclaveSurvivesAndDiplomacyInvalidatesScene()
    {
        SetBorder(Player, new Vector2(1000, 1000), 160000);
        SetBorder(Enemy, new Vector2(61000, 1000), 45000);
        var empires = new[] { Player, Enemy };
        var scene = new BorderScene(empires);
        Vector2 enclave = new(61000, 1000);
        Assert.IsTrue(scene.Territory(0, enclave, out _) < 0f);
        Assert.IsTrue(scene.Territory(1, enclave, out _) >= 0f);
        Assert.AreSame(scene, BorderScene.Capture(scene, empires));
        Player.GetRelations(Enemy).AtWar = true;
        Enemy.GetRelations(Player).AtWar = true;
        BorderScene war = BorderScene.Capture(scene, empires);
        Assert.AreNotSame(scene, war);
        Assert.IsTrue(war.Territory(0, enclave, out _) >= 0f);
        Assert.IsTrue(war.Territory(1, enclave, out _) >= 0f);
        // The old worker input remains a peace snapshot.
        Assert.IsTrue(scene.Territory(0, enclave, out _) < 0f);
    }

    [TestMethod]
    public void CompletedVisualsArePublishedWithoutReadingLiveNodes()
    {
        SetBorder(Player, new Vector2(1000, 1000), 100000);
        SetBorder(Enemy, new Vector2(500000, 0), 100000);
        var scene = new BorderScene(new[] { Player, Enemy });
        var cache = new BorderNodeCache();
        cache.SetScene(scene, 0);
        Player.BorderNodes = System.Array.Empty<Empire.InfluenceNode>();
        Assert.IsTrue(SpinWait.SpinUntil(() => { cache.Update(Player); return cache.OutlineSegments.Length > 0; }, 10000));
        Assert.AreEqual(1, cache.BorderNodes.Length);
        Assert.IsTrue(cache.FillBounds.W > 0);
    }

    [TestMethod]
    public void SupersededVisualBuildCannotPublishOldTerritory()
    {
        SetBorder(Player, new Vector2(1000, 1000), 100000);
        var cache = new BorderNodeCache();
        cache.SetScene(new BorderScene(new[] { Player }), 0);
        cache.Update(Player);
        SetBorder(Player, new Vector2(800000, 1000), 100000);
        BorderSnapshot replacement = Player.PreparedBorders;
        cache.SetScene(new BorderScene(new[] { Player }), 0);
        Assert.IsTrue(SpinWait.SpinUntil(() =>
        {
            cache.Update(Player);
            if (cache.BorderNodes.Length == 0) return false;
            Assert.AreSame(replacement.Nodes, cache.BorderNodes);
            return cache.OutlineSegments.Length > 0;
        }, 10000));
    }

    [TestMethod]
    public void SameCountColonyReplacementRejectsOldSimulationBuild()
    {
        AddDummyPlanetToEmpire(new Vector2(100000, 1000), Enemy);
        Enemy.UpdateContactsAndBorders(Universe, FixedSimTime.Zero);
        Enemy.ClearAllPlanets();
        Planet replacement = AddDummyPlanetToEmpire(new Vector2(800000, 1000), Enemy);
        Assert.IsTrue(SpinWait.SpinUntil(() =>
        {
            Enemy.UpdateContactsAndBorders(Universe, FixedSimTime.Zero);
            BorderSnapshot snapshot = Enemy.PreparedBorders;
            if (snapshot == null) return false;
            Assert.AreEqual(1, snapshot.Nodes.Length);
            Assert.AreSame(replacement.System, snapshot.Nodes[0].Source);
            return true;
        }, 10000));
        Enemy.ClearAllPlanets();
        Enemy.UpdateContactsAndBorders(Universe, FixedSimTime.Zero);
        Assert.AreEqual(0, Enemy.BorderConnections.Length);
        Assert.IsFalse(Enemy.IsInBorderTerritory(replacement.System.Position));
    }

    [TestMethod]
    public void AddingColonyKeepsCompletedBorderUntilReplacementIsReady()
    {
        SetBorder(Player, new Vector2(100000, 1000), 100000);
        BorderSnapshot previous = Player.PreparedBorders;
        AddDummyPlanetToEmpire(new Vector2(400000, 1000), Player);
        Player.UpdateContactsAndBorders(Universe, FixedSimTime.Zero);
        Assert.AreSame(previous, Player.PreparedBorders);
        Assert.IsTrue(SpinWait.SpinUntil(() =>
        {
            Player.UpdateContactsAndBorders(Universe, FixedSimTime.Zero);
            return Player.PreparedBorders?.Nodes.Length == 2;
        }, 10000));
    }
}
