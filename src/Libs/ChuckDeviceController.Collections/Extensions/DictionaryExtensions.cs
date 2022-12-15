namespace ChuckDeviceController.Collections.Extensions
{
    public static class DictionaryExtensions
    {
        public static SortedDictionary<TKey, TValue> ToSorted<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            IComparer<TKey> comparer)
            where TKey : notnull
        {
            var sorted = new SortedDictionary<TKey, TValue>(dict, comparer);
            return sorted;
        }
    }
}