using SDUtils;
using SDGraphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using GraphicsDevice = Microsoft.Xna.Framework.Graphics.GraphicsDevice;
using SurfaceFormat = Microsoft.Xna.Framework.Graphics.SurfaceFormat;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#pragma warning disable CA2231

namespace Ship_Game.Universe
{
    public struct InfluenceConnection : IEquatable<InfluenceConnection>
    {
        public Empire.InfluenceNode Node1;
        public Empire.InfluenceNode Node2;

        public InfluenceConnection(in Empire.InfluenceNode node1, in Empire.InfluenceNode node2)
        {
            if (node1.Source.Id < node2.Source.Id)
            {
                Node1 = node1;
                Node2 = node2;
            }
            else
            {
                Node1 = node2;
                Node2 = node1;
            }
        }

        public bool Equals(InfluenceConnection other)
        {
            return Node1.Source == other.Node1.Source && Node2.Source == other.Node2.Source;
        }

        public override bool Equals(object obj)
        {
            return obj is InfluenceConnection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Node1.Source.Id.GetHashCode() + Node2.Source.Id.GetHashCode();
        }
    }


    /// <summary>
    /// Caches an Empire's border node connections
    /// </summary>
    public class BorderNodeCache
    {
        const float BridgeReachInProjectorRadii = 6f;
        const float CavityClosingReachInProjectorRadii = 2.5f;

        public HashSet<InfluenceConnection> Connections = new();
        public Empire.InfluenceNode[] BorderNodes = Empty<Empire.InfluenceNode>.Array;
        // A low-resolution scalar-field texture, linearly filtered by the GPU.
        // This gives the inward border band a continuous gradient without drawing
        // thousands of flat-alpha rectangles that visibly formed square steps.
        Color[] FillPixels = Empty<Color>.Array;
        int FillWidth;
        int FillHeight;
        int FillVersion;
        int UploadedFillVersion;
        Texture2D FillTexture;
        Color[] OccupationPixels = Empty<Color>.Array;
        int UploadedOccupationVersion;
        Texture2D OccupationTexture;
        public RectF FillBounds { get; private set; }
        // Consecutive pairs form the outer edge of the union of nodes + bridges.
        public Vector2[] OutlineSegments = Empty<Vector2>.Array;
        public Empire[] OutlineRivals = Empty<Empire>.Array;
        int CachedSignature;
        bool HasSignature;
        sealed class SceneRequest
        {
            public BorderScene Scene;
            public int Owner;
        }
        volatile SceneRequest LatestScene;
        SceneRequest BuildingScene;
        Task<BorderNodeCache> PendingBuild;

        internal void SetScene(BorderScene scene, int owner)
        {
            if (LatestScene?.Scene != scene)
                LatestScene = new SceneRequest { Scene = scene, Owner = owner };
        }

        readonly struct ConnectionCandidate
        {
            public readonly int A;
            public readonly int B;
            public readonly float Cost;
            public readonly bool NeedsBridge;
            public readonly bool ClosesCavity;
            public ConnectionCandidate(int a, int b, float cost, bool needsBridge, bool closesCavity)
            {
                A = a; B = b; Cost = cost;
                NeedsBridge = needsBridge;
                ClosesCavity = closesCavity;
            }
        }

        public BorderNodeCache()
        {
        }

