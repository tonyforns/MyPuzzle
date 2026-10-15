using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public static class SnapSystem
    {
        public static PuzzleState Resolve(PuzzleState state, int groupId, float threshold)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (threshold < 0f)
                throw new ArgumentOutOfRangeException(nameof(threshold));

            var released = FindGroup(state, groupId);
            if (released == null)
                throw new ArgumentException("The group does not belong to this puzzle.", nameof(groupId));

            var origin = new CellId[released.Members.Count];
            for (var i = 0; i < origin.Length; i++)
                origin[i] = released.Members[i];

            var activeId = groupId;
            var guard = state.Groups.Count;
            while (guard-- > 0)
            {
                var active = FindGroup(state, activeId);
                if (active == null || !TrySnap(state, active, origin, threshold, out var otherId, out var desired))
                    break;

                state = GroupOperations.Move(state, active.Id, desired);
                state = GroupOperations.Merge(state, active.Id, otherId);
                activeId = otherId;
            }

            if (!IsComplete(state))
                return state;

            return new PuzzleState(state.Definition, state.Groups, SessionPhase.Completo);
        }

        static bool TrySnap(
            PuzzleState state,
            GroupState active,
            CellId[] origin,
            float threshold,
            out int otherId,
            out Pose desired)
        {
            otherId = -1;
            desired = default;
            var bestDistance = 0f;
            var cellSize = state.Definition.CellSize;

            for (var i = 0; i < origin.Length; i++)
            {
                if (!Contains(active, origin[i]))
                    continue;

                for (var direction = Direction.North; direction <= Direction.West; direction++)
                {
                    if (!GridTopology.TryGetNeighbor(
                            state.Definition.Columns,
                            state.Definition.Rows,
                            origin[i],
                            direction,
                            out var neighbor))
                        continue;
                    if (Contains(active, neighbor))
                        continue;

                    var other = FindGroupContaining(state, neighbor);
                    if (other == null || other.Pose.QuarterTurns != active.Pose.QuarterTurns)
                        continue;

                    var pose = AlignedPose(active, other, cellSize);
                    var distance = Distance(active.Pose, pose);
                    if (distance > threshold)
                        continue;
                    if (otherId >= 0 && (distance > bestDistance || (distance == bestDistance && other.Id > otherId)))
                        continue;

                    otherId = other.Id;
                    desired = pose;
                    bestDistance = distance;
                }
            }

            return otherId >= 0;
        }

        static Pose AlignedPose(GroupState moving, GroupState stationary, float cellSize)
        {
            var offsetX = moving.Anchor.Column - stationary.Anchor.Column;
            var offsetY = moving.Anchor.Row - stationary.Anchor.Row;
            Rotate(ref offsetX, ref offsetY, moving.Pose.QuarterTurns);
            return new Pose(
                stationary.Pose.X + (offsetX * cellSize),
                stationary.Pose.Y + (offsetY * cellSize),
                moving.Pose.QuarterTurns);
        }

        static float Distance(Pose current, Pose desired)
        {
            var deltaX = current.X - desired.X;
            var deltaY = current.Y - desired.Y;
            return (float)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        static bool IsComplete(PuzzleState state)
        {
            return state.Groups.Count == 1
                && state.Groups[0].Members.Count == state.Definition.Columns * state.Definition.Rows;
        }

        static GroupState FindGroup(PuzzleState state, int groupId)
        {
            for (var i = 0; i < state.Groups.Count; i++)
            {
                if (state.Groups[i].Id == groupId)
                    return state.Groups[i];
            }

            return null;
        }

        static GroupState FindGroupContaining(PuzzleState state, CellId cell)
        {
            for (var i = 0; i < state.Groups.Count; i++)
            {
                if (Contains(state.Groups[i], cell))
                    return state.Groups[i];
            }

            return null;
        }

        static bool Contains(GroupState group, CellId cell)
        {
            for (var i = 0; i < group.Members.Count; i++)
            {
                if (group.Members[i].Equals(cell))
                    return true;
            }

            return false;
        }

        static void Rotate(ref int x, ref int y, int quarterTurns)
        {
            switch (quarterTurns)
            {
                case 0:
                    return;
                case 1:
                    var turnedX = -y;
                    y = x;
                    x = turnedX;
                    return;
                case 2:
                    x = -x;
                    y = -y;
                    return;
                case 3:
                    var turned = y;
                    y = -x;
                    x = turned;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quarterTurns));
            }
        }
    }
}
