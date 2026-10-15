using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class GroupOperationsTests
    {
        [Test]
        public void Move_ChangesPositionOnly()
        {
            var state = Solved();
            var moved = GroupOperations.Move(state, groupId: 0, new Pose(4f, 2f, quarterTurns: 2));
            var group = Find(moved, 0);

            Assert.That(group.Members.Count, Is.EqualTo(1));
            Assert.That(group.Members[0], Is.EqualTo(new CellId(0, 0)));
            Assert.That(group.Pose.QuarterTurns, Is.EqualTo(0));
            Assert.That(group.Pose.X, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(group.Pose.Y, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(moved.Groups.Count, Is.EqualTo(state.Groups.Count));
            Assert.That(Find(moved, 1).Pose, Is.EqualTo(Find(state, 1).Pose));
        }

        [Test]
        public void Merge_PlacesBothNeighborsOnTheGrid()
        {
            var state = Solved();
            var merged = GroupOperations.Merge(state, sourceId: 1, targetId: 0);

            Assert.That(merged.Groups.Count, Is.EqualTo(11));
            Assert.That(FindOrNull(merged, 1), Is.Null);

            var group = Find(merged, 0);
            Assert.That(group.Anchor, Is.EqualTo(new CellId(0, 0)));
            Assert.That(group.Members.Count, Is.EqualTo(2));
            Assert.That(group.Members, Does.Contain(new CellId(0, 0)));
            Assert.That(group.Members, Does.Contain(new CellId(1, 0)));
            Assert.That(group.Pose.QuarterTurns, Is.EqualTo(0));

            var origin = GroupOperations.CellWorldPose(group, new CellId(0, 0), cellSize: 1f);
            var neighbor = GroupOperations.CellWorldPose(group, new CellId(1, 0), cellSize: 1f);
            Assert.That(origin, Is.EqualTo(Pose.AtCellCenter(new CellId(0, 0), 1f)));
            Assert.That(neighbor, Is.EqualTo(Pose.AtCellCenter(new CellId(1, 0), 1f)));
        }

        [Test]
        public void CellWorldPose_QuarterTurn_RotatesTheOffsetAroundTheAnchor()
        {
            var anchor = new CellId(0, 0);
            var neighbor = new CellId(1, 0);
            var group = new GroupState(
                0,
                new Pose(0.5f, 0.5f, quarterTurns: 1),
                anchor,
                new[] { anchor, neighbor });

            var world = GroupOperations.CellWorldPose(group, neighbor, cellSize: 1f);

            Assert.That(world.X, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(world.Y, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(world.QuarterTurns, Is.EqualTo(1));
        }

        [Test]
        public void ClampTranslation_KeepsThePieceInsideTheView()
        {
            var state = Solved();
            var group = Find(state, 0);
            var viewMinX = 0f;
            var viewMaxX = 5f;
            var viewMinY = 0f;
            var viewMaxY = 4f;

            var pushedRight = GroupOperations.ClampTranslation(
                group, 1f, new Pose(100f, group.Pose.Y, 0), viewMinX, viewMinY, viewMaxX, viewMaxY);
            var pushedLeft = GroupOperations.ClampTranslation(
                group, 1f, new Pose(-10f, group.Pose.Y, 0), viewMinX, viewMinY, viewMaxX, viewMaxY);

            Assert.That(pushedRight.X, Is.EqualTo(4.5f).Within(0.0001f));
            Assert.That(pushedRight.QuarterTurns, Is.EqualTo(0));
            Assert.That(pushedLeft.X, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Rotate_CyclesQuarterTurnsWithoutMoving()
        {
            var state = Solved();
            var origin = Find(state, 0);

            var turned = GroupOperations.Rotate(state, groupId: 0, steps: 1);
            var once = Find(turned, 0);
            Assert.That(once.Pose.QuarterTurns, Is.EqualTo(1));
            Assert.That(once.Pose.X, Is.EqualTo(origin.Pose.X).Within(0.0001f));
            Assert.That(once.Pose.Y, Is.EqualTo(origin.Pose.Y).Within(0.0001f));
            Assert.That(once.Members.Count, Is.EqualTo(1));

            var restored = GroupOperations.Rotate(turned, groupId: 0, steps: -1);
            Assert.That(Find(restored, 0).Pose.QuarterTurns, Is.EqualTo(0));

            var fullTurn = GroupOperations.Rotate(state, groupId: 0, steps: 4);
            Assert.That(Find(fullTurn, 0).Pose.QuarterTurns, Is.EqualTo(0));
        }

        [Test]
        public void Detach_PullsOneCellOutAndKeepsTheRestStill()
        {
            var state = Solved();
            state = GroupOperations.Merge(state, sourceId: 1, targetId: 0);
            state = GroupOperations.Merge(state, sourceId: 2, targetId: 0);
            var before = GroupOperations.CellWorldPose(CellOf(state, new CellId(2, 0)), new CellId(2, 0), 1f);

            var detached = GroupOperations.Detach(state, new CellId(1, 0));

            Assert.That(detached.Groups.Count, Is.EqualTo(state.Groups.Count + 1));
            var kept = Find(detached, 0);
            Assert.That(kept.Members.Count, Is.EqualTo(2));
            Assert.That(kept.Members, Does.Contain(new CellId(0, 0)));
            Assert.That(kept.Members, Does.Contain(new CellId(2, 0)));
            Assert.That(kept.Pose, Is.EqualTo(Find(state, 0).Pose));

            var middle = CellOf(detached, new CellId(1, 0));
            Assert.That(middle.Id, Is.Not.EqualTo(0));
            Assert.That(middle.Members.Count, Is.EqualTo(1));
            Assert.That(GroupOperations.CellWorldPose(middle, new CellId(1, 0), 1f).X, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(GroupOperations.CellWorldPose(kept, new CellId(2, 0), 1f), Is.EqualTo(before));
        }

        [Test]
        public void Detach_SingleCell_LeavesTheGroupAlone()
        {
            var state = Solved();
            var detached = GroupOperations.Detach(state, new CellId(0, 0));

            Assert.That(detached.Groups.Count, Is.EqualTo(state.Groups.Count));
            Assert.That(Find(detached, 0).Members.Count, Is.EqualTo(1));
        }

        static PuzzleState Solved()
        {
            var config = new PuzzleConfig("Easy", 4, 3, 1f, 0.35f, 0.5f);
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

        static GroupState CellOf(PuzzleState state, CellId cell)
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
