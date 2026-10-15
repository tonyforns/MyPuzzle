using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class PieceOutlineTests
    {
        [Test]
        public void Build_ClosesAndMirrorsTheNeighbor()
        {
            var definition = PuzzleGenerator.Create(new PuzzleConfig("Easy", 4, 3, 1f, 0.35f, 0.5f), seed: 1);

            foreach (var cell in GridTopology.AllCells(definition.Columns, definition.Rows))
            {
                var outline = definition.OutlineOf(cell);
                var loop = PieceOutline.Build(outline, definition.CellSize);
                Assert.That(loop.Count, Is.GreaterThan(4));
                Assert.That(SignedArea(loop), Is.GreaterThan(0f));
                Assert.That(PolygonTriangulation.Triangulate(loop).Length, Is.EqualTo((loop.Count - 2) * 3));

                var south = PieceOutline.Side(outline, Direction.South, definition.CellSize);
                var west = PieceOutline.Side(outline, Direction.West, definition.CellSize);
                AssertPoint(loop[0], south[0]);
                AssertPoint(west[west.Count - 1], south[0]);

                for (var direction = Direction.North; direction <= Direction.West; direction++)
                {
                    if (GridTopology.IsBorder(definition.Columns, definition.Rows, cell, direction))
                    {
                        Assert.That(outline.Side(direction), Is.EqualTo(EdgeKind.Edge));
                        Assert.That(PieceOutline.Side(outline, direction, definition.CellSize).Count, Is.EqualTo(2));
                        continue;
                    }

                    Assert.That(GridTopology.TryGetNeighbor(
                        definition.Columns,
                        definition.Rows,
                        cell,
                        direction,
                        out var neighbor), Is.True);

                    var mine = PieceOutline.Side(outline, direction, definition.CellSize);
                    var other = PieceOutline.Side(
                        definition.OutlineOf(neighbor),
                        GridTopology.Opposite(direction),
                        definition.CellSize);

                    Assert.That(other.Count, Is.EqualTo(mine.Count));
                    for (var i = 0; i < mine.Count; i++)
                        AssertPoint(mine[i], other[other.Count - 1 - i]);

                    Assert.That(MaxDistanceFromChord(mine), Is.GreaterThan(definition.CellSize * 0.2f));
                }
            }
        }

        static float SignedArea(System.Collections.Generic.IReadOnlyList<GridPoint> loop)
        {
            double sum = 0d;
            for (var i = 0; i < loop.Count; i++)
            {
                var next = loop[(i + 1) % loop.Count];
                sum += (loop[i].X * next.Y) - (next.X * loop[i].Y);
            }

            return (float)(sum * 0.5d);
        }

        static float MaxDistanceFromChord(System.Collections.Generic.IReadOnlyList<GridPoint> side)
        {
            var start = side[0];
            var end = side[side.Count - 1];
            var spanX = end.X - start.X;
            var spanY = end.Y - start.Y;
            var length = (float)System.Math.Sqrt((spanX * spanX) + (spanY * spanY));
            var max = 0f;
            for (var i = 0; i < side.Count; i++)
            {
                var cross = ((side[i].X - start.X) * spanY) - ((side[i].Y - start.Y) * spanX);
                var distance = System.Math.Abs(cross) / length;
                if (distance > max)
                    max = distance;
            }

            return max;
        }

        static void AssertPoint(GridPoint actual, GridPoint expected)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.0001f));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.0001f));
        }
    }
}
