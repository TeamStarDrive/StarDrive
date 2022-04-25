namespace SDGraphics;

public struct Capsule
{
    public Vector2 Start;
    public Vector2 End;
    public float Radius;
    public Vector2 Center => (Start + End) * 0.5f;
    public Capsule(Vector2 start, Vector2 end, float radius)
    {
        Start = start;
        End = end;
        Radius = radius;
    }
    public Capsule(Vector2d start, Vector2d end, double radius)
    {
        Start = start.ToVec2f();
        End = end.ToVec2f();
        Radius = (float)radius;
    }
    public bool HitTest(Vector2 hitPos, float hitRadius)
    {
        return hitPos.RayHitTestCircle(hitRadius, Start, End, Radius);
    }
}