        public void Update(Empire empire)
        {
            SceneRequest request = LatestScene;
            if (request == null) return;
            BorderScene.Entry entry = request.Scene.Empires[request.Owner];
            if (PendingBuild?.IsCompleted == true)
            {
                if (PendingBuild.IsCompletedSuccessfully && BuildingScene.Scene.Signature == request.Scene.Signature)
                {
                    BorderNodeCache result = PendingBuild.Result;
                    BorderNodes = result.BorderNodes;
                    Connections = result.Connections;
                    OutlineSegments = result.OutlineSegments;
                    OutlineRivals = result.OutlineRivals;
                    FillPixels = result.FillPixels;
                    OccupationPixels = result.OccupationPixels;
                    FillWidth = result.FillWidth;
                    FillHeight = result.FillHeight;
                    FillBounds = result.FillBounds;
                    ++FillVersion;
                    CachedSignature = request.Scene.Signature;
                    HasSignature = true;
                }
                else if (PendingBuild.IsFaulted)
                    Log.Error(PendingBuild.Exception, "Background border outline failed");
                PendingBuild = null;
                BuildingScene = null;
            }
            if (!entry.Active || entry.Snapshot == null)
            {
                BorderNodes = Empty<Empire.InfluenceNode>.Array;
                OutlineSegments = Empty<Vector2>.Array;
                OutlineRivals = Empty<Empire>.Array;
                Connections.Clear();
                FillPixels = OccupationPixels = Empty<Color>.Array;
                HasSignature = false;
                return;
            }
            if (PendingBuild != null || HasSignature && CachedSignature == request.Scene.Signature) return;
            BuildingScene = request;
            PendingBuild = BorderWorker.TryRun(() =>
            {
                var result = new BorderNodeCache { BorderNodes = entry.Snapshot.Nodes };
                foreach (InfluenceConnection connection in entry.Snapshot.Connections)
                    if (connection.Node1.KnownToPlayer && connection.Node2.KnownToPlayer)
                        result.Connections.Add(connection);
                result.BuildOutline(request.Scene, request.Owner);
                return result;
            });
        }

        /// <summary>Overlapping and closely spaced colonies all receive broad joins,
        /// removing crescent/U cavities inside dense systems. Separate local groups are then
        /// connected with a minimum-spanning forest so distant colonies do not
        /// create an artificial web across the map.</summary>
        public static void BuildConnections(Empire empire, Empire.InfluenceNode[] nodes,
                                            bool knownOnly, HashSet<InfluenceConnection> output)
            => BuildConnections(empire.GetProjectorRadius(), nodes, knownOnly, output);

        internal static void BuildConnections(float projectorRadius, Empire.InfluenceNode[] nodes,
                                             bool knownOnly, HashSet<InfluenceConnection> output)
        {
            output.Clear();
            float bridgeReach = projectorRadius * BridgeReachInProjectorRadii;
            float cavityReach = projectorRadius * CavityClosingReachInProjectorRadii;
            var candidates = new List<ConnectionCandidate>();
            for (int i = 0; i < nodes.Length; ++i)
            {
                ref Empire.InfluenceNode a = ref nodes[i];
                if (knownOnly && !a.KnownToPlayer)
                    continue;
                for (int j = i + 1; j < nodes.Length; ++j)
                {
                    ref Empire.InfluenceNode b = ref nodes[j];
                    if (knownOnly && !b.KnownToPlayer)
                        continue;
                    float distance = a.Position.Distance(b.Position);
                    float combinedRadius = a.Radius + b.Radius;
                    if (distance <= combinedRadius + bridgeReach)
                        candidates.Add(new ConnectionCandidate(i, j,
                            distance / combinedRadius.LowerBound(1f),
                            distance > combinedRadius,
                            distance <= combinedRadius + cavityReach));
                }
            }
            candidates.Sort((a, b) =>
            {
                int order = a.Cost.CompareTo(b.Cost);
                if (order == 0) order = a.A.CompareTo(b.A);
                return order == 0 ? a.B.CompareTo(b.B) : order;
            });
            int[] parent = new int[nodes.Length];
            for (int i = 0; i < parent.Length; ++i) parent[i] = i;
            foreach (ConnectionCandidate candidate in candidates)
            {
                int rootA = Root(candidate.A);
                int rootB = Root(candidate.B);
                if (candidate.ClosesCavity)
                    output.Add(new InfluenceConnection(nodes[candidate.A], nodes[candidate.B]));
                if (rootA == rootB)
                    continue;
                parent[rootB] = rootA;
                if (candidate.NeedsBridge)
                    output.Add(new InfluenceConnection(nodes[candidate.A], nodes[candidate.B]));
            }

            int Root(int node)
            {
                while (parent[node] != node)
                {
                    parent[node] = parent[parent[node]];
                    node = parent[node];
                }
                return node;
            }
        }

