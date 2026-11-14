using System;

namespace ProceduralGen.Wfc.Core
{
    // Pick the new cell with the lowest entropy
    public sealed class MinimumEntropySelector : ICellSelector
    {
        private WfcSolver _solver;
        private Random _rng;

        public void Begin(WfcSolver solver, int startCell, Random rng)
        {
            _solver = solver;
            _rng = rng;
        }

        public int NextCell()
        {
            int best = -1;
            double bestEntropy = double.MaxValue;
            int count = _solver.Grid.Count;

            for (int cell = 0; cell < count; cell++)
            {
                if (_solver.PossibleCount(cell) <= 1) continue;
                double entropy = _solver.Entropy(cell) + 1e-6 * _rng.NextDouble();
                if (entropy < bestEntropy)
                {
                    bestEntropy = entropy;
                    best = cell;
                }
            }
            return best;
        }
    }
}
