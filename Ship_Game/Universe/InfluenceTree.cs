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

        IInfluences[] Grid;
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
            Grid = new IInfluences[Size * Size];
        }

        public struct Influence
        {
            public GameObject Source;
            public float Radius;
        #if DEBUG
            public AABoundingBox2D Bounds;
        #endif
        }

        (Vector2, float, float) GetInfluenceCenterAndMaxRadius(GameObject source)
        {
            if (source is Ship projector)
            {
                float radius = projector.Loyalty.GetProjectorRadius();
                return (source.Position, radius, radius);
            }

            if (source is Planet planet)
            {
                // for Planets, because they constantly orbit their solar system
                // we set their influence bounds to the center of the system
                // with their orbital radius added to the influence radius
                if (planet.OrbitalRadius == 0f)
                    Log.Error("InfluenceTree: Planet OrbitalRadius was not initialized!");

                float radius = planet.GetProjectorRange();
                float maxRadius = planet.OrbitalRadius + radius;
                return (planet.ParentSystem.Position, radius, maxRadius);
            }

            throw new InvalidOperationException($"Unsupported object: {source}");
        }

        public void Insert(Empire owner, GameObject source)
        {
            (Vector2 center, float radius, float maxRadius) = GetInfluenceCenterAndMaxRadius(source);
            AABoundingBox2Di cb = GetCellBounds(center, maxRadius);

            var inf = new Influence
            {
                Source = source,
                Radius = radius,
            #if DEBUG
                Bounds = new AABoundingBox2D(center, maxRadius)
            #endif
            };

            float origin = WorldOrigin;
            float cellSize = CellSize;

            for (int y = cb.Y1; y < cb.Y2; ++y)
            {
                for (int x = cb.X1; x < cb.X2; ++x)
                {
                    float cx = origin + x * cellSize;
                    float cy = origin + y * cellSize;
                    var cellBounds = new AABoundingBox2D(cx, cy, cx + cellSize, cy + cellSize);
                    if (!cellBounds.Overlaps(center.X, center.Y, maxRadius))
                        continue;

                    IInfluences cell = GetCellAt(x, y);
                    if (cell == null)
                        Grid[x + y * Size] = new OneInfluence(owner, inf);
                    else
                        cell.Insert(owner, inf, ref Grid[x + y * Size]);
                }
            }
        }

        public void Remove(Empire owner, GameObject source)
        {
            (Vector2 center, float _, float maxRadius) = GetInfluenceCenterAndMaxRadius(source);
            AABoundingBox2Di cb = GetCellBounds(center, maxRadius);

            float origin = WorldOrigin;
            float cellSize = CellSize;

            for (int y = cb.Y1; y < cb.Y2; ++y)
            {
                for (int x = cb.X1; x < cb.X2; ++x)
                {
                    float cx = origin + x * cellSize;
                    float cy = origin + y * cellSize;
                    var cellBounds = new AABoundingBox2D(cx, cy, cx + cellSize, cy + cellSize);
                    if (!cellBounds.Overlaps(center.X, center.Y, maxRadius))
                        continue;

                    IInfluences cell = GetCellAt(x, y);
                    if (cell != null && cell.Remove(owner, source))
                        Grid[x + y * Size] = null;
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
            IInfluences cell = GetCellByWorld(worldPos.X, worldPos.Y);
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

        IInfluences GetCellAt(int x, int y)
        {
            if ((uint)x >= Size || (uint)y >= Size)
                return null;
            return Grid[x + y * Size];
        }

        IInfluences GetCellByWorld(float worldX, float worldY)
        {
            float cellSize = CellSize;
            float offset = WorldOrigin;
            int x = (int)((worldX - offset) / cellSize);
            int y = (int)((worldY - offset) / cellSize);
            return GetCellAt(x, y);
        }

        static readonly Color Brown = new(Color.SaddleBrown, 150);
        static readonly Color Yellow = new(Color.Yellow, 100);
        static readonly Color YellowBright = new(255, 255, 0, 255);

        public void DebugVisualize(UniverseScreen screen)
        {
            var world = new AABoundingBox2D(WorldOrigin, WorldOrigin, -WorldOrigin, -WorldOrigin);
            screen.DrawRectProjected(world, Yellow, 2);

            for (float y = world.Y1; y < world.Y2; y += CellSize)
            {
                for (float x = world.X1; x < world.X2; x += CellSize)
                {
                    IInfluences cell = GetCellByWorld(x, y);
                    if (cell != null)
                    {
                        var bounds = new AABoundingBox2D(x, y, x + CellSize, y + CellSize);
                        cell.Draw(screen, bounds);
                    }
                }
            }
        }

        static Vector2 DefaultTextPos(in AABoundingBox2D bounds) => bounds.TopLeft + new Vector2(5000);

        static void DrawText(GameScreen screen, in AABoundingBox2D bounds, 
                             ref Vector2 textPos, string text, Color color)
        {
            float cellSize = bounds.Width;
            screen.DrawStringProjected(textPos, cellSize*0.1f, color, text);
            textPos.Y += cellSize*0.1f;
        }

        static void DrawBounds(GameScreen screen, in AABoundingBox2D bounds, Color color, float rectWidth)
        {
            screen.DrawRectProjected(bounds, color, rectWidth);
        }

        static void DrawInfluence(GameScreen screen, in AABoundingBox2D bounds,
                                  in Influence inf, Empire owner)
        {
            screen.DrawCircleProjected(inf.Source.Position, inf.Radius, owner.EmpireColor);
            screen.DrawLineProjected(inf.Source.Position, bounds.Center, owner.EmpireColor);
            #if DEBUG
                DrawBounds(screen, inf.Bounds, owner.EmpireColor, 1f);
            #endif
        }
        
        interface IInfluences
        {
            Empire Owner { get; }
            void Insert(Empire owner, in Influence inf, ref IInfluences influences);
            bool Remove(Empire owner, GameObject source);
            Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos);
            void Draw(UniverseScreen screen, in AABoundingBox2D bounds);
            void DrawInfluences(UniverseScreen screen, in AABoundingBox2D bounds,
                                ref Vector2 textPos, float rectWidth);
        }

        // only stores a single Influence node
        class OneInfluence : IInfluences
        {
            public Empire Owner { get; }
            Influence Influence;

            public OneInfluence(Empire owner, in Influence influence)
            {
                Owner = owner;
                Influence = influence;
            }

            public void Insert(Empire owner, in Influence inf, ref IInfluences influences)
            {
                // always expand
                if (Owner == owner) influences = new OneEmpireInfluence(Owner, Influence, inf);
                else                influences = new MultiEmpireInfluence(this, owner, inf);
            }

            public bool Remove(Empire owner, GameObject source)
            {
                if (source == Influence.Source)
                {
                    Influence = default;
                    return true;
                }
                return false;
            }

            public Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos)
            {
                return worldPos.InRadius(Influence.Source.Position, Influence.Radius) ? Owner : null;
            }

            public void Draw(UniverseScreen screen, in AABoundingBox2D bounds)
            {
                Vector2 textPos = bounds.TopLeft + new Vector2(5000);
                DrawText(screen, bounds, ref textPos, "OneNode", Owner.EmpireColor);
                DrawInfluences(screen, bounds, ref textPos, rectWidth:2f);
            }

            public void DrawInfluences(UniverseScreen screen, in AABoundingBox2D bounds, 
                                       ref Vector2 textPos, float rectWidth)
            {
                DrawBounds(screen, bounds, Owner.EmpireColor, rectWidth);
                DrawText(screen, bounds, ref textPos, $"{Owner.data.ArchetypeName}=1", Owner.EmpireColor);
                DrawInfluence(screen, bounds, Influence, Owner);
            }
        }

        // stores multiple influence nodes, but only for a single empire
        class OneEmpireInfluence : IInfluences
        {
            public Empire Owner { get; }
            readonly Array<Influence> Influences = new();

            public OneEmpireInfluence(Empire owner, in Influence inf1, in Influence inf2)
            {
                Owner = owner;
                Influences.Add(inf1);
                Influences.Add(inf2);
            }

            public void Insert(Empire owner, in Influence inf, ref IInfluences influences)
            {
                if (Owner == owner)
                    Influences.Add(inf);
                else // expand from OneEmpire to MultiEmpire
                    influences = new MultiEmpireInfluence(this, owner, inf);
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
                int count = Influences.Count;
                Influence[] influencesArr = Influences.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    ref Influence inf = ref influencesArr[i];
                    if (worldPos.InRadius(inf.Source.Position, inf.Radius))
                        return Owner;
                }
                return null;
            }

            public void Draw(UniverseScreen screen, in AABoundingBox2D bounds)
            {
                Vector2 textPos = DefaultTextPos(bounds);
                DrawText(screen, bounds, ref textPos, "OneEmpire", Owner.EmpireColor);
                DrawInfluences(screen, bounds, ref textPos, rectWidth:2f);
            }

            public void DrawInfluences(UniverseScreen screen, in AABoundingBox2D bounds,
                                       ref Vector2 textPos, float rectWidth)
            {
                DrawBounds(screen, bounds, Owner.EmpireColor, rectWidth);
                DrawText(screen, bounds, ref textPos, $"{Owner.data.ArchetypeName}={Influences.Count}", Owner.EmpireColor);
                foreach (Influence inf in Influences)
                    DrawInfluence(screen, bounds, inf, Owner);
            }
        }

        // stores influence node(s) for multiple empires
        class MultiEmpireInfluence : IInfluences
        {
            IInfluences[] InfluenceByEmpire = Empty<IInfluences>.Array;
            int NumEmpires;
            public Empire Owner => null;

            public MultiEmpireInfluence(IInfluences other, Empire owner, in Influence inf)
            {
                SetInfluence(other.Owner, other);
                SetInfluence(owner, new OneInfluence(owner, inf));
            }

            void SetInfluence(Empire e, IInfluences influence)
            {
                int empireIdx = e.Id - 1;
                if (empireIdx >= InfluenceByEmpire.Length)
                    Array.Resize(ref InfluenceByEmpire, e.Universum.NumEmpires);
                InfluenceByEmpire[empireIdx] = influence;
                NumEmpires += influence != null ? +1 : -1;
            }

            bool GetInfluence(Empire e, out IInfluences influence)
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

            public void Insert(Empire owner, in Influence inf, ref IInfluences _)
            {
                if (!GetInfluence(owner, out IInfluences influences))
                    SetInfluence(owner, new OneInfluence(owner, inf));
                else
                    influences.Insert(owner, inf, ref influences);
            }

            public bool Remove(Empire owner, GameObject source)
            {
                if (GetInfluence(owner, out IInfluences influences)
                    && influences.Remove(owner, source))
                {
                    SetInfluence(owner, null);
                }
                return NumEmpires == 0;
            }

            public Empire GetPrimaryInfluence(Empire owner, in Vector2 worldPos)
            {
                // check against our own influence first, Own influence is highest priority
                if (GetInfluence(owner, out IInfluences ourInfluences))
                {
                    Empire us = ourInfluences.GetPrimaryInfluence(owner, worldPos);
                    if (us != null) return us;
                }

                Empire enemy = null;
                int numEmpires = NumEmpires;

                for (int i = 0; numEmpires > 0 && i < InfluenceByEmpire.Length; ++i)
                {
                    IInfluences influences = InfluenceByEmpire[i];
                    if (influences == null)
                        continue;

                    --numEmpires;
                    int empireId = i + 1;
                    if (empireId != owner.Id)
                    {
                        Relationship r = owner.GetRelationsOrNull(empireId);

                        // if we have an allied influence, it also takes priority
                        if (r.Treaty_Alliance || r.Treaty_Trade)
                        {
                            Empire friend = influences.GetPrimaryInfluence(owner, worldPos);
                            if (friend != null) return friend;
                        }

                        // we save the enemy marker, if we don't get any allied influences,
                        // then this one will be the last priority
                        if (enemy == null && r.AtWar)
                        {
                            enemy = influences.GetPrimaryInfluence(owner, worldPos);
                        }
                    }
                }

                // if enemy is null, then we have neutral influence
                return enemy;
            }

            public void Draw(UniverseScreen screen, in AABoundingBox2D bounds)
            {
                Vector2 textPos = DefaultTextPos(bounds);
                DrawText(screen, bounds, ref textPos, $"Empires={NumEmpires}", YellowBright);
                DrawInfluences(screen, bounds, ref textPos, rectWidth:1f);
            }

            public void DrawInfluences(UniverseScreen screen, in AABoundingBox2D bounds, ref Vector2 textPos, float rectWidth)
            {
                int numEmpires = NumEmpires;
                if (numEmpires == 0)
                {
                    DrawBounds(screen, bounds, Brown, rectWidth);
                    return;
                }

                for (int i = 0; numEmpires > 0 && i < InfluenceByEmpire.Length; ++i)
                {
                    IInfluences influences = InfluenceByEmpire[i];
                    if (influences == null)
                        continue;
                    --numEmpires;
                    influences.DrawInfluences(screen, bounds, ref textPos, numEmpires * 2f + 2f);
                }
            }
        }
    }
}
