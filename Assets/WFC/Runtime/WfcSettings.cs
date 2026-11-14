using System;
using ProceduralGen.Wfc.Core;
using UnityEngine;

namespace ProceduralGen.Wfc
{
    // Inspector parameters
    [Serializable]
    public sealed class WfcSettings
    {
        public const int MaxKernelSize = 16;

        [Header("Patterns")]
        [Tooltip("Size N of the N×N kernel. Small kernels give noisy output, large kernels copy bigger structures.")]
        [Range(2, MaxKernelSize)]
        public int kernelSize = 3;

        [Tooltip("How many columns/rows two neighbouring patterns must share (1 .. N-1). " +
                 "Each new cell adds N - overlap new columns/rows. Overlap N-1 is classic overlapping WFC.")]
        public int overlap = 2;

        [Tooltip("Rotated / mirrored copies added per pattern: 1 = none, 2 = + mirror, 4 = + 90° rotation, 8 = all.")]
        [Range(1, 8)]
        public int symmetry = 1;

        [Tooltip("Treat the input as tileable: kernels may wrap around its edges.")]
        public bool periodicInput;

        [Tooltip("Duplicate patterns are always merged into one. Choose whether their count becomes the weight.")]
        public DuplicateHandling duplicates = DuplicateHandling.WeightByFrequency;

        [Header("Output")]
        [Min(1)] public int outputWidth = 48;
        [Min(1)] public int outputHeight = 48;

        [Tooltip("Wrap the output around its edges so the result tiles seamlessly. Size is rounded up to a multiple of the stride.")]
        public bool periodicOutput;

        [Header("Solver")]
        [Tooltip("Order in which cells are collapsed after the random starting cell.")]
        public CellSelectionHeuristic heuristic = CellSelectionHeuristic.MinimumEntropy;

        public int seed = 12345;

        [Tooltip("Pick a new seed on every generation (it is written back into Seed so a result can be reproduced).")]
        public bool randomizeSeed;

        [Tooltip("Fresh attempts with a derived seed when the solver hits a contradiction.")]
        [Min(1)] public int maxAttempts = 20;

        public int Stride => kernelSize - overlap;

        public void Validate()
        {
            kernelSize = Mathf.Clamp(kernelSize, 2, MaxKernelSize);
            overlap = Mathf.Clamp(overlap, 1, kernelSize - 1);
            symmetry = Mathf.Clamp(symmetry, 1, Symmetry.Max);
            outputWidth = Mathf.Max(1, outputWidth);
            outputHeight = Mathf.Max(1, outputHeight);
            maxAttempts = Mathf.Max(1, maxAttempts);
        }

        public ExtractionOptions ToExtractionOptions() => new ExtractionOptions
        {
            KernelSize = kernelSize,
            Symmetry = symmetry,
            PeriodicInput = periodicInput,
            Duplicates = duplicates,
        };

        public GenerationOptions ToGenerationOptions() => new GenerationOptions
        {
            Width = outputWidth,
            Height = outputHeight,
            PeriodicOutput = periodicOutput,
            Heuristic = heuristic,
            Seed = seed,
            MaxAttempts = maxAttempts,
        };
    }
}