        void BuildOutline(BorderScene scene, int owner)
        {
            BorderSnapshot snapshot = scene.Empires[owner].Snapshot;
            BorderField known = snapshot.KnownField;
            if (known.Columns == 0)
            {
                OutlineSegments = Empty<Vector2>.Array;
                OutlineRivals = Empty<Empire>.Array;
                FillPixels = Empty<Color>.Array;
                OccupationPixels = Empty<Color>.Array;
                FillBounds = default;
                ++FillVersion;
                return;
            }

            Vector2 min = known.Origin;
            float cell = known.Cell;
            int columns = known.Columns, rows = known.Rows;
            float[] field = new float[columns * rows];
            bool[] occupied = new bool[field.Length];

            for (int y = 0; y < rows; ++y)
            {
                for (int x = 0; x < columns; ++x)
                {
                    Vector2 p = min + new Vector2(x * cell, y * cell);
                    int index = x + y * columns;
                    field[index] = scene.Territory(owner, p, out occupied[index]);
                }
            }

            float fadeDepth = Math.Max(snapshot.ProjectorRadius * 0.5f, cell * 5f);
            BuildFillTextureData(field, columns, rows, min, cell, fadeDepth);
            BuildOccupationTextureData(occupied, columns, rows);

            var segments = new Array<Vector2>();
            for (int y = 0; y < rows - 1; ++y)
            {
                for (int x = 0; x < columns - 1; ++x)
                {
                    float tl = field[x     + y       * columns];
                    float tr = field[x + 1 + y       * columns];
                    float br = field[x + 1 + (y + 1) * columns];
                    float bl = field[x     + (y + 1) * columns];
                    int mask = 0;
                    if (tl >= 0f) mask |= 1;
                    if (tr >= 0f) mask |= 2;
                    if (br >= 0f) mask |= 4;
                    if (bl >= 0f) mask |= 8;
                    if (mask == 0 || mask == 15)
                        continue;

                    Vector2 corner = min + new Vector2(x * cell, y * cell);
                    Vector2 top = corner + new Vector2(EdgeFraction(tl, tr) * cell, 0f);
                    Vector2 right = corner + new Vector2(cell, EdgeFraction(tr, br) * cell);
                    Vector2 bottom = corner + new Vector2(EdgeFraction(bl, br) * cell, cell);
                    Vector2 left = corner + new Vector2(0f, EdgeFraction(tl, bl) * cell);
                    // Asymptotic decider for the two saddle configurations.
                    bool connectTopLeft = (double)tl * br - (double)tr * bl >= 0;
                    AddMarchingSegments(mask, top, right, bottom, left, segments, connectTopLeft);
                }
            }
            OutlineSegments = SmoothContourSegments(segments, cell * 0.1f);
            OutlineRivals = FindOutlineRivals(scene, owner, OutlineSegments);
        }

        void BuildOccupationTextureData(bool[] occupied, int columns, int rows)
        {
            var pixels = new Color[occupied.Length];
            for (int y = 0; y < rows; ++y)
            {
                for (int x = 0; x < columns; ++x)
                {
                    if (!occupied[x + y * columns])
                        continue;
                    // Four-cell diagonal checks remain legible after linear filtering.
                    byte alpha = (byte)((((x + y) / 4) & 1) == 0 ? 235 : 55);
                    pixels[x + y * columns] = new Color(alpha, alpha, alpha, alpha);
                }
            }
            OccupationPixels = pixels;
        }

