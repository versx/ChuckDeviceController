namespace ChuckDeviceConfigurator.Collections
{
    // Credits: http://davidknopp.net/code-samples/indexed-priority-queue-c-unity/
    public sealed class IndexedPriorityQueue<T> where T : IComparable
    {
        #region Variables

        private List<T> _objects;
        private int[] _heap;
        private int[] _heapInverse;
        private int _count;

        #endregion

        #region Properties

        public int Count => _count;

        public T this[int index]
        {
            get
            {
                /*
                Assert.IsTrue(index < _objects.Length && index >= 0,
                               string.Format("IndexedPriorityQueue.[]: Index '{0}' out of range", index));
                */
                return _objects[index];
            }
            set
            {
                /*
                Assert.IsTrue(index < _objects.Length && index >= 0,
                               string.Format("IndexedPriorityQueue.[]: Index '{0}' out of range", index));
                */
                Set(index, value);
            }
        }

        public IReadOnlyList<T> Values => _objects;

        #endregion

        #region Constructor

        public IndexedPriorityQueue(int maxSize)
        {
            Resize(maxSize);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inserts a new value with the given index
        /// </summary>
        /// <param name="index">index to insert at</param>
        /// <param name="value">value to insert</param>
        public void Insert(int index, T value)
        {
            /*
            Assert.IsTrue(index < _objects.Length && index >= 0,
                           string.Format("IndexedPriorityQueue.Insert: Index '{0}' out of range", index));
            */

            ++_count;

            if (_objects.Contains(value))
            {
                // update index
                Set(index, value);
            }
            else
            {
                // add object
                _objects.Insert(index, value);
            }

            // add to heap
            _heapInverse[index] = _count;
            _heap[_count] = index;

            // update heap
            SortHeapUpward(_count);
        }

        /// <summary>
        /// Gets the top element of the queue
        /// </summary>
        /// <returns>The top element</returns>
        public T Top()
        {
            // top of heap [first element is 1, not 0]
            return _objects[_heap[1]];
        }

        /// <summary>
        /// Removes the top element from the queue
        /// </summary>
        /// <returns>The removed element</returns>
        public T Pop()
        {
            //Assert.IsTrue(_count > 0, "IndexedPriorityQueue.Pop: Queue is empty");

            if (_count == 0)
            {
                return default(T);
            }

            // swap front to back for removal
            Swap(1, _count--);

            // re-sort heap
            SortHeapDownward(1);

            // return popped object
            return _objects[_heap[_count + 1]];
        }

        public T PopLast()
        {
            if (_count == 0)
            {
                return default(T);
            }

            // swap front to back for removal
            Swap(1, _count--);

            var last = _objects[_heap[_count]];

            // re-sort heap
            SortHeapDownward(1);

            // return popped object
            return _objects[_heap[_count + 1]];
        }

        public bool Contains(T obj) => _objects.Contains(obj);

        public void RemoveAt(int index)
        {
            // TODO: Potentially need to lock _objects
            if (index >= 0 && index <= _objects.Count)
            {
                _objects.RemoveAt(index);
            }
        }

        public int IndexOf(T obj) => _objects.IndexOf(obj);

        /// <summary>
        /// Updates the value at the given index. Note that this function is not
        /// as efficient as the DecreaseIndex/IncreaseIndex methods, but is
        /// best when the value at the index is not known
        /// </summary>
        /// <param name="index">index of the value to set</param>
        /// <param name="obj">new value</param>
        public void Set(int index, T obj)
        {
            if (obj.CompareTo(_objects[index]) <= 0)
            {
                DecreaseIndex(index, obj);
            }
            else
            {
                IncreaseIndex(index, obj);
            }
        }

        /// <summary>
        /// Decrease the value at the current index
        /// </summary>
        /// <param name="index">index to decrease value of</param>
        /// <param name="obj">new value</param>
        public void DecreaseIndex(int index, T obj)
        {
            /*
            Assert.IsTrue(index < _objects.Length && index >= 0,
                           string.Format("IndexedPriorityQueue.DecreaseIndex: Index '{0}' out of range",
                           index));
            Assert.IsTrue(obj.CompareTo(_objects[index]) <= 0,
                           string.Format("IndexedPriorityQueue.DecreaseIndex: object '{0}' isn't less than current value '{1}'",
                           obj, _objects[index]));
            */

            _objects[index] = obj;
            SortUpward(index);
        }

        /// <summary>
        /// Increase the value at the current index
        /// </summary>
        /// <param name="index">index to increase value of</param>
        /// <param name="obj">new value</param>
        public void IncreaseIndex(int index, T obj)
        {
            /*
            Assert.IsTrue(index < _objects.Length && index >= 0,
                          string.Format("IndexedPriorityQueue.DecreaseIndex: Index '{0}' out of range",
                          index));
            Assert.IsTrue(obj.CompareTo(_objects[index]) >= 0,
                           string.Format("IndexedPriorityQueue.DecreaseIndex: object '{0}' isn't greater than current value '{1}'",
                           obj, _objects[index]));
            */

            _objects[index] = obj;
            SortDownward(index);
        }

        public void Clear()
        {
            _count = 0;
        }

        /// <summary>
        /// Set the maximum capacity of the queue
        /// </summary>
        /// <param name="maxSize">new maximum capacity</param>
        public void Resize(int maxSize)
        {
            /*
            Assert.IsTrue(maxSize >= 0,
                           string.Format("IndexedPriorityQueue.Resize: Invalid size '{0}'", maxSize));
            */

            _objects = new List<T>(maxSize);
            _heap = new int[maxSize + 1];
            _heapInverse = new int[maxSize];
            _count = 0;
        }

        #endregion

        #region Private Methods

        private void SortUpward(int index)
        {
            SortHeapUpward(_heapInverse[index]);
        }

        private void SortDownward(int index)
        {
            SortHeapDownward(_heapInverse[index]);
        }

        private void SortHeapUpward(int heapIndex)
        {
            // move toward top if better than parent
            while (heapIndex > 1 &&
                    _objects[_heap[heapIndex]].CompareTo(_objects[_heap[Parent(heapIndex)]]) < 0)
            {
                // swap this node with its parent
                Swap(heapIndex, Parent(heapIndex));

                // reset iterator to be at parents old position
                // (child's new position)
                heapIndex = Parent(heapIndex);
            }
        }

        private void SortHeapDownward(int heapIndex)
        {
            // move node downward if less than children
            while (FirstChild(heapIndex) <= _count)
            {
                int child = FirstChild(heapIndex);

                // find smallest of two children (if 2 exist)
                if (child < _count &&
                     _objects[_heap[child + 1]].CompareTo(_objects[_heap[child]]) < 0)
                {
                    ++child;
                }

                // swap with child if less
                if (_objects[_heap[child]].CompareTo(_objects[_heap[heapIndex]]) < 0)
                {
                    Swap(child, heapIndex);
                    heapIndex = child;
                }
                // no swap necessary
                else
                {
                    break;
                }
            }
        }

        private void Swap(int i, int j)
        {
            // swap elements in heap
            int temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;

            // reset inverses
            _heapInverse[_heap[i]] = i;
            _heapInverse[_heap[j]] = j;
        }

        private static int Parent(int heapIndex)
        {
            return (heapIndex / 2);
        }

        private static int FirstChild(int heapIndex)
        {
            return heapIndex * 2;
        }

        private static int SecondChild(int heapIndex)
        {
            return heapIndex * 2 + 1;
        }

        #endregion
    }
}