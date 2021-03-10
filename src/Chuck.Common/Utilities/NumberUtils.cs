namespace Chuck.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class NumberUtils
    {
        public static List<T> GenerateRange<T>(string ids, int min, int max)
        {
            if (string.IsNullOrEmpty(ids))
                return new List<T>();
            if (ids == "*")
            {
                return (List<T>)Enumerable.Range(min, max);
            }
            return (List<T>)ids.Split('\n').Select(int.Parse);
        }
    }
}