        void BuildFillTextureData(float[] field, int columns, int rows,
                                  Vector2 min, float cell, float fadeDepth)
        {
            var pixels = new Color[field.Length];
            for (int i = 0; i < field.Length; ++i)
            {
                float depth = field[i];
                float alpha = depth < 0f ? 0f : (1f - depth / fadeDepth).Clamped(0f, 1f);
                byte a = (byte)(alpha * 255f);
                // Premultiplied white: RGB follows alpha, preventing dark fringes
                // when the GPU linearly filters across the transparent exterior.
                pixels[i] = new Color(a, a, a, a);
            }
            FillPixels = pixels;
            FillWidth = columns;
            FillHeight = rows;
            FillBounds = new RectF(min.X, min.Y, (columns - 1) * cell, (rows - 1) * cell);
            ++FillVersion;
        }

        public Texture2D GetFillTexture(GraphicsDevice device)
        {
            if (FillPixels.Length == 0)
                return null;
            if (FillTexture == null || FillTexture.IsDisposed
                || FillTexture.Width != FillWidth || FillTexture.Height != FillHeight)
            {
                FillTexture?.Dispose();
                FillTexture = new Texture2D(device, FillWidth, FillHeight, false, SurfaceFormat.Color);
                UploadedFillVersion = 0;
            }
            if (UploadedFillVersion != FillVersion)
            {
                FillTexture.SetData(FillPixels);
                UploadedFillVersion = FillVersion;
            }
            return FillTexture;
        }

        public Texture2D GetOccupationTexture(GraphicsDevice device)
        {
            if (OccupationPixels.Length == 0)
                return null;
            if (OccupationTexture == null || OccupationTexture.IsDisposed
                || OccupationTexture.Width != FillWidth || OccupationTexture.Height != FillHeight)
            {
                OccupationTexture?.Dispose();
                OccupationTexture = new Texture2D(device, FillWidth, FillHeight, false, SurfaceFormat.Color);
                UploadedOccupationVersion = 0;
            }
            if (UploadedOccupationVersion != FillVersion)
            {
                OccupationTexture.SetData(OccupationPixels);
                UploadedOccupationVersion = FillVersion;
            }
            return OccupationTexture;
        }

        static float EdgeFraction(float a, float b)
        {
            float denominator = a - b;
            return Math.Abs(denominator) < 0.0001f ? 0.5f : (a / denominator).Clamped(0f, 1f);
        }

        static float DistanceToSegmentSquared(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float length2 = ab.SqLen();
            if (length2 < 1f)
                return point.SqDist(a);
            float t = ((point - a).Dot(ab) / length2).Clamped(0f, 1f);
            return point.SqDist(a + ab * t);
        }

        static Empire[] FindOutlineRivals(BorderScene scene, int owner, Vector2[] outline)
        {
            var rivals = new Empire[outline.Length / 2];
            for (int segment = 0; segment < rivals.Length; ++segment)
            {
                Vector2 midpoint = (outline[segment * 2] + outline[segment * 2 + 1]) * 0.5f;
                float bestStrength = 0f;
                foreach (BorderScene.Entry other in scene.Empires)
                {
                    if (other == scene.Empires[owner] || !other.Active || other.Snapshot == null)
                        continue;
                    Empire.InfluenceNode[] nodes = other.Snapshot.Nodes;
                    for (int i = 0; i < nodes.Length; ++i)
                    {
                        ref Empire.InfluenceNode node = ref nodes[i];
                        if (!node.KnownToPlayer)
                            continue;
                        float strength = 1f - midpoint.Distance(node.Position) / node.Radius;
                        if (strength > bestStrength)
                        {
                            bestStrength = strength;
                            rivals[segment] = other.Owner;
                        }
                    }
                }
            }
            return rivals;
        }

