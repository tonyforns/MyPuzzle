using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public readonly struct EdgeRecord : IEquatable<EdgeRecord>
    {
        public EdgeRecord(EdgeId id, EdgeKind kind)
        {
            Id = id;
            Kind = kind;
        }

        public EdgeId Id { get; }

        public EdgeKind Kind { get; }

        public bool Equals(EdgeRecord other) => Id.Equals(other.Id) && Kind == other.Kind;

        public override bool Equals(object obj) => obj is EdgeRecord other && Equals(other);

        public override int GetHashCode() => (Id.GetHashCode() * 397) ^ (int)Kind;

        public static bool operator ==(EdgeRecord left, EdgeRecord right) => left.Equals(right);

        public static bool operator !=(EdgeRecord left, EdgeRecord right) => !left.Equals(right);
    }

    public sealed class PuzzleDefinition
    {
        public PuzzleDefinition(int columns, int rows, float cellSize, int seed, IReadOnlyList<EdgeRecord> edges)
        {
            if (columns < 2)
                throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 2)
                throw new ArgumentOutOfRangeException(nameof(rows));
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));

            Columns = columns;
            Rows = rows;
            CellSize = cellSize;
            Seed = seed;
            Edges = edges;
        }

        public int Columns { get; }

        public int Rows { get; }

        public float CellSize { get; }

        public int Seed { get; }

        public IReadOnlyList<EdgeRecord> Edges { get; }

        public CellOutline OutlineOf(CellId cell)
        {
            if (!GridTopology.Contains(Columns, Rows, cell))
                throw new ArgumentOutOfRangeException(nameof(cell));

            var x0 = cell.Column * CellSize;
            var y0 = cell.Row * CellSize;
            var x1 = x0 + CellSize;
            var y1 = y0 + CellSize;

            return new CellOutline(
                cell,
                Resolve(cell, Direction.North),
                Resolve(cell, Direction.East),
                Resolve(cell, Direction.South),
                Resolve(cell, Direction.West),
                new GridPoint(x0, y1),
                new GridPoint(x1, y1),
                new GridPoint(x1, y0),
                new GridPoint(x0, y0));
        }

        EdgeKind Resolve(CellId cell, Direction direction)
        {
            if (GridTopology.IsBorder(Columns, Rows, cell, direction))
                return EdgeKind.Edge;

            if (direction == Direction.North || direction == Direction.East)
                return Require(new EdgeId(cell, direction));

            if (!GridTopology.TryGetNeighbor(Columns, Rows, cell, direction, out var neighbor))
                throw new InvalidOperationException("A border side has no connector.");

            var ownedByNeighbor = Require(new EdgeId(neighbor, GridTopology.Opposite(direction)));
            return ownedByNeighbor == EdgeKind.Tab ? EdgeKind.Blank : EdgeKind.Tab;
        }

        EdgeKind Require(EdgeId id)
        {
            for (var i = 0; i < Edges.Count; i++)
            {
                if (!Edges[i].Id.Equals(id))
                    continue;

                var kind = Edges[i].Kind;
                if (kind == EdgeKind.Edge)
                    throw new InvalidOperationException("An internal edge is a tab or a blank.");

                return kind;
            }

            throw new InvalidOperationException("The grid is missing an internal edge.");
        }
    }
}
