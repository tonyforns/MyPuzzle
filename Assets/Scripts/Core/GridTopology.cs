using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    /// <summary>
    /// Rectangular grid. Row 0 is the south edge, so north increases the row and the world Y.
    /// </summary>
    public static class GridTopology
    {
        public static Direction Opposite(Direction direction)
        {
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.East => Direction.West,
                Direction.South => Direction.North,
                Direction.West => Direction.East,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public static bool Contains(int columns, int rows, CellId cell)
        {
            return cell.Column >= 0 && cell.Row >= 0 && cell.Column < columns && cell.Row < rows;
        }

        public static bool TryGetNeighbor(int columns, int rows, CellId cell, Direction direction, out CellId neighbor)
        {
            var column = cell.Column;
            var row = cell.Row;
            switch (direction)
            {
                case Direction.North:
                    row++;
                    break;
                case Direction.East:
                    column++;
                    break;
                case Direction.South:
                    row--;
                    break;
                case Direction.West:
                    column--;
                    break;
                default:
                    neighbor = default;
                    return false;
            }

            neighbor = new CellId(column, row);
            return Contains(columns, rows, neighbor);
        }

        public static bool IsBorder(int columns, int rows, CellId cell, Direction direction)
        {
            return !TryGetNeighbor(columns, rows, cell, direction, out _);
        }

        public static IEnumerable<CellId> AllCells(int columns, int rows)
        {
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                    yield return new CellId(column, row);
            }
        }
    }
}
