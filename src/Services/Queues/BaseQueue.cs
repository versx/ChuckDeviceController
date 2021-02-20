namespace ChuckDeviceController.Services.Queues
{
    using ChuckDeviceController.Data.Entities;
    using System.Collections.Generic;

    public abstract class BaseQueue<T> : IQueue<T> where T : BaseEntity
    {
        private readonly Queue<T> _queue;

        protected BaseQueue()
        {
            _queue = new Queue<T>();
        }

        public int Count => _queue?.Count ?? 0;

        public List<T> Dequeue(int amount = 10)
        {
            if (_queue.Count == 0)
            {
                return null;
            }

            List<T> list = new List<T>();
            for (int i = 0; i < amount; i++)
            {
                list.Add(_queue.Dequeue());
            }
            return list;
        }

        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        public void Enqueue(List<T> data)
        {
            if (data?.Count == 0)
            {
                return;
            }

            data.ForEach(Enqueue);
        }

        public void Enqueue(T data)
        {
            _queue.Enqueue(data);
        }
    }
}