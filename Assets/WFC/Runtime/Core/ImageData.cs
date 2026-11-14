using System;
using System.Collections.Generic;

namespace ProceduralGen.Wfc.Core
{
    // Engine independent image class
    // Pixels are row-major with (0,0) in the bottom-left corner
    public sealed class ImageData
    {
        public int Width { get; }
        public int Height { get; }

        // Palette index per pixel, x + y * Width
        public int[] Pixels { get; }
        
        public int[] Palette { get; }

        public ImageData(int width, int height, int[] pixels, int[] palette)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException($"Image size must be positive, got {width}x{height}.");
            if (pixels == null || pixels.Length != width * height)
                throw new ArgumentException("Pixel array length must equal width * height.");

            Width = width;
            Height = height;
            Pixels = pixels;
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));
        }

        public int this[int x, int y] => Pixels[x + y * Width];

        // Builds an indexed image from packed RGBA colors, collecting distinct colors into a palette
        public static ImageData FromRgba(int width, int height, IReadOnlyList<int> rgba)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));

            var lookup = new Dictionary<int, int>();
            var palette = new List<int>();
            var pixels = new int[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                int colour = rgba[i];
                if (!lookup.TryGetValue(colour, out int index)) // first time seeing a color
                {
                    index = palette.Count;
                    lookup.Add(colour, index);
                    palette.Add(colour);
                }
                pixels[i] = index;
            }

            return new ImageData(width, height, pixels, palette.ToArray());
        }

        public static int PackRgba(byte r, byte g, byte b, byte a) => (r << 24) | (g << 16) | (b << 8) | a; // rgba to int

        public static void UnpackRgba(int rgba, out byte r, out byte g, out byte b, out byte a) // int to rgba
        {
            r = (byte)(rgba >> 24);
            g = (byte)(rgba >> 16);
            b = (byte)(rgba >> 8);
            a = (byte)rgba;
        }
    }
}
