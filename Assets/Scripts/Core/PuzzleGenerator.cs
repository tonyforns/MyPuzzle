using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public static class PuzzleGenerator
    {
        public static PuzzleDefinition Create(PuzzleConfig config, int seed)
        {
            var random = new Random(seed);
            var edges = new List<EdgeRecord>();

            foreach (var cell in GridTopology.AllCells(config.Columns, config.Rows))
            {
                AddOwnedEdge(config, cell, Direction.North, random, edges);
                AddOwnedEdge(config, cell, Direction.East, random, edges);
            }

            return new PuzzleDefinition(config.Columns, config.Rows, config.CellSize, seed, edges.ToArray());
        }

        static void AddOwnedEdge(PuzzleConfig config, CellId cell, Direction direction, Random random, List<EdgeRecord> edges)
        {
            if (!GridTopology.TryGetNeighbor(config.Columns, config.Rows, cell, direction, out var neighbor))
                return;

            if (cell.Index(config.Columns) >= neighbor.Index(config.Columns))
                throw new InvalidOperationException("An internal edge is stored on the cell with the lower index.");

            var kind = random.Next(2) == 0 ? EdgeKind.Tab : EdgeKind.Blank;
            edges.Add(new EdgeRecord(new EdgeId(cell, direction), kind));
        }
    }
}
