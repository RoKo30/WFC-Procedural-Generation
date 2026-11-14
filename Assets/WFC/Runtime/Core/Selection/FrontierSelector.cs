using System;
using System.Collections.Generic;

namespace ProceduralGen.Wfc.Core
{
    // Visits cells by graph traversal
    // Queue for BFS
    // Stack for DFS

    public sealed class FrontierSelector : ICellSelector
    {
        private readonly bool _depthFirst;
        private readonly LinkedList<int> _frontier = new LinkedList<int>();
        private readonly int[] _order = { 0, 1, 2, 3 };

        private WfcSolver _solver;
        private Random _rng;
        private bool[] _visited;
        private int _fallbackScan;

        public FrontierSelector(bool depthFirst)
        {
            _depthFirst = depthFirst;
        }

        public void Begin(WfcSolver solver, int startCell, Random rng)
        {
            _solver = solver;
            _rng = rng;
            _visited = new bool[solver.Grid.Count];
            _frontier.Clear();
            _fallbackScan = 0;
            _frontier.AddLast(startCell);
        }

        public int NextCell()
        {
            while (_frontier.Count > 0)
            {
                int cell = Take();
                if (_visited[cell]) continue;
                _visited[cell] = true;
                PushNeighbours(cell);
                if (_solver.PossibleCount(cell) > 1) return cell;
            }
            
            for (; _fallbackScan < _visited.Length; _fallbackScan++)
            {
                if (_visited[_fallbackScan] || _solver.PossibleCount(_fallbackScan) <= 1) continue;
                int cell = _fallbackScan;
                _visited[cell] = true;
                PushNeighbours(cell);
                return cell;
            }
            return -1;
        }

        private int Take()
        {
            LinkedListNode<int> node = _depthFirst ? _frontier.Last : _frontier.First;
            _frontier.Remove(node);
            return node.Value;
        }

        private void PushNeighbours(int cell)
        {
            Shuffle(_order);
            for (int i = 0; i < _order.Length; i++)
            {
                int neighbour = _solver.Grid.Neighbour(cell, _order[i]);
                if (neighbour >= 0 && !_visited[neighbour]) _frontier.AddLast(neighbour);
            }
        }

        private void Shuffle(int[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
