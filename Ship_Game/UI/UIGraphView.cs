using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class UIGraphView : UIPanel
    {
        public float TimeRange { get; private set; } = 1f;
        public float Min { get; private set; } = 0f;
        public float Max { get; private set; } = 1f;

        // This is the DEFAULT line color, however, it can be overriden in AddTimedSample
        public Color LineColor = Color.Red;

        float CurrentTime;
        public bool ActiveTimeLine;

        struct Sample
        {
            public float Time;
            public float Value;
        }

        class Graph
        {
            public Color Color;
            public bool AddedThisFrame;
            public readonly Array<Sample> Samples = new Array<Sample>();
        }

        readonly Map<Color, Graph> Graphs = new Map<Color, Graph>();

        public UIGraphView()
        {
        }

        public void SetRange(float timeRange, float min=0, float max=1)
        {
            TimeRange = timeRange;
            Min = min;
            Max = max;
        }

        public void Clear()
        {
            ActiveTimeLine = false;
            Graphs.Clear();
        }

        Graph GetOrCreateGraph(Color color)
        {
            if (Graphs.Get(color, out Graph graph))
                return graph;

            graph = new Graph{ Color = color };
            Graphs[color] = graph;
            return graph;
        }

        public void AddFixedSample(float time, float value)
        {
            Graph graph = GetOrCreateGraph(LineColor);
            graph.Samples.Add(new Sample{ Time = time, Value = value });
        }

        public void AddTimedSample(float value)
        {
            AddTimedSample(value, LineColor);
        }

        public void AddTimedSample(float value, Color sampleColor)
        {
            Graph g = GetOrCreateGraph(sampleColor);
            g.AddedThisFrame = true;
            ActiveTimeLine = true;
            g.Samples.Add(new Sample{ Time = CurrentTime, Value = value, });
        }

        public override void Update(float fixedDeltaTime)
        {
            if (ActiveTimeLine)
            {
                CurrentTime += fixedDeltaTime;
                float tooOld = CurrentTime - TimeRange;

                foreach (Graph g in Graphs.Values)
                {
                    for (int i = 0; i < g.Samples.Count; ++i)
                    {
                        // if we encounter a point that is OK, remove all data points before us
                        if (g.Samples[i].Time >= tooOld)
                        {
                            g.Samples.RemoveRange(0, i);
                            break;
                        }
                    }

                    if (!g.AddedThisFrame) // add dummy value
                    {
                        g.Samples.Add(new Sample{ Time = CurrentTime, Value = Min, });
                    }
                    g.AddedThisFrame = false;
                }
            }
            base.Update(fixedDeltaTime);
        }

        Vector2 AbsolutePos(float start, in Sample p, in Rectangle rect)
        {
            float timeOffset = p.Time - start;
            float relX  = timeOffset / TimeRange;
            float value = p.Value.Clamped(Min, Max);
            float relY  = (value - Min) / (Max - Min);

            return new Vector2(rect.RelativeX(relX),
                               rect.RelativeY(1f - relY));
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            Rectangle inner = Rect.Bevel(-15);

            var brown = new Color(Color.SaddleBrown, 150);
            var backBrown = new Color(Color.SaddleBrown, 75);
            batch.DrawRectangle(Rect, brown);  // outer
            batch.DrawRectangle(inner, brown); // inner

            batch.DrawLine(inner.RelPos(0, 0.25f), inner.RelPos(1, 0.25f), backBrown);
            batch.DrawLine(inner.RelPos(0, 0.50f), inner.RelPos(1, 0.50f), backBrown);
            batch.DrawLine(inner.RelPos(0, 0.75f), inner.RelPos(1, 0.75f), backBrown);
            
            batch.DrawLine(inner.RelPos(0.25f, 0), inner.RelPos(0.25f, 1), backBrown);
            batch.DrawLine(inner.RelPos(0.50f, 0), inner.RelPos(0.50f, 1), backBrown);
            batch.DrawLine(inner.RelPos(0.75f, 0), inner.RelPos(0.75f, 1), backBrown);

            foreach (Graph g in Graphs.Values)
            {
                if (g.Samples.Count <= 1)
                    continue;

                Sample first = g.Samples[0];
                Vector2 p0 = AbsolutePos(first.Time, first, inner);
                for (int i = 1; i < g.Samples.Count; ++i)
                {
                    Sample sample = g.Samples[i];
                    Vector2 p1 = AbsolutePos(first.Time, sample, inner);
                    batch.DrawLine(p0, p1, g.Color, 2);
                    p0 = p1;
                }
            }

            const int ticks = 20;
            for (int i = 1; i < ticks; ++i)
            {
                Vector2 a = inner.RelPos(0, i * (1f / ticks));
                Vector2 b = a + new Vector2(6, 0);
                batch.DrawLine(a, b, brown);
            }

            for (int i = 1; i < ticks; ++i)
            {
                Vector2 a = inner.RelPos(i * (1f / ticks), 1);
                Vector2 b = a - new Vector2(0, 6);
                batch.DrawLine(a, b, brown);
            }

            batch.DrawString(Fonts.Arial12, $"Max: {Max}", inner.RelPos(0,0)-new Vector2(0,15), LineColor);
            batch.DrawString(Fonts.Arial12, $"Min: {Min}", inner.RelPos(0,1), LineColor);

            int titleX = inner.CenterX() - Fonts.Arial12.TextWidth(Name)/2;
            batch.DrawString(Fonts.Arial12, Name, new Vector2(titleX, Rect.Y), LineColor);
        }
    }
}
