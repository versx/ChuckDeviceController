namespace ChuckDeviceController.Extensions
{
    using System;
    using System.Collections.Generic;

    public static class ListExtensions
    {
        private static readonly Random _rand = new Random();

        /// <summary>
        /// Shuffles the element order of the specified list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> ts)
        {
            int count = ts.Count;
            for (int i = 0; i < count - 1; ++i)
            {
                int r = _rand.Next(i, count);
                T tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }
}