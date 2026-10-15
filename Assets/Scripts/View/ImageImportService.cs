using System;
using System.IO;
using MyPuzzle.Core;
using UnityEngine;

namespace MyPuzzle.View
{
    public readonly struct ImageImport
    {
        public ImageImport(bool succeeded, Texture2D texture, string message)
        {
            Succeeded = succeeded;
            Texture = texture;
            Message = message;
        }

        public bool Succeeded { get; }

        public Texture2D Texture { get; }

        public string Message { get; }
    }

    public static class ImageImportService
    {
        public static bool HasSupportedExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var extension = Path.GetExtension(path);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        public static ImageImport Open(string path)
        {
            if (!HasSupportedExtension(path))
                return Fail("Elegí un archivo PNG o JPG.");
            if (!File.Exists(path))
                return Fail("No se encontró el archivo.");

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException)
            {
                return Fail("No se pudo leer el archivo.");
            }

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(bytes) || source.width < 2 || source.height < 2)
            {
                UnityEngine.Object.Destroy(source);
                return Fail("No se pudo leer la imagen.");
            }

            source.name = Path.GetFileNameWithoutExtension(path);
            return new ImageImport(true, source, null);
        }

        public static Texture2D FitToBoard(Texture2D source, int columns, int rows, float cellSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Compose(source, columns, rows, cellSize);
        }

        static ImageImport Fail(string message)
        {
            return new ImageImport(false, null, message);
        }

        static Texture2D Compose(Texture2D source, int columns, int rows, float cellSize)
        {
            var fit = ImageFrame.Place(columns, rows, cellSize, source.width, source.height);
            var boardAspect = fit.Board.Width / fit.Board.Height;
            const int longSide = 1024;
            int canvasWidth;
            int canvasHeight;
            if (boardAspect >= 1f)
            {
                canvasWidth = longSide;
                canvasHeight = Math.Max(2, (int)Math.Round(longSide / boardAspect));
            }
            else
            {
                canvasHeight = longSide;
                canvasWidth = Math.Max(2, (int)Math.Round(longSide * boardAspect));
            }

            var sourcePixels = source.GetPixels32();
            var canvas = new Color32[canvasWidth * canvasHeight];
            var margin = new Color32(219, 209, 188, 255);
            for (var i = 0; i < canvas.Length; i++)
                canvas[i] = margin;

            var u0 = fit.Content.MinX / fit.Board.Width;
            var v0 = fit.Content.MinY / fit.Board.Height;
            var u1 = fit.Content.MaxX / fit.Board.Width;
            var v1 = fit.Content.MaxY / fit.Board.Height;
            var xStart = Math.Max(0, (int)Math.Floor(u0 * canvasWidth));
            var xEnd = Math.Min(canvasWidth, (int)Math.Ceiling(u1 * canvasWidth));
            var yStart = Math.Max(0, (int)Math.Floor(v0 * canvasHeight));
            var yEnd = Math.Min(canvasHeight, (int)Math.Ceiling(v1 * canvasHeight));

            for (var y = yStart; y < yEnd; y++)
            {
                var sourceV = (((y + 0.5f) / canvasHeight) - v0) / (v1 - v0);
                for (var x = xStart; x < xEnd; x++)
                {
                    var sourceU = (((x + 0.5f) / canvasWidth) - u0) / (u1 - u0);
                    canvas[(y * canvasWidth) + x] = Sample(sourcePixels, source.width, source.height, sourceU, sourceV);
                }
            }

            var texture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false);
            texture.SetPixels32(canvas);
            texture.Apply(false, false);
            return texture;
        }

        static Color32 Sample(Color32[] pixels, int width, int height, float u, float v)
        {
            u = Math.Clamp(u, 0f, 1f);
            v = Math.Clamp(v, 0f, 1f);
            var x = u * (width - 1);
            var y = v * (height - 1);
            var x0 = (int)Math.Floor(x);
            var y0 = (int)Math.Floor(y);
            var x1 = Math.Min(x0 + 1, width - 1);
            var y1 = Math.Min(y0 + 1, height - 1);
            var tx = x - x0;
            var ty = y - y0;
            var bottom = Lerp(pixels[(y0 * width) + x0], pixels[(y0 * width) + x1], tx);
            var top = Lerp(pixels[(y1 * width) + x0], pixels[(y1 * width) + x1], tx);
            return Lerp(bottom, top, ty);
        }

        static Color32 Lerp(Color32 from, Color32 to, float t)
        {
            byte Channel(byte start, byte end)
            {
                return (byte)(start + ((end - start) * t));
            }

            return new Color32(Channel(from.r, to.r), Channel(from.g, to.g), Channel(from.b, to.b), 255);
        }
    }
}
