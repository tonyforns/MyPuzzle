using System.Collections.Generic;
using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class SnapSystemTests
    {
        const float Threshold = 0.35f;

        [Test]
        public void Snap_NeighborBeyondThreshold_StaysApart()
        {
            var state = GroupOperations.Move(Solved(), 1, new Pose(2f, 0.5f, 0));

            var resolved = SnapSystem.Resolve(state, groupId: 1, Threshold);

            Assert.That(resolved.Groups.Count, Is.EqualTo(12));
            Assert.That(resolved.Phase, Is.EqualTo(SessionPhase.Armando));
            Assert.That(Find(resolved, 1).Pose.X, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Snap_NeighborWithinThreshold_JoinsAndAligns()
        {
            var state = Solved();
            for (var id = 2; id < 12; id++)
                state = GroupOperations.Move(state, id, new Pose(30f + id, 30f, 0));

            state = GroupOperations.Move(state, 1, new Pose(1.7f, 0.5f, 0));
            var resolved = SnapSystem.Resolve(state, groupId: 1, Threshold);

            Assert.That(resolved.Groups.Count, Is.EqualTo(11));
            Assert.That(FindOrNull(resolved, 1), Is.Null);

            var group = FindContaining(resolved, new CellId(1, 0));
            Assert.That(group.Members, Does.Contain(new CellId(0, 0)));
            Assert.That(group.Members, Does.Contain(new CellId(1, 0)));

            var origin = GroupOperations.CellWorldPose(group, new CellId(0, 0), 1f);
            var neighbor = GroupOperations.CellWorldPose(group, new CellId(1, 0), 1f);
            Assert.That(origin.X, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(origin.Y, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(neighbor.X, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(neighbor.Y, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Snap_CloseCellThatIsNotANeighbor_StaysApart()
        {
            var state = GroupOperations.Move(Solved(), 2, new Pose(1.6f, 0.5f, 0));

            var resolved = SnapSystem.Resolve(state, groupId: 2, Threshold);

            Assert.That(resolved.Groups.Count, Is.EqualTo(12));
            Assert.That(Find(resolved, 2).Pose.X, Is.EqualTo(1.6f).Within(0.0001f));
            Assert.That(Find(resolved, 0).Members.Count, Is.EqualTo(1));
        }

        [Test]
        public void Snap_DifferentQuarterTurns_StayApart()
        {
            var solved = Solved();
            var groups = new List<GroupState>(solved.Groups.Count);
            for (var i = 0; i < solved.Groups.Count; i++)
            {
                var group = solved.Groups[i];
                if (group.Id != 1)
                {
                    groups.Add(group);
                    continue;
                }

                groups.Add(new GroupState(group.Id, new Pose(1.7f, 0.5f, quarterTurns: 1), group.Anchor, group.Members));
            }

            var turned = new PuzzleState(solved.Definition, groups, solved.Phase);
            var resolved = SnapSystem.Resolve(turned, groupId: 1, Threshold);

            Assert.That(resolved.Groups.Count, Is.EqualTo(12));
            Assert.That(Find(resolved, 1).Pose.QuarterTurns, Is.EqualTo(1));
        }

        [Test]
        public void Snap_TwoBlocks_JoinOnTheSharedEdge()
        {
            var state = Solved();
            state = GroupOperations.Merge(state, sourceId: 1, targetId: 0);
            state = GroupOperations.Merge(state, sourceId: 5, targetId: 4);
            foreach (var id in new[] { 2, 3, 6, 7, 8, 9, 10, 11 })
                state = GroupOperations.Move(state, id, new Pose(40f + id, 40f, 0));

            state = GroupOperations.Move(state, 4, new Pose(0.7f, 1.5f, 0));
            var resolved = SnapSystem.Resolve(state, groupId: 4, Threshold);

            Assert.That(FindOrNull(resolved, 4), Is.Null);
            var group = FindContaining(resolved, new CellId(0, 1));
            Assert.That(group.Members.Count, Is.EqualTo(4));
            Assert.That(group.Members, Does.Contain(new CellId(0, 0)));
            Assert.That(group.Members, Does.Contain(new CellId(1, 1)));

            var south = GroupOperations.CellWorldPose(group, new CellId(0, 0), 1f);
            var north = GroupOperations.CellWorldPose(group, new CellId(0, 1), 1f);
            Assert.That(north.X, Is.EqualTo(south.X).Within(0.0001f));
            Assert.That(north.Y, Is.EqualTo(south.Y + 1f).Within(0.0001f));
        }

        [Test]
        public void Snap_LastPiece_CompletesThePuzzle()
        {
            var state = Solved();
            for (var id = 1; id <= 10; id++)
                state = GroupOperations.Merge(state, sourceId: id, targetId: 0);

            state = GroupOperations.Move(state, 11, new Pose(3.7f, 2.5f, 0));
            var resolved = SnapSystem.Resolve(state, groupId: 11, Threshold);

            Assert.That(resolved.Phase, Is.EqualTo(SessionPhase.Completo));
            Assert.That(resolved.Groups.Count, Is.EqualTo(1));
            Assert.That(resolved.Groups[0].Members.Count, Is.EqualTo(12));
        }

        static PuzzleState Solved()
        {
            var config = new PuzzleConfig("Easy", 4, 3, 1f, Threshold, 0.5f);
            return PuzzleStateFactory.CreateEmpty(config, seed: 1);
        }

        static GroupState Find(PuzzleState state, int groupId)
        {
            var group = FindOrNull(state, groupId);
            Assert.That(group, Is.Not.Null);
            return group;
        }

        static GroupState FindOrNull(PuzzleState state, int groupId)
        {
            for (var i = 0; i < state.Groups.Count; i++)
            {
                if (state.Groups[i].Id == groupId)
                    return state.Groups[i];
            }

            return null;
        }

        static GroupState FindContaining(PuzzleState state, CellId cell)
        {
            for (var i = 0; i < state.Groups.Count; i++)
            {
                var group = state.Groups[i];
                for (var member = 0; member < group.Members.Count; member++)
                {
                    if (group.Members[member].Equals(cell))
                        return group;
                }
            }

            Assert.Fail("Missing cell " + cell.Column + "," + cell.Row);
            return null;
        }
    }
}
