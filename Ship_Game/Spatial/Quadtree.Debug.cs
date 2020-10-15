using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed partial class Quadtree
    {
        static readonly Color Brown = new Color(Color.SaddleBrown, 150);
        
        // "Allies are Blue, Enemies are Red, what should I do, with our Quadtree?" - RedFox
        static readonly Color VioletDim = new Color(199, 21, 133, 100 );
        static readonly Color VioletBright = new Color(199, 21, 133, 150);
        static readonly Color Purple = new Color(96, 63, 139, 150);
        static readonly Color Yellow = new Color(Color.Yellow, 100);

        public void DebugVisualize(GameScreen screen)
        {
            AABoundingBox2D visibleWorld = screen.GetVisibleWorldRect();
            SpatialObj[] spatialObjects = SpatialObjects;
            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(Root);
            screen.DrawRectProjected(Root.AABB, Yellow);
            do
            {
                QtreeNode node = buffer.Pop();
                Vector2 center = node.AABB.Center;
                screen.DrawRectProjected(node.AABB, Brown);

                if (node.NW != null) // isBranch
                {
                    var over = new OverlapsRect(node.AABB, visibleWorld);
                    if (over.NW != 0) buffer.PushBack(node.NW);
                    if (over.NE != 0) buffer.PushBack(node.NE);
                    if (over.SE != 0) buffer.PushBack(node.SE);
                    if (over.SW != 0) buffer.PushBack(node.SW);
                }
                else // isLeaf
                {
                    for (int i = 0; i < node.Count; ++i)
                    {
                        int objectId = node.Items[i];
                        if (objectId >= spatialObjects.Length)
                            continue; // hmmmm
                        ref SpatialObj so = ref spatialObjects[objectId];

                        Color color = (so.Loyalty % 2 == 0) ? VioletBright : Purple;
                        screen.DrawRectProjected(so.AABB, color);
                        screen.DrawLineProjected(center, so.AABB.Center, VioletDim);
                    }
                }
            } while (buffer.NextNode >= 0);
        }
    }
}