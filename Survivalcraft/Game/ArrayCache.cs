using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game
{
    public class ArrayCache<T>
    {
        public ArrayCache(IEnumerable<int> bucketSizes, float minCacheRatio1, float minCacheTime1, float minCacheRatio2, float minCacheTime2)
        {
            this.m_buckets = Enumerable.ToArray<ArrayCache<T>.Bucket>(Enumerable.Select<int, ArrayCache<T>.Bucket>(Enumerable.OrderBy<int, int>(bucketSizes, (int s) => s), (int s) => new ArrayCache<T>.Bucket
            {
                Capacity = s
            }));
            this.m_minCacheRatio1 = minCacheRatio1;
            this.m_minCacheDuration1 = minCacheTime1;
            this.m_minCacheRatio2 = minCacheRatio2;
            this.m_minCacheDuration2 = minCacheTime2;
            this.m_minCacheRatioLastTime1 = Time.FrameStartTime;
            this.m_minCacheRatioLastTime2 = Time.FrameStartTime;
            Window.LowMemory += new Action(this.ClearCache);
            Time.QueueTimeDelayedExecution(0.0, new Action(this.CheckCache));
        }

        public T[] Rent(int capacity, bool clearArray)
        {
            object @lock = this.m_lock;
            T[] array2;
            lock (@lock)
            {
                ArrayCache<T>.Bucket bucket = this.GetBucket(capacity);
                if (bucket != null)
                {
                    if (bucket.Stack.Count > 0)
                    {
                        T[] array = bucket.Stack.Pop();
                        if (clearArray)
                        {
                            Array.Clear(array, 0, array.Length);
                        }
                        this.m_cachedCount -= (long)array.Length;
                        this.m_usedCount += (long)array.Length;
                        array2 = array;
                    }
                    else
                    {
                        this.m_usedCount += (long)bucket.Capacity;
                        array2 = new T[bucket.Capacity];
                    }
                }
                else
                {
                    array2 = new T[capacity];
                }
            }
            return array2;
        }

        public void Return(T[] array)
        {
            object @lock = this.m_lock;
            lock (@lock)
            {
                ArrayCache<T>.Bucket bucket = this.GetBucket(array.Length);
                if (bucket != null)
                {
                    bucket.Stack.Push(array);
                    this.m_cachedCount += (long)array.Length;
                    this.m_usedCount -= (long)array.Length;
                }
                float num = this.CalculateCacheRatio();
                if (num >= this.m_minCacheRatio1)
                {
                    this.m_minCacheRatioLastTime1 = Time.FrameStartTime;
                }
                if (num >= this.m_minCacheRatio2)
                {
                    this.m_minCacheRatioLastTime2 = Time.FrameStartTime;
                }
            }
        }

        private void CheckCache()
        {
            object @lock = this.m_lock;
            lock (@lock)
            {
                float num = this.CalculateCacheRatio();
                if ((num < this.m_minCacheRatio1 && Time.FrameStartTime - this.m_minCacheRatioLastTime1 > (double)this.m_minCacheDuration1) || (num < this.m_minCacheRatio2 && Time.FrameStartTime - this.m_minCacheRatioLastTime2 > (double)this.m_minCacheDuration2))
                {
                    this.ClearCache();
                }
                Time.QueueTimeDelayedExecution(Time.FrameStartTime + (double)(MathUtils.Min(this.m_minCacheDuration1, this.m_minCacheDuration2) / 5f), new Action(this.CheckCache));
            }
        }

        private ArrayCache<T>.Bucket GetBucket(int capacity)
        {
            for (int i = 0; i < this.m_buckets.Length; i++)
            {
                if (this.m_buckets[i].Capacity >= capacity)
                {
                    return this.m_buckets[i];
                }
            }
            return null;
        }

        private void ClearCache()
        {
            ArrayCache<T>.Bucket[] buckets = this.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i].Stack.Clear();
            }
            this.m_cachedCount = 0L;
            this.m_minCacheRatioLastTime1 = Time.FrameStartTime;
            this.m_minCacheRatioLastTime2 = Time.FrameStartTime;
        }

        private float CalculateCacheRatio()
        {
            if (this.m_cachedCount <= 0L)
            {
                return 1f;
            }
            return (float)this.m_usedCount / (float)(this.m_usedCount + this.m_cachedCount);
        }

        private object m_lock = new object();

        private ArrayCache<T>.Bucket[] m_buckets;

        private long m_cachedCount;

        private long m_usedCount;

        private float m_minCacheRatio1;

        private float m_minCacheDuration1;

        private double m_minCacheRatioLastTime1;

        private float m_minCacheRatio2;

        private float m_minCacheDuration2;

        private double m_minCacheRatioLastTime2;

        private class Bucket
        {
            public int Capacity;

            public Stack<T[]> Stack = new Stack<T[]>();
        }
    }
}
