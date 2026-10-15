using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public static class GroupOperations
    {
        public static PuzzleState Move(PuzzleState state, int groupId, Pose destination)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var found = false;
            var groups = new GroupState[state.Groups.Count];
            for (var i = 0; i < state.Groups.Count; i++)
            {
                var group = state.Groups[i];
                if (group.Id != groupId)
                {
                    groups[i] = group;
                    continue;
                }

                found = true;
                var pose = new Pose(destination.X, destination.Y, group.Pose.QuarterTurns);
                groups[i] = new GroupState(group.Id, pose, group.Anchor, group.Members);
            }

            if (!found)
                throw new ArgumentException("The group does not belong to this puzzle.", nameof(groupId));

            return new PuzzleState(state.Definition, groups, state.Phase);
        }

        public static PuzzleState Rotate(PuzzleState state, int groupId, int steps)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var found = false;
            var groups = new GroupState[state.Groups.Count];
            for (var i = 0; i < state.Groups.Count; i++)
            {
                var group = state.Groups[i];
                if (group.Id != groupId)
                {
                    groups[i] = group;
                    continue;
                }

                found = true;
                var turns = group.Pose.QuarterTurns + steps;
                turns %= 4;
                if (turns < 0)
                    turns += 4;

                groups[i] = new GroupState(
                    group.Id,
                    new Pose(group.Pose.X, group.Pose.Y, turns),
                    group.Anchor,
                    group.Members);
            }

            if (!found)
                throw new ArgumentException("The group does not belong to this puzzle.", nameof(groupId));

            return new PuzzleState(state.Definition, groups, state.Phase);
        }

        public static PuzzleState Merge(PuzzleState state, int sourceId, int targetId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (sourceId == targetId)
                throw new ArgumentException("A group cannot merge with itself.", nameof(sourceId));

            GroupState source = null;
            GroupState target = null;
            for (var i = 0; i < state.Groups.Count; i++)
            {
                var group = state.Groups[i];
                if (group.Id == sourceId)
                    source = group;
                else if (group.Id == targetId)
                    target = group;
            }

            if (source == null)
                throw new ArgumentException("The source group does not belong to this puzzle.", nameof(sourceId));
            if (target == null)
                throw new ArgumentException("The target group does not belong to this puzzle.", nameof(targetId));
            if (source.Pose.QuarterTurns != target.Pose.QuarterTurns)
                throw new InvalidOperationException("Merged groups must share the same quarter turns.");

            var members = new List<CellId>(target.Members.Count + source.Members.Count);
            members.AddRange(target.Members);
            for (var i = 0; i < source.Members.Count; i++)
            {
                if (members.Contains(source.Members[i]))
                    throw new InvalidOperationException("Merged groups cannot share a cell.");

                members.Add(source.Members[i]);
            }

            var merged = new GroupState(target.Id, target.Pose, target.Anchor, members);
            var groups = new List<GroupState>(state.Groups.Count - 1);
            for (var i = 0; i < state.Groups.Count; i++)
            {
                var group = state.Groups[i];
                if (group.Id == sourceId)
                    continue;

                groups.Add(group.Id == targetId ? merged : group);
            }

            return new PuzzleState(state.Definition, groups, state.Phase);
        }

        public static PuzzleState Detach(PuzzleState state, CellId cell)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            GroupState source = null;
            for (var i = 0; i < state.Groups.Count; i++)
            {
                if (Contains(state.Groups[i], cell))
                    source = state.Groups[i];
            }

            if (source == null)
                throw new ArgumentException("The cell does not belong to this puzzle.", nameof(cell));
            if (source.Members.Count == 1)
                return state;

            var columns = state.Definition.Columns;
            var cellSize = state.Definition.CellSize;
            var remaining = new List<CellId>(source.Members.Count - 1);
            for (var i = 0; i < source.Members.Count; i++)
            {
                if (!source.Members[i].Equals(cell))
                    remaining.Add(source.Members[i]);
            }

            GroupState kept;
            if (!source.Anchor.Equals(cell))
                kept = new GroupState(source.Id, source.Pose, source.Anchor, remaining);
            else
            {
                var anchor = Lowest(remaining, columns);
                kept = new GroupState(source.Id, CellWorldPose(source, anchor, cellSize), anchor, remaining);
            }

            var nextId = 0;
            for (var i = 0; i < state.Groups.Count; i++)
                nextId = Math.Max(nextId, state.Groups[i].Id + 1);

            var detached = new GroupState(nextId, CellWorldPose(source, cell, cellSize), cell, new[] { cell });
            var groups = new List<GroupState>(state.Groups.Count + 1);
            for (var i = 0; i < state.Groups.Count; i++)
            {
                var group = state.Groups[i];
                if (group.Id != source.Id)
                {
                    groups.Add(group);
                    continue;
                }

                groups.Add(kept);
                groups.Add(detached);
            }

            var phase = state.Phase == SessionPhase.Completo ? SessionPhase.Armando : state.Phase;
            return new PuzzleState(state.Definition, groups, phase);
        }

        public static Pose CellWorldPose(GroupState group, CellId cell, float cellSize)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (!Contains(group, cell))
                throw new ArgumentException("The cell does not belong to the group.", nameof(cell));

            var offsetX = cell.Column - group.Anchor.Column;
            var offsetY = cell.Row - group.Anchor.Row;
            Rotate(ref offsetX, ref offsetY, group.Pose.QuarterTurns);

            return new Pose(
                group.Pose.X + (offsetX * cellSize),
                group.Pose.Y + (offsetY * cellSize),
                group.Pose.QuarterTurns);
        }

        public static Pose ClampTranslation(
            GroupState group,
            float cellSize,
            Pose desired,
            float viewMinX,
            float viewMinY,
            float viewMaxX,
            float viewMaxY)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (viewMaxX < viewMinX || viewMaxY < viewMinY)
                throw new ArgumentException("The view rectangle is inverted.");

            GetBounds(group, cellSize, out var minX, out var minY, out var maxX, out var maxY);
            var deltaX = ClampDelta(desired.X - group.Pose.X, viewMinX - minX, viewMaxX - maxX);
            var deltaY = ClampDelta(desired.Y - group.Pose.Y, viewMinY - minY, viewMaxY - maxY);
            return new Pose(group.Pose.X + deltaX, group.Pose.Y + deltaY, group.Pose.QuarterTurns);
        }

        static void GetBounds(GroupState group, float cellSize, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = float.PositiveInfinity;
            minY = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            maxY = float.NegativeInfinity;
            var half = cellSize * 0.5f;

            for (var i = 0; i < group.Members.Count; i++)
            {
                var world = CellWorldPose(group, group.Members[i], cellSize);
                minX = Math.Min(minX, world.X - half);
                minY = Math.Min(minY, world.Y - half);
                maxX = Math.Max(maxX, world.X + half);
                maxY = Math.Max(maxY, world.Y + half);
            }
        }

        static float ClampDelta(float delta, float minDelta, float maxDelta)
        {
            if (minDelta > maxDelta)
                return (minDelta + maxDelta) * 0.5f;

            if (delta < minDelta)
                return minDelta;

            if (delta > maxDelta)
                return maxDelta;

            return delta;
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

        static CellId Lowest(List<CellId> cells, int columns)
        {
            var best = cells[0];
            var bestIndex = best.Index(columns);
            for (var i = 1; i < cells.Count; i++)
            {
                var index = cells[i].Index(columns);
                if (index >= bestIndex)
                    continue;

                best = cells[i];
                bestIndex = index;
            }

            return best;
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
