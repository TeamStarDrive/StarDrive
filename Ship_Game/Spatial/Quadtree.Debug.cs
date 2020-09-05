using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed partial class Quadtree
    {
        static bool ShouldStoreDebugInfo => Empire.Universe.Debug
                                            && Empire.Universe.DebugWin != null
                                            && Debug.DebugInfoScreen.Mode == Debug.DebugModes.SpatialManager;

        static void AddNearbyDebug(GameplayObject obj, float radius, GameplayObject[] nearby)
        {
            var debug = new FindNearbyDebug {Obj = obj, Radius = radius, Nearby = nearby, Timer = 2f};
            for (int i = 0; i < DebugFindNearby.Count; ++i)
                if (DebugFindNearby[i].Obj == obj)
                {
                    DebugFindNearby[i] = debug;
                    return;
                }

            DebugFindNearby.Add(debug);
        }

        struct FindNearbyDebug
        {
            public GameplayObject Obj;
            public float Radius;
            public GameplayObject[] Nearby;
            public float Timer;
        }

        static readonly Array<FindNearbyDebug> DebugFindNearby = new Array<FindNearbyDebug>();
        static SpatialObj[] DebugDrawBuffer = NoObjects;
        static readonly Color Brown = new Color(Color.SaddleBrown, 150);
        
        // "Allies are Blue, Enemies are Red, what should I do, with our Quadtree?" - RedFox
        static readonly Color Violet = new Color(Color.MediumVioletRed, 100);
        static readonly Color Blue = new Color(Color.CadetBlue, 100);
        static readonly Color Red = new Color(Color.OrangeRed, 100);
        static readonly Color Yellow = new Color(Color.Yellow, 100);

        static void DebugVisualize(GameScreen screen, ref Vector2 topleft, ref Vector2 botright, QtreeNode node)
        {
            var center = new Vector2((node.X + node.LastX) / 2, (node.Y + node.LastY) / 2);
            var size = new Vector2(node.LastX - node.X, node.LastY - node.Y);
            screen.DrawRectangleProjected(center, size, 0f, Brown);

            // @todo This is a hack to reduce concurrency related bugs.
            //       once the main drawing and simulation loops are stable enough, this copying can be removed
            //       In most cases it doesn't matter, because this is only used during DEBUG display...
            int count = node.Count;
            if (DebugDrawBuffer.Length < count) DebugDrawBuffer = new SpatialObj[count];
            Array.Copy(node.Items, DebugDrawBuffer, count);

            for (int i = 0; i < count; ++i)
            {
                ref SpatialObj so = ref DebugDrawBuffer[i];
                var soCenter = new Vector2((so.X + so.LastX) / 2, (so.Y + so.LastY) / 2);
                var soSize = new Vector2(so.LastX - so.X, so.LastY - so.Y);
                screen.DrawRectangleProjected(soCenter, soSize, 0f, Violet);
                screen.DrawCircleProjected(soCenter, so.Radius, Violet);
                screen.DrawLineProjected(center, soCenter, Violet);
            }

            if (node.NW != null)
            {
                if (node.NW.Overlaps(ref topleft, ref botright))
                    DebugVisualize(screen, ref topleft, ref botright, node.NW);
                if (node.NE.Overlaps(ref topleft, ref botright))
                    DebugVisualize(screen, ref topleft, ref botright, node.NE);
                if (node.SE.Overlaps(ref topleft, ref botright))
                    DebugVisualize(screen, ref topleft, ref botright, node.SE);
                if (node.SW.Overlaps(ref topleft, ref botright))
                    DebugVisualize(screen, ref topleft, ref botright, node.SW);
            }
        }

        public void DebugVisualize(GameScreen screen)
        {
            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topLeft = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            Vector2 botRight = screen.UnprojectToWorldPosition(screenSize);
            DebugVisualize(screen, ref topLeft, ref botRight, Root);

            Array.Clear(DebugDrawBuffer, 0, DebugDrawBuffer.Length); // prevent zombie objects

            //for (int i = 0; i < DebugFindNearby.Count; ++i)
            //{
            //    FindNearbyDebug debug = DebugFindNearby[i];
            //    if (debug.Obj == null) continue;
            //    screen.DrawCircleProjected(debug.Obj.Center, debug.Radius, 36, Golden);
            //    for (int j = 0; j < debug.Nearby.Length; ++j)
            //    {
            //        GameplayObject nearby = debug.Nearby[j];
            //        screen.DrawLineProjected(debug.Obj.Center, nearby.Center, GetRelationColor(debug.Obj, nearby));
            //    }

            //    debug.Timer -= screen.SimulationDeltaTime;
            //    if (debug.Timer > 0f)
            //        DebugFindNearby[i] = debug;
            //    else
            //        DebugFindNearby.RemoveAtSwapLast(i--);
            //}
        }

        static Color GetRelationColor(GameplayObject a, GameplayObject b)
        {
            Empire e1 = EmpireManager.GetEmpireById(a.GetLoyaltyId());
            Empire e2 = EmpireManager.GetEmpireById(b.GetLoyaltyId());
            if (e1 != null && e2 != null)
            {
                if (e1 == e2)
                    return Blue;
                if (e1.IsEmpireAttackable(e2)) // hostile?
                    return Red;
                if (e1.TryGetRelations(e2, out Relationship relations) && relations.Treaty_Alliance)
                    return Blue;
            }

            return Yellow; // neutral relation
        }
    }
}