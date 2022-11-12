namespace ChuckDeviceController.Collections.Cache
{
    // Credits: https://codereview.stackexchange.com/questions/229094/leetcode-time-based-key-value-store-c
    public class TimedMap<T>
    {
        private static readonly object _lock = new();
        private readonly Dictionary<string, List<KeyValuePair<ulong, T>>> _entries;

        public TimedMap()
        {
            _entries = new Dictionary<string, List<KeyValuePair<ulong, T>>>();
        }

        public void Set(string key, T value, ulong timestamp)
        {
            /*
            if (!_entries.TryGetValue(key, out var list))
            {
                if (_entries.ContainsKey(key))
                {
                    _entries[key] = new List<KeyValuePair<ulong, T>>();
                }
                else
                {
                    _entries.Add(key, new List<KeyValuePair<ulong, T>>());
                }
                list = _entries[key];
            }
            list.Add(new(timestamp, value));
            */
            lock (_lock)
            {
                if (!_entries.ContainsKey(key))
                {
                    _entries.Add(key, new List<KeyValuePair<ulong, T>>());
                }
                _entries[key].Add(new(timestamp, value));
            }
        }

        public T? Get(string key, ulong timestamp)
        {
            lock (_lock)
            {
                if (!_entries.ContainsKey(key))
                {
                    return default;
                }

                var list = _entries[key];
                //var i = list.BinarySearch(new KeyValuePair<ulong, T>(timestamp, "}"), new TimeComparer());
                var i = list.BinarySearch(new KeyValuePair<ulong, T>(timestamp, default), new TimeComparer());
                if (i >= 0)
                {
                    return list[i].Value;
                }
                else if (i == -1)
                {
                    return default;
                }

                // If BinarySearch returns a negative number, it respresents the binary complement of
                // the index of the first item in list that is higher than the item we looked for.
                // Since we want the first item that is one lower, -1.
                var tempKey = ~i - 1;
                return list[tempKey].Value;
            }
        }

        private class TimeComparer : IComparer<KeyValuePair<ulong, T>>
        {
            public int Compare(
                KeyValuePair<ulong, T> x,
                KeyValuePair<ulong, T> y) => x.Key.CompareTo(y.Key);
        }
    }
}