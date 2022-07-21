namespace ChuckDeviceConfigurator.Collections
{
    using System.Collections;

    public class PokemonPriorityQueue<T> : IList<T>
    {
        private readonly List<T> _queue;

        #region Properties

        public int MaxCapacity { get; private set; }

        public int Count => _queue.Count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                // TODO: Validation checks
                return _queue[index];
            }
            set
            {
                // TODO: Validation checks
                _queue[index] = value;
            }
        }

        #endregion

        #region Constructor

        public PokemonPriorityQueue(int maxCapacity)
        {
            MaxCapacity = maxCapacity;
            _queue = new List<T>(maxCapacity);
        }

        #endregion

        #region Public Methods

        public T Pop()
        {
            var obj = _queue.FirstOrDefault();
            _queue.RemoveAt(0);

            return obj;
        }

        public T PopLast()
        {
            var obj = _queue.LastOrDefault();
            _queue.RemoveAt(_queue.Count);

            return obj;
        }

        public int IndexOf(T item) => _queue.IndexOf(item);

        public void Insert(int index, T item)
        {
            _queue.Insert(index, item);

            EnsureMaxCapacity();
        }

        public void RemoveAt(int index) => _queue.RemoveAt(index);

        public void Add(T item)
        {
            _queue.Add(item);

            EnsureMaxCapacity();
        }

        public void Clear() => _queue.Clear();

        public bool Contains(T item) => _queue.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item) => _queue.Remove(item);

        public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

        #endregion

        #region Private Methods

        IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();

        private void EnsureMaxCapacity()
        {
            if (_queue.Count > MaxCapacity)
            {
                // TODO: Remove excess
                while (_queue.Count > MaxCapacity)
                {
                    _queue.RemoveAt(_queue.Count);
                }
            }
        }

        #endregion
    }
}