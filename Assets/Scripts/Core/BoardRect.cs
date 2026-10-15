using System;

namespace MyPuzzle.Core
{
    public readonly struct BoardRect : IEquatable<BoardRect>
    {
        public BoardRect(float minX, float minY, float maxX, float maxY)
        {
            if (maxX < minX || maxY < minY)
                throw new ArgumentException("The rectangle is inverted.");

            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public float MinX { get; }

        public float MinY { get; }

        public float MaxX { get; }

        public float MaxY { get; }

        public float Width => MaxX - MinX;

        public float Height => MaxY - MinY;

        public BoardRect Inflated(float amount)
        {
            return new BoardRect(MinX - amount, MinY - amount, MaxX + amount, MaxY + amount);
        }

        public bool Contains(BoardRect other)
        {
            return other.MinX >= MinX && other.MinY >= MinY && other.MaxX <= MaxX && other.MaxY <= MaxY;
        }

        public bool Overlaps(BoardRect other)
        {
            return MinX < other.MaxX && MaxX > other.MinX && MinY < other.MaxY && MaxY > other.MinY;
        }

        public bool Equals(BoardRect other)
        {
            return MinX.Equals(other.MinX) && MinY.Equals(other.MinY) && MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY);
        }

        public override bool Equals(object obj) => obj is BoardRect other && Equals(other);

        public override int GetHashCode()
        {
            return (((MinX.GetHashCode() * 397) ^ MinY.GetHashCode()) * 397 ^ MaxX.GetHashCode()) * 397 ^ MaxY.GetHashCode();
        }
    }
}
