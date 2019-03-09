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

        public Color LineColor = Color.Red;
        float CurrentTime;
        public bool ActiveTimeLine;
        bool AddedThisFrame;

        struct DataPoint
        {
            public float Time;
            public float Value;
        }

        readonly Array<DataPoint> Points = new Array<DataPoint>();

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
            AddedThisFrame = false;
            Points.Clear();
        }

        public void AddFixedSample(float time, float value)
        {
            Points.Add(new DataPoint{ Time = time, Value = value });
        }

        public void AddTimedSample(float value)
        {
            ActiveTimeLine = true;
            AddedThisFrame = true;
            Points.Add(new DataPoint{ Time = CurrentTime, Value = value });
        }

        public override void Update(float deltaTime)
        {
            if (ActiveTimeLine)
            {
                CurrentTime += deltaTime;
                float tooOld = CurrentTime - TimeRange;

                for (int i = 0; i < Points.Count; ++i)
                {
                    // if we encounter a point that is OK, remove all data points before us
                    if (Points[i].Time >= tooOld)
                    {
                        Points.RemoveRange(0, i);
                        break;
                    }
                }

                if (!AddedThisFrame) // add dummy value
                {
                    Points.Add(new DataPoint{ Time = CurrentTime, Value = Min });
                }
                AddedThisFrame = false;
            }
            base.Update(deltaTime);
        }

        Vector2 AbsolutePos(int pointId, in Rectangle rect)
        {
            DataPoint p = Points[pointId];
            float timeOffset = p.Time - Points[0].Time;
            float relX  = timeOffset / TimeRange;
            float value = p.Value.Clamped(Min, Max);
            float relY  = (value - Min) / (Max - Min);

            return new Vector2(rect.RelativeX(relX),
                               rect.RelativeY(1f - relY));
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

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

            if (!Points.IsEmpty)
            {
                Vector2 p0 = AbsolutePos(0, inner);
                for (int i = 1; i < Points.Count; ++i)
                {
                    Vector2 p1 = AbsolutePos(i, inner);
                    batch.DrawLine(p0, p1, LineColor, 2);
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
