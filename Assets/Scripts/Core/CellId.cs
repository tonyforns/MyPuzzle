using System;

namespace MyPuzzle.Core
{
    public readonly struct CellId : IEquatable<CellId>
    {
        public CellId(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public int Column { get; }

        public int Row { get; }

        public int Index(int columns) => Column + (Row * columns);

        public bool Equals(CellId other) => Column == other.Column && Row == other.Row;

        public override bool Equals(object obj) => obj is CellId other && Equals(other);

        public override int GetHashCode() => (Column * 397) ^ Row;

        public static bool operator ==(CellId left, CellId right) => left.Equals(right);

        public static bool operator !=(CellId left, CellId right) => !left.Equals(right);
    }
}
