namespace ChuckDeviceController.Collections.Queues;

using System.Collections;

public class SortedPriorityQueue<T> : ICollection<T>
{
    // Credits: https://stackoverflow.com/a/40846923

    #region Constants

    private const int DefaultMaxCapacity = 100;

    #endregion

    #region Variables

    private readonly object _lock = new();
    private readonly List<T> _innerList;
    private readonly IComparer<T> _comparer;
    private readonly int _maxCapacity;

    #endregion

    #region Properties

    public int Count => _innerList.Count;

    public bool IsReadOnly => false;

    public T this[int index] => _innerList[index];

    #endregion

    #region Constructors

    public SortedPriorityQueue()
        : this(DefaultMaxCapacity, Comparer<T>.Default)
    {
    }

    public SortedPriorityQueue(int maxCapacity, IComparer<T> comparer)
    {
        _innerList = new List<T>();
        _maxCapacity = maxCapacity;
        _comparer = comparer;
    }

    #endregion

    public void Enqueue(T item) => Add(item);

    public T Dequeue()
    {
        T item;
        lock (_lock)
        {
            if (!_innerList.Any())
            {
                return default!;
            }

            item = _innerList[0];
            _innerList.RemoveAt(0);
        }

        EnsureCapacity();
        return item;
    }

    public T DequeueLast()
    {
        T item;
        lock (_lock)
        {
            if (!_innerList.Any())
            {
                return default!;
            }

            var lastIndex = _innerList.Count - 1; // ^1;
            item = _innerList[lastIndex];
            _innerList.RemoveAt(lastIndex);
        }

        EnsureCapacity();
        return item;
    }

    #region FindIndex

    public int FindIndex(Predicate<T> match) => FindIndex(0, Count, match);

    public int FindIndex(int startIndex, Predicate<T> match)
    {
        lock (_lock)
        {
            var index = _innerList.FindIndex(startIndex, match);
            return index;
        }
    }

    public int FindIndex(int startIndex, int count, Predicate<T> match)
    {
        lock (_lock)
        {
            var index = _innerList.FindIndex(startIndex, count, match);
            return index;
        }
    }

    #endregion

    #region LastIndexOf

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at the end
    // and ending at the first element in the list. The elements of the list
    // are compared to the given value using the Object.Equals method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public int LastIndexOf(T item)
    {
        if (Count == 0)
        {  // Special case for empty list
            return -1;
        }
        else
        {
            return LastIndexOf(item, Count - 1, Count);
        }
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at index
    // index and ending at the first element in the list. The
    // elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public int LastIndexOf(T item, int index)
    {
        if (index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return LastIndexOf(item, index, index + 1);
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at index
    // index and upto count elements. The elements of
    // the list are compared to the given value using the Object.Equals
    // method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public int LastIndexOf(T item, int index, int count)
    {
        if ((Count != 0) && (index < 0))
        {
            throw new IndexOutOfRangeException(nameof(index));
        }

        if ((Count != 0) && (count < 0))
        {
            throw new IndexOutOfRangeException(nameof(count));
        }

        if (Count == 0)
        {  // Special case for empty list
            return -1;
        }

        if (index >= Count)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }

        if (count > index + 1)
        {
            throw new IndexOutOfRangeException(nameof(count));
        }

        lock (_lock)
        {
            var items = _innerList.ToArray();
            return Array.LastIndexOf(items, item, index, count);
        }
    }

    public uint? LastIndexOf(T item, Func<T, uint?> match)
    {
        var lastIndex = match(item);
        return lastIndex;
    }

    #endregion

    public void Insert(int index, T item)
    {
        lock (_lock)
        {
            //var insertIndex = FindIndexForSortedInsert(_innerList, _comparer, item);
            //_innerList.Insert(insertIndex, item);
            _innerList.Insert(index, item);
        }
        EnsureCapacity();
    }


    public void Add(T item)
    {
        lock (_lock)
        {
            var insertIndex = FindIndexForSortedInsert(_innerList, _comparer, item);
            _innerList.Insert(insertIndex, item);
        }
        EnsureCapacity();
    }

    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index
    /// of the first occurrence within the entire SortedList<T>.
    /// </summary>
    public int IndexOf(T item)
    {
        lock (_lock)
        {
            var insertIndex = FindIndexForSortedInsert(_innerList, _comparer, item);
            if (insertIndex == _innerList.Count)
            {
                return -1;
            }
            if (_comparer.Compare(item, _innerList[insertIndex]) == 0)
            {
                var index = insertIndex;
                while (index > 0 && _comparer.Compare(item, _innerList[index - 1]) == 0)
                {
                    index--;
                }
                return index;
            }
            return -1;
        }
    }

    public bool Remove(T item)
    {
        lock (_lock)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                _innerList.RemoveAt(index);
                EnsureCapacity();
                return true;
            }
            return false;
        }
    }

    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            _innerList.RemoveAt(index);
        }
        EnsureCapacity();
    }

    public void CopyTo(T[] array)
    {
        lock (_lock)
        {
            _innerList.CopyTo(array);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_lock)
        {
            _innerList.CopyTo(array, arrayIndex);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _innerList.Clear();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            return _innerList.GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => _innerList.GetEnumerator();

    private static int FindIndexForSortedInsert(List<T> list, IComparer<T> comparer, T item)
    {
        if (list.Count == 0)
        {
            return 0;
        }

        var lowerIndex = 0;
        var upperIndex = list.Count - 1;
        int comparisonResult;
        while (lowerIndex < upperIndex)
        {
            var middleIndex = (lowerIndex + upperIndex) / 2;
            var middle = list[middleIndex];
            comparisonResult = comparer.Compare(middle, item);
            if (comparisonResult == 0)
            {
                return middleIndex;
            }
            else if (comparisonResult > 0) // middle > item
            {
                upperIndex = middleIndex - 1;
            }
            else // middle < item
            {
                lowerIndex = middleIndex + 1;
            }
        }

        // At this point any entry following 'middle' is greater than 'item',
        // and any entry preceding 'middle' is lesser than 'item'.
        // So we either put 'item' before or after 'middle'.
        comparisonResult = comparer.Compare(list[lowerIndex], item);
        if (comparisonResult < 0) // middle < item
        {
            return lowerIndex + 1;
        }
        return lowerIndex;
    }

    private void EnsureCapacity()
    {
        lock (_lock)
        {
            if (_innerList.Count <= _maxCapacity)
                return;

            var count = Math.Min(_maxCapacity, _innerList.Count + 1);
            var diff = _innerList.Count - _maxCapacity;
            var startIndex = count - diff;
            var indexes = Enumerable.Range(startIndex, diff);
            foreach (var index in indexes)
            {
                _innerList.RemoveAt(index);
            }
        }
    }
}