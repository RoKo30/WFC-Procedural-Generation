using System;
using System.Collections.Generic;

namespace ProceduralGen.Wfc.Core
{
    // Unique N×N patterns with weights and positions
    public sealed class PatternSet
    {
        private readonly int[][] _pixels;
        private readonly int[] _frequencies;
        private readonly double[] _weights;
        private readonly int[][] _occurrences;

        public int KernelSize { get; }

        public int Count => _pixels.Length;

        public int[] Palette { get; }

        internal PatternSet(int kernelSize, int[] palette, int[][] pixels, int[] frequencies, double[] weights, int[][] occurrences)
        {
            KernelSize = kernelSize;
            Palette = palette;
            _pixels = pixels;
            _frequencies = frequencies;
            _weights = weights;
            _occurrences = occurrences;
        }

        public int GetPixel(int pattern, int x, int y) => _pixels[pattern][x + y * KernelSize];

        public int GetPixel(int pattern, int localIndex) => _pixels[pattern][localIndex];

        public IReadOnlyList<int> GetPixels(int pattern) => _pixels[pattern];

        public int GetFrequency(int pattern) => _frequencies[pattern];

        public double GetWeight(int pattern) => _weights[pattern];
        
        public void SetWeight(int pattern, double weight)
        {
            if (!(weight > 0)) throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be positive.");
            _weights[pattern] = weight;
        }

        public int SamplePositionCount => _occurrences[0].Length;
        
        public int SampleStartPattern(Random rng)
        {
            var variant = _occurrences[rng.Next(_occurrences.Length)];
            return variant[rng.Next(variant.Length)];
        }
    }
}
