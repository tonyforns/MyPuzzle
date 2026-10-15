using System;
using MyPuzzle.Core;
using UnityEngine;

namespace MyPuzzle.View
{
    public static class PieceMeshBuilder
    {
        const float RimFraction = 0.045f;

        public static Mesh Build(CellOutline outline, int columns, int rows, CellId cell)
        {
            if (columns < 2)
                throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 2)
                throw new ArgumentOutOfRangeException(nameof(rows));
            if (!outline.Cell.Equals(cell))
                throw new ArgumentException("The outline does not belong to the requested cell.", nameof(cell));

            var cellWidth = outline.NorthEast.X - outline.NorthWest.X;
            var cellHeight = outline.NorthWest.Y - outline.SouthWest.Y;
            if (cellWidth <= 0f || cellHeight <= 0f)
                throw new ArgumentException("The cell outline is degenerate.", nameof(outline));

            var loop = PieceOutline.Build(outline, cellWidth);
            var centerX = (outline.NorthWest.X + outline.NorthEast.X) * 0.5f;
            var centerY = (outline.SouthWest.Y + outline.NorthWest.Y) * 0.5f;
            var totalWidth = cellWidth * columns;
            var totalHeight = cellHeight * rows;
            var rim = cellHeight * RimFraction;
            var triangles = PolygonTriangulation.Triangulate(loop);

            var count = loop.Count;
            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            for (var i = 0; i < count; i++)
            {
                var localX = loop[i].X - centerX;
                var localY = loop[i].Y - centerY;
                vertices[i] = new Vector3(localX, localY - rim, 0f);
                vertices[i + count] = new Vector3(localX, localY, -0.01f);
                var uv = new Vector2(loop[i].X / totalWidth, loop[i].Y / totalHeight);
                uvs[i] = uv;
                uvs[i + count] = uv;
            }

            var face = new int[triangles.Length];
            for (var i = 0; i < triangles.Length; i++)
                face[i] = triangles[i] + count;

            var mesh = new Mesh
            {
                name = $"Piece {cell.Column},{cell.Row}",
                subMeshCount = 2
            };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);
            mesh.SetTriangles(face, 1);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
