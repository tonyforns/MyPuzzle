using System.Linq;
using MyPuzzle.Core;
using MyPuzzle.View;
using NUnit.Framework;
using UnityEditor;

namespace MyPuzzle.Tests
{
    public sealed class SettingsAssetTests
    {
        [TestCase("Assets/Puzzle/Settings/Easy.asset", "Easy", 4, 3)]
        [TestCase("Assets/Puzzle/Settings/Medium.asset", "Medium", 6, 4)]
        [TestCase("Assets/Puzzle/Settings/Hard.asset", "Hard", 8, 6)]
        [TestCase("Assets/Puzzle/Settings/Expert.asset", "Expert", 10, 8)]
        public void Preset_MatchesGrid(string path, string presetName, int columns, int rows)
        {
            var settings = AssetDatabase.LoadAssetAtPath<PuzzleSettings>(path);

            Assert.That(settings, Is.Not.Null, path);
            var config = settings.ToConfig();
            Assert.That(config.PresetName, Is.EqualTo(presetName));
            Assert.That(config.Columns, Is.EqualTo(columns));
            Assert.That(config.Rows, Is.EqualTo(rows));
            Assert.That(config.CellSize, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(config.StartSolved, Is.False);

            var state = PuzzleStateFactory.CreateEmpty(config, seed: 1);
            Assert.That(state.Groups, Has.Count.EqualTo(columns * rows));
            Assert.That(state.Groups.All(group => group.Pose.QuarterTurns == 0), Is.True);
            Assert.That(state.Phase, Is.EqualTo(SessionPhase.Armando));
        }
    }
}
