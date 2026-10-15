using System;

namespace MyPuzzle.Core
{
    public readonly struct GridPoint : IEquatable<GridPoint>
    {
        public GridPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public bool Equals(GridPoint other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is GridPoint other && Equals(other);

        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();

        public static bool operator ==(GridPoint left, GridPoint right) => left.Equals(right);

        public static bool operator !=(GridPoint left, GridPoint right) => !left.Equals(right);
    }

    public readonly struct CellOutline
    {
        public CellOutline(
            CellId cell,
            EdgeKind north,
            EdgeKind east,
            EdgeKind south,
            EdgeKind west,
            GridPoint northWest,
            GridPoint northEast,
            GridPoint southEast,
            GridPoint southWest)
        {
            Cell = cell;
            North = north;
            East = east;
            South = south;
            West = west;
            NorthWest = northWest;
            NorthEast = northEast;
            SouthEast = southEast;
            SouthWest = southWest;
        }

        public CellId Cell { get; }

        public EdgeKind North { get; }

        public EdgeKind East { get; }

        public EdgeKind South { get; }

        public EdgeKind West { get; }

        public GridPoint NorthWest { get; }

        public GridPoint NorthEast { get; }

        public GridPoint SouthEast { get; }

        public GridPoint SouthWest { get; }

        public EdgeKind Side(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return North;
                case Direction.East:
                    return East;
                case Direction.South:
                    return South;
                case Direction.West:
                    return West;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        public EdgeKind SideAt(int index)
        {
            switch (index)
            {
                case 0:
                    return North;
                case 1:
                    return East;
                case 2:
                    return South;
                case 3:
                    return West;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public GridPoint CornerAt(int index)
        {
            switch (index)
            {
                case 0:
                    return NorthWest;
                case 1:
                    return NorthEast;
                case 2:
                    return SouthEast;
                case 3:
                    return SouthWest;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
