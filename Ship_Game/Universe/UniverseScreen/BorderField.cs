using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SDGraphics;
using SDUtils;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTests")]

namespace Ship_Game.Universe;

// Shared budget for simulation geometry and visual builds. No queued jobs and no
// waits on the simulation/render threads; callers retry with their newest input.
internal static class BorderWorker
{
    static readonly SemaphoreSlim Slots = new(Math.Max(1, Math.Min(2, Environment.ProcessorCount - 2)));

    public static Task<T> TryRun<T>(Func<T> build)
    {
        if (!Slots.Wait(0)) return null;
        return Task.Run(() =>
        {
            try { return build(); }
            finally { Slots.Release(); }
        });
    }
}

// CPU-only, immutable once published. Movement and rendering sample exactly the
// same field. All population-dependent widths are captured before starting work.
internal sealed class BorderField
{
    public readonly Vector2 Origin;
    public readonly float Cell;
    public readonly int Columns, Rows;
    readonly float[] Raw;
    readonly float[] Smoothed;
    public RectF Bounds => new(Origin.X, Origin.Y, (Columns - 1) * Cell, (Rows - 1) * Cell);

    public readonly struct Node
    {
        public readonly Vector2 Position;
        public readonly float Radius, Phase, Growth;
        public Node(Vector2 position, float radius, float phase, float growth)
        { Position = position; Radius = radius; Phase = phase; Growth = growth; }

        public float Influence(Vector2 point)
        {
            Vector2 offset = point - Position;
            float cutoff = Radius * (1f + Empire.BorderShapeMaxVariation) * 3f;
            float distance2 = offset.SqLen();
            if (distance2 >= cutoff * cutoff) return 0f;
            float angle = (float)Math.Atan2(offset.Y, offset.X);
            float radius = Math.Max(1f, Radius * (1f
                + 0.032f * (float)Math.Sin(angle * 2f + Phase)
                + 0.017f * (float)Math.Sin(angle * 3f - Phase * 0.73f)
                + 0.006f * (float)Math.Sin(angle * 5f + Phase * 1.37f)));
            float normalized2 = distance2 / (radius * radius);
            return normalized2 >= 9f ? 0f : (float)Math.Exp(-0.5f * normalized2);
        }
    }

    public readonly struct Bridge
    {
        readonly Vector2 A, AB;
        readonly float InverseLength2, Radius1, Radius2;
        public readonly Vector2 Min, Max;
        public Bridge(Node a, Node b)
        {
            A = a.Position;
            AB = b.Position - A;
            InverseLength2 = AB.SqLen() < 1f ? 0f : 1f / AB.SqLen();
            float width = 1.02f + Math.Min(a.Growth, b.Growth) * 0.03f;
            Radius1 = a.Radius * width;
            Radius2 = b.Radius * width;
            float support = Math.Max(Radius1, Radius2) * 1.12f * 3f;
            Min = new(Math.Min(A.X, b.Position.X) - support, Math.Min(A.Y, b.Position.Y) - support);
            Max = new(Math.Max(A.X, b.Position.X) + support, Math.Max(A.Y, b.Position.Y) + support);
        }
        public float Influence(Vector2 point)
        {
            float t = ((point - A).Dot(AB) * InverseLength2).Clamped(0f, 1f);
            float radius = Math.Max(1f, (Radius1 + (Radius2 - Radius1) * t)
                * (1f + 0.12f * (float)Math.Sin(Math.PI * t)));
            float normalized2 = point.SqDist(A + AB * t) / (radius * radius);
            return normalized2 >= 9f ? 0f : (float)Math.Exp(-0.5f * normalized2);
        }
    }

