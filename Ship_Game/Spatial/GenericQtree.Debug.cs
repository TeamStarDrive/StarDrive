using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.Spatial;

public partial class GenericQtree
{
    static readonly Color Brown = new(Color.SaddleBrown, 150);
    static readonly Color BrownDim = new(89, 39, 5, 150);

    // "Allies are Blue, Enemies are Red, what should I do, with our Quadtree?" - RedFox
    static readonly Color VioletDim = new(199, 21, 133, 100);
    static readonly Color VioletBright = new(199, 21, 133, 150);
    static readonly Color Purple = new(96, 63, 139, 150);
    static readonly Color Yellow = new(Color.Yellow, 100);
    static readonly Color YellowBright = new(255, 255, 0, 255);
    static readonly Color Blue = new(95, 158, 160, 200);
    static readonly Color Red = new(255, 80, 80, 200);
    
    QtreeDebug Debug;
    
    DebugQtreeFind GetFindDebug(in SearchOptions opt)
    {
        if (opt.DebugId == 0)
            return null;
        Debug ??= new();
        return Debug.GetFindDebug(opt);
    }

    public void DebugVisualize(GameScreen screen, VisualizerOptions opt)
    {
        VisualizerOptions o = opt.Enabled ? opt : VisualizerOptions.None;

        AABoundingBox2D visibleWorld = screen.VisibleWorldRect;
        ObjectsState state = State;
        FindResultBuffer<Node> buffer = GetThreadLocalTraversalBuffer(state.Root);
        screen.DrawRectProjected(state.Root.AABB, Yellow, zAxis: opt.ZPlane);
        do
        {
            Node current = buffer.Pop();
            Vector2 center = current.AABB.Center;
            float size = current.AABB.Width.UpperBound(10_000);
            if (o.NodeBounds)
            {
                Color color = current.LoyaltyCount > 1 ? Brown : BrownDim;
                screen.DrawRectProjected(current.AABB, color, zAxis: opt.ZPlane);
            }

            if (current.NW != null) // isBranch
            {
                if (o.NodeText)
                    screen.DrawStringProjected(center, size / 2, Yellow, "BR");

                buffer.PushOverlappingQuadrants(current, visibleWorld);
            }
            else // isLeaf
            {
                if (o.NodeText)
                    screen.DrawStringProjected(center, size / 2, Yellow, $"LF n={current.Count}");

                for (int i = 0; i < current.Count; ++i)
                {
                    ObjectRef so = current.Items[i];

                    if (o.ObjectBounds)
                    {
                        Color color = (so.Loyalty % 2 == 0) ? VioletBright : Purple;
                        screen.DrawRectProjected(so.AABB, color, zAxis: opt.ZPlane);
                    }

                    if (o.ObjectToLeaf)
                    {
                        Color color = (so.Loyalty % 2 == 0) ? VioletDim : Purple;
                        screen.DrawLineProjected(center, so.AABB.Center, color, zAxis: opt.ZPlane);
                    }

                    if (o.ObjectText)
                    {
                        screen.DrawStringProjected(so.AABB.Center, size, Blue, $"o={so.Source}");
                    }
                }
            }
        } while (buffer.NextNode >= 0);

        Debug?.Draw(screen, opt);
    }
}