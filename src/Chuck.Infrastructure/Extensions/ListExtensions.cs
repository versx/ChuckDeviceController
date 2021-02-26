namespace Chuck.Infrastructure.Extensions
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
            var count = ts.Count;
            for (var i = 0; i < count - 1; ++i)
            {
                var r = _rand.Next(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }
}