using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Bunzora.Smoothing
{
    public class WeightedMovingAverage : ISmoothing
    {
        public List<Vector2> Points { get; set; } = new List<Vector2>();
        public void EndStroke()
        {
            Points.Clear();
        }

        public Vector2 Stroke(Vector2 position, int intensity, int maxHistory)
        {
            Points.Add(position);
            if (Points.Count > maxHistory)
                Points.RemoveAt(0);

            Vector2 sum = Vector2.Zero;
            float totalWeight = 0;

            // Weighted average (recent positions have higher weight)
            for (int i = 0; i < Points.Count; i++)
            {
                float weight = (i + 1) / (float)Points.Count;
                sum += Points[i] * weight;
                totalWeight += weight;
            }

            return sum / totalWeight;
        }
    }

    public class ExponentialMovingAverage : ISmoothing
    {
        public List<Vector2> Points { get; set; } = new List<Vector2>();

        public void EndStroke()
        {
            Points.Clear();
        }

        public Vector2 Stroke(Vector2 position, int intensity, int maxHistory)
        {
            if (Points.Count == 0)
            {
                Points.Add(position);
                return position;
            }

            Vector2 last = Points.Last();
            float alpha = Math.Clamp(1 - (intensity / 100), 0.01f, 0.99f);
            Vector2 smoothed = Vector2.Lerp(last, position, alpha);

            Points.Add(smoothed);
            if (Points.Count > maxHistory)
                Points.RemoveAt(0);

            return smoothed;
        }
    }
}