    public BorderField(Node[] nodes, Bridge[] bridges)
    {
        if (nodes.Length == 0)
        {
            Cell = 1f; Columns = Rows = 0;
            Raw = Smoothed = System.Array.Empty<float>();
            return;
        }
        Vector2 min = new(float.MaxValue), max = new(float.MinValue);
        float minRadius = float.MaxValue;
        foreach (Node n in nodes)
        {
            // Include the entire finite Gaussian support, even for dense groups.
            float support = n.Radius * (1f + Empire.BorderShapeMaxVariation) * 3f;
            Include(n.Position - new Vector2(support), n.Position + new Vector2(support));
            minRadius = Math.Min(minRadius, n.Radius);
        }
        foreach (Bridge b in bridges) Include(b.Min, b.Max);
        Cell = Math.Max(1000f, Math.Max(minRadius / 24f, Math.Max(max.X - min.X, max.Y - min.Y) / 384f));
        Origin = min - new Vector2(Cell * 3f);
        Columns = (int)Math.Ceiling((max.X - min.X) / Cell) + 7;
        Rows = (int)Math.Ceiling((max.Y - min.Y) / Cell) + 7;
        Raw = new float[Columns * Rows];
        foreach (Node node in nodes)
        {
            float support = node.Radius * (1f + Empire.BorderShapeMaxVariation) * 3f;
            Splat(node.Position - new Vector2(support), node.Position + new Vector2(support), node.Influence);
        }
        foreach (Bridge bridge in bridges) Splat(bridge.Min, bridge.Max, bridge.Influence);
        for (int i = 0; i < Raw.Length; ++i)
            Raw[i] = Empire.CombineGaussianInfluence(Raw[i]) - Empire.GaussianBorderThreshold;
        // Never close features wider than a small fraction of the smallest node.
        int closingRadius = Math.Min(2, (int)(minRadius * 0.12f / Cell));
        int maxHoleCells = Math.Min(64, (int)(Math.PI * Math.Pow(minRadius * 0.35f / Cell, 2)));
        Smoothed = CloseCavities(Raw, Columns, Rows, closingRadius, maxHoleCells);

        void Include(Vector2 a, Vector2 b)
        {
            min.X = Math.Min(min.X, a.X); min.Y = Math.Min(min.Y, a.Y);
            max.X = Math.Max(max.X, b.X); max.Y = Math.Max(max.Y, b.Y);
        }
    }

    // Rasterize only each primitive's support rectangle instead of evaluating
    // every node/bridge of every empire at every grid point.
    void Splat(Vector2 min, Vector2 max, Func<Vector2, float> influence)
    {
        int x0 = Math.Max(0, (int)Math.Floor((min.X - Origin.X) / Cell));
        int y0 = Math.Max(0, (int)Math.Floor((min.Y - Origin.Y) / Cell));
        int x1 = Math.Min(Columns - 1, (int)Math.Ceiling((max.X - Origin.X) / Cell));
        int y1 = Math.Min(Rows - 1, (int)Math.Ceiling((max.Y - Origin.Y) / Cell));
        for (int y = y0; y <= y1; ++y)
            for (int x = x0; x <= x1; ++x)
                Empire.AccumulateGaussianInfluence(ref Raw[x + y * Columns],
                    influence(Origin + new Vector2(x * Cell, y * Cell)));
    }

    public float Strength(Vector2 point, bool smooth = true)
    {
        float x = (point.X - Origin.X) / Cell, y = (point.Y - Origin.Y) / Cell;
        if (x < 0 || y < 0 || x >= Columns - 1 || y >= Rows - 1)
            return -Empire.GaussianBorderThreshold;
        int ix = (int)x, iy = (int)y, index = ix + iy * Columns;
        float tx = x - ix, ty = y - iy;
        float[] field = smooth ? Smoothed : Raw;
        float a = field[index] + (field[index + 1] - field[index]) * tx;
        float b = field[index + Columns] + (field[index + Columns + 1] - field[index + Columns]) * tx;
        return a + (b - a) * ty;
    }

