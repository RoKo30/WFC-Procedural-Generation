namespace ProceduralGen.Wfc.Core
{
    public static class Directions
    {
        public const int Count = 4;

        public const int Right = 0;
        public const int Up = 1;
        public const int Left = 2;
        public const int Down = 3;

        public static readonly int[] DX = { 1, 0, -1, 0 };
        public static readonly int[] DY = { 0, 1, 0, -1 };

        public static int Opposite(int direction) => (direction + 2) & 3; // opposite direction
    }
}
