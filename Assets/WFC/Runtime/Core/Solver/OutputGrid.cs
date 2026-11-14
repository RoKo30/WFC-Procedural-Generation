using System;

namespace ProceduralGen.Wfc.Core
{
    // Layout of the output
    public sealed class OutputGrid
    {
        private readonly int[] _neighbours; // cell * 4 + direction, -1 when none

        public int CellsX { get; }
        public int CellsY { get; }
        public int Count => CellsX * CellsY;

        public int KernelSize { get; }
        public int Stride { get; }
        public bool Periodic { get; }

        public int ImageWidth { get; }
        public int ImageHeight { get; }

        private OutputGrid(int cellsX, int cellsY, int kernelSize, int stride, bool periodic, int imageWidth, int imageHeight)
        {
            CellsX = cellsX;
            CellsY = cellsY;
            KernelSize = kernelSize;
            Stride = stride;
            Periodic = periodic;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;

            _neighbours = new int[Count * Directions.Count];
            for (int y = 0; y < cellsY; y++)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    int cell = x + y * cellsX;
                    for (int d = 0; d < Directions.Count; d++)
                        _neighbours[cell * Directions.Count + d] = ComputeNeighbour(x + Directions.DX[d], y + Directions.DY[d]);
                }
            }
        }

        public static OutputGrid Create(int width, int height, int kernelSize, int stride, bool periodic)
        {
            if (width < 1 || height < 1)
                throw new ArgumentException($"Output size must be positive, got {width}x{height}.");
            if (stride < 1 || stride >= kernelSize)
                throw new ArgumentException($"Stride must be between 1 and {kernelSize - 1}, got {stride}.");

            if (periodic)
            {
                int cx = CeilDiv(width, stride);
                int cy = CeilDiv(height, stride);
                return new OutputGrid(cx, cy, kernelSize, stride, true, cx * stride, cy * stride);
            }

            int cellsX = width <= kernelSize ? 1 : CeilDiv(width - kernelSize, stride) + 1;
            int cellsY = height <= kernelSize ? 1 : CeilDiv(height - kernelSize, stride) + 1;
            return new OutputGrid(cellsX, cellsY, kernelSize, stride, false, width, height);
        }

        public int CellX(int cell) => cell % CellsX;
        public int CellY(int cell) => cell / CellsX;
        public int CellIndex(int x, int y) => x + y * CellsX;

        public int Neighbour(int cell, int direction) => _neighbours[cell * Directions.Count + direction];

        // d is set when the cell has a neighbor in direction d
        public int NeighbourMask(int cell)
        {
            int mask = 0;
            for (int d = 0; d < Directions.Count; d++)
                if (Neighbour(cell, d) >= 0) mask |= 1 << d;
            return mask;
        }

        public int CellAtPixel(int px, int py, out int localIndex)
        {
            int cx = Math.Min(px / Stride, CellsX - 1);
            int cy = Math.Min(py / Stride, CellsY - 1);
            int lx = px - cx * Stride;
            int ly = py - cy * Stride;
            localIndex = lx + ly * KernelSize;
            return CellIndex(cx, cy);
        }

        private int ComputeNeighbour(int x, int y)
        {
            if (Periodic)
            {
                x = (x % CellsX + CellsX) % CellsX;
                y = (y % CellsY + CellsY) % CellsY;
                return CellIndex(x, y);
            }
            if (x < 0 || y < 0 || x >= CellsX || y >= CellsY) return -1;
            return CellIndex(x, y);
        }

        private static int CeilDiv(int a, int b) => (a + b - 1) / b;
    }
}
