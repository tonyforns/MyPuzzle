using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public static class PieceOutline
    {
        static readonly Direction[] Loop =
        {
            Direction.South,
            Direction.East,
            Direction.North,
            Direction.West
        };

        static readonly float[] CurveAlong;
        static readonly float[] CurveOutward;

        static PieceOutline()
        {
            var leftAlong = new[] { 0f, 0.34f, 0.38f, 0.40f, 0.46f, 0.40f, 0.36f, 0.42f, 0.50f };
            var leftOutward = new[] { 0f, 0f, 0.06f, 0.14f, 0.16f, 0.24f, 0.32f, 0.38f, 0.42f };
            var count = (leftAlong.Length * 2) - 1;
            CurveAlong = new float[count];
            CurveOutward = new float[count];

            for (var i = 0; i < leftAlong.Length; i++)
            {
                CurveAlong[i] = leftAlong[i];
                CurveOutward[i] = leftOutward[i];
            }

            for (var i = leftAlong.Length - 2; i >= 0; i--)
            {
                var index = (leftAlong.Length * 2) - 2 - i;
                CurveAlong[index] = 1f - leftAlong[i];
                CurveOutward[index] = leftOutward[i];
            }
        }

        public static IReadOnlyList<GridPoint> Build(CellOutline cell, float cellSize)
        {
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));

            var points = new List<GridPoint>();
            for (var sideIndex = 0; sideIndex < Loop.Length; sideIndex++)
            {
                var side = Side(cell, Loop[sideIndex], cellSize);
                var start = points.Count == 0 ? 0 : 1;
                var end = sideIndex == Loop.Length - 1 ? side.Count - 1 : side.Count;
                for (var i = start; i < end; i++)
                    points.Add(side[i]);
            }

            return points;
        }

        public static IReadOnlyList<GridPoint> Side(CellOutline cell, Direction direction, float cellSize)
        {
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));

            EdgeFrame(cell, direction, out var start, out var end, out var outwardX, out var outwardY);
            var kind = cell.Side(direction);
            var count = kind == EdgeKind.Edge ? 2 : CurveAlong.Length;
            var points = new GridPoint[count];
            var sign = kind == EdgeKind.Blank ? -1f : kind == EdgeKind.Tab ? 1f : 0f;
            var spanX = end.X - start.X;
            var spanY = end.Y - start.Y;

            for (var i = 0; i < count; i++)
            {
                var along = kind == EdgeKind.Edge ? i : CurveAlong[i];
                var outward = kind == EdgeKind.Edge ? 0f : CurveOutward[i] * sign;
                points[i] = new GridPoint(
                    start.X + (spanX * along) + (outwardX * outward * cellSize),
                    start.Y + (spanY * along) + (outwardY * outward * cellSize));
            }

            return points;
        }

        static void EdgeFrame(
            CellOutline cell,
            Direction direction,
            out GridPoint start,
            out GridPoint end,
            out float outwardX,
            out float outwardY)
        {
            switch (direction)
            {
                case Direction.South:
                    start = cell.SouthWest;
                    end = cell.SouthEast;
                    outwardX = 0f;
                    outwardY = -1f;
                    return;
                case Direction.East:
                    start = cell.SouthEast;
                    end = cell.NorthEast;
                    outwardX = 1f;
                    outwardY = 0f;
                    return;
                case Direction.North:
                    start = cell.NorthEast;
                    end = cell.NorthWest;
                    outwardX = 0f;
                    outwardY = 1f;
                    return;
                case Direction.West:
                    start = cell.NorthWest;
                    end = cell.SouthWest;
                    outwardX = -1f;
                    outwardY = 0f;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
