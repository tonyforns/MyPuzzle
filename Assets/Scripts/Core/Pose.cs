using System;

namespace MyPuzzle.Core
{
    public readonly struct Pose : IEquatable<Pose>
    {
        public Pose(float x, float y, int quarterTurns)
        {
            X = x;
            Y = y;
            QuarterTurns = quarterTurns;
        }

        public float X { get; }

        public float Y { get; }

        public int QuarterTurns { get; }

        public static Pose AtCellCenter(CellId cell, float cellSize)
        {
            return new Pose((cell.Column + 0.5f) * cellSize, (cell.Row + 0.5f) * cellSize, 0);
        }

        public bool Equals(Pose other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && QuarterTurns == other.QuarterTurns;
        }

        public override bool Equals(object obj) => obj is Pose other && Equals(other);

        public override int GetHashCode() => (X.GetHashCode() * 397) ^ (Y.GetHashCode() * 397) ^ QuarterTurns;

        public static bool operator ==(Pose left, Pose right) => left.Equals(right);

        public static bool operator !=(Pose left, Pose right) => !left.Equals(right);
    }
}
