namespace ChuckDeviceController.Collections;

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Thread safe concurrent collection.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <reference>https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.iproducerconsumercollection-1?view=net-7.0</reference>
public class SafeCollection<T> : IProducerConsumerCollection<T>
{
    #region Variables

    private readonly object _lock = new();
    private readonly List<T> _entities;

    #endregion

    #region Properties

    public int Count => _entities?.Count ?? 0;

    public bool IsSynchronized => true;

    public object SyncRoot => _lock;

    public T? this[int index]
    {
        get
        {
            if (index < _entities.Count)
            {
                return _entities[index];
            }
            return default;
        }
    }

    #endregion

    #region Constructors

    public SafeCollection()
    {
        _entities = new List<T>();
    }

    public SafeCollection(IEnumerable<T> collection)
    {
        _entities = new(collection);
    }

    #endregion

    #region Collection Impl

    public void AddRange(IEnumerable<T> items)
    {
        lock (_lock)
        {
            _entities.AddRange(items);
        }
    }

    public bool TryAdd(T item)
    {
        lock (_lock) _entities.Add(item);
        return true;
    }

    public bool TryTake([MaybeNullWhen(false)] out T item)
    {
        var value = true;
        lock (_lock)
        {
            if (_entities.Count == 0)
            {
                item = default;
                value = false;
            }
            else
            {
                item = _entities.FirstOrDefault();
                if (!_entities.Remove(item!))
                {
                    // Failed to remove item from list
                }
            }
        }
        return value;
    }

    public void CopyTo(T[] array, int index)
    {
        lock (_lock)
        {
            _entities.CopyTo(array, index);
        }
    }

    public void CopyTo(Array array, int index)
    {
        lock (_lock)
        {
            ((ICollection)_entities).CopyTo(array, index);
        }
    }

    public T[] ToArray()
    {
        T[]? value = null;
        lock (_lock)
        {
            value = _entities.ToArray();
        }
        return value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        List<T>? copy = null;
        lock (_lock)
        {
            copy = new(_entities);
        }
        return copy.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    #endregion

    #region Extensions

    public IEnumerable<T> Take(int count, CancellationToken stoppingToken = default)
    {
        IEnumerable<T>? entities = null;
        lock (_lock)
        {
            var index = 0;
            var minTake = Math.Min(count, _entities.Count);
            entities = _entities
                .TakeWhile(_ => _entities.Any() && index++ < minTake)
                .ToList();
            _entities.RemoveAll(entities.Contains);
        }
        return entities;
    }

    public IEnumerable<T> TryTake(int count)
    {
        try
        {
            var entities = Take(count);
            return entities;
        }
        catch (Exception)
        {
            return Array.Empty<T>();
        }
    }

    public T TakeFirst()
    {
        T entity;
        lock (_lock)
        {
            entity = _entities[0];
            _entities.RemoveAt(0);
        }
        return entity;
    }

    public T TakeLast()
    {
        T entity;
        lock (_lock)
        {
            var lastIndex = _entities.Count - 1;
            entity = _entities[lastIndex];
            _entities.RemoveAt(lastIndex);
        }
        return entity;
    }

    public T? Get(Predicate<T> predicate)
    {
        T? item = default;
        lock (_lock)
        {
            if (_entities.Count == 0)
            {
                item = default;
            }
            else
            {
                item = _entities.Find(predicate);
            }
        }
        return item;
    }

    public T? TryGet(Predicate<T> predicate)
    {
        try
        {
            var item = Get(predicate);
            return item;
        }
        catch (Exception ex)
        {
            return default;
        }
    }

    public bool Remove(Predicate<T> predicate)
    {
        lock (_lock)
        {
            var count = _entities.RemoveAll(predicate);
            return count > 0;
        }
    }

    public void Remove(T item)
    {
        lock (_lock)
        {
            _entities.Remove(item);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entities.Clear();
        }

        #endregion
    }
}