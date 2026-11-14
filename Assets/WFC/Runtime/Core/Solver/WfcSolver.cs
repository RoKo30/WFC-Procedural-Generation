using System;

namespace ProceduralGen.Wfc.Core
{
    /// The constraint-propagation core of WFC: the "wave" (which patterns are still possible in each cell),
    /// banning patterns, collapsing cells and propagating to neighbors.
    public sealed class WfcSolver
    {
        private readonly PatternSet _patterns;
        private readonly AdjacencyRules _rules;
        private readonly OutputGrid _grid;
        private readonly int _patternCount;
        private readonly int _cellCount;

        // wave[cell * T + t], pattern t is possible in cell
        private readonly bool[] _wave;

        // compatible[(cell * T + t) * 4 + d], how many patterns still possible in the neighbour on side Opposite(d)
        // When it reaches 0, t loses all support from that side and is banned
        private readonly int[] _compatible;

        private readonly int[] _possibleCount;
        private readonly double[] _sumWeights;
        private readonly double[] _sumWeightLogWeights;
        private readonly double[] _entropy;

        private readonly double[] _weights;
        private readonly double[] _weightLogWeights;
        private readonly double _totalWeight;
        private readonly double _totalWeightLogWeight;
        private readonly double _startEntropy;

        private readonly int[] _deadEndMask;

        private readonly int[] _stack; // (cell, pattern) pairs waiting to be propagated
        private int _stackSize;

        public PatternSet Patterns => _patterns;
        public AdjacencyRules Rules => _rules;
        public OutputGrid Grid => _grid;

        public bool HasContradiction { get; private set; }

        // Cells that still have more than one possible pattern
        public int UndecidedCount { get; private set; }

        public bool IsComplete => !HasContradiction && UndecidedCount == 0;

        public WfcSolver(PatternSet patterns, AdjacencyRules rules, OutputGrid grid)
        {
            _patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            if (rules.PatternCount != patterns.Count)
                throw new ArgumentException("Adjacency rules were built for a different pattern set.");

            _patternCount = patterns.Count;
            _cellCount = grid.Count;

            _wave = new bool[_cellCount * _patternCount];
            _compatible = new int[_cellCount * _patternCount * Directions.Count];
            _possibleCount = new int[_cellCount];
            _sumWeights = new double[_cellCount];
            _sumWeightLogWeights = new double[_cellCount];
            _entropy = new double[_cellCount];
            _stack = new int[_cellCount * _patternCount * 2];

            _weights = new double[_patternCount];
            _weightLogWeights = new double[_patternCount];
            _deadEndMask = new int[_patternCount];
            for (int t = 0; t < _patternCount; t++)
            {
                double w = patterns.GetWeight(t);
                _weights[t] = w;
                _weightLogWeights[t] = w * Math.Log(w);
                _totalWeight += w;
                _totalWeightLogWeight += _weightLogWeights[t];

                for (int d = 0; d < Directions.Count; d++)
                    if (rules.Compatible(d, t).Length == 0) _deadEndMask[t] |= 1 << d;
            }
            _startEntropy = Math.Log(_totalWeight) - _totalWeightLogWeight / _totalWeight;

            Reset();
        }

        // Put every cell back into full superposition
        public void Reset()
        {
            HasContradiction = false;
            _stackSize = 0;
            UndecidedCount = _patternCount > 1 ? _cellCount : 0;

            for (int cell = 0; cell < _cellCount; cell++)
            {
                for (int t = 0; t < _patternCount; t++)
                {
                    int k = cell * _patternCount + t;
                    _wave[k] = true;
                    for (int d = 0; d < Directions.Count; d++)
                        _compatible[k * Directions.Count + d] = _rules.Compatible(Directions.Opposite(d), t).Length;
                }
                _possibleCount[cell] = _patternCount;
                _sumWeights[cell] = _totalWeight;
                _sumWeightLogWeights[cell] = _totalWeightLogWeight;
                _entropy[cell] = _startEntropy;
            }

            // Patterns that can never have a neighbor on some side are impossible wherever the grid has one there
            for (int cell = 0; cell < _cellCount; cell++)
            {
                int mask = _grid.NeighbourMask(cell);
                for (int t = 0; t < _patternCount; t++)
                    if ((_deadEndMask[t] & mask) != 0) BanWithoutPropagation(cell, t);
            }
            Propagate();
        }

