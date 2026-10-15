using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPuzzle.Core
{
    public sealed class GroupState
    {
        public GroupState(int id, Pose pose, CellId anchor, IReadOnlyList<CellId> members)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));
            if (pose.QuarterTurns is < 0 or > 3)
                throw new ArgumentOutOfRangeException(nameof(pose), "Quarter turns must be 0, 1, 2, or 3.");
            if (members == null || members.Count == 0)
                throw new ArgumentException("A group needs at least one cell.", nameof(members));
            if (!members.Contains(anchor))
                throw new ArgumentException("The anchor must belong to the group.", nameof(anchor));

            Id = id;
            Pose = pose;
            Anchor = anchor;
            Members = members.ToArray();
        }

        public int Id { get; }

        public Pose Pose { get; }

        public CellId Anchor { get; }

        public IReadOnlyList<CellId> Members { get; }
    }
}
