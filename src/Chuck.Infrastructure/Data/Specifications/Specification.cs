namespace Chuck.Infrastructure.Data.Specifications
{
    using System;
    using System.Linq.Expressions;

    public class Specification<T> : BaseSpecification<T>
    {
        public Specification(Expression<Func<T, bool>> criteria) : base(criteria)
        {
        }
    }
}