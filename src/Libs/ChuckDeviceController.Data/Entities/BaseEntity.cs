namespace ChuckDeviceController.Data.Entities
{
    using ChuckDeviceController.Common.Data.Contracts;

    // This can easily be modified to be BaseEntity<T> and public T Id to support different key types.
    // Using non-generic integer types for simplicity and to ease caching logic
    public abstract class BaseEntity : IBaseEntity //<T> where T : Type
    {
        //public virtual int Id { get; protected set; }
    }
}