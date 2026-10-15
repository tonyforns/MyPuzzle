using System;
using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class SessionArchiveTests
    {
        [Test]
        public void WriteRead_KeepsTheTable()
        {
            var config = new PuzzleConfig("Medio", 4, 3, 1f, 0.35f, 0.5f);
            var state = PuzzleStateFactory.CreateSolved(PuzzleGenerator.Create(config, seed: 7));
            state = GroupOperations.Merge(state, sourceId: 1, targetId: 0);
            state = GroupOperations.Move(state, groupId: 0, new Pose(8.25f, -1.5f, quarterTurns: 0));
            state = GroupOperations.Rotate(state, groupId: 0, steps: 3);

            var saved = new SavedSession(config, @"C:\fotos\playa.png", state);
            var loaded = SessionArchive.Read(SessionArchive.Write(saved));

            Assert.That(loaded.Config.PresetName, Is.EqualTo("Medio"));
            Assert.That(loaded.Config.Columns, Is.EqualTo(4));
            Assert.That(loaded.Config.Rows, Is.EqualTo(3));
            Assert.That(loaded.ImagePath, Is.EqualTo(@"C:\fotos\playa.png"));
            Assert.That(loaded.State.Phase, Is.EqualTo(SessionPhase.Armando));
            Assert.That(loaded.State.Definition.Seed, Is.EqualTo(7));
            Assert.That(loaded.State.Definition.Edges.Count, Is.EqualTo(state.Definition.Edges.Count));
            Assert.That(loaded.State.Groups.Count, Is.EqualTo(11));

            var group = Find(loaded.State, 0);
            Assert.That(group.Pose.X, Is.EqualTo(8.25f).Within(0.0001f));
            Assert.That(group.Pose.Y, Is.EqualTo(-1.5f).Within(0.0001f));
            Assert.That(group.Pose.QuarterTurns, Is.EqualTo(3));
            Assert.That(group.Members.Count, Is.EqualTo(2));
            Assert.That(group.Anchor, Is.EqualTo(new CellId(0, 0)));
            Assert.That(Find(loaded.State, 5).Pose, Is.EqualTo(Find(state, 5).Pose));
        }

        [Test]
        public void WriteRead_AllowsAnEmptyImagePath()
        {
            var config = new PuzzleConfig("Easy", 4, 3, 1f, 0.35f, 0.5f);
            var state = PuzzleStateFactory.CreateSolved(PuzzleGenerator.Create(config, seed: 1));
            var loaded = SessionArchive.Read(SessionArchive.Write(new SavedSession(config, null, state)));

            Assert.That(loaded.ImagePath, Is.EqualTo(string.Empty));
            Assert.That(loaded.State.Groups.Count, Is.EqualTo(12));
        }

        [Test]
        public void Read_RejectsABrokenFile()
        {
            Assert.Throws<FormatException>(() => SessionArchive.Read("not a puzzle"));
        }

        static GroupState Find(PuzzleState state, int groupId)
        {
            for (var i = 0; i < state.Groups.Count; i++)
            {
                if (state.Groups[i].Id == groupId)
                    return state.Groups[i];
            }

            Assert.Fail("Missing group " + groupId);
            return null;
        }
    }
}
