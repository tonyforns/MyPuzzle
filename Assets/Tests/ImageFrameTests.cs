using MyPuzzle.Core;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class ImageFrameTests
    {
        [Test]
        public void Place_WidePhoto_KeepsAspectInsideTheBoard()
        {
            var fit = ImageFrame.Place(4, 3, 1f, 1600f, 900f);

            Assert.That(fit.Board.Contains(fit.Content), Is.True);
            Assert.That(fit.Content.Width / fit.Content.Height, Is.EqualTo(16f / 9f).Within(0.0001f));
            Assert.That(fit.Content.MinX, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(fit.Content.MaxX, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(fit.Content.Height, Is.LessThan(fit.Board.Height));
        }

        [Test]
        public void Place_TallPhoto_LeavesSideMargin()
        {
            var fit = ImageFrame.Place(4, 3, 1f, 1000f, 1000f);

            Assert.That(fit.Board.Contains(fit.Content), Is.True);
            Assert.That(fit.Content.Width, Is.EqualTo(fit.Content.Height).Within(0.0001f));
            Assert.That(fit.Content.MinY, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(fit.Content.MaxY, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(fit.Content.MinX, Is.GreaterThan(fit.Board.MinX));
        }
    }
}
