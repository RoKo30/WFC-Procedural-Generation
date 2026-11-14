using System;

namespace ProceduralGen.Wfc.Core
{
    public enum CellSelectionHeuristic
    {
        MinimumEntropy,
        BreadthFirst,
        DepthFirst,
    }

    public interface ICellSelector
    {
        void Begin(WfcSolver solver, int startCell, Random rng);

        int NextCell();
    }

    public static class CellSelectors
    {
        public static ICellSelector Create(CellSelectionHeuristic heuristic)
        {
            switch (heuristic)
            {
                case CellSelectionHeuristic.MinimumEntropy: return new MinimumEntropySelector();
                case CellSelectionHeuristic.BreadthFirst: return new FrontierSelector(depthFirst: false);
                case CellSelectionHeuristic.DepthFirst: return new FrontierSelector(depthFirst: true);
                default: throw new ArgumentOutOfRangeException(nameof(heuristic), heuristic, null);
            }
        }
    }
}
