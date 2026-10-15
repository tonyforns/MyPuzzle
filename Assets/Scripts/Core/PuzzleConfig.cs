using System;

namespace MyPuzzle.Core
{
    public sealed class PuzzleConfig
    {
        public PuzzleConfig(
            string presetName,
            int columns,
            int rows,
            float cellSize,
            float snapThreshold,
            float margin,
            bool startSolved = false)
        {
            if (string.IsNullOrWhiteSpace(presetName))
                throw new ArgumentException("A preset needs a name.", nameof(presetName));
            if (columns < 2)
                throw new ArgumentOutOfRangeException(nameof(columns), "A puzzle needs at least 2 columns.");
            if (rows < 2)
                throw new ArgumentOutOfRangeException(nameof(rows), "A puzzle needs at least 2 rows.");
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            if (snapThreshold < 0f)
                throw new ArgumentOutOfRangeException(nameof(snapThreshold), "Snap threshold cannot be negative.");
            if (margin < 0f)
                throw new ArgumentOutOfRangeException(nameof(margin), "Margin cannot be negative.");

            PresetName = presetName;
            Columns = columns;
            Rows = rows;
            CellSize = cellSize;
            SnapThreshold = snapThreshold;
            Margin = margin;
            StartSolved = startSolved;
        }

        public string PresetName { get; }

        public int Columns { get; }

        public int Rows { get; }

        public float CellSize { get; }

        public float SnapThreshold { get; }

        public float Margin { get; }

        public bool StartSolved { get; }

        public int PieceCount => Columns * Rows;
    }
}
