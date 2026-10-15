using MyPuzzle.View;
using NUnit.Framework;

namespace MyPuzzle.Tests
{
    public sealed class ImageImportServiceTests
    {
        [TestCase("foto.png", true)]
        [TestCase("foto.JPG", true)]
        [TestCase("foto.jpeg", true)]
        [TestCase("foto.gif", false)]
        [TestCase("", false)]
        public void HasSupportedExtension_AcceptsPngAndJpg(string path, bool supported)
        {
            Assert.That(ImageImportService.HasSupportedExtension(path), Is.EqualTo(supported));
        }
    }
}
