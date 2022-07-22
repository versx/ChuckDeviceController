namespace ChuckDeviceConfigurator.Collections
{
    public class PokemonPriorityQueue<T> : List<T>
    {
        public PokemonPriorityQueue()
        {
        }

        public PokemonPriorityQueue(int maxCapacity)
            : base(maxCapacity)
        {
            Capacity = maxCapacity;
        }

        public T? Pop()
        {
            var obj = this.FirstOrDefault();
            RemoveAt(0);

            return obj;
        }

        public T? PopLast()
        {
            var obj = this.LastOrDefault();
            RemoveAt(Count - 1);

            return obj;
        }
    }
}