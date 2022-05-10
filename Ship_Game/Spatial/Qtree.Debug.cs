using System;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Spatial
{
    public sealed partial class Qtree
    {
        static readonly Color Brown = new Color(Color.SaddleBrown, 150);
        static readonly Color BrownDim = new Color(89, 39,  5, 150);
        
        // "Allies are Blue, Enemies are Red, what should I do, with our Quadtree?" - RedFox
        static readonly Color VioletDim = new Color(199, 21, 133, 100 );
        static readonly Color VioletBright = new Color(199, 21, 133, 150);
        static readonly Color Purple = new Color(96, 63, 139, 150);
        static readonly Color Yellow = new Color(Color.Yellow, 100);
        static readonly Color YellowBright = new Color(255, 255, 0, 255);
        static readonly Color Blue   = new Color( 95, 158, 160, 200);
        static readonly Color Red    = new Color(255, 80, 80, 200);

        static Map<int, DebugFindNearby> FindNearbyDbg = new Map<int, DebugFindNearby>();

        public unsafe void DebugVisualize(GameScreen screen, VisualizerOptions opt)
        {
            VisualizerOptions o = opt.Enabled ? opt : VisualizerOptions.None;

            AABoundingBox2D visibleWorld = screen.VisibleWorldRect;
            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(Root);
            screen.DrawRectProjected(Root.AABB, Yellow);
            do
            {
                QtreeNode current = buffer.Pop();
                Vector2 center = current.AABB.Center;
                if (o.NodeBounds)
                {
                    Color color = current.LoyaltyCount > 1 ? Brown : BrownDim;
                    screen.DrawRectProjected(current.AABB, color);
                }

                if (current.NW != null) // isBranch
                {
                    if (o.NodeText)
                        screen.DrawStringProjected(center, current.AABB.Width/2, Yellow, "BR");

                    var over = new OverlapsRect(current.AABB, visibleWorld);
                    if (over.NW != 0) buffer.PushBack(current.NW);
                    if (over.NE != 0) buffer.PushBack(current.NE);
                    if (over.SE != 0) buffer.PushBack(current.SE);
                    if (over.SW != 0) buffer.PushBack(current.SW);
                }
                else // isLeaf
                {
                    if (o.NodeText)
                        screen.DrawStringProjected(center, current.AABB.Width/2, Yellow, $"LF n={current.Count}");

                    for (int i = 0; i < current.Count; ++i)
                    {
                        SpatialObj* so = current.Items[i];

                        if (o.ObjectBounds)
                        {
                            Color color = (so->Loyalty % 2 == 0) ? VioletBright : Purple;
                            screen.DrawRectProjected(so->AABB, color);
                        }
                        if (o.ObjectToLeaf)
                        {
                            Color color = (so->Loyalty % 2 == 0) ? VioletDim : Purple;
                            screen.DrawLineProjected(center, so->AABB.Center, color);
                        }
                        if (o.ObjectText)
                        {
                            screen.DrawStringProjected(so->AABB.Center, so->AABB.Width, Blue, $"o={so->ObjectId}");
                        }
                    }
                }
            } while (buffer.NextNode >= 0);

            foreach (var kv in FindNearbyDbg)
            {
                kv.Value.Draw(screen, opt);
            }
        }

        class DebugFindNearby
        {
            public AABoundingBox2D SearchArea;
            public Vector2 FilterOrigin;
            public float RadialFilter;
            public Array<AABoundingBox2D> FindCells = new Array<AABoundingBox2D>();
            public Array<GameObject> SearchResults = new Array<GameObject>();

            public void Draw(GameScreen screen, VisualizerOptions opt)
            {
                if (!SearchArea.IsEmpty)
                    screen.DrawRectProjected(SearchArea, Yellow);

                if (RadialFilter > 0)
                    screen.DrawCircleProjected(FilterOrigin, RadialFilter, Yellow);

                foreach (AABoundingBox2D r in FindCells)
                    screen.DrawRectProjected(r, Blue);

                if (opt.SearchResults)
                {
                    foreach (GameObject go in SearchResults)
                    {
                        screen.DrawRectProjected(new AABoundingBox2D(go), YellowBright);
                    }
                }
            }
        }
    }
}