using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe
{
    public enum InfluenceStatus
    {
        Neutral,
        Friendly,
        Enemy
    }

    /// <summary>
    /// This caches influence areas from all Empires,
    /// allowing for rapid check of influence sources.
    /// </summary>
    public class InfluenceTree
    {
        readonly float CellSize;
        readonly float WorldOrigin; // TopLeft of the Universe
        readonly int Size; // Width and Height of the grid

        IInfluence[] Grid;
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
            Grid = new IInfluence[Size * Size];
        }

        public struct InfluenceObj
        {
            public GameObject Source; // Ship(projector) or Planet
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

            var inf = new InfluenceObj
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

                    IInfluence cell = GetCellAt(x, y);
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

                    IInfluence cell = GetCellAt(x, y);
                    if (cell != null && cell.Remove(owner, source))
                        Grid[x + y * Size] = null;
                }
            }
        }

        /// <summary>
        /// Gets the Primary influence status under WorldPos for Empire owner.
        /// Owner empire (Friendly) is always the most prioritized influence
        /// Then Friendly influences
        /// And lastly Enemy influences
        /// For neutral influences the result will be null
        /// </summary>
        /// <param name="us">Our empire</param>
        /// <param name="worldPos">World position to check</param>
        public InfluenceStatus GetInfluenceStatus(Empire us, in Vector2 worldPos)
        {
            IInfluence cell = GetCellByWorld(worldPos.X, worldPos.Y);
            if (cell == null) return InfluenceStatus.Neutral;
            return cell.GetInfluenceStatus(us, worldPos);
        }

        public bool IsInInfluenceOf(Empire of, in Vector2 worldPos)
        {
            IInfluence cell = GetCellByWorld(worldPos.X, worldPos.Y);
            if (cell == null) return false;
            return cell.IsInInfluenceOf(of, worldPos);
        }

        /// <summary>
        /// SLOW: Enumerates this position for all influences
        /// </summary>
        public IEnumerable<Empire> GetEmpireInfluences(in Vector2 worldPos)
        {
            IInfluence cell = GetCellByWorld(worldPos.X, worldPos.Y);
            if (cell == null)
                return Empty<Empire>.Array;
            return cell?.GetEmpireInfluences();
        }

        static InfluenceStatus GetStatus(Empire us, Empire other)
        {
            if (us == other)
                return InfluenceStatus.Friendly;

            Relationship r = us.GetRelations(other);
            if (r.Treaty_Alliance || r.Treaty_Trade)
                return InfluenceStatus.Friendly;
            if (r.AtWar)
                return InfluenceStatus.Enemy;
            return InfluenceStatus.Neutral;
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

        IInfluence GetCellAt(int x, int y)
        {
            if ((uint)x >= Size || (uint)y >= Size)
                return null;
            return Grid[x + y * Size];
        }

        IInfluence GetCellByWorld(float worldX, float worldY)
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
                    IInfluence cell = GetCellByWorld(x, y);
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
                                  in InfluenceObj inf, Empire owner)
        {
            screen.DrawCircleProjected(inf.Source.Position, inf.Radius, owner.EmpireColor);
            screen.DrawLineProjected(inf.Source.Position, bounds.Center, owner.EmpireColor);
            #if DEBUG
                DrawBounds(screen, inf.Bounds, owner.EmpireColor, 1f);
            #endif
        }
        
        interface IInfluence
        {
            Empire Owner { get; }
            void Insert(Empire owner, in InfluenceObj obj, ref IInfluence inf);
            bool Remove(Empire owner, GameObject source);

            bool InRadius(in Vector2 worldPos);
            bool IsInInfluenceOf(Empire of, in Vector2 worldPos);
            InfluenceStatus GetInfluenceStatus(Empire us, in Vector2 worldPos);
            IEnumerable<Empire> GetEmpireInfluences();

            void Draw(UniverseScreen screen, in AABoundingBox2D bounds);
            void DrawInfluences(UniverseScreen screen, in AABoundingBox2D bounds,
                                ref Vector2 textPos, float rectWidth);
        }

        // only stores a single Influence node
        class OneInfluence : IInfluence
        {
            public Empire Owner { get; }
            InfluenceObj Obj;

            public OneInfluence(Empire owner, in InfluenceObj obj)
            {
                Owner = owner;
                Obj = obj;
            }

            public void Insert(Empire owner, in InfluenceObj obj, ref IInfluence inf)
            {
                // always expand
                if (Owner == owner) inf = new OneEmpireInfluence(Owner, Obj, obj);
                else                inf = new MultiEmpireInfluence(this, owner, obj);
            }

            public bool Remove(Empire owner, GameObject source)
            {
                if (source == Obj.Source)
                {
                    Obj = default;
                    return true;
                }
                return false;
            }

            public bool InRadius(in Vector2 worldPos)
            {
                return worldPos.InRadius(Obj.Source.Position, Obj.Radius);
            }

            public bool IsInInfluenceOf(Empire of, in Vector2 worldPos)
            {
                return Owner == of && InRadius(worldPos);
            }

            public InfluenceStatus GetInfluenceStatus(Empire us, in Vector2 worldPos)
            {
                return InRadius(worldPos) ? GetStatus(us, Owner) : InfluenceStatus.Neutral;
            }

            public IEnumerable<Empire> GetEmpireInfluences()
            {
                yield return Owner;
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
                DrawInfluence(screen, bounds, Obj, Owner);
            }
        }

        // stores multiple influence nodes, but only for a single empire
        class OneEmpireInfluence : IInfluence
        {
            public Empire Owner { get; }
            readonly Array<InfluenceObj> Objects = new();

            public OneEmpireInfluence(Empire owner, in InfluenceObj obj1, in InfluenceObj obj2)
            {
                Owner = owner;
                Objects.Add(obj1);
                Objects.Add(obj2);
            }

            public void Insert(Empire owner, in InfluenceObj obj, ref IInfluence inf)
            {
                if (Owner == owner)
                    Objects.Add(obj);
                else // expand from OneEmpire to MultiEmpire
                    inf = new MultiEmpireInfluence(this, owner, obj);
            }

            public bool Remove(Empire owner, GameObject source)
            {
                int count = Objects.Count;
                InfluenceObj[] objects = Objects.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    ref InfluenceObj obj = ref objects[i];
                    if (obj.Source == source)
                    {
                        Objects.RemoveAtSwapLast(i);
                        break;
                    }
                }
                return Objects.IsEmpty;
            }

            public bool InRadius(in Vector2 worldPos)
            {
                int count = Objects.Count;
                InfluenceObj[] objects = Objects.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    ref InfluenceObj obj = ref objects[i];
                    if (worldPos.InRadius(obj.Source.Position, obj.Radius))
                        return true;
                }
                return false;
            }

            public bool IsInInfluenceOf(Empire of, in Vector2 worldPos)
            {
                return Owner == of && InRadius(worldPos);
            }

            public InfluenceStatus GetInfluenceStatus(Empire us, in Vector2 worldPos)
            {
                return InRadius(worldPos) ? GetStatus(us, Owner) : InfluenceStatus.Neutral;
            }

            public IEnumerable<Empire> GetEmpireInfluences()
            {
                yield return Owner;
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
                DrawText(screen, bounds, ref textPos, $"{Owner.data.ArchetypeName}={Objects.Count}", Owner.EmpireColor);
                foreach (InfluenceObj inf in Objects)
                    DrawInfluence(screen, bounds, inf, Owner);
            }
        }

        // stores influence node(s) for multiple empires
        class MultiEmpireInfluence : IInfluence
        {
            IInfluence[] InfluenceByEmpire = Empty<IInfluence>.Array;
            int NumEmpires;
            public Empire Owner => null;

            public MultiEmpireInfluence(IInfluence other, Empire owner, in InfluenceObj obj)
            {
                SetInfluence(other.Owner, other);
                SetInfluence(owner, new OneInfluence(owner, obj));
            }

            void SetInfluence(Empire e, IInfluence inf)
            {
                int empireIdx = e.Id - 1;
                if (empireIdx >= InfluenceByEmpire.Length)
                    Array.Resize(ref InfluenceByEmpire, e.Universum.NumEmpires);
                InfluenceByEmpire[empireIdx] = inf;
                NumEmpires += inf != null ? +1 : -1;
            }

            bool GetInfluence(Empire e, out IInfluence inf)
            {
                int empireIdx = e.Id - 1;
                if (empireIdx < InfluenceByEmpire.Length)
                {
                    inf = InfluenceByEmpire[empireIdx];
                    return inf != null;
                }
                inf = null;
                return false;
            }

            public void Insert(Empire owner, in InfluenceObj obj, ref IInfluence inf)
            {
                if (!GetInfluence(owner, out IInfluence influences))
                    SetInfluence(owner, new OneInfluence(owner, obj));
                else
                    influences.Insert(owner, obj, ref influences);
            }

            public bool Remove(Empire owner, GameObject source)
            {
                if (GetInfluence(owner, out IInfluence inf) && inf.Remove(owner, source))
                {
                    SetInfluence(owner, null);
                }
                return NumEmpires == 0;
            }

            public bool InRadius(in Vector2 worldPos)
            {
                throw new NotImplementedException();
            }

            public bool IsInInfluenceOf(Empire of, in Vector2 worldPos)
            {
                return GetInfluence(of, out IInfluence inf) && inf.InRadius(worldPos);
            }

            public InfluenceStatus GetInfluenceStatus(Empire us, in Vector2 worldPos)
            {
                // check against our own influence first, Own influence is highest priority
                if (IsInInfluenceOf(us, worldPos))
                    return InfluenceStatus.Friendly;

                bool enemy = false;
                int numEmpires = NumEmpires;

                for (int i = 0; numEmpires > 0 && i < InfluenceByEmpire.Length; ++i)
                {
                    IInfluence inf = InfluenceByEmpire[i];
                    if (inf == null)
                        continue;

                    --numEmpires;

                    Empire other = inf.Owner;
                    if (other != us)
                    {
                        InfluenceStatus status = GetStatus(us, other);

                        // if we have an allied influence, it also takes priority
                        if (status == InfluenceStatus.Friendly && inf.InRadius(worldPos))
                            return InfluenceStatus.Friendly;

                        // we save the enemy marker, if we don't get any allied influences,
                        // then this one will be the last priority
                        if (!enemy && status == InfluenceStatus.Enemy && inf.InRadius(worldPos))
                            enemy = true;
                    }
                }

                // if no enemy, then we have neutral influence
                return enemy ? InfluenceStatus.Enemy : InfluenceStatus.Neutral;
            }

            public IEnumerable<Empire> GetEmpireInfluences()
            {
                int numEmpires = NumEmpires;
                for (int i = 0; numEmpires > 0 && i < InfluenceByEmpire.Length; ++i)
                {
                    IInfluence influence = InfluenceByEmpire[i];
                    if (influence != null)
                    {
                        --numEmpires;
                        yield return influence.Owner;
                    }
                }
            }

            public void Draw(UniverseScreen screen, in AABoundingBox2D bounds)
            {
                Vector2 textPos = DefaultTextPos(bounds);
                DrawText(screen, bounds, ref textPos, $"Empires={NumEmpires}", YellowBright);
                DrawInfluences(screen, bounds, ref textPos, rectWidth:1f);
            }

            public void DrawInfluences(UniverseScreen screen, in AABoundingBox2D bounds,
                                       ref Vector2 textPos, float rectWidth)
            {
                int numEmpires = NumEmpires;
                if (numEmpires == 0)
                {
                    DrawBounds(screen, bounds, Brown, rectWidth);
                    return;
                }

                for (int i = 0; numEmpires > 0 && i < InfluenceByEmpire.Length; ++i)
                {
                    IInfluence influence = InfluenceByEmpire[i];
                    if (influence == null)
                        continue;
                    --numEmpires;
                    influence.DrawInfluences(screen, bounds, ref textPos, numEmpires * 2f + 2f);
                }
            }
        }
    }
}
