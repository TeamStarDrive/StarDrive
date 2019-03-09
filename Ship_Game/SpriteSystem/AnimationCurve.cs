using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.SpriteSystem
{
    public class AnimationCurve
    {
        // A finalized curve
        readonly Array<Vector2> Curve  = new Array<Vector2>();
        readonly Array<Vector2> Points = new Array<Vector2>();
        public float Granularity = 0.01f;
        public float AutoCurveRate = 0.66f; // automatic curve control point distance [0.0, 1.0]

        public AnimationCurve(float granularity = 0.01f)
        {
            Granularity = granularity.Clamped(0.001f, 100f);
        }

        public AnimationCurve(IEnumerable<ValueTuple<float, float>> elements)
        {
            foreach ((float x, float y) in elements)
                Points.Add(new Vector2(x, y));
        }

        public AnimationCurve(IEnumerable<Vector2> elements)
        {
            foreach (Vector2 p in elements)
                Points.Add(p);
        }

        public void AddRange(IEnumerable<Vector2> elements)
        {
            Curve.Clear();
            foreach (Vector2 p in elements)
                Points.Add(p);
        }

        public void AddRange(IEnumerable<ValueTuple<float, float>> elements)
        {
            Curve.Clear();
            foreach ((float x, float y) in elements)
                Points.Add(new Vector2(x, y));
        }

        public void Add(float x, float y)
        {
            Curve.Clear();
            Points.Add(new Vector2(x, y));
        }

        public void Add(Vector2 point)
        {
            Curve.Clear();
            Points.Add(point);
        }

        public float GetY(float x)
        {
            if (Curve.IsEmpty)
                CalculateCurve();

            int min = 0;
            int max = Curve.Count - 1;

            // somewhat customize binary-search
            // we are looking for a [min, max] pair,
            // where A[min] < x && x < A[max]
            while ((max - min) > 1)
            {
                int mid = (min + max) >> 1;
                float midX = Curve[mid].X;
                if (midX < x)
                {
                    min = mid + 1;
                    if (min == max) { min = mid; break; } // custom adjust
                }
                else if (midX > x)
                {
                    max = mid - 1;
                    if (max == min) { max = mid; break; } // custom adjust
                }
            }

            Vector2 a = Curve[min];
            Vector2 b = Curve[max];

            float span = (b.X - a.X);
            if (span.AlmostZero()) 
                return a.Y;

            float relPos = (x - a.X) / span;
            return a.Y.LerpTo(b.Y, relPos);
        }

        public void DrawCurveTo(UIGraphView graph, float startX, float endX, float step = 0.05f)
        {
            if (Curve.IsEmpty)
                CalculateCurve();

            for (float x = startX; x <= endX; x += step)
            {
                float y = GetY(x);
                graph.AddFixedSample(x, y);
            }
        }

        void CalculateCurve()
        {
            if (Points.Count < 2)
            {
                Log.Error("Invalid Animation Curve. At least 2 points are required!");
                return;
            }

            for (int i = 1; i < Points.Count; ++i)
            {
                Vector2 p0 = Points[i - 1]; // example: BottomLeft
                Vector2 p3 = Points[i];     // example: TopRight
                var p1 = new Vector2(p0.X.LerpTo(p3.X, AutoCurveRate), p0.Y); // example: BottomCenter
                var p2 = new Vector2(p3.X.LerpTo(p0.X, AutoCurveRate), p3.Y); // example: TopCenter

                float absoluteSpan = p3.X - p0.X;
                int numSteps = Math.Max(2, (int)Math.Round(absoluteSpan / Granularity));
                float step = 1f / numSteps;
                
                for (int j = 0; j < numSteps; ++j)
                {
                    float t = j * step;
                    Curve.Add(CubeBezier(p0, p1, p2, p3, t));
                }
                Curve.Add(CubeBezier(p0, p1, p2, p3, 1f));
            }
        }
        
        static Vector2 CubeBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float r = 1f - t;
            float f0 = r * r * r;
            float f1 = r * r * t * 3;
            float f2 = r * t * t * 3;
            float f3 = t * t * t;
            return f0*p0 + f1*p1 + f2*p2 + f3*p3;
        }

    }
}
