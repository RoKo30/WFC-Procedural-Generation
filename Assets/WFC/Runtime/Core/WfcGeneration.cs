using System;
using System.Collections.Generic;

namespace ProceduralGen.Wfc.Core
{
    public sealed class GenerationOptions
    {
        // Output size
        public int Width = 48;
        public int Height = 48;

        public bool PeriodicOutput;

        public CellSelectionHeuristic Heuristic = CellSelectionHeuristic.MinimumEntropy;

        public int Seed;

        public int MaxAttempts = 20; // Failed attempt number

        public void Validate()
        {
            if (Width < 1 || Height < 1)
                throw new ArgumentException($"Output size must be positive, got {Width}x{Height}.");
            if (MaxAttempts < 1)
                throw new ArgumentException("Max attempts must be at least 1.");
        }
    }

    public enum GenerationState
    {
        Running,
        Succeeded,
        Failed,
    }

    // Generation
    public sealed class WfcGeneration
    {
        private const int StartPatternTries = 64;

        private readonly IReadOnlyList<IWfcConstraint> _constraints;
        private ICellSelector _selector;
        private Random _rng;

        public PatternSet Patterns { get; }
        public AdjacencyRules Rules { get; }
        public OutputGrid Grid { get; }
        public WfcSolver Solver { get; }
        public GenerationOptions Options { get; }

        public GenerationState State { get; private set; } = GenerationState.Running;

        public int Attempt { get; private set; }

        public int StartCell { get; private set; } = -1;
        public int StartPattern { get; private set; } = -1;

        public int Observations { get; private set; }

        public event Action<WfcGeneration> AttemptStarted;

        public WfcGeneration(PatternSet patterns, AdjacencyRules rules, GenerationOptions options,
                             IReadOnlyList<IWfcConstraint> constraints = null)
        {
            Patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            options.Validate();
            _constraints = constraints ?? Array.Empty<IWfcConstraint>();

            Grid = OutputGrid.Create(options.Width, options.Height, patterns.KernelSize, rules.Stride, options.PeriodicOutput);
            Solver = new WfcSolver(patterns, rules, Grid);
            StartNextAttempt();
        }

        // Collapse one cell and propagate
        // Restart on contradiction
        public GenerationState Step()
        {
            if (State != GenerationState.Running) return State;

            int cell = _selector.NextCell();
            if (cell < 0)
            {
                State = Solver.IsComplete ? GenerationState.Succeeded : GenerationState.Failed;
                return State;
            }

            Observations++;
            int pattern = Solver.ChoosePattern(cell, _rng);
            if (!Solver.Collapse(cell, pattern))
                StartNextAttempt();

            return State;
        }

        public GenerationState RunToCompletion()
        {
            while (Step() == GenerationState.Running) { }
            return State;
        }

        private void StartNextAttempt()
        {
            while (Attempt < Options.MaxAttempts)
            {
                Attempt++;
                Observations = 0;
                _rng = new Random(DeriveSeed(Options.Seed, Attempt - 1));
                Solver.Reset();

                if (Solver.HasContradiction || !ApplyConstraints() || !PlaceStart())
                    continue;

                _selector = CellSelectors.Create(Options.Heuristic);
                _selector.Begin(Solver, StartCell, _rng);
                AttemptStarted?.Invoke(this);
                return;
            }
            State = GenerationState.Failed;
        }

        private bool ApplyConstraints()
        {
            for (int i = 0; i < _constraints.Count; i++)
                if (!_constraints[i].Apply(Solver, _rng)) return false;
            return true;
        }

        // Choose a random output cell and give it the pattern found at a random position of the input image
        private bool PlaceStart()
        {
            StartCell = _rng.Next(Grid.Count);
            StartPattern = -1;
            for (int i = 0; i < StartPatternTries; i++)
            {
                int candidate = Patterns.SampleStartPattern(_rng);
                if (Solver.IsPossible(StartCell, candidate))
                {
                    StartPattern = candidate;
                    break;
                }
            }
            if (StartPattern < 0) StartPattern = Solver.ChoosePattern(StartCell, _rng);
            if (StartPattern < 0) return false;

            return Solver.Collapse(StartCell, StartPattern);
        }

        private static int DeriveSeed(int seed, int attempt)
        {
            unchecked
            {
                return attempt == 0 ? seed : (seed * 486187739) ^ (attempt * 16777619);
            }
        }
    }
}