        internal static Vector2[] SmoothContourSegments(Array<Vector2> rawSegments, float joinTolerance)
        {
            int segmentCount = rawSegments.Count / 2;
            if (segmentCount < 3)
                return rawSegments.ToArray();

            bool[] used = new bool[segmentCount];
            float tolerance2 = joinTolerance * joinTolerance;
            var output = new Array<Vector2>(rawSegments.Count * 2);
            var endpoints = new Dictionary<(int X, int Y), List<int>>();
            for (int i = 0; i < rawSegments.Count; ++i)
            {
                var key = Bucket(rawSegments[i]);
                if (!endpoints.TryGetValue(key, out List<int> bucket))
                    endpoints[key] = bucket = new List<int>();
                bucket.Add(i);
            }

            (int X, int Y) Bucket(Vector2 p)
                => ((int)Math.Floor(p.X / joinTolerance), (int)Math.Floor(p.Y / joinTolerance));

            for (int first = 0; first < segmentCount; ++first)
            {
                if (used[first])
                    continue;
                used[first] = true;
                var points = new List<Vector2>
                {
                    rawSegments[first * 2], rawSegments[first * 2 + 1]
                };

                bool closed = false;
                while (points.Count <= segmentCount + 1)
                {
                    Vector2 end = points[points.Count - 1];
                    int nextSegment = -1;
                    Vector2 nextPoint = default;
                    var key = Bucket(end);
                    for (int dy = -1; dy <= 1; ++dy)
                    for (int dx = -1; dx <= 1; ++dx)
                    {
                        if (!endpoints.TryGetValue((key.X + dx, key.Y + dy), out List<int> bucket)) continue;
                        foreach (int endpoint in bucket)
                        {
                            int candidate = endpoint / 2;
                            if (used[candidate] || nextSegment >= 0 && candidate >= nextSegment) continue;
                            if (end.SqDist(rawSegments[endpoint]) <= tolerance2)
                            {
                                nextSegment = candidate;
                                nextPoint = rawSegments[endpoint ^ 1];
                            }
                        }
                    }

                    if (nextSegment < 0)
                        break;
                    used[nextSegment] = true;
                    if (points.Count > 3 && nextPoint.SqDist(points[0]) <= tolerance2)
                    {
                        closed = true;
                        break;
                    }
                    points.Add(nextPoint);
                }

                // Two Chaikin passes round the grid corners while retaining the
                // topology and shared frontier produced by marching squares.
                for (int pass = 0; pass < 2 && points.Count >= 3; ++pass)
                {
                    var smooth = new List<Vector2>(points.Count * 2);
                    if (!closed)
                        smooth.Add(points[0]);
                    int edgeCount = closed ? points.Count : points.Count - 1;
                    for (int i = 0; i < edgeCount; ++i)
                    {
                        Vector2 a = points[i];
                        Vector2 b = points[(i + 1) % points.Count];
                        smooth.Add(a * 0.75f + b * 0.25f);
                        smooth.Add(a * 0.25f + b * 0.75f);
                    }
                    if (!closed)
                        smooth.Add(points[points.Count - 1]);
                    points = smooth;
                }

                for (int i = 0; i < points.Count - 1; ++i)
                {
                    output.Add(points[i]);
                    output.Add(points[i + 1]);
                }
                if (closed)
                {
                    output.Add(points[points.Count - 1]);
                    output.Add(points[0]);
                }
            }
            return output.ToArray();
        }

        internal static void AddMarchingSegments(int mask, Vector2 top, Vector2 right,
                                        Vector2 bottom, Vector2 left, Array<Vector2> output, bool connectTopLeft)
        {
            void Add(Vector2 a, Vector2 b) { output.Add(a); output.Add(b); }
            switch (mask)
            {
                case 1:  Add(left, top); break;
                case 2:  Add(top, right); break;
                case 3:  Add(left, right); break;
                case 4:  Add(right, bottom); break;
                case 5:
                case 10:
                    if (connectTopLeft) { Add(left, bottom); Add(top, right); }
                    else { Add(top, left); Add(right, bottom); }
                    break;
                case 6:  Add(top, bottom); break;
                case 7:  Add(left, bottom); break;
                case 8:  Add(bottom, left); break;
                case 9:  Add(top, bottom); break;
                case 11: Add(right, bottom); break;
                case 12: Add(left, right); break;
                case 13: Add(top, right); break;
                case 14: Add(left, top); break;
            }
        }
    }
}
