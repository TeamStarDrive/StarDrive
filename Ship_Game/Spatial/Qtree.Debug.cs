using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        static readonly Color Blue   = new Color( 95, 158, 160, 200);

        public unsafe void DebugVisualize(GameScreen screen, VisualizerOptions opt)
        {
            VisualizerOptions o = opt.Enabled ? opt : VisualizerOptions.None;

            AABoundingBox2D visibleWorld = screen.GetVisibleWorldRect();
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
        }
    }
}