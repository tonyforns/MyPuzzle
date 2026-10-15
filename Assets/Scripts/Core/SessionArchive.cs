using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MyPuzzle.Core
{
    public sealed class SavedSession
    {
        public SavedSession(PuzzleConfig config, string imagePath, PuzzleState state)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (config.Columns != state.Definition.Columns || config.Rows != state.Definition.Rows)
                throw new ArgumentException("The preset grid does not match the puzzle.", nameof(config));
            if (Math.Abs(config.CellSize - state.Definition.CellSize) > 0.0001f)
                throw new ArgumentException("The preset cell size does not match the puzzle.", nameof(config));

            Config = config;
            ImagePath = imagePath ?? string.Empty;
            State = state;
        }

        public PuzzleConfig Config { get; }

        public string ImagePath { get; }

        public PuzzleState State { get; }
    }

    public static class SessionArchive
    {
        public static string Write(SavedSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var culture = CultureInfo.InvariantCulture;
            var builder = new StringBuilder();
            builder.AppendLine("MyPuzzle 1");
            builder.Append("image");
            if (session.ImagePath.Length > 0)
            {
                builder.Append(' ');
                builder.Append(session.ImagePath.Replace("\r", string.Empty).Replace("\n", string.Empty));
            }

            builder.AppendLine();
            builder.Append("preset ").AppendLine(OneLine(session.Config.PresetName));
            builder.Append("columns ").AppendLine(session.Config.Columns.ToString(culture));
            builder.Append("rows ").AppendLine(session.Config.Rows.ToString(culture));
            builder.Append("cellSize ").AppendLine(session.Config.CellSize.ToString("R", culture));
            builder.Append("snapThreshold ").AppendLine(session.Config.SnapThreshold.ToString("R", culture));
            builder.Append("margin ").AppendLine(session.Config.Margin.ToString("R", culture));
            builder.Append("startSolved ").AppendLine(session.Config.StartSolved ? "true" : "false");
            builder.Append("seed ").AppendLine(session.State.Definition.Seed.ToString(culture));
            builder.Append("phase ").AppendLine(session.State.Phase.ToString());

            var edges = session.State.Definition.Edges;
            builder.Append("edges ").AppendLine(edges.Count.ToString(culture));
            for (var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                builder.Append(edge.Id.Cell.Column.ToString(culture)).Append(' ');
                builder.Append(edge.Id.Cell.Row.ToString(culture)).Append(' ');
                builder.Append(edge.Id.Side).Append(' ');
                builder.AppendLine(edge.Kind.ToString());
            }

            var groups = session.State.Groups;
            builder.Append("groups ").AppendLine(groups.Count.ToString(culture));
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                builder.Append(group.Id.ToString(culture)).Append(' ');
                builder.Append(group.Pose.X.ToString("R", culture)).Append(' ');
                builder.Append(group.Pose.Y.ToString("R", culture)).Append(' ');
                builder.Append(group.Pose.QuarterTurns.ToString(culture)).Append(' ');
                builder.Append(group.Anchor.Column.ToString(culture)).Append(' ');
                builder.Append(group.Anchor.Row.ToString(culture)).Append(' ');
                builder.AppendLine(group.Members.Count.ToString(culture));
                for (var member = 0; member < group.Members.Count; member++)
                {
                    builder.Append(group.Members[member].Column.ToString(culture));
                    builder.Append(' ');
                    builder.AppendLine(group.Members[member].Row.ToString(culture));
                }
            }

            return builder.ToString();
        }

        public static SavedSession Read(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new FormatException("The save file is empty.");

            try
            {
                return Parse(text);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new FormatException("The save file is not a puzzle.", exception);
            }
        }

        static SavedSession Parse(string text)
        {
            var reader = new LineReader(text);
            if (reader.Next() != "MyPuzzle 1")
                throw new FormatException("The save file is not a MyPuzzle game.");

            var imagePath = ValueOf(reader.Next(), "image");
            var presetName = Required(reader.Next(), "preset");
            var columns = IntOf(reader.Next(), "columns");
            var rows = IntOf(reader.Next(), "rows");
            var cellSize = FloatOf(reader.Next(), "cellSize");
            var snapThreshold = FloatOf(reader.Next(), "snapThreshold");
            var margin = FloatOf(reader.Next(), "margin");
            var startSolved = BoolOf(reader.Next(), "startSolved");
            var seed = IntOf(reader.Next(), "seed");
            var phase = EnumOf<SessionPhase>(Required(reader.Next(), "phase"));
            var edgeCount = IntOf(reader.Next(), "edges");
            if (edgeCount != (columns * (rows - 1)) + (rows * (columns - 1)))
                throw new FormatException("The save file has the wrong number of edges.");

            var edges = new EdgeRecord[edgeCount];
            var seenEdges = new HashSet<EdgeId>();
            for (var i = 0; i < edgeCount; i++)
            {
                var parts = Split(reader.Next(), 4);
                var cell = new CellId(ParseInt(parts[0]), ParseInt(parts[1]));
                var side = EnumOf<Direction>(parts[2]);
                var kind = EnumOf<EdgeKind>(parts[3]);
                if (side != Direction.North && side != Direction.East)
                    throw new FormatException("An internal edge is stored on the north or east side.");
                if (kind == EdgeKind.Edge)
                    throw new FormatException("An internal edge is a tab or a blank.");
                if (!GridTopology.Contains(columns, rows, cell)
                    || !GridTopology.TryGetNeighbor(columns, rows, cell, side, out _))
                    throw new FormatException("An edge does not belong to this grid.");

                var id = new EdgeId(cell, side);
                if (!seenEdges.Add(id))
                    throw new FormatException("An edge is stored twice.");

                edges[i] = new EdgeRecord(id, kind);
            }

            var definition = new PuzzleDefinition(columns, rows, cellSize, seed, edges);
            foreach (var cell in GridTopology.AllCells(columns, rows))
                definition.OutlineOf(cell);

            var groupCount = IntOf(reader.Next(), "groups");
            if (groupCount < 1)
                throw new FormatException("A puzzle state needs at least one group.");

            var groups = new GroupState[groupCount];
            var seenGroups = new HashSet<int>();
            var seenCells = new HashSet<int>();
            for (var i = 0; i < groupCount; i++)
            {
                var parts = Split(reader.Next(), 7);
                var id = ParseInt(parts[0]);
                if (!seenGroups.Add(id))
                    throw new FormatException("A group id is repeated.");

                var pose = new Pose(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseInt(parts[3]));
                var anchor = new CellId(ParseInt(parts[4]), ParseInt(parts[5]));
                var memberCount = ParseInt(parts[6]);
                if (memberCount < 1)
                    throw new FormatException("A group needs at least one cell.");

                var members = new CellId[memberCount];
                for (var member = 0; member < memberCount; member++)
                {
                    var cellParts = Split(reader.Next(), 2);
                    var cell = new CellId(ParseInt(cellParts[0]), ParseInt(cellParts[1]));
                    if (!GridTopology.Contains(columns, rows, cell) || !seenCells.Add(cell.Index(columns)))
                        throw new FormatException("A cell is missing or repeated.");

                    members[member] = cell;
                }

                groups[i] = new GroupState(id, pose, anchor, members);
            }

            if (seenCells.Count != columns * rows)
                throw new FormatException("The groups do not cover the grid.");
            if (phase == SessionPhase.Completo && groupCount != 1)
                throw new FormatException("A finished puzzle is a single group.");

            reader.End();
            var config = new PuzzleConfig(presetName, columns, rows, cellSize, snapThreshold, margin, startSolved);
            return new SavedSession(config, imagePath, new PuzzleState(definition, groups, phase));
        }

        static string OneLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        static string ValueOf(string line, string key)
        {
            if (line == key)
                return string.Empty;

            var prefix = key + " ";
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                throw new FormatException("Expected " + key + ".");

            return line.Substring(prefix.Length);
        }

        static string Required(string line, string key)
        {
            var value = ValueOf(line, key);
            if (value.Length == 0)
                throw new FormatException("Expected " + key + ".");

            return value;
        }

        static int IntOf(string line, string key)
        {
            return ParseInt(Required(line, key));
        }

        static float FloatOf(string line, string key)
        {
            return ParseFloat(Required(line, key));
        }

        static bool BoolOf(string line, string key)
        {
            var value = Required(line, key);
            if (value == "true")
                return true;
            if (value == "false")
                return false;

            throw new FormatException("Expected true or false.");
        }

        static T EnumOf<T>(string token) where T : struct
        {
            if (token.Length == 0 || char.IsDigit(token[0]) || token[0] == '-'
                || !Enum.TryParse(token, false, out T value)
                || !Enum.IsDefined(typeof(T), value))
                throw new FormatException("Unknown value " + token + ".");

            return value;
        }

        static int ParseInt(string token)
        {
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                throw new FormatException("Expected a whole number.");

            return value;
        }

        static float ParseFloat(string token)
        {
            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new FormatException("Expected a number.");

            return value;
        }

        static string[] Split(string line, int count)
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != count)
                throw new FormatException("A save line has the wrong number of values.");

            return parts;
        }

        sealed class LineReader
        {
            readonly string[] lines;
            int index;

            public LineReader(string text)
            {
                lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            }

            public string Next()
            {
                while (index < lines.Length)
                {
                    var line = lines[index++].Trim();
                    if (line.Length > 0)
                        return line;
                }

                throw new FormatException("The save file ended early.");
            }

            public void End()
            {
                while (index < lines.Length)
                {
                    if (lines[index++].Trim().Length > 0)
                        throw new FormatException("The save file has extra lines.");
                }
            }
        }
    }
}
