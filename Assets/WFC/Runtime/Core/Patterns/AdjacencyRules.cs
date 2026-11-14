using System;
using System.Collections.Generic;

namespace ProceduralGen.Wfc.Core
{
    // Which patterns may sit next to which, for a given overlap
    public sealed class AdjacencyRules
    {
        private readonly int[][][] _compatible;

        public int KernelSize { get; }
        public int Overlap { get; }
        public int Stride => KernelSize - Overlap;
        public int PatternCount { get; }

        private AdjacencyRules(int kernelSize, int overlap, int patternCount, int[][][] compatible)
        {
            KernelSize = kernelSize;
            Overlap = overlap;
            PatternCount = patternCount;
            _compatible = compatible;
        }

        public int[] Compatible(int direction, int pattern) => _compatible[direction][pattern];

        public static void ValidateOverlap(int kernelSize, int overlap)
        {
            if (overlap < 1 || overlap > kernelSize - 1)
                throw new ArgumentException(
                    $"Overlap must be between 1 and {kernelSize - 1} for a {kernelSize}x{kernelSize} kernel (got {overlap}).");
        }

        public static AdjacencyRules Build(PatternSet patterns, int overlap)
        {
            if (patterns == null) throw new ArgumentNullException(nameof(patterns));
            int n = patterns.KernelSize;
            ValidateOverlap(n, overlap);

            int count = patterns.Count;
            var compatible = new int[Directions.Count][][];

            for (int d = 0; d < Directions.Count; d++)
            {
                var byFacingEdge = new Dictionary<int[], List<int>>(IntArrayComparer.Instance);
                int facingSide = Directions.Opposite(d);
                for (int t = 0; t < count; t++)
                {
                    int[] edge = Edge(patterns, t, facingSide, overlap);
                    if (!byFacingEdge.TryGetValue(edge, out var list))
                        byFacingEdge.Add(edge, list = new List<int>());
                    list.Add(t);
                }

                var groups = new Dictionary<int[], int[]>(IntArrayComparer.Instance);
                foreach (var pair in byFacingEdge)
                    groups.Add(pair.Key, pair.Value.ToArray());

                compatible[d] = new int[count][];
                for (int t = 0; t < count; t++)
                {
                    int[] edge = Edge(patterns, t, d, overlap);
                    compatible[d][t] = groups.TryGetValue(edge, out var match) ? match : Array.Empty<int>();
                }
            }

            return new AdjacencyRules(n, overlap, count, compatible);
        }

        private static int[] Edge(PatternSet patterns, int pattern, int side, int overlap)
        {
            int n = patterns.KernelSize;
            int stride = n - overlap;
            int x0 = 0, x1 = n, y0 = 0, y1 = n;
            switch (side)
            {
                case Directions.Right: x0 = stride; break;
                case Directions.Left: x1 = overlap; break;
                case Directions.Up: y0 = stride; break;
                case Directions.Down: y1 = overlap; break;
            }

            var edge = new int[(x1 - x0) * (y1 - y0)];
            int i = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    edge[i++] = patterns.GetPixel(pattern, x, y);
            return edge;
        }
    }
}
