namespace ChuckDeviceController.Services.Queues
{
    using System.Collections.Generic;

    public interface IQueue<T>
    {
        public int Count { get; }

        public void Enqueue(T data);

        public void Enqueue(List<T> data);

        public T Dequeue();

        public List<T> Dequeue(int amount = 10);
    }
}