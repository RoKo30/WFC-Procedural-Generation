using System;
using ProceduralGen.Wfc.Core;
using UnityEngine;

namespace ProceduralGen.Wfc
{
    // Draws the current state of a solver into Texture2D
    public sealed class WfcTextureRenderer : IDisposable
    {
        private readonly PatternSet _patterns;
        private readonly OutputGrid _grid;
        private readonly Color32[] _palette;
        private readonly Color32[] _pixels;
        private readonly int[] _pixelCell;
        private readonly int[] _pixelLocal;
        private readonly int[] _cellPattern;

        public Texture2D Texture { get; private set; }

        public WfcTextureRenderer(PatternSet patterns, OutputGrid grid)
        {
            _patterns = patterns;
            _grid = grid;
            _palette = TextureConversion.ToColors(patterns.Palette);

            int width = grid.ImageWidth, height = grid.ImageHeight;
            _pixels = new Color32[width * height];
            _pixelCell = new int[_pixels.Length];
            _pixelLocal = new int[_pixels.Length];
            _cellPattern = new int[grid.Count];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = x + y * width;
                    _pixelCell[i] = grid.CellAtPixel(x, y, out _pixelLocal[i]);
                }

            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "WFC Output",
                filterMode = FilterMode.Point,
                wrapMode = grid.Periodic ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
        }

        public void Render(WfcSolver solver, bool showSuperposition, Color32 undecided)
        {
            for (int cell = 0; cell < _cellPattern.Length; cell++)
                _cellPattern[cell] = solver.GetDecidedPattern(cell);

            for (int i = 0; i < _pixels.Length; i++)
            {
                int cell = _pixelCell[i];
                int pattern = _cellPattern[cell];
                if (pattern >= 0)
                    _pixels[i] = _palette[_patterns.GetPixel(pattern, _pixelLocal[i])];
                else if (showSuperposition && solver.PossibleCount(cell) > 0)
                    _pixels[i] = Average(solver, cell, _pixelLocal[i], undecided);
                else
                    _pixels[i] = undecided;
            }

            Texture.SetPixels32(_pixels);
            Texture.Apply(false);
        }

        private Color32 Average(WfcSolver solver, int cell, int local, Color32 fallback)
        {
            double r = 0, g = 0, b = 0, a = 0, total = 0;
            for (int t = 0; t < _patterns.Count; t++)
            {
                if (!solver.IsPossible(cell, t)) continue;
                double w = _patterns.GetWeight(t);
                Color32 c = _palette[_patterns.GetPixel(t, local)];
                r += c.r * w; g += c.g * w; b += c.b * w; a += c.a * w;
                total += w;
            }
            if (total <= 0) return fallback;
            return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), (byte)(a / total));
        }

        public void Dispose()
        {
            if (Texture == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(Texture);
            else UnityEngine.Object.DestroyImmediate(Texture);
            Texture = null;
        }
    }
}
