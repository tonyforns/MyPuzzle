using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class TableLayoutTests
    {
        [Test]
        public void Scatter_SameSeed_SamePositions()
        {
            var definition = Definition(4, 3);
            var frame = TableLayout.AssemblyFrame(definition).Inflated(0.5f);
            var zone = TableLayout.PieceZone(definition, 0.5f);

            var first = TableLayout.Scatter(definition, seed: 1, frame, zone);
            var second = TableLayout.Scatter(definition, seed: 1, frame, zone);

            Assert.That(first.Count, Is.EqualTo(12));
            for (var i = 0; i < first.Count; i++)
                Assert.That(first[i], Is.EqualTo(second[i]));
        }

        [Test]
        public void Scatter_KeepsEveryPieceInsideTheZoneAndOffTheFrame()
        {
            var definition = Definition(4, 3);
            var frame = TableLayout.AssemblyFrame(definition).Inflated(0.5f);
            var zone = TableLayout.PieceZone(definition, 0.5f);
            var poses = TableLayout.Scatter(definition, seed: 1, frame, zone);

            for (var i = 0; i < poses.Count; i++)
            {
                var piece = Piece(poses[i], definition.CellSize);
                Assert.That(zone.Contains(piece), Is.True, "Piece " + i);
                Assert.That(frame.Overlaps(piece), Is.False, "Piece " + i);
            }
        }

        [Test]
        public void Scatter_ExpertGridFitsThePlannedZone()
        {
            var definition = Definition(10, 8);
            var frame = TableLayout.AssemblyFrame(definition).Inflated(0.5f);
            var zone = TableLayout.PieceZone(definition, 0.5f);
            var poses = TableLayout.Scatter(definition, seed: 1, frame, zone);

            Assert.That(poses.Count, Is.EqualTo(80));
            for (var i = 0; i < poses.Count; i++)
            {
                var piece = Piece(poses[i], definition.CellSize);
                Assert.That(zone.Contains(piece), Is.True, "Piece " + i);
                Assert.That(frame.Overlaps(piece), Is.False, "Piece " + i);
            }
        }

        static PuzzleDefinition Definition(int columns, int rows)
        {
            return new PuzzleDefinition(columns, rows, 1f, seed: 1, System.Array.Empty<EdgeRecord>());
        }

        static BoardRect Piece(Pose pose, float cellSize)
        {
            var half = cellSize * 0.5f;
            return new BoardRect(pose.X - half, pose.Y - half, pose.X + half, pose.Y + half);
        }
    }
}
