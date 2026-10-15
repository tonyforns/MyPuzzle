using System.Collections.Generic;
using MyPuzzle.Core;
using MyPuzzle.View;
using NUnit.Framework;
using UnityEngine;

namespace MyPuzzle.Tests
{
    public sealed class PieceMeshBuilderTests
    {
        [Test]
        public void Build_SolvedGrid_TilesTheTextureWithoutGaps()
        {
            var config = new PuzzleConfig("Easy", 4, 3, 1f, 0.35f, 0.5f);
            var definition = PuzzleGenerator.Create(config, seed: 1);

            var created = new List<Mesh>();
            var minU = float.PositiveInfinity;
            var maxU = float.NegativeInfinity;
            var minV = float.PositiveInfinity;
            var maxV = float.NegativeInfinity;

            try
            {
                foreach (var cell in GridTopology.AllCells(definition.Columns, definition.Rows))
                {
                    var outline = definition.OutlineOf(cell);
                    var mesh = PieceMeshBuilder.Build(outline, definition.Columns, definition.Rows, cell);
                    created.Add(mesh);
                    var pose = MyPuzzle.Core.Pose.AtCellCenter(cell, definition.CellSize);
                    var face = mesh.GetTriangles(1);

                    Assert.That(mesh.subMeshCount, Is.EqualTo(2));
                    Assert.That(mesh.GetTriangles(0).Length, Is.GreaterThan(0));
                    Assert.That(face.Length, Is.GreaterThan(6));

                    var seen = new HashSet<int>();
                    for (var i = 0; i < face.Length; i++)
                    {
                        var index = face[i];
                        if (!seen.Add(index))
                            continue;

                        var worldX = mesh.vertices[index].x + pose.X;
                        var worldY = mesh.vertices[index].y + pose.Y;
                        Assert.That(mesh.uv[index].x, Is.EqualTo(worldX / 4f).Within(0.0001f));
                        Assert.That(mesh.uv[index].y, Is.EqualTo(worldY / 3f).Within(0.0001f));
                        minU = Mathf.Min(minU, mesh.uv[index].x);
                        maxU = Mathf.Max(maxU, mesh.uv[index].x);
                        minV = Mathf.Min(minV, mesh.uv[index].y);
                        maxV = Mathf.Max(maxV, mesh.uv[index].y);
                    }
                }

                Assert.That(minU, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(maxU, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(minV, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(maxV, Is.EqualTo(1f).Within(0.0001f));
            }
            finally
            {
                for (var i = 0; i < created.Count; i++)
                {
                    if (created[i] != null)
                        Object.DestroyImmediate(created[i]);
                }
            }
        }
    }
}
