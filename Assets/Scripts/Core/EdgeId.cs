using System;

namespace MyPuzzle.Core
{
    public readonly struct EdgeId : IEquatable<EdgeId>
    {
        public EdgeId(CellId cell, Direction side)
        {
            Cell = cell;
            Side = side;
        }

        public CellId Cell { get; }

        public Direction Side { get; }

        public bool Equals(EdgeId other) => Cell.Equals(other.Cell) && Side == other.Side;

        public override bool Equals(object obj) => obj is EdgeId other && Equals(other);

        public override int GetHashCode() => (Cell.GetHashCode() * 397) ^ (int)Side;

        public static bool operator ==(EdgeId left, EdgeId right) => left.Equals(right);

        public static bool operator !=(EdgeId left, EdgeId right) => !left.Equals(right);
    }
}
