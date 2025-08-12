using System;
using System.Linq;
using Engine;

namespace Game
{
    public class TerrainGeometryDynamicArray<T> : DynamicArray<T>, IDisposable
    {
        public void Dispose()
        {
            base.Count = 0;
            base.Capacity = 0;
        }

        protected override T[] Allocate(int capacity)
        {
            return TerrainGeometryDynamicArray<T>.m_cache.Rent(capacity, false);
        }

        protected override void Free(T[] array)
        {
            TerrainGeometryDynamicArray<T>.m_cache.Return(array);
        }

        private static ArrayCache<T> m_cache = new ArrayCache<T>(Enumerable.Select<int, int>(Enumerable.Range(4, 30), (int n) => 1 << n), 0.66f, 60f, 0.33f, 5f);
    }
}