    // Conservative closing followed by size-limited enclosed-hole filling.
    // Only adds claims; rival raw claims take precedence in BorderScene/Empire.
    internal static float[] CloseCavities(float[] raw, int columns, int rows, int radius, int maxHoleCells)
    {
        bool[] inside = new bool[raw.Length];
        for (int i = 0; i < raw.Length; ++i) inside[i] = raw[i] >= 0f;
        if (radius > 0)
        {
            bool[] dilated = new bool[raw.Length];
            for (int y = 0; y < rows; ++y)
                for (int x = 0; x < columns; ++x)
                    if (inside[x + y * columns])
                        for (int dy = -radius; dy <= radius; ++dy)
                            for (int dx = -radius; dx <= radius; ++dx)
                                if (dx * dx + dy * dy <= radius * radius
                                    && x + dx >= 0 && x + dx < columns && y + dy >= 0 && y + dy < rows)
                                    dilated[x + dx + (y + dy) * columns] = true;
            for (int y = radius; y < rows - radius; ++y)
                for (int x = radius; x < columns - radius; ++x)
                {
                    bool closed = true;
                    for (int dy = -radius; dy <= radius && closed; ++dy)
                        for (int dx = -radius; dx <= radius; ++dx)
                            if (dx * dx + dy * dy <= radius * radius && !dilated[x + dx + (y + dy) * columns])
                            { closed = false; break; }
                    inside[x + y * columns] |= closed;
                }
        }
        bool[] visited = new bool[raw.Length];
        int[] queue = new int[raw.Length];
        for (int start = 0; start < raw.Length; ++start)
        {
            if (inside[start] || visited[start]) continue;
            int count = 1; queue[0] = start; visited[start] = true;
            bool exterior = false;
            for (int head = 0; head < count; ++head)
            {
                int index = queue[head], x = index % columns, y = index / columns;
                exterior |= x == 0 || y == 0 || x == columns - 1 || y == rows - 1;
                // Eight neighbours preserve diagonal passages to the exterior.
                for (int dy = -1; dy <= 1; ++dy)
                    for (int dx = -1; dx <= 1; ++dx)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= columns || ny < 0 || ny >= rows) continue;
                        int next = nx + ny * columns;
                        if (inside[next] || visited[next]) continue;
                        visited[next] = true; queue[count++] = next;
                    }
            }
            if (!exterior && count <= maxHoleCells)
                for (int i = 0; i < count; ++i) inside[queue[i]] = true;
        }
        var result = (float[])raw.Clone();
        for (int i = 0; i < result.Length; ++i)
            if (inside[i] && result[i] < 0f) result[i] = 0.015f;
        return result;
    }
}

internal sealed class BorderSnapshot
{
    public readonly Empire.InfluenceNode[] Nodes;
    public readonly InfluenceConnection[] Connections;
    public readonly BorderField Field, KnownField;
    public readonly float ProjectorRadius;

    public BorderSnapshot(Empire.InfluenceNode[] nodes, BorderField.Node[] samples, float projectorRadius)
    {
        Nodes = nodes;
        ProjectorRadius = projectorRadius;
        var connections = new HashSet<InfluenceConnection>();
        BorderNodeCache.BuildConnections(projectorRadius, nodes, false, connections);
        Connections = new InfluenceConnection[connections.Count];
        connections.CopyTo(Connections);
        var indices = new Dictionary<GameObject, int>();
        for (int i = 0; i < nodes.Length; ++i) indices[nodes[i].Source] = i;
        var bridges = new BorderField.Bridge[Connections.Length];
        for (int i = 0; i < bridges.Length; ++i)
            bridges[i] = new(samples[indices[Connections[i].Node1.Source]], samples[indices[Connections[i].Node2.Source]]);
        Field = new(samples, bridges);
        var knownNodes = new List<BorderField.Node>();
        var knownBridges = new List<BorderField.Bridge>();
        for (int i = 0; i < nodes.Length; ++i)
            if (nodes[i].KnownToPlayer) knownNodes.Add(samples[i]);
        for (int i = 0; i < bridges.Length; ++i)
            if (Connections[i].Node1.KnownToPlayer && Connections[i].Node2.KnownToPlayer) knownBridges.Add(bridges[i]);
        KnownField = knownNodes.Count == nodes.Length ? Field : new(knownNodes.ToArray(), knownBridges.ToArray());
    }
}

// Captured on the simulation thread after all empire updates have joined. Worker
// code never walks live planet lists, relations, or mutable empire collections.
internal sealed class BorderScene
{
    internal sealed class Entry
    {
        public Empire Owner; // identity only, for rendering labels
        public int Id;
        public BorderSnapshot Snapshot;
        public bool Active;
    }
    public readonly Entry[] Empires;
    readonly bool[,] Overlap;
    readonly BorderField.Node[][] SharedSystems;
    public readonly int Signature;

    public static BorderScene Capture(BorderScene previous, Empire[] empires)
    {
        int signature = GetSignature(empires);
        return previous != null && previous.Signature == signature ? previous : new BorderScene(empires);
    }

