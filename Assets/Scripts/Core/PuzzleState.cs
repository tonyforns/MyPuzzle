using System;
using System.Collections.Generic;

namespace MyPuzzle.Core
{
    public enum SessionPhase
    {
        Armando = 0,
        Completo = 1
    }

    public sealed class PuzzleState
    {
        public PuzzleState(PuzzleDefinition definition, IReadOnlyList<GroupState> groups, SessionPhase phase)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (groups == null || groups.Count == 0)
                throw new ArgumentException("A puzzle state needs at least one group.", nameof(groups));

            Definition = definition;
            Groups = groups;
            Phase = phase;
        }

        public PuzzleDefinition Definition { get; }

        public IReadOnlyList<GroupState> Groups { get; }

        public SessionPhase Phase { get; }
    }
}
