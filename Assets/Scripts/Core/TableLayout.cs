using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public static class TableLayout
    {
        const int AttemptsPerPiece = 40;
        const int SeparationSteps = 5;
        const float SeparationScale = 0.72f;
        const float NudgeFraction = 0.18f;

        public static BoardRect AssemblyFrame(PuzzleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var width = definition.Columns * definition.CellSize;
            var height = definition.Rows * definition.CellSize;
            return new BoardRect(0f, 0f, width, height);
        }

        public static BoardRect PieceZone(PuzzleDefinition definition, float frameMargin)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (frameMargin < 0f)
                throw new ArgumentOutOfRangeException(nameof(frameMargin));

            var frame = AssemblyFrame(definition);
            var band = frameMargin + (definition.CellSize * Rings(definition.Columns * definition.Rows));
            return new BoardRect(frame.MinX - band, frame.MinY - band, frame.MaxX + band, frame.MaxY + band);
        }

        public static IReadOnlyList<Pose> Scatter(PuzzleDefinition definition, int seed, BoardRect frame, BoardRect zone)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var count = definition.Columns * definition.Rows;
            var cellSize = definition.CellSize;
            var half = cellSize * 0.5f;
            var inset = new BoardRect(zone.MinX + half, zone.MinY + half, zone.MaxX - half, zone.MaxY - half);

            var separation = cellSize;
            for (var step = 0; step < SeparationSteps; step++)
            {
                if (TryPlace(count, seed, frame, inset, cellSize, separation, out var placed))
                    return placed;

                separation *= SeparationScale;
            }

            return PlaceWithNudge(count, seed, frame, inset, cellSize);
        }

        static bool TryPlace(
            int count,
            int seed,
            BoardRect frame,
            BoardRect inset,
            float cellSize,
            float separation,
            out Pose[] placed)
        {
            placed = new Pose[count];
            var random = new Random(seed);
            var accepted = 0;

            for (var piece = 0; piece < count; piece++)
            {
                var found = false;
                for (var attempt = 0; attempt < AttemptsPerPiece; attempt++)
                {
                    var x = Lerp(inset.MinX, inset.MaxX, random);
                    var y = Lerp(inset.MinY, inset.MaxY, random);
                    if (OverlapsFrame(x, y, cellSize, frame) || !FarEnough(x, y, placed, accepted, separation))
                        continue;

                    placed[accepted] = new Pose(x, y, 0);
                    accepted++;
                    found = true;
                    break;
                }

                if (!found)
                    return false;
            }

            return true;
        }

        static Pose[] PlaceWithNudge(int count, int seed, BoardRect frame, BoardRect inset, float cellSize)
        {
            var spacing = cellSize;
            var slots = new List<Pose>();
            for (var step = 0; step < SeparationSteps; step++)
            {
                slots = CollectSlots(inset, frame, cellSize, spacing, avoidFrame: true);
                if (slots.Count >= count)
                    break;

                spacing *= SeparationScale;
            }

            if (slots.Count == 0)
            {
                slots = CollectSlots(inset, frame, cellSize, cellSize * SeparationScale, avoidFrame: false);
                if (slots.Count == 0)
                {
                    slots.Add(new Pose(
                        (inset.MinX + inset.MaxX) * 0.5f,
                        (inset.MinY + inset.MaxY) * 0.5f,
                        0));
                }
            }

            Shuffle(slots, new Random(seed));
            var nudge = cellSize * NudgeFraction;
            var placed = new Pose[count];
            for (var i = 0; i < count; i++)
            {
                var slot = slots[i % slots.Count];
                var layer = i / slots.Count;
                var x = slot.X + (layer * nudge);
                var y = slot.Y + (layer * nudge * 0.5f);
                if (x < inset.MinX || x > inset.MaxX || y < inset.MinY || y > inset.MaxY || OverlapsFrame(x, y, cellSize, frame))
                {
                    x = slot.X;
                    y = slot.Y;
                }

                placed[i] = new Pose(x, y, 0);
            }

            return placed;
        }

        static List<Pose> CollectSlots(BoardRect inset, BoardRect frame, float cellSize, float spacing, bool avoidFrame)
        {
            var slots = new List<Pose>();
            if (spacing <= 0f)
                return slots;

            for (var y = inset.MinY; y <= inset.MaxY; y += spacing)
            {
                for (var x = inset.MinX; x <= inset.MaxX; x += spacing)
                {
                    if (avoidFrame && OverlapsFrame(x, y, cellSize, frame))
                        continue;

                    slots.Add(new Pose(x, y, 0));
                }
            }

            return slots;
        }

        static void Shuffle(List<Pose> slots, Random random)
        {
            for (var i = slots.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                var swap = slots[i];
                slots[i] = slots[swapIndex];
                slots[swapIndex] = swap;
            }
        }

        static bool FarEnough(float x, float y, Pose[] placed, int count, float separation)
        {
            for (var i = 0; i < count; i++)
            {
                var gapX = Math.Abs(x - placed[i].X);
                var gapY = Math.Abs(y - placed[i].Y);
                if (Math.Max(gapX, gapY) < separation)
                    return false;
            }

            return true;
        }

        static bool OverlapsFrame(float x, float y, float cellSize, BoardRect frame)
        {
            var half = cellSize * 0.5f;
            var piece = new BoardRect(x - half, y - half, x + half, y + half);
            return frame.Overlaps(piece);
        }

        static float Lerp(float min, float max, Random random)
        {
            return min + ((float)random.NextDouble() * (max - min));
        }

        static float Rings(int pieceCount)
        {
            return pieceCount <= 24 ? 2.5f : 3.5f;
        }
    }
}
