using System;
using System.Collections.Generic;

namespace ProceduralGen.Wfc.Core
{
    public enum DuplicateHandling
    {
        WeightByFrequency,
        Uniform,
    }

    public sealed class ExtractionOptions
    {
        public int KernelSize = 3;

        public int Symmetry = 1;

        public bool PeriodicInput;

        public DuplicateHandling Duplicates = DuplicateHandling.WeightByFrequency;

        public void Validate(ImageData image)
        {
            if (KernelSize < 2)
                throw new ArgumentException($"Kernel size must be at least 2 (got {KernelSize}).");
            if (Symmetry < 1 || Symmetry > Core.Symmetry.Max)
                throw new ArgumentException($"Symmetry must be between 1 and {Core.Symmetry.Max} (got {Symmetry}).");
            if (!PeriodicInput && (KernelSize > image.Width || KernelSize > image.Height))
                throw new ArgumentException(
                    $"Kernel {KernelSize}x{KernelSize} does not fit inside the {image.Width}x{image.Height} input. " +
                    "Use a smaller kernel or enable periodic input.");
        }
    }

    // Slide kernel over the input image and collects unique patterns
    public static class PatternExtractor
    {
        public static PatternSet Extract(ImageData image, ExtractionOptions options)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.Validate(image);

            int n = options.KernelSize;
            int columns = options.PeriodicInput ? image.Width : image.Width - n + 1;
            int rows = options.PeriodicInput ? image.Height : image.Height - n + 1;

            var indexOf = new Dictionary<int[], int>(IntArrayComparer.Instance);
            var patterns = new List<int[]>();
            var frequencies = new List<int>();

            var occurrences = new int[options.Symmetry][];
            for (int v = 0; v < occurrences.Length; v++)
                occurrences[v] = new int[columns * rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int[][] variants = Core.Symmetry.Variants(ReadWindow(image, x, y, n), n, options.Symmetry);
                    for (int v = 0; v < variants.Length; v++)
                    {
                        int[] pattern = variants[v];
                        if (!indexOf.TryGetValue(pattern, out int index)) // not seen yet
                        {
                            index = patterns.Count;
                            indexOf.Add(pattern, index);
                            patterns.Add(pattern);
                            frequencies.Add(0);
                        }
                        frequencies[index]++;
                        occurrences[v][x + y * columns] = index;
                    }
                }
            }

            var weights = new double[patterns.Count];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = options.Duplicates == DuplicateHandling.Uniform ? 1.0 : frequencies[i];

            return new PatternSet(n, image.Palette, patterns.ToArray(), frequencies.ToArray(), weights, occurrences);
        }

        // Wrap helper
        private static int[] ReadWindow(ImageData image, int left, int bottom, int n)
        {
            var window = new int[n * n];
            for (int dy = 0; dy < n; dy++)
            {
                int y = (bottom + dy) % image.Height;
                for (int dx = 0; dx < n; dx++)
                    window[dx + dy * n] = image[(left + dx) % image.Width, y];
            }
            return window;
        }
    }
}
