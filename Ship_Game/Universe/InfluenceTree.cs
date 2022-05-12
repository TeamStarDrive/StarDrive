using System;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe
{
    /// <summary>
    /// This caches influence areas from all Empires,
    /// allowing for rapid check of influence sources.
    /// </summary>
    public class InfluenceTree
    {
        readonly float CellSize;
        readonly float WorldOrigin; // TopLeft of the Universe
        readonly int Size; // Width and Height of the grid

        Cell[] Grid;
        public readonly float WorldSize;

        public InfluenceTree(float universeRadius, float projectorRadius)
        {
            WorldSize = universeRadius * 2f;
            WorldOrigin = -universeRadius;

            CellSize = (float)Math.Floor(projectorRadius * 1.5f * 1000) / 1000;
            Size = (int)Math.Ceiling(WorldSize / CellSize);

            Clear();
        }

        public void Clear()
        {
            Grid = new Cell[Size * Size];
        }

        public struct Influence
        {
            public GameObject Source;
            public float Radius;
        #if DEBUG
            public AABoundingBox2D Bounds;
        #endif
        }

        (AABoundingBox2Di, Influence) GetInfluenceBounds(GameObject source)
        {
            var inf = new Influence { Source = source };

            if (source is Ship projector)
            {
                inf.Radius = projector.Loyalty.GetProjectorRadius();
            #if DEBUG
                inf.Bounds = new AABoundingBox2D(source.Position, inf.Radius);
            #endif
                return (GetCellBounds(source.Position, inf.Radius), inf);
            }

            if (source is Planet planet)
            {
                inf.Radius = planet.GetProjectorRange();

                // for Planets, because they constantly orbit their solar system
                // we set their influence bounds to the center of the system
                // with their orbital radius added to the influence radius
                if (planet.OrbitalRadius == 0f)
                    Log.Error("InfluenceTree: Planet OrbitalRadius was not initialized!");
                float maxInfluenceRadius = planet.OrbitalRadius + inf.Radius;
            #if DEBUG
                inf.Bounds = new AABoundingBox2D(planet.ParentSystem.Position, maxInfluenceRadius);
            #endif
                return (GetCellBounds(planet.ParentSystem.Position, maxInfluenceRadius), inf);
            }

            throw new InvalidOperationException($"Unsupported object: {source}");
        }

        public void Insert(Empire owner, GameObject source)
        {
            (AABoundingBox2Di cb, Influence inf) = GetInfluenceBounds(source);

            for (int y = cb.Y1; y < cb.Y2; ++y)
            {
                for (int x = cb.X1; x < cb.X2; ++x)
                {
                    GetOrCreateCellAt(x, y)?.Insert(owner, inf);
                }
            }
        }

        public void Remove(Empire owner, GameObject source)
        {
            (AABoundingBox2Di cb, _) = GetInfluenceBounds(source);
            
            for (int y = cb.Y1; y < cb.Y2; ++y)
            {
                for (int x = cb.X1; x < cb.X2; ++x)
                {
                    // we don't delete cells after remove,
                    // because there is a potential race condition
                    GetCell(x, y)?.Remove(owner, source);
                }
            }
        }

        /// <summary>
        /// Gets the Primary influence under WorldPos for Empire owner.
        /// Owner empire is always the most prioritized influence
        /// Then Friendly influences
        /// And lastly Enemy influences
        /// For neutral influences the result will be null
        /// </summary>
        /// <param name="owner">Owner empire</param>
        /// <param name="worldPos">World position to check</param>
        public Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos)
        {
            Cell cell = GetCell(worldPos.X, worldPos.Y);
            return cell?.GetPrimaryInfluence(owner, worldPos);
        }

        AABoundingBox2Di GetCellBounds(Vector2 pos, float radius)
        {
            float cellSize = CellSize;
            float offset1 = WorldOrigin + radius;
            float offset2 = WorldOrigin - radius;
            int x = (int)((pos.X - offset1) / cellSize);
            int y = (int)((pos.Y - offset1) / cellSize);
            int x2 = (int)((pos.X - offset2) / cellSize) + 1;
            int y2 = (int)((pos.Y - offset2) / cellSize) + 1;
            return new AABoundingBox2Di(x, y, x2, y2);
        }

        Cell GetCellAt(int x, int y)
        {
            if ((uint)x >= Size || (uint)y >= Size)
                return null;
            return Grid[x + y * Size];
        }

        Cell GetCell(float worldX, float worldY)
        {
            float cellSize = CellSize;
            float offset = WorldOrigin;
            int x = (int)((worldX - offset) / cellSize);
            int y = (int)((worldY - offset) / cellSize);
            return GetCellAt(x, y);
        }

        Cell GetOrCreateCellAt(int x, int y)
        {
            if ((uint)x >= Size || (uint)y >= Size)
                return null;

            int cellIdx = x + y * Size;
            Cell cell = Grid[cellIdx];
            if (cell == null)
            {
                Grid[cellIdx] = cell = new Cell();
            }
            return cell;
        }

        public class Cell
        {
            IInfluences Influences;

            public Cell()
            {
            }

            public void Insert(Empire owner, in Influence inf)
            {
                if (Influences == null)
                    Influences = new OneEmpireInfluence(owner);
                else if (!Influences.CanInsert(owner))
                    Influences = new MultiEmpireInfluence(Influences as OneEmpireInfluence);

                Influences.Insert(owner, inf);
            }

            public void Remove(Empire owner, GameObject source)
            {
                if (Influences.Remove(owner, source))
                    Influences = null;
            }

            public Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos)
            {
                return Influences?.GetPrimaryInfluence(owner, worldPos);
            }

            public void DebugVisualize(UniverseScreen screen, in AABoundingBox2D bounds)
            {
                Influences?.Draw(screen, bounds);
            }
        }

        interface IInfluences
        {
            Empire Owner { get; }
            bool IsEmpty { get; }
            bool CanInsert(Empire owner);
            void Insert(Empire owner, in Influence inf);
            bool Remove(Empire owner, GameObject source);
            Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos);
            void Draw(UniverseScreen screen, in AABoundingBox2D bounds);
        }

        class OneEmpireInfluence : IInfluences
        {
            readonly Array<Influence> Influences = new();

            public Empire Owner { get; }
            public bool IsEmpty => Influences.IsEmpty;
            public int Count => Influences.Count;
            public bool CanInsert(Empire owner) => Owner == owner;

            public OneEmpireInfluence(Empire owner)
            {
                Owner = owner;
            }

            public void Insert(Empire owner, in Influence inf)
            {
                Influences.Add(inf);
            }

            public bool Remove(Empire owner, GameObject source)
            {
                int count = Influences.Count;
                Influence[] influencesArr = Influences.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    ref Influence inf = ref influencesArr[i];
                    if (inf.Source == source)
                    {
                        Influences.RemoveAtSwapLast(i);
                        break;
                    }
                }
                return Influences.IsEmpty;
            }

            public Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos)
            {
                if (InRange(worldPos))
                    return Owner;
                return null;
            }

            public bool InRange(in Vector2 worldPos)
            {
                int count = Influences.Count;
                Influence[] influencesArr = Influences.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    ref Influence inf = ref influencesArr[i];
                    if (worldPos.InRadius(inf.Source.Position, inf.Radius))
                        return true;
                }
                return false;
            }

            public void Draw(UniverseScreen screen, in AABoundingBox2D bounds)
            {
                Vector2 textPos = bounds.TopLeft + new Vector2(5000);

                float cellSize = bounds.Width;
                screen.DrawStringProjected(textPos, cellSize*0.15f, Owner.EmpireColor, "One");
                textPos.Y += cellSize*0.15f;

                DrawCell(screen, bounds, textPos, rectWidth:2);
            }

            public void DrawCell(UniverseScreen screen, in AABoundingBox2D bounds, in Vector2 textPos, float rectWidth)
            {
                screen.DrawRectProjected(bounds, Owner.EmpireColor, rectWidth);

                screen.DrawStringProjected(textPos, bounds.Width*0.15f, Owner.EmpireColor,
                                           $"{Owner.Name}\nnodes={Influences.Count}");

                foreach (Influence inf in Influences)
                {
                    screen.DrawCircleProjected(inf.Source.Position, inf.Radius, Owner.EmpireColor);
                #if DEBUG
                    screen.DrawRectProjected(inf.Bounds, Owner.EmpireColor);
                #endif
                }
            }
        }

        class MultiEmpireInfluence : IInfluences
        {
            OneEmpireInfluence[] InfluenceByEmpire = Empty<OneEmpireInfluence>.Array;
            int NumInfluences;
            
            public Empire Owner => null;
            public bool IsEmpty => false;
            public bool CanInsert(Empire owner) => true;

            public MultiEmpireInfluence(OneEmpireInfluence one)
            {
                SetInfluence(one.Owner, one);
            }

            void SetInfluence(Empire e, OneEmpireInfluence influence)
            {
                int empireIdx = e.Id - 1;
                if (empireIdx >= InfluenceByEmpire.Length)
                    Array.Resize(ref InfluenceByEmpire, e.Universum.NumEmpires);

                InfluenceByEmpire[empireIdx] = influence;
                NumInfluences += (influence != null) ? +1 : -1;
            }

            bool GetInfluence(Empire e, out OneEmpireInfluence influence)
            {
                int empireIdx = e.Id - 1;
                if (empireIdx < InfluenceByEmpire.Length)
                {
                    influence = InfluenceByEmpire[empireIdx];
                    return influence != null;
                }
                influence = null;
                return false;
            }

            public void Insert(Empire owner, in Influence inf)
            {
                if (!GetInfluence(owner, out OneEmpireInfluence influences))
                {
                    influences = new OneEmpireInfluence(owner);
                    SetInfluence(owner, influences);
                }
                influences.Insert(owner, inf);
            }

            public bool Remove(Empire owner, GameObject source)
            {
                if (GetInfluence(owner, out OneEmpireInfluence influences))
                {
                    influences.Remove(owner, source);
                    if (influences.IsEmpty)
                    {
                        SetInfluence(owner, null);
                    }
                }
                return NumInfluences == 0;
            }

            public Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos)
            {
                if (GetInfluence(owner, out OneEmpireInfluence ourInfluences))
                {
                    if (ourInfluences.InRange(worldPos))
                        return owner;
                }

                Empire enemy = null;
                int numInfluences = NumInfluences;

                for (int i = 0; numInfluences > 0 && i < InfluenceByEmpire.Length; ++i)
                {
                    OneEmpireInfluence influences = InfluenceByEmpire[i];
                    if (influences != null)
                    {
                        --numInfluences;

                        int empireId = i + 1;
                        if (empireId != owner.Id)
                        {
                            Relationship r = owner.GetRelationsOrNull(empireId);
                            if (r.Treaty_Alliance || r.Treaty_Trade)
                            {
                                if (influences.InRange(worldPos))
                                    return influences.Owner;
                            }
                            if (enemy == null && r.AtWar)
                            {
                                if (influences.InRange(worldPos))
                                    enemy = influences.Owner;
                            }
                        }
                    }
                }
                return enemy;
            }

            public void Draw(UniverseScreen screen, in AABoundingBox2D bounds)
            {
                if (NumInfluences == 0)
                {
                    screen.DrawRectProjected(bounds, Brown, 1);
                    return;
                }

                Vector2 textPos = bounds.TopLeft + new Vector2(5000);
                float cellSize = bounds.Width;
                int numInfluences = NumInfluences;

                screen.DrawStringProjected(textPos, cellSize*0.15f, Brown, $"Empires={NumInfluences}");
                textPos.Y += cellSize*0.15f;

                for (int i = 0; numInfluences > 0 && i < InfluenceByEmpire.Length; ++i)
                {
                    OneEmpireInfluence influences = InfluenceByEmpire[i];
                    if (influences != null)
                    {
                        --numInfluences;
                        influences.DrawCell(screen, bounds, textPos, numInfluences*2f + 2f);
                        textPos.Y += cellSize*0.15f;
                    }
                }
            }
        }

        static readonly Color Brown = new(Color.SaddleBrown, 150);
        static readonly Color Yellow = new(Color.Yellow, 100);

        public void DebugVisualize(UniverseScreen screen)
        {
            var world = new AABoundingBox2D(WorldOrigin, WorldOrigin, -WorldOrigin, -WorldOrigin);
            screen.DrawRectProjected(world, Yellow, 2);

            for (float y = world.Y1; y < world.Y2; y += CellSize)
            {
                for (float x = world.X1; x < world.X2; x += CellSize)
                {
                    Cell cell = GetCell(x, y);
                    if (cell != null)
                    {
                        var bounds = new AABoundingBox2D(x, y, x + CellSize, y + CellSize);
                        cell.DebugVisualize(screen, bounds);
                    }
                }
            }
        }
    }
}
