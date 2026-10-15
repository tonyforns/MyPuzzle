using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public static class PolygonTriangulation
    {
        public static int[] Triangulate(IReadOnlyList<GridPoint> polygon)
        {
            if (polygon == null)
                throw new ArgumentNullException(nameof(polygon));
            if (polygon.Count < 3)
                throw new ArgumentException("A polygon needs at least three points.", nameof(polygon));

            var remaining = new List<int>(polygon.Count);
            for (var i = 0; i < polygon.Count; i++)
                remaining.Add(i);

            var triangles = new List<int>((polygon.Count - 2) * 3);
            var guard = polygon.Count * polygon.Count;
            while (remaining.Count > 3 && guard-- > 0)
            {
                var clipped = false;
                for (var i = 0; i < remaining.Count; i++)
                {
                    var previous = remaining[(i + remaining.Count - 1) % remaining.Count];
                    var current = remaining[i];
                    var next = remaining[(i + 1) % remaining.Count];
                    if (Cross(polygon[previous], polygon[current], polygon[next]) <= 0.0000001f)
                        continue;
                    if (ContainsAnotherPoint(polygon, remaining, previous, current, next))
                        continue;

                    triangles.Add(previous);
                    triangles.Add(current);
                    triangles.Add(next);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                    throw new InvalidOperationException("The outline could not be triangulated.");
            }

            if (remaining.Count != 3)
                throw new InvalidOperationException("The outline could not be triangulated.");

            triangles.Add(remaining[0]);
            triangles.Add(remaining[1]);
            triangles.Add(remaining[2]);
            return triangles.ToArray();
        }

        public static bool Contains(IReadOnlyList<GridPoint> polygon, float x, float y)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var current = polygon[i];
                var previous = polygon[j];
                var crosses = (current.Y > y) != (previous.Y > y);
                if (!crosses)
                    continue;

                var xAtY = ((previous.X - current.X) * (y - current.Y) / (previous.Y - current.Y)) + current.X;
                if (x < xAtY)
                    inside = !inside;
            }

            return inside;
        }

        static bool ContainsAnotherPoint(IReadOnlyList<GridPoint> polygon, List<int> remaining, int a, int b, int c)
        {
            for (var i = 0; i < remaining.Count; i++)
            {
                var index = remaining[i];
                if (index == a || index == b || index == c)
                    continue;
                if (PointInTriangle(polygon[index], polygon[a], polygon[b], polygon[c]))
                    return true;
            }

            return false;
        }

        static bool PointInTriangle(GridPoint point, GridPoint a, GridPoint b, GridPoint c)
        {
            var ab = Cross(a, b, point);
            var bc = Cross(b, c, point);
            var ca = Cross(c, a, point);
            var hasNegative = ab < -0.0000001f || bc < -0.0000001f || ca < -0.0000001f;
            var hasPositive = ab > 0.0000001f || bc > 0.0000001f || ca > 0.0000001f;
            return !(hasNegative && hasPositive);
        }

        static float Cross(GridPoint a, GridPoint b, GridPoint c)
        {
            return ((b.X - a.X) * (c.Y - b.Y)) - ((b.Y - a.Y) * (c.X - b.X));
        }
    }
}
