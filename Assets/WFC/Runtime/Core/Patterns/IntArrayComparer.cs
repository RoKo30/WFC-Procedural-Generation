using System.Collections.Generic;

namespace ProceduralGen.Wfc.Core
{
    internal sealed class IntArrayComparer : IEqualityComparer<int[]>
    {
        public static readonly IntArrayComparer Instance = new IntArrayComparer();

        public bool Equals(int[] a, int[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        public int GetHashCode(int[] values)
        {
            unchecked
            {
                int hash = (int)2166136261;
                for (int i = 0; i < values.Length; i++)
                    hash = (hash ^ values[i]) * 16777619;
                return hash;
            }
        }
    }
}