        public bool IsPossible(int cell, int pattern) => _wave[cell * _patternCount + pattern];
        public int PossibleCount(int cell) => _possibleCount[cell];
        public bool IsDecided(int cell) => _possibleCount[cell] == 1;

        // Shannon entropy of the cell's remaining 
        public double Entropy(int cell) => _entropy[cell];

        // The single remaining pattern of a decided cell, or -1 if not decided
        public int GetDecidedPattern(int cell)
        {
            if (_possibleCount[cell] != 1) return -1;
            int offset = cell * _patternCount;
            for (int t = 0; t < _patternCount; t++)
                if (_wave[offset + t]) return t;
            return -1;
        }

        // Pick one of the cell's remaining patterns at random, proportionally to weights
        public int ChoosePattern(int cell, Random rng)
        {
            int offset = cell * _patternCount;
            double total = 0;
            for (int t = 0; t < _patternCount; t++)
                if (_wave[offset + t]) total += _weights[t];

            double r = rng.NextDouble() * total;
            int last = -1;
            for (int t = 0; t < _patternCount; t++)
            {
                if (!_wave[offset + t]) continue;
                last = t;
                r -= _weights[t];
                if (r < 0) return t;
            }
            return last; // floating-point leftovers
        }

        // Fix the cell to one pattern and propagate
        // Return false on contradiction
        public bool Collapse(int cell, int pattern)
        {
            int offset = cell * _patternCount;
            for (int t = 0; t < _patternCount; t++)
                if (t != pattern && _wave[offset + t]) BanWithoutPropagation(cell, t);
            return Propagate();
        }

        // Remove one pattern from a cell and propagate
        // Return false on contradiction
        public bool Ban(int cell, int pattern)
        {
            BanWithoutPropagation(cell, pattern);
            return Propagate();
        }

        private void BanWithoutPropagation(int cell, int pattern)
        {
            int k = cell * _patternCount + pattern;
            if (!_wave[k]) return;
            _wave[k] = false;

            int c = k * Directions.Count;
            for (int d = 0; d < Directions.Count; d++) _compatible[c + d] = 0;

            _stack[_stackSize * 2] = cell;
            _stack[_stackSize * 2 + 1] = pattern;
            _stackSize++;

            int remaining = --_possibleCount[cell];
            if (remaining == 1) UndecidedCount--;
            else if (remaining == 0) HasContradiction = true;

            _sumWeights[cell] -= _weights[pattern];
            _sumWeightLogWeights[cell] -= _weightLogWeights[pattern];
            double sum = _sumWeights[cell];
            _entropy[cell] = sum > 0 ? Math.Log(sum) - _sumWeightLogWeights[cell] / sum : 0;
        }

        private bool Propagate()
        {
            while (_stackSize > 0)
            {
                if (HasContradiction)
                {
                    _stackSize = 0;
                    return false;
                }

                _stackSize--;
                int cell = _stack[_stackSize * 2];
                int banned = _stack[_stackSize * 2 + 1];

                for (int d = 0; d < Directions.Count; d++)
                {
                    int neighbour = _grid.Neighbour(cell, d);
                    if (neighbour < 0) continue;

                    int[] supported = _rules.Compatible(d, banned);
                    int baseIndex = neighbour * _patternCount;
                    for (int i = 0; i < supported.Length; i++)
                    {
                        int t = supported[i];
                        if (--_compatible[(baseIndex + t) * Directions.Count + d] == 0)
                            BanWithoutPropagation(neighbour, t);
                    }
                }
            }
            return !HasContradiction;
        }
    }
}
