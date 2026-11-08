using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Gameplay;
using Ship_Game.Ships.Legacy;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Point = SDGraphics.Point;
using Vector4 = SDGraphics.Vector4;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This describes an unique StarShip Hull. Ship Designs are built upon
    /// the slots described by this Hull definition
    ///
    /// It also describes visual aspects of the StarShip, such as the Mesh and its visual offset
    ///
    /// This class is Serialized/Deserialized using a custom text-based format
    /// </summary>
    public class ShipHull
    {
        // Current version of ShipData files
        // If we introduce incompatibilities we need to convert old to new
        public const int Version = 1;
        public string HullName; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string VisibleName; // Visible name of the Hull in the UI, ex: Misc/HaulerSmall -> "Small Freighter"
        public string ModName; // null if vanilla, else mod name eg "Combined Arms"
        public string Style; // "Terran"
        public string Description; // "With the advent of more powerful StarDrives, this giant cruiser hull was ..."
        public Point Size;
        public int SurfaceArea;
        public Vector2 MeshOffset; // offset of the mesh from Mesh object center, for grid to match model
        public string IconPath; // "ShipIcons/shuttle"
        public string ModelPath; // "Model/Ships/Terran/Shuttle/ship08"

        public RoleName Role = RoleName.fighter;
        public string SelectIcon = "";
        public bool Animated;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        public ThrusterZone[] Thrusters = Empty<ThrusterZone>.Array;
        public HullSlot[] HullSlots;

        // offset from grid TopLeft to the Center slot
        public Point GridCenter;

        public bool Unlockable = true;
        public HashSet<string> TechsNeeded = new();

        // header comment lines such as "#Model Author ...", written back on save
        public string[] Comments = Empty<string>.Array;
        const string ThrusterHeader = "#Thruster PosX,PosY,PosZ,Scale";

        // original source of this ShipHull
        public FileInfo Source;
        // last modification time of the Source file, if this has changed, the hull should be reloaded
        public DateTime SourceModifiedTime;

        public SubTexture Icon => ResourceManager.Texture(IconPath);
        public Vector3 Volume { get; private set; }
        public float ModelZ { get; private set; }
        public bool IsValidForCurrentMod => GlobalStats.IsValidForCurrentMod(ModName);

        // Per-grid-cell most-negative mesh Z (the hull surface facing the
        // camera). NaN cells = no vertex sampled. Populated lazily on first
        // LoadModel; null until the mesh is in memory.
        float[,] SurfaceMap;
        // and the most-positive, the surface that faces the camera when the hull is upside down, as a tumbling derelict
        float[,] BackSurfaceMap;

        // a model cut into parts: each part's triangles, once the model has loaded
        XnaVector3[][] PartTriangles;
        public int NumParts => PartTriangles?.Length ?? 0;
        // its model has loaded in one piece, so its ships have no parts to count
        public bool ModelInOnePiece { get; private set; }

        // the part map with the model and grid placement it was made for, so a hull editor's change of placement makes it again
        sealed record PartGrid(XnaVector3[][] Triangles, Vector2 MeshOffset, Point GridCenter, Point Size, byte[,] Map);
        PartGrid PartMapGrid;
        const int PartSamples = 4; // a cell's samples a side

        // the part of a model cut into parts each grid cell shows from above, 0 for the core; null until the model loads, or for a model in one piece
        public byte[,] PartMap => CurrentPartGrid()?.Map;

        // the part map and its part count, read together
        public bool GetPartMap(out byte[,] map, out int numParts)
        {
            PartGrid grid = CurrentPartGrid();
            map = grid?.Map;
            numParts = grid?.Triangles.Length ?? 0;
            return grid != null;
        }

        PartGrid CurrentPartGrid()
        {
            XnaVector3[][] triangles = PartTriangles;
            if (triangles == null)
                return null;
            PartGrid grid = PartMapGrid;
            if (grid == null || grid.Triangles != triangles || grid.MeshOffset != MeshOffset || grid.GridCenter != GridCenter || grid.Size != Size)
            {
                grid = new(triangles, MeshOffset, GridCenter, Size, BuildPartMap(triangles));
                PartMapGrid = grid;
            }
            return grid;
        }

        // the part each thruster sits on, with the model, placement and thruster positions it was found for
        sealed record ThrusterPartList(XnaVector3[][] Triangles, Vector2 MeshOffset, Vector3[] Positions, byte[] Parts);
        ThrusterPartList ThrusterPartsFound;

        // the part each thruster sits on, which takes it out while lost till Ship.ThrusterBackShare of it is back; null until the model loads, or for a model in one piece
        public byte[] ThrusterParts
        {
            get
            {
                XnaVector3[][] triangles = PartTriangles;
                if (triangles == null)
                    return null;
                ThrusterZone[] thrusters = Thrusters;
                ThrusterPartList found = ThrusterPartsFound;
                if (found == null || found.Triangles != triangles || found.MeshOffset != MeshOffset || !SamePositions(found.Positions, thrusters))
                {
                    var positions = new Vector3[thrusters.Length];
                    var parts = new byte[thrusters.Length];
                    for (int i = 0; i < parts.Length; ++i)
                    {
                        Vector3 pos = positions[i] = thrusters[i].Position;
                        parts[i] = (byte)PartUnder(triangles, new XnaVector3(pos.X - MeshOffset.X, pos.Y - MeshOffset.Y, pos.Z));
                    }
                    found = new(triangles, MeshOffset, positions, parts);
                    ThrusterPartsFound = found;
                }
                return found.Parts;
            }
        }

        // the hull editor moves thrusters in place
        static bool SamePositions(Vector3[] positions, ThrusterZone[] thrusters)
        {
            if (positions.Length != thrusters.Length)
                return false;
            for (int i = 0; i < positions.Length; ++i)
                if (positions[i] != thrusters[i].Position)
                    return false;
            return true;
        }

        // a touch cell's side at the least: two parts touch where their surfaces come within about four cells, some 9 units on a ship and more on a big model
        const float MinTouchCell = 2f;

        // each part's touching parts, with the model they were found for
        sealed record PartLinkList(XnaVector3[][] Triangles, uint[] Links);
        PartLinkList PartLinksFound;

        // the parts each part touches, a bit each, 0 the core, a part reaching the core through none taken as touching it; a part stays on only while a chain of whole parts it touches reaches the core; null until the model loads, or for a model in one piece
        public uint[] PartLinks
        {
            get
            {
                XnaVector3[][] triangles = PartTriangles;
                if (triangles == null)
                    return null;
                PartLinkList found = PartLinksFound;
                if (found == null || found.Triangles != triangles)
                {
                    found = new(triangles, FindPartLinks(triangles, out _));
                    PartLinksFound = found;
                }
                return found.Links;
            }
        }

        // the grid of touch cells over a model has about this many, never more than twice, its cells growing past MinTouchCell on a big model
        const int MaxTouchCells = 1 << 20;

        // points sampled over each part's surface fall in touch cells, and parts in the same or neighboring cells touch; loose, the parts that reached the core through none, now touching it
        internal static uint[] FindPartLinks(XnaVector3[][] partTriangles, out uint loose)
        {
            int numParts = Math.Min(partTriangles.Length, StaticMesh.MaxParts);
            var min = new XnaVector3(float.MaxValue);
            var max = new XnaVector3(float.MinValue);
            for (int part = 0; part < numParts; ++part)
            {
                foreach (XnaVector3 v in partTriangles[part])
                {
                    min = XnaVector3.Min(min, v);
                    max = XnaVector3.Max(max, v);
                }
            }

            var links = new uint[numParts];
            if (min.X <= max.X)
            {
                XnaVector3 size = max - min;
                var cells = new TouchCells(min, size);
                for (int part = 0; part < numParts; ++part)
                {
                    XnaVector3[] corners = partTriangles[part];
                    for (int t = 0; t + 2 < corners.Length; t += 3)
                        cells.Mark(corners[t], corners[t + 1], corners[t + 2], 1u << part);
                }
                cells.Link(links);
            }

            uint reached = 1u;
            for (uint grown = 1u; grown != 0u;)
            {
                grown = 0u;
                for (int part = 0; part < numParts; ++part)
                    if ((reached & 1u << part) != 0)
                        grown |= links[part] & ~reached;
                reached |= grown;
            }
            loose = 0u;
            for (int part = 1; part < numParts; ++part)
            {
                if ((reached & 1u << part) != 0)
                    continue;
                loose |= 1u << part;
                links[part] |= 1u;
                links[0] |= 1u << part;
            }
            return links;
        }

        // each cell of a grid over a model, the parts whose surface passes through it, a bit each
        sealed class TouchCells
        {
            readonly XnaVector3 Origin;
            readonly float Size;
            readonly int NX, NY, NZ;
            readonly uint[] Parts;
            readonly Array<int> Used = new();

            public TouchCells(XnaVector3 origin, XnaVector3 extent)
            {
                Origin = origin;
                Size = Math.Max(MinTouchCell, MathF.Cbrt((extent.X + 1f) * (extent.Y + 1f) * (extent.Z + 1f) / MaxTouchCells));
                while (((long)(extent.X / Size) + 2) * ((long)(extent.Y / Size) + 2) * ((long)(extent.Z / Size) + 2) > 2L * MaxTouchCells)
                    Size *= 1.25f; // a flat model's grid is wider than its volume says
                NX = (int)(extent.X / Size) + 2;
                NY = (int)(extent.Y / Size) + 2;
                NZ = (int)(extent.Z / Size) + 2;
                Parts = new uint[NX * NY * NZ];
            }

            // the cells a triangle passes through, sampled in rows from its longest edge to its far corner, no more than 0.7 of a cell apart
            public void Mark(XnaVector3 a, XnaVector3 b, XnaVector3 c, uint bit)
            {
                float ab = (b - a).LengthSquared(), bc = (c - b).LengthSquared(), ca = (a - c).LengthSquared();
                if (bc > ab && bc >= ca)
                    (a, b, c) = (b, c, a);
                else if (ca > ab && ca > bc)
                    (a, b, c) = (c, a, b);
                float step = Size * 0.7f;
                float edge = (b - a).Length();
                float height = edge > 0f ? XnaVector3.Cross(b - a, c - a).Length() / edge : (c - a).Length();
                int rows = Math.Max(1, (int)MathF.Ceiling(height / step));
                for (int r = 0; r <= rows; ++r)
                {
                    float t = r / (float)rows;
                    XnaVector3 from = a + (c - a) * t, to = b + (c - b) * t;
                    int points = Math.Max(1, (int)MathF.Ceiling((to - from).Length() / step));
                    for (int i = 0; i <= points; ++i)
                    {
                        XnaVector3 p = (from + (to - from) * (i / (float)points) - Origin) / Size;
                        int cell = Math.Clamp((int)p.X, 0, NX - 1) + NX * (Math.Clamp((int)p.Y, 0, NY - 1) + NY * Math.Clamp((int)p.Z, 0, NZ - 1));
                        if (Parts[cell] == 0u)
                            Used.Add(cell);
                        Parts[cell] |= bit;
                    }
                }
            }

            // the parts in each cell touch those in it and in the cells around it
            public void Link(uint[] links)
            {
                for (int i = 0; i < Used.Count; ++i)
                {
                    int cell = Used[i];
                    uint parts = Parts[cell];
                    LinkParts(links, parts, parts);
                    int x = cell % NX, y = cell / NX % NY, z = cell / (NX * NY);
                    for (int dz = 0; dz <= 1 && z + dz < NZ; ++dz) // each pair of neighbors once: those after it
                    for (int dy = dz == 0 ? 0 : -1; dy <= 1; ++dy)
                    for (int dx = dz == 0 && dy == 0 ? 1 : -1; dx <= 1; ++dx)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && nx < NX && ny >= 0 && ny < NY)
                            LinkParts(links, parts, Parts[nx + NX * (ny + NY * (z + dz))]);
                    }
                }
            }

            static void LinkParts(uint[] links, uint a, uint b)
            {
                uint both = a | b;
                if (b == 0u || (both & (both - 1)) == 0)
                    return; // one part only
                for (int part = 0; part < links.Length; ++part)
                {
                    if ((a & 1u << part) != 0)
                        links[part] |= b & ~(1u << part);
                    if ((b & 1u << part) != 0)
                        links[part] |= a & ~(1u << part);
                }
            }
        }

        // a part's center in the model, the radius about it that holds it, and the model's part count; false before the model loads, or for a part with no triangles
        public bool GetPartBounds(int part, out XnaVector3 center, out float radius, out int numParts)
        {
            center = default;
            radius = 0f;
            XnaVector3[][] triangles = PartTriangles;
            numParts = triangles?.Length ?? 0;
            if (triangles == null || (uint)part >= (uint)triangles.Length || triangles[part].Length == 0)
                return false;
            BoundsOf(triangles[part], out center, out radius);
            return true;
        }

        static void BoundsOf(XnaVector3[] corners, out XnaVector3 center, out float radius)
        {
            var min = new XnaVector3(float.MaxValue);
            var max = new XnaVector3(float.MinValue);
            foreach (XnaVector3 v in corners)
            {
                min = XnaVector3.Min(min, v);
                max = XnaVector3.Max(max, v);
            }
            center = (min + max) * 0.5f;
            radius = (max - min).Length() * 0.5f;
        }

        // a random point on a part's surface in the model, each triangle as likely as its area, and the radius about the part's center that holds it; false before the model loads, or for a part with no triangles
        public bool RandomPointOnPart(int part, RandomBase random, out XnaVector3 point, out float radius)
        {
            point = default;
            radius = 0f;
            XnaVector3[][] triangles = PartTriangles;
            if (triangles == null || (uint)part >= (uint)triangles.Length || triangles[part].Length < 3)
                return false;
            XnaVector3[] corners = triangles[part];
            BoundsOf(corners, out XnaVector3 _, out radius);

            float total = 0f;
            for (int i = 0; i + 2 < corners.Length; i += 3)
                total += TwiceArea(corners, i);
            float pick = random.Float() * total;
            int t = 0;
            while (t + 5 < corners.Length && (pick -= TwiceArea(corners, t)) > 0f)
                t += 3;

            float u = random.Float(), v = random.Float();
            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }
            point = corners[t] + (corners[t + 1] - corners[t]) * u + (corners[t + 2] - corners[t]) * v;
            return true;
        }

        static float TwiceArea(XnaVector3[] corners, int i)
            => XnaVector3.Cross(corners[i + 1] - corners[i], corners[i + 2] - corners[i]).Length();

        // the part whose surface is on top at a model point seen from above, as a module's, or for a point off the model's outline the part with the surface nearest it
        public static int PartUnder(XnaVector3[][] partTriangles, XnaVector3 p)
        {
            int top = -1;
            float topZ = float.MaxValue;
            for (int part = 0; part < partTriangles.Length; ++part)
            {
                XnaVector3[] corners = partTriangles[part];
                for (int t = 0; t + 2 < corners.Length; t += 3)
                {
                    XnaVector3 a = corners[t], b = corners[t + 1], c = corners[t + 2];
                    float area = (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
                    if (Math.Abs(area) < 1e-6f)
                        continue; // edge-on from above, as a side wall
                    float wa = ((b.X - p.X) * (c.Y - p.Y) - (c.X - p.X) * (b.Y - p.Y)) / area;
                    float wb = ((c.X - p.X) * (a.Y - p.Y) - (a.X - p.X) * (c.Y - p.Y)) / area;
                    float wc = 1f - wa - wb;
                    const float edge = -1e-4f;
                    if (wa < edge || wb < edge || wc < edge)
                        continue;
                    float z = wa * a.Z + wb * b.Z + wc * c.Z; // most negative is on top
                    if (z < topZ)
                    {
                        topZ = z;
                        top = part;
                    }
                }
            }
            if (top >= 0)
                return top;

            int nearest = 0;
            float nearestSq = float.MaxValue;
            for (int part = 0; part < partTriangles.Length; ++part)
            {
                XnaVector3[] corners = partTriangles[part];
                for (int t = 0; t + 2 < corners.Length; t += 3)
                {
                    float distanceSq = XnaVector3.DistanceSquared(p, ClosestOnTriangle(p, corners[t], corners[t + 1], corners[t + 2]));
                    if (distanceSq < nearestSq)
                    {
                        nearestSq = distanceSq;
                        nearest = part;
                    }
                }
            }
            return nearest;
        }

        // the point of triangle abc closest to p
        static XnaVector3 ClosestOnTriangle(XnaVector3 p, XnaVector3 a, XnaVector3 b, XnaVector3 c)
        {
            XnaVector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = XnaVector3.Dot(ab, ap), d2 = XnaVector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
                return a;
            XnaVector3 bp = p - b;
            float d3 = XnaVector3.Dot(ab, bp), d4 = XnaVector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
                return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
                return a + ab * (d1 / (d1 - d3));
            XnaVector3 cp = p - c;
            float d5 = XnaVector3.Dot(ab, cp), d6 = XnaVector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
                return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
                return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
                return b + (c - b) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
            float denom = 1f / (va + vb + vc);
            return a + ab * (vb * denom) + ac * (vc * denom);
        }

        // Lift VFX slightly toward the camera so emitters sit just above the
        // deck instead of co-planar with the mesh surface.
        const float SurfaceLift = -2f;

        public override string ToString() => $"ShipHull={HullName} Source={Source?.FullName} Model={ModelPath}";

        public struct ThrusterZone
        {
            public Vector3 Position;
            public float Scale;

            public Vector2 WorldPos2D =>
                Thruster.GetPosition(Vector2.Zero, 0, Vector3.Down, Position).ToVec2();

            public float WorldRadius => Scale / 8f;

            public void SetWorldPos2D(Vector2 worldPos)
            {
                Position.X = (float)Math.Round(worldPos.X);
                Position.Y = (float)Math.Round(worldPos.Y);
            }

            public void SetWorldScale(float worldScale)
            {
                Scale = (float)Math.Round(worldScale * 8);
            }
        }

        public HullSlot FindSlot(Point p)
        {
            for (int i = 0; i < HullSlots.Length; ++i)
            {
                var slot = HullSlots[i];
                if (slot.Pos == p)
                    return slot;
            }
            return null;
        }

        // findPartLinks, for a ship in a game with hull plates on: which parts touch is found with the model, not at a part's loss in a battle
        public bool LoadModel(out SceneObject shipSO, GameContentManager content, bool findPartLinks = false)
        {
            shipSO = null;
            lock (HullSlots)
            {
                // The mesh is cached by content manager. extractVertexPositions
                // only pays the per-vertex copy on hull loads — planets / asteroids /
                // projectile meshes skip it.
                StaticMesh mesh = StaticMesh.LoadMesh(content, ModelPath, Animated, extractVertexPositions: true);
                if (mesh == null)
                    return false;

                shipSO = mesh.CreateSceneObject();
                if (shipSO == null)
                    return false;

                // update volume if needed
                if (Volume.X.AlmostEqual(0f))
                {
                    Volume = new Vector3(mesh.Bounds.Max - mesh.Bounds.Min);
                    ModelZ = Volume.Z;
                }

                if (SurfaceMap == null)
                {
                    // Claim the positions array into a local before any other thread
                    // that shares this cached StaticMesh can null it — protects
                    // BuildSurfaceMap's read from racing the post-build cleanup.
                    var positions = mesh.VertexPositions;
                    mesh.VertexPositions = null;
                    if (positions != null)
                        BuildSurfaceMap(positions);
                }

                XnaVector3[][] triangles = mesh.NumParts > 0 ? mesh.PartTriangles : null;
                if (findPartLinks && triangles != null && PartLinksFound?.Triangles != triangles)
                    PartLinksFound = new(triangles, FindPartLinks(triangles, out _)); // before the parts show, so a ship counting them finds the links ready
                PartTriangles = triangles; // a reloaded hull may show another model
                ModelInOnePiece = PartTriangles == null;
                return true;
            }
        }

        // each cell takes the part whose surface is on top at most of its samples, seen from above as the grid sits on the model
        byte[,] BuildPartMap(XnaVector3[][] partTriangles)
        {
            int w = Math.Max(Size.X, 0), h = Math.Max(Size.Y, 0);
            int numParts = partTriangles.Length;

            int sw = w * PartSamples, sh = h * PartSamples;
            var top = new float[sw * sh];
            var topPart = new byte[sw * sh];
            Array.Fill(top, float.MaxValue);
            for (int part = 0; part < partTriangles.Length; ++part)
            {
                XnaVector3[] corners = partTriangles[part];
                for (int t = 0; t + 2 < corners.Length; t += 3)
                    RasterizeTop(corners[t], corners[t + 1], corners[t + 2], (byte)part, top, topPart, sw, sh);
            }

            var map = new byte[w, h];
            var samples = new int[numParts];
            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                Array.Clear(samples);
                for (int sy = 0; sy < PartSamples; ++sy)
                for (int sx = 0; sx < PartSamples; ++sx)
                {
                    int i = (x * PartSamples + sx) + (y * PartSamples + sy) * sw;
                    if (top[i] < float.MaxValue)
                        ++samples[topPart[i]];
                }
                map[x, y] = (byte)MostCovered(samples);
            }
            return map;
        }

        // the part covering the most, a tie going to a part over the core, then to the lower part number
        public static int MostCovered(ReadOnlySpan<int> coverByPart)
        {
            int best = 0;
            for (int part = 1; part < coverByPart.Length; ++part)
                if (coverByPart[part] > 0 && (best == 0 ? coverByPart[part] >= coverByPart[0] : coverByPart[part] > coverByPart[best]))
                    best = part;
            return best;
        }

        // the triangle's top surface over the samples it covers, sample i of a row centered at (i + 0.5) / PartSamples cells
        void RasterizeTop(XnaVector3 a, XnaVector3 b, XnaVector3 c, byte part, float[] top, byte[] topPart, int sw, int sh)
        {
            float ax = SampleX(a), ay = SampleY(a), bx = SampleX(b), by = SampleY(b), cx = SampleX(c), cy = SampleY(c);
            float area = (bx - ax) * (cy - ay) - (cx - ax) * (by - ay);
            if (Math.Abs(area) < 1e-6f)
                return; // edge-on from above, as a side wall

            int minX = Math.Max(0, (int)Math.Ceiling(Math.Min(ax, Math.Min(bx, cx))));
            int maxX = Math.Min(sw - 1, (int)Math.Floor(Math.Max(ax, Math.Max(bx, cx))));
            int minY = Math.Max(0, (int)Math.Ceiling(Math.Min(ay, Math.Min(by, cy))));
            int maxY = Math.Min(sh - 1, (int)Math.Floor(Math.Max(ay, Math.Max(by, cy))));
            const float edge = -1e-4f;
            for (int py = minY; py <= maxY; ++py)
            for (int px = minX; px <= maxX; ++px)
            {
                float wa = ((bx - px) * (cy - py) - (cx - px) * (by - py)) / area;
                float wb = ((cx - px) * (ay - py) - (ax - px) * (cy - py)) / area;
                float wc = 1f - wa - wb;
                if (wa < edge || wb < edge || wc < edge)
                    continue;
                float z = wa * a.Z + wb * b.Z + wc * c.Z; // most negative is on top
                int i = px + py * sw;
                if (z < top[i])
                {
                    top[i] = z;
                    topPart[i] = part;
                }
            }
        }

        // a model point's place in sample units, as BuildSurfaceMap places it on the grid
        float SampleX(XnaVector3 v) => ((v.X + MeshOffset.X) / 16f + GridCenter.X) * PartSamples - 0.5f;
        float SampleY(XnaVector3 v) => ((v.Y + MeshOffset.Y) / 16f + GridCenter.Y) * PartSamples - 0.5f;

        // the part shown from above at a grid cell, 0 for the core, outside the grid, or before the model loads
        public static int PartAt(byte[,] map, Point cell)
        {
            if (map == null || (uint)cell.X >= (uint)map.GetLength(0) || (uint)cell.Y >= (uint)map.GetLength(1))
                return 0;
            return map[cell.X, cell.Y];
        }

        // Bins mesh vertices into module-grid cells, keeping the most-negative
        // Z (camera-facing surface) per cell. Mapping: a vertex at object-space
        // (vx, vy, vz) sits at world XY (vx+MeshOffset.X, vy+MeshOffset.Y) when
        // the ship is at origin, and ShipModule.LocalCenter recenters world XY
        // on hull.GridCenter at 16 units per cell.
        void BuildSurfaceMap(XnaVector3[] positions)
        {
            if (positions.Length == 0 || Size.X <= 0 || Size.Y <= 0)
                return;

            int w = Size.X, h = Size.Y;
            var map = new float[w, h];
            var back = new float[w, h];
            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                    map[x, y] = back[x, y] = float.NaN;

            for (int i = 0; i < positions.Length; ++i)
            {
                var v = positions[i];
                int gx = (int)Math.Floor((v.X + MeshOffset.X) / 16f) + GridCenter.X;
                int gy = (int)Math.Floor((v.Y + MeshOffset.Y) / 16f) + GridCenter.Y;
                if ((uint)gx >= (uint)w || (uint)gy >= (uint)h)
                    continue;

                float current = map[gx, gy];
                if (float.IsNaN(current) || v.Z < current)
                    map[gx, gy] = v.Z;
                float behind = back[gx, gy];
                if (float.IsNaN(behind) || v.Z > behind)
                    back[gx, gy] = v.Z;
            }

            BackSurfaceMap = back;
            SurfaceMap = map;
        }

        // Most-negative Z under the module's footprint, plus SurfaceLift. Falls
        // back to 0 (play-plane) for empty cells and for hulls whose mesh hasn't
        // loaded yet — SurfaceLift is applied either way for consistency.
        public float SampleSurfaceZ(Point pos, int xSize, int ySize)
        {
            float[,] map = SurfaceMap;
            if (map == null) return SurfaceLift;

            int w = map.GetLength(0), h = map.GetLength(1);
            float best = float.NaN;
            for (int dy = 0; dy < ySize; ++dy)
                for (int dx = 0; dx < xSize; ++dx)
                {
                    int gx = pos.X + dx, gy = pos.Y + dy;
                    if ((uint)gx >= (uint)w || (uint)gy >= (uint)h) continue;
                    float z = map[gx, gy];
                    if (!float.IsNaN(z) && (float.IsNaN(best) || z < best))
                        best = z;
                }
            return (float.IsNaN(best) ? 0f : best) + SurfaceLift;
        }

        // the same for one cell of the back surface, lifted the other way, as it faces the camera upside down
        public float SampleBackSurfaceZ(Point pos)
        {
            float[,] map = BackSurfaceMap;
            if (map == null) return -SurfaceLift;
            if ((uint)pos.X >= (uint)map.GetLength(0) || (uint)pos.Y >= (uint)map.GetLength(1)) return -SurfaceLift;
            float z = map[pos.X, pos.Y];
            return (float.IsNaN(z) ? 0f : z) - SurfaceLift;
        }

        public ShipHull()
        {
        }

        // LEGACY: convert old ShipData hulls into new .hull
        public ShipHull(LegacyShipData sd)
        {
            HullName = sd.Hull;
            VisibleName = sd.Name;
            ModName = sd.ModName ?? "";
            Style = sd.ShipStyle;
            Size = sd.GridInfo.Size;
            SurfaceArea = sd.GridInfo.SurfaceArea;
            MeshOffset  = sd.GridInfo.MeshOffset;
            IconPath = sd.IconPath;
            ModelPath = sd.ModelPath;

            Role = (RoleName)(int)sd.Role;
            SelectIcon = sd.SelectionGraphic;
            Animated = sd.Animated;

            Thrusters = new ThrusterZone[sd.ThrusterList.Length];
            for (int i = 0; i < sd.ThrusterList.Length; ++i)
            {
                Thrusters[i].Position = sd.ThrusterList[i].Position;
                Thrusters[i].Scale = sd.ThrusterList[i].Scale;
            }

            Vector2 origin = sd.GridInfo.VirtualOrigin;
            var legacyOffset = new Vector2(LegacyShipGridInfo.ModuleSlotOffset);

            HullSlots = new HullSlot[sd.ModuleSlots.Length];
            for (int i = 0; i < sd.ModuleSlots.Length; ++i)
            {
                LegacyModuleSlotData msd = sd.ModuleSlots[i];
                Vector2 pos = (msd.Position - legacyOffset) - origin;
                HullSlots[i] = new HullSlot((int)(pos.X / 16f),
                                            (int)(pos.Y / 16f),
                                            msd.Restrictions);
            }

            Array.Sort(HullSlots, HullSlot.Sorter);

            GridCenter = sd.GridInfo.GridCenter;
        }

        public ShipHull(string filePath) : this(new FileInfo(filePath))
        {
        }

        public ShipHull(FileInfo file)
        {
            Load(file);
        }

        void Load(FileInfo file)
        {
            ModName = "";
            Source = file;
            SourceModifiedTime = File.GetLastWriteTimeUtc(file.FullName);

            // because we allow reloading, all lists need to be reset
            Thrusters = Empty<ThrusterZone>.Array;
            HullSlots = Empty<HullSlot>.Array;

            string[] lines = File.ReadAllLines(file.FullName);
            bool parsingSlots = false;
            var slots = new Array<HullSlot>();
            var comments = new Array<string>();
            int height = 0;

            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i];
                if (line.Length == 0)
                    continue;
                if (line.StartsWith("#"))
                {
                    if (!parsingSlots && line != ThrusterHeader)
                        comments.Add(line);
                    continue;
                }

                if (!parsingSlots)
                {
                    string[] parts = line.Split('=');
                    string val = parts.Length > 1 ? parts[1].Trim() : "";
                    switch (parts[0])
                    {
                        case "Version":
                            int version = int.Parse(val);
                            // only emit a Warning, because we expect this format to stay stable
                            if (version != Version)
                                Log.Warning($"Hull {file.NameNoExt()} file version={version} does not match current={Version}");
                            break;
                        case "HullName":   HullName = val; break;
                        case "VisibleName": VisibleName = val; break;
                        case "Role":       Enum.TryParse(val, out Role); break;
                        case "ModName":    ModName = val; break;
                        case "Description":Description = val; break;
                        case "Style":      Style = val; break;
                        case "Size":       Size = PointSerializer.FromString(val); break;
                        case "MeshOffset": MeshOffset = Vector2Serializer.FromString(val); break;
                        case "IconPath":   IconPath = val; break;
                        case "ModelPath":  ModelPath = val; break;
                        case "SelectIcon": SelectIcon = val; break;
                        case "Animated":   Animated = (val == "true"); break;
                        case "IsShipyard": IsShipyard = (val == "true"); break;
                        case "IsOrbitalDefense": IsOrbitalDefense = (val == "true"); break;
                        case "Thruster":
                            Vector4 t = Vector4Serializer.FromString(val);
                            AddThruster(pos:new Vector3(t.X, t.Y, t.Z), scale:t.W);
                            break;
                        case "Slots":
                            parsingSlots = true;
                            break;
                    }
                }
                else // parsingSlots:
                {
                    string[] cols = line.Split('|');
                    if (cols.Length != Size.X)
                        Log.Error($"Hull {file.NameNoExt()} line {i+1} design columns={cols.Length} does not match defined Size Width={Size.X}");

                    for (int x = 0; x < cols.Length; ++x)
                    {
                        string col = cols[x];
                        if (col != "___")
                        {
                            if (col.IndexOf('C') != -1) // grid center marker
                            {
                                GridCenter = new Point(x, height);
                                if (col != "C__")
                                {
                                    var r = Restrictions.I;
                                    if      (col == "IOC") r = Restrictions.IO;
                                    else if (col == "OC ") r = Restrictions.O;
                                    else if (col == "EC ") r = Restrictions.E;
                                    else if (col == "IEC") r = Restrictions.IE;
                                    else if (col == "OEC") r = Restrictions.OE;
                                    slots.Add(new HullSlot(x, height, r));
                                }
                            }
                            else if (Enum.TryParse(col.Trim(), out Restrictions r))
                            {
                                slots.Add(new HullSlot(x, height, r));
                            }
                        }
                    }
                    ++height;
                }
            }

            if (height != Size.Y)
                Log.Error($"Hull {file.NameNoExt()} design rows={height} does not match defined Size Height={Size.Y}");

            if (GridCenter == Point.Zero)
                Log.Error($"Hull {file.NameNoExt()} invalid GridCenter={GridCenter}, is `IC` slot missing?");

            HullSlots = slots.ToArray();
            SurfaceArea = HullSlots.Length;
            Comments = comments.ToArray();
        }

        public void AddThruster(Vector3 pos, float scale)
        {
            Array.Resize(ref Thrusters, Thrusters.Length+1);
            Thrusters[Thrusters.Length-1] = new ThrusterZone{ Position = pos, Scale = scale };
        }

        public void RemoveThruster(int index)
        {
            if (index < Thrusters.Length)
            {
                var thrusters = new Array<ThrusterZone>(Thrusters);
                thrusters.RemoveAt(index);
                Thrusters = thrusters.ToArray();
            }
        }

        public ShipHull GetClone()
        {
            ShipHull hull = (ShipHull)MemberwiseClone();
            hull.HullSlots = HullSlots.CloneArray();
            hull.TechsNeeded = new HashSet<string>(TechsNeeded);
            hull.Thrusters = Thrusters.CloneArray();
            return hull;
        }

        // the ship's center line runs along the left edge of the GridCenter column
        public Point MirroredSlotPos(Point pos) => new(2 * GridCenter.X - 1 - pos.X, pos.Y);

        // Sets hull slots of this design and recalculates grid size
        public void SetHullSlots(Array<HullSlot> slots)
        {
            HullSlots = slots.ToArray();
            var (newSize, newTL) = ShipGridInfo.GetEditedHullInfo(HullSlots);

            Size = newSize;
            SurfaceArea = HullSlots.Length;

            if (newTL != Point.Zero)
            {
                GridCenter = GridCenter.Sub(newTL);

                for (int i = 0; i < HullSlots.Length; ++i)
                {
                    HullSlot slot = HullSlots[i];
                    HullSlots[i] = new HullSlot(slot.Pos.X - newTL.X, slot.Pos.Y - newTL.Y, slot.R);
                }
            }

            // validate to prevent regression
            foreach (HullSlot slot in HullSlots)
            {
                if (slot.Pos.X < 0 || (slot.Pos.X+1) > Size.X)
                    Log.Error($"HullEdit X pos out of range! X:{slot.Pos.X} Size.X:{Size.X}");
                if (slot.Pos.Y < 0 || (slot.Pos.Y+1) > Size.Y)
                    Log.Error($"HullEdit Y pos out of range! Y:{slot.Pos.Y} Size.Y:{Size.Y}");
            }

            Array.Sort(HullSlots, HullSlot.Sorter);
        }

        // rounding a small negative editor position gives -0, which would be written as "-0"
        static float NoNegativeZero(float value) => value == 0f ? 0f : value;

        public void Save(string filePath)
        {
            Save(new FileInfo(filePath));
        }

        public void Save(FileInfo file)
        {
            var sw = new ShipDesignWriter();
            sw.Write("Version", Version);
            sw.Write("HullName", HullName);
            sw.Write("VisibleName", VisibleName);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", Style);
            sw.Write("Description", Description);
            sw.Write("Size", Size.X+","+Size.Y);
            // GridCenter is saved as IOC / IC slot
            sw.Write("MeshOffset", $"{NoNegativeZero(MeshOffset.X)},{NoNegativeZero(MeshOffset.Y)}");
            sw.Write("IconPath", IconPath);
            sw.Write("ModelPath", ModelPath);
            sw.Write("SelectIcon", SelectIcon);

            sw.Write("Animated", Animated);
            sw.Write("IsShipyard", IsShipyard);
            sw.Write("IsOrbitalDefense", IsOrbitalDefense);

            foreach (string comment in Comments)
                sw.WriteLine(comment);

            sw.WriteLine(ThrusterHeader);
            foreach (ThrusterZone t in Thrusters)
                sw.Write("Thruster", $"{NoNegativeZero(t.Position.X)},{NoNegativeZero(t.Position.Y)},{NoNegativeZero(t.Position.Z)},{t.Scale}");

            sw.WriteLine("Slots");
            var gridInfo = new ShipGridInfo { Size = Size };
            var grid = new ModuleGrid<HullSlot>(gridInfo, HullSlots);

            var sb = new StringBuilder();
            for (int y = 0; y < grid.Height; ++y)
            {
                // For Hulls, all slots are 1x1, there are no coordinates, the layout is described
                // by lines and columns, each column is 3 characters wide and separated by a |
                // ___|O  |___
                // IO |E  |IO 
                for (int x = 0; x < grid.Width; ++x)
                {
                    HullSlot slot = grid[x, y];
                    if (slot == null)
                    {
                        if (GridCenter.X == x && GridCenter.Y == y)
                            sb.Append("C__"); // GridCenter in an empty slot (hole in center of ship)
                        else
                            sb.Append("___");
                    }
                    else
                    {
                        string r;
                        if (slot.Pos == GridCenter) // GridCenter ontop of an existing slot
                        {
                            if      (slot.R == Restrictions.IO) r = "IOC";
                            else if (slot.R == Restrictions.O)  r = "OC ";
                            else if (slot.R == Restrictions.E)  r = "EC ";
                            else if (slot.R == Restrictions.IE) r = "IEC";
                            else if (slot.R == Restrictions.OE) r = "OEC";
                            else                                r = "IC ";
                        }
                        else
                        {
                            r = slot.R.ToString();
                        }

                        sb.Append(r);
                        if (3 - r.Length > 0)
                            sb.Append(' ', 3 - r.Length);
                    }
                    if (x != (grid.Width-1))
                        sb.Append('|');
                }
                sw.WriteLine(sb.ToString());
                sb.Clear();
            }

            file.Directory?.Create();
            sw.FlushToFile(file);
            Log.Info($"Saved '{HullName}' to {file.FullName}");

            Source = file;
            SourceModifiedTime = File.GetLastWriteTimeUtc(file.FullName);
        }

        public void ReloadIfNeeded()
        {
            if (File.GetLastWriteTimeUtc(Source.FullName) != SourceModifiedTime)
            {
                Load(Source);
            }
        }
    }
}
