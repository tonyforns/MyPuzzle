using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public static class PuzzleStateFactory
    {
        public static PuzzleState CreateEmpty(PuzzleConfig config, int seed)
        {
            var definition = new PuzzleDefinition(
                config.Columns,
                config.Rows,
                config.CellSize,
                seed,
                System.Array.Empty<EdgeRecord>());

            return CreateSolved(definition);
        }

        public static PuzzleState CreateSolved(PuzzleDefinition definition)
        {
            var groups = new List<GroupState>(definition.Columns * definition.Rows);
            foreach (var cell in GridTopology.AllCells(definition.Columns, definition.Rows))
            {
                groups.Add(new GroupState(
                    cell.Index(definition.Columns),
                    Pose.AtCellCenter(cell, definition.CellSize),
                    cell,
                    new[] { cell }));
            }

            return new PuzzleState(definition, groups, SessionPhase.Armando);
        }
    }
}
