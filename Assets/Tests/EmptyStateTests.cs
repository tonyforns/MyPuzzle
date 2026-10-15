using System.Linq;
using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class EmptyStateTests
    {
        [Test]
        public void CreateEmpty_FourByThree_HasOneGroupPerCellAtRotationZero()
        {
            var config = new PuzzleConfig("Easy", 4, 3, 1f, 0.35f, 0.5f);

            var state = PuzzleStateFactory.CreateEmpty(config, seed: 7);

            Assert.That(state.Phase, Is.EqualTo(SessionPhase.Armando));
            Assert.That(state.Definition.Seed, Is.EqualTo(7));
            Assert.That(state.Definition.Edges, Is.Empty);
            Assert.That(state.Groups, Has.Count.EqualTo(12));

            var cells = state.Groups.Select(group =>
            {
                Assert.That(group.Members.Count, Is.EqualTo(1));
                Assert.That(group.Pose.QuarterTurns, Is.EqualTo(0));
                Assert.That(group.Anchor, Is.EqualTo(group.Members[0]));
                Assert.That(group.Pose.X, Is.EqualTo((group.Anchor.Column + 0.5f) * config.CellSize).Within(0.0001f));
                Assert.That(group.Pose.Y, Is.EqualTo((group.Anchor.Row + 0.5f) * config.CellSize).Within(0.0001f));
                return group.Anchor;
            }).ToArray();

            Assert.That(cells.Distinct().Count(), Is.EqualTo(12));
            Assert.That(cells, Is.EquivalentTo(GridTopology.AllCells(4, 3)));
        }
    }
}