    static int GetSignature(Empire[] empires)
    {
        var hash = new HashCode();
        foreach (Empire owner in empires)
        {
            BorderSnapshot snapshot = owner.PreparedBorders;
            hash.Add(owner.Id); hash.Add(snapshot);
            hash.Add(owner.IsDefeated); hash.Add(owner.InfluenceActive);
            hash.Add(owner.WeArePirates); hash.Add(owner.WeAreRemnants);
            foreach (Empire rival in empires)
                if (owner != rival) hash.Add(owner.IsAtWarWith(rival));
            if (snapshot == null) continue;
            foreach (Empire.InfluenceNode node in snapshot.Nodes)
                if (node.Source is SolarSystem system)
                    for (int p = 0; p < system.PlanetList.Count; ++p)
                        hash.Add(system.PlanetList[p].Owner?.Id ?? 0);
        }
        return hash.ToHashCode();
    }

    public BorderScene(Empire[] empires)
    {
        int count = empires.Length;
        Empires = new Entry[count];
        Overlap = new bool[count, count];
        SharedSystems = new BorderField.Node[count * count][];
        for (int i = 0; i < count; ++i)
        {
            Empire owner = empires[i];
            Empires[i] = new Entry { Owner = owner, Id = owner.Id, Snapshot = owner.PreparedBorders,
                Active = !owner.IsDefeated && owner.InfluenceActive };
        }
        for (int i = 0; i < count; ++i)
            for (int j = 0; j < count; ++j)
            {
                Empire a = empires[i], b = empires[j];
                Overlap[i, j] = a != b && (a.WeArePirates || b.WeArePirates || a.WeAreRemnants || b.WeAreRemnants || a.IsAtWarWith(b));
                if (i == j || Overlap[i, j] || Empires[i].Snapshot == null) continue;
                var shared = new List<BorderField.Node>();
                foreach (Empire.InfluenceNode node in Empires[i].Snapshot.Nodes)
                {
                    if (node.Source is not SolarSystem system) continue;
                    bool ours = false, theirs = false;
                    for (int p = 0; p < system.PlanetList.Count; ++p)
                    {
                        Empire owner = system.PlanetList[p].Owner;
                        ours |= owner == a; theirs |= owner == b;
                    }
                    if (!ours || !theirs) continue;
                    shared.Add(new(node.Position, node.Radius, node.Source.Id * 0.754877666f + a.Id * 1.618033989f, 0f));
                }
                SharedSystems[i * count + j] = shared.ToArray();
            }
        Signature = GetSignature(empires);
    }

    bool Overlaps(int a, int b, Vector2 point)
    {
        if (Overlap[a, b]) return true;
        BorderField.Node[] shared = SharedSystems[a * Empires.Length + b];
        if (shared != null)
            foreach (BorderField.Node node in shared)
                if (node.Influence(point) >= Empire.GaussianBorderThreshold) return true;
        return false;
    }

    float Strength(int owner, Vector2 point)
    {
        BorderField field = Empires[owner].Snapshot.Field;
        float raw = field.Strength(point, smooth: false), smooth = field.Strength(point);
        if (raw < 0f && smooth >= 0f)
            for (int i = 0; i < Empires.Length; ++i)
                if (i != owner && Empires[i].Active && Empires[i].Snapshot != null
                    && Empires[i].Snapshot.Field.Strength(point, smooth: false) >= 0f && !Overlaps(owner, i, point))
                    return raw;
        return smooth;
    }

    public float Territory(int owner, Vector2 point, out bool occupied)
    {
        occupied = false;
        Entry ours = Empires[owner];
        float scale = ours.Snapshot.ProjectorRadius * 2f;
        float visible = ours.Snapshot.KnownField.Strength(point);
        if (visible < 0f) return visible * scale;
        float strength = Strength(owner, point);
        float result = Math.Min(visible, strength);
        for (int i = 0; i < Empires.Length; ++i)
        {
            Entry rival = Empires[i];
            if (i == owner || !rival.Active || rival.Snapshot == null) continue;
            float other = Strength(i, point);
            if (other >= 0f && Overlaps(owner, i, point))
            {
                occupied |= ours.Id < rival.Id;
                continue;
            }
            // Continuous competition field gives marching squares a real shared
            // frontier instead of a binary sign flip of our entire claim.
            float advantage = strength - other + (ours.Id < rival.Id ? 0.001f : -0.001f);
            result = Math.Min(result, advantage);
        }
        occupied &= result >= 0f;
        return result * scale;
    }
}
