namespace ChuckDeviceConfigurator.Extensions
{
    using System.Linq.Expressions;

    using ChuckDeviceController.Common.Data;

    public static class LinqExtensions
    {
        public static IQueryable<TEntity> FilterBy<TEntity>(
            this IQueryable<TEntity> query,
            Expression<Func<TEntity, bool>>? predicate)
        {
            var filtered = query.Where(predicate);
            return filtered;
        }

        public static IOrderedQueryable<TEntity> Order<TEntity, TKey>(
            this IQueryable<TEntity> query,
            Expression<Func<TEntity, TKey>>? order = null,
            SortOrderDirection sortDirection = SortOrderDirection.Asc)
        {
            if (order == null)
                return null;

            var ordered = sortDirection == SortOrderDirection.Asc
                ? query.OrderBy(order)
                : query.OrderByDescending(order);
            return ordered;
        }
    }
}