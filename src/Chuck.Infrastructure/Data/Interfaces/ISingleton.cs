namespace Chuck.Infrastructure.Data.Interfaces
{
    public interface ISingleton<T>
    {
        static T Instance { get; }
    }
}