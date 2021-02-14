namespace ChuckDeviceController.Services.Queues
{
    using System.Collections.Generic;

    using ChuckDeviceController.Data.Entities;

    public class S2CellQueue : BaseQueue<Cell>
    {
    }

    public interface IQueue<T>
    {
        public void Start();

        public void Stop();

        public void Enqueue(T data);

        public T Dequeue(int amount = 10);
    }

    public abstract class BaseQueue<T> : IQueue<T> where T : BaseEntity
    {
        private readonly Queue<T> _queue;

        protected BaseQueue()
        {
            _queue = new Queue<T>();
        }

        public T Dequeue(int amount = 10)
        {
            if (_queue.Count == 0)
            {
                return null;
            }
            return _queue.Dequeue();
        }

        public void Enqueue(T data)
        {
            _queue.Enqueue(data);
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}