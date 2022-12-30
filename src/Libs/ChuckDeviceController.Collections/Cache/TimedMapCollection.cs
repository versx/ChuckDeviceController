namespace ChuckDeviceController.Collections.Cache;

public record TimedMapEntry<TValue>(ulong Time, TValue Value);

public class TimedMapCollection<TKey, TValue>
    where TKey : IEquatable<TKey>
    where TValue : struct
{
    private readonly Dictionary<TKey, List<TimedMapEntry<TValue>>> _entries;
    private readonly object _lock = new();
    private readonly uint _length;

    public uint Count => _length;

    public TimedMapCollection(uint length)
    {
        _length = length;
        _entries = new Dictionary<TKey, List<TimedMapEntry<TValue>>>();
    }

    public void SetValue(TKey key, TimedMapEntry<TValue> entry)
    {
        SetValue(key, entry.Value, entry.Time);
    }

    public void SetValue(TKey key, TValue value, ulong time)
    {
        lock (_lock )
        {
            if (_entries.TryGetValue(key, out var result))
            {
                var lastIndex = result?.FindLastIndex(v => v.Time >= time);
                if (lastIndex > -1)
                {
                    _entries[key].Insert(lastIndex ?? 0, new(time, value));
                }
                else
                {
                    _entries[key].Add(new(time, value));
                }
                if (_entries[key].Count > _length)
                {
                    _entries[key].RemoveAt(0);
                }
            }
            else
            {
                _entries.Add(key, new() { new(time, value) });
            }
        }
    }

    public TValue? GetValueAt(TKey key, ulong time)
    {
        TValue? value = null;
        lock (_lock )
        {
            if (_entries.TryGetValue(key, out var result))
            {
                value = result.FindLast(x => x.Time >= time)?.Value;
            }
        }
        return value;
    }
}