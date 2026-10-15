using System;

namespace MyPuzzle.Core
{
    public static class ImageFrame
    {
        public readonly struct Fit
        {
            public Fit(BoardRect board, BoardRect content)
            {
                Board = board;
                Content = content;
            }

            public BoardRect Board { get; }

            public BoardRect Content { get; }
        }

        public static Fit Place(int columns, int rows, float cellSize, float imageWidth, float imageHeight)
        {
            if (columns < 2)
                throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 2)
                throw new ArgumentOutOfRangeException(nameof(rows));
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (imageWidth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(imageWidth));
            if (imageHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(imageHeight));

            var boardWidth = columns * cellSize;
            var boardHeight = rows * cellSize;
            var imageAspect = imageWidth / imageHeight;
            var boardAspect = boardWidth / boardHeight;
            float contentWidth;
            float contentHeight;
            if (imageAspect >= boardAspect)
            {
                contentWidth = boardWidth;
                contentHeight = boardWidth / imageAspect;
            }
            else
            {
                contentHeight = boardHeight;
                contentWidth = boardHeight * imageAspect;
            }

            var x = (boardWidth - contentWidth) * 0.5f;
            var y = (boardHeight - contentHeight) * 0.5f;
            return new Fit(
                new BoardRect(0f, 0f, boardWidth, boardHeight),
                new BoardRect(x, y, x + contentWidth, y + contentHeight));
        }
    }
}
