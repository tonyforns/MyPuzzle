using System.Collections.Generic;
using System.Linq;
using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class PuzzleGeneratorTests
    {
        static readonly Direction[] Directions =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        [TestCase(4, 3)]
        [TestCase(10, 8)]
        public void Create_AssignsOneOwnerAndComplementaryConnectors(int columns, int rows)
        {
            var definition = Create(columns, rows, seed: 11);
            var expectedEdges = (columns * (rows - 1)) + ((columns - 1) * rows);

            Assert.That(definition.Edges.Count, Is.EqualTo(expectedEdges));
            Assert.That(definition.Seed, Is.EqualTo(11));

            var owners = new HashSet<EdgeId>();
            foreach (var edge in definition.Edges)
            {
                Assert.That(owners.Add(edge.Id), Is.True, "Each internal edge has one owner.");
                Assert.That(edge.Kind, Is.Not.EqualTo(EdgeKind.Edge));
                Assert.That(GridTopology.TryGetNeighbor(columns, rows, edge.Id.Cell, edge.Id.Side, out var neighbor), Is.True);
                Assert.That(edge.Id.Cell.Index(columns), Is.LessThan(neighbor.Index(columns)));
            }

            foreach (var cell in GridTopology.AllCells(columns, rows))
            {
                var outline = definition.OutlineOf(cell);
                AssertRectangle(outline, cell, definition.CellSize);

                for (var index = 0; index < Directions.Length; index++)
                {
                    var direction = Directions[index];
                    Assert.That(outline.SideAt(index), Is.EqualTo(outline.Side(direction)));

                    if (GridTopology.IsBorder(columns, rows, cell, direction))
                    {
                        Assert.That(outline.Side(direction), Is.EqualTo(EdgeKind.Edge));
                        continue;
                    }

                    Assert.That(outline.Side(direction), Is.Not.EqualTo(EdgeKind.Edge));
                    Assert.That(GridTopology.TryGetNeighbor(columns, rows, cell, direction, out var neighbor), Is.True);
                    var neighborSide = definition.OutlineOf(neighbor).Side(GridTopology.Opposite(direction));
                    Assert.That(neighborSide, Is.EqualTo(Complement(outline.Side(direction))));
                }
            }
        }

        [Test]
        public void Create_SameSeed_RepeatsEdges()
        {
            var first = Create(4, 3, seed: 42);
            var second = Create(4, 3, seed: 42);

            Assert.That(second.Edges.Count, Is.EqualTo(first.Edges.Count));
            for (var i = 0; i < first.Edges.Count; i++)
                Assert.That(second.Edges[i], Is.EqualTo(first.Edges[i]));
        }

        [Test]
        public void Create_DifferentSeeds_ChangeAtLeastOneConnector()
        {
            var first = Create(4, 3, seed: 1);
            var second = Create(4, 3, seed: 2);

            Assert.That(second.Edges.Count, Is.EqualTo(first.Edges.Count));
            var changed = false;
            for (var i = 0; i < first.Edges.Count; i++)
            {
                Assert.That(second.Edges[i].Id, Is.EqualTo(first.Edges[i].Id));
                if (second.Edges[i].Kind != first.Edges[i].Kind)
                    changed = true;
            }

            Assert.That(changed, Is.True);
        }

        static PuzzleDefinition Create(int columns, int rows, int seed)
        {
            var config = new PuzzleConfig("Test", columns, rows, 1f, 0.35f, 0.5f);
            return PuzzleGenerator.Create(config, seed);
        }

        static void AssertRectangle(CellOutline outline, CellId cell, float cellSize)
        {
            var x0 = cell.Column * cellSize;
            var y0 = cell.Row * cellSize;
            var x1 = x0 + cellSize;
            var y1 = y0 + cellSize;

            AssertPoint(outline.CornerAt(0), x0, y1);
            AssertPoint(outline.CornerAt(1), x1, y1);
            AssertPoint(outline.CornerAt(2), x1, y0);
            AssertPoint(outline.CornerAt(3), x0, y0);
        }

        static void AssertPoint(GridPoint point, float x, float y)
        {
            Assert.That(point.X, Is.EqualTo(x).Within(0.0001f));
            Assert.That(point.Y, Is.EqualTo(y).Within(0.0001f));
        }

        static EdgeKind Complement(EdgeKind kind)
        {
            return kind == EdgeKind.Tab ? EdgeKind.Blank : EdgeKind.Tab;
        }
    }
}
