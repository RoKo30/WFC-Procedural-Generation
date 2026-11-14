using System;

namespace ProceduralGen.Wfc.Core
{
    // Produce rotated/mirrored copies of N×N pattern
    public static class Symmetry
    {
        public const int Max = 8;

        public static int[][] Variants(int[] pattern, int size, int count)
        {
            if (count < 1 || count > Max)
                throw new ArgumentOutOfRangeException(nameof(count), $"Symmetry must be between 1 and {Max}.");

            var variants = new int[count][];
            variants[0] = pattern;
            for (int i = 1; i < count; i++)
            {
                // Odd entries mirror the previous entry, even entries rotate the entry two steps back
                variants[i] = (i & 1) == 1 ? Reflect(variants[i - 1], size) : Rotate(variants[i - 2], size);
            }
            return variants;
        }

        // Rotate the pattern 90 degrees
        public static int[] Rotate(int[] p, int n)
        {
            var result = new int[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    result[x + y * n] = p[n - 1 - y + x * n];
            return result;
        }

        // Mirrors the pattern horizontally
        public static int[] Reflect(int[] p, int n)
        {
            var result = new int[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    result[x + y * n] = p[n - 1 - x + y * n];
            return result;
        }
    }
}
