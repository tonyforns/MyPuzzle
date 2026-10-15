using MyPuzzle.Core;
using UnityEngine;

namespace MyPuzzle.View
{
    [CreateAssetMenu(fileName = "PuzzleSettings", menuName = "MyPuzzle/Puzzle Settings")]
    public sealed class PuzzleSettings : ScriptableObject
    {
        [SerializeField] string presetName = "Easy";
        [SerializeField] int columns = 4;
        [SerializeField] int rows = 3;
        [SerializeField] float cellSize = 1f;
        [SerializeField] float snapThreshold = 0.35f;
        [SerializeField] float margin = 0.5f;
        [SerializeField] bool startSolved;

        public PuzzleConfig ToConfig()
        {
            return new PuzzleConfig(presetName, columns, rows, cellSize, snapThreshold, margin, startSolved);
        }
    }
